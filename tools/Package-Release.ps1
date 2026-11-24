param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = (Get-Item -LiteralPath (Split-Path -Parent $projectRoot)).FullName.TrimEnd('\')
$versionText = Get-Content -LiteralPath (Join-Path $projectRoot 'src/AppVersion.cs') -Raw
$version = [regex]::Match($versionText, 'Current\s*=\s*"(\d+\.\d+\.\d+)"').Groups[1].Value
if (-not $version) { throw 'Missing release version' }
$binary = Join-Path $projectRoot 'dist/DragonCompanion.exe'
$resultsRoot = Join-Path $projectRoot ('tests/results-release-v' + $version)
$previewRoot = Join-Path $projectRoot ('tests/release-preview-v' + $version)
$stage = Join-Path $projectRoot ('release/GPTCompanion-Windows-v' + $version)
$archivePath = Join-Path $workspaceRoot ('GC v' + $version + '.zip')
$temporaryArchive = Join-Path $projectRoot ('release/GC-v' + $version + '.building.zip')
foreach ($path in @($stage, $archivePath, $temporaryArchive)) {
    if (-not ([IO.Path]::GetFullPath($path)).StartsWith($workspaceRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Out-of-workspace package path' }
    if (Test-Path -LiteralPath $path) { throw ('Refusing to overwrite existing release artifact: ' + $path) }
}
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($binary).FileVersion -ne ($version + '.0')) { throw 'Wrong release executable version' }
$report = Get-Content -LiteralPath (Join-Path $resultsRoot 'test-results.txt')
if ($report.Count -lt 2000 -or ($report | Where-Object { -not $_.StartsWith('PASS ') })) { throw 'Full release checks must pass before packaging' }
$binaryHash = (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash
$accepted = Get-Content -LiteralPath (Join-Path $previewRoot 'actions/verification.json') -Raw | ConvertFrom-Json
if ($accepted.binary_sha256 -ne $binaryHash -or @($accepted.approved_rgba_byte_identical_samples.PSObject.Properties).Count -ne 4) { throw 'Release action render or frozen-action verification is missing' }
foreach ($p in $accepted.approved_rgba_byte_identical_samples.PSObject.Properties) { if ($p.Value -ne 132) { throw 'An approved action changed' } }
New-Item -ItemType Directory -Path $stage | Out-Null
$verification = Join-Path $stage 'verification'
New-Item -ItemType Directory -Path $verification | Out-Null
Copy-Item -LiteralPath $binary -Destination (Join-Path $stage 'DragonCompanion.exe')
foreach ($name in @('更新日志.txt','README.md','RELEASE_NOTES.md','VALIDATION.md','DESIGN_GUIDE.md','ASSET_PROMPTS.md','ANIMATION_ASSETS.md','ACTIVITY_ASSET_PROMPTS.md','ACTIVITY_MODEL_PROMPTS.md','WALK_ASSET_PROMPTS.md','CS2_ASSET_PROMPTS.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $stage
}
Copy-Item -LiteralPath (Join-Path $projectRoot '词库说明.txt') -Destination $stage
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets/vocabulary/vocabulary-data-source.zip') -Destination $stage
# Only curated reports and pictures; never copy isolated profiles or account data.
foreach ($file in Get-ChildItem -LiteralPath $resultsRoot -File) {
    if ($file.Name -eq 'test-results.txt' -or $file.Name -like 'ui-*.png' -or $file.Name -like 'exe-icon-*' -or $file.Name -in @('activity-baselines.txt','menu-layout.txt','resize-transition.txt','planner-live-sample.json','vocabulary-live-sample.json','icon-plans.png')) { Copy-Item -LiteralPath $file.FullName -Destination $verification }
}
foreach ($name in @('activities-pixel.gif','activities-anime.gif','reading-aligned.gif','activity-preview.html','vocabulary-flip.gif')) { Copy-Item -LiteralPath (Join-Path $resultsRoot $name) -Destination $verification }
foreach ($pair in @(@('idle','idle.gif'),@('actions','tea-typing.gif'),@('actions','anime-tea.gif'),@('bell','bell-preview.gif'),@('bell','grip-detail.gif'))) {
    Copy-Item -LiteralPath (Join-Path (Join-Path $previewRoot $pair[0]) $pair[1]) -Destination $verification
}
foreach ($group in @('idle','actions','bell')) {
    $metadata = Get-Content -LiteralPath (Join-Path $previewRoot ($group + '/verification.json')) -Raw | ConvertFrom-Json
    if ($metadata.binary_sha256 -ne $binaryHash) { throw ('Preview came from another binary: ' + $group) }
    $metadata.packaged = $true
    $metadata | Add-Member -NotePropertyName release_version -NotePropertyValue $version -Force
    $metadata | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $verification ($group + '-verification.json')) -Encoding utf8
}
Copy-Item -LiteralPath (Join-Path $previewRoot 'actions/checks.txt') -Destination (Join-Path $verification 'animation-checks.txt')
$htmlPath = Join-Path $verification 'activity-preview.html'
$html = Get-Content -LiteralPath $htmlPath -Raw -Encoding utf8
$html = $html.Replace('<footer>', '<h2>正常待机 · 18 秒</h2><img src="idle.gif" alt="眨眼、左右张望及间歇甩尾"><h2>已确认的喝茶与打字</h2><img src="tea-typing.gif" alt="喝茶与打字"><h2>二次元喝茶手腕细节</h2><img src="anime-tea.gif" alt="完整握杯手腕"><h2>铃铛握持与摆动</h2><img src="grip-detail.gif" alt="铃铛顶部拉环握持"><footer>')
[IO.File]::WriteAllText($htmlPath, $html, [Text.UTF8Encoding]::new($false))
$files = @(Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object {
    [ordered]@{path=$_.FullName.Substring($stage.Length+1).Replace('\','/'); bytes=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
})
$manifest = [ordered]@{version=$version; exe_version=($version+'.0'); exe_sha256=$binaryHash; checks_passed=$report.Count; failed=0; frozen_action_rgba_samples=528; files=$files}
$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $verification 'package-manifest.json') -Encoding utf8
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$temporaryArchive,[IO.Compression.CompressionLevel]::Optimal,$true)
$zip = [IO.Compression.ZipFile]::OpenRead($temporaryArchive)
try {
    $prefix = 'GPTCompanion-Windows-v' + $version + '/'
    if (@($zip.Entries | Where-Object { $_.Name }).Count -ne $files.Count + 1) { throw 'Unexpected files in release archive' }
    foreach ($file in $files) {
        $name = $prefix + $file.path
        $entry = @($zip.Entries | Where-Object { $_.FullName.Replace('\','/') -eq $name })
        if ($entry.Count -ne 1 -or $entry[0].Length -ne $file.bytes) { throw ('Missing or invalid archived file: ' + $name) }
        $stream=$entry[0].Open(); $sha=[Security.Cryptography.SHA256]::Create()
        try { $hash=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') } finally { $stream.Dispose(); $sha.Dispose() }
        if ($hash -ne $file.sha256) { throw ('Archived file hash mismatch: ' + $name) }
    }
    $manifestEntry=$zip.GetEntry($prefix+'verification/package-manifest.json')
    if (-not $manifestEntry) { throw 'Archive manifest is missing' }
    $reader=[IO.StreamReader]::new($manifestEntry.Open())
    try { $archivedManifest=$reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($archivedManifest.version -ne $version -or $archivedManifest.exe_sha256 -ne $binaryHash -or $archivedManifest.checks_passed -ne $report.Count) { throw 'Archived manifest mismatch' }
    $exeEntry = $zip.GetEntry($prefix + 'DragonCompanion.exe')
    $extracted = Join-Path $resultsRoot 'archived-DragonCompanion.exe'
    [IO.Compression.ZipFileExtensions]::ExtractToFile($exeEntry,$extracted,$false)
} finally { $zip.Dispose() }
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($extracted).FileVersion -ne ($version+'.0') -or (Get-FileHash -LiteralPath $extracted -Algorithm SHA256).Hash -ne $binaryHash) { throw 'Archived EXE version/hash mismatch' }
& (Join-Path $PSScriptRoot 'Test-AppIcon.ps1') -ExecutablePath $extracted -IconPath (Join-Path $projectRoot 'assets/app.ico') | Out-Null
Move-Item -LiteralPath $temporaryArchive -Destination $archivePath
$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
[ordered]@{version=$version; archive=$archivePath; archive_sha256=$archiveHash; exe_sha256=$binaryHash; checks_passed=$report.Count; verified_payload_files=$files.Count; archived_exe_verified=$true; icon_verified=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $resultsRoot 'package-verification.json') -Encoding utf8
Write-Output ('Verified release: ' + $archivePath)
Write-Output ('Payload files: ' + $files.Count + '; checks passed: ' + $report.Count)

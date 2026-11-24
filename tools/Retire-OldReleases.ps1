param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = (Get-Item -LiteralPath (Split-Path -Parent $projectRoot)).FullName.TrimEnd('\')
$releaseRoot = (Get-Item -LiteralPath (Join-Path $projectRoot 'release')).FullName.TrimEnd('\')
$versionText = Get-Content -LiteralPath (Join-Path $projectRoot 'src/AppVersion.cs') -Raw -Encoding UTF8
$version = [regex]::Match($versionText, 'Current\s*=\s*"(\d+\.\d+\.\d+)"').Groups[1].Value
if (-not $version) { throw 'Cannot determine the current release version' }
$currentVersion = [version]$version
$archivePath = Join-Path $workspaceRoot ('GC v' + $version + '.zip')
$currentExe = Join-Path $projectRoot 'dist/DragonCompanion.exe'
if ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($currentExe).FileVersion -ne ($version + '.0')) { throw 'Current executable version is inconsistent' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    $prefix = 'GPTCompanion-Windows-v' + $version + '/'
    $entry = $archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq ($prefix + 'DragonCompanion.exe') }
    if (-not $entry) { throw 'New user archive has no current executable' }
    $stream = $entry.Open(); $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $archivedHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') } finally { $stream.Dispose(); $sha.Dispose() }
    if ($archivedHash -ne (Get-FileHash -LiteralPath $currentExe -Algorithm SHA256).Hash) { throw 'New archive contains an older executable' }
    $entry = $archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq ($prefix + 'verification/test-results.txt') }
    if (-not $entry) { throw 'New archive has no verification report' }
    $reader = [System.IO.StreamReader]::new($entry.Open())
    try { $report = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($report -match '(?m)^FAIL ' -or $report -notmatch '(?m)^PASS ') { throw 'New archive has not passed verification' }
} finally { $archive.Dispose() }

$retired = [System.Collections.Generic.List[System.IO.FileSystemInfo]]::new()
foreach ($item in Get-ChildItem -LiteralPath $workspaceRoot -Force) {
    $match = [regex]::Match($item.Name, '^GC v(\d+\.\d+\.\d+)(\.zip)?$')
    if ($match.Success -and [version]$match.Groups[1].Value -lt $currentVersion) { $retired.Add($item) }
    $installed = [regex]::Match($item.Name, '^GPTCompanion-Windows-v(\d+\.\d+\.\d+)$')
    if ($installed.Success -and $item -is [System.IO.DirectoryInfo] -and [version]$installed.Groups[1].Value -lt $currentVersion) {
        $installedExe = Join-Path $item.FullName 'DragonCompanion.exe'
        if ((Test-Path -LiteralPath $installedExe -PathType Leaf) -and [Diagnostics.FileVersionInfo]::GetVersionInfo($installedExe).FileVersion -eq ($installed.Groups[1].Value+'.0')) { $retired.Add($item) }
    }
}
foreach ($item in Get-ChildItem -LiteralPath $releaseRoot -Force) {
    $match = [regex]::Match($item.Name, '^GPTCompanion-(Windows|Source)-v(\d+\.\d+\.\d+)(\.zip)?$')
    if ($match.Success -and [version]$match.Groups[2].Value -lt $currentVersion) { $retired.Add($item) }
}
$distRoot = (Get-Item -LiteralPath (Join-Path $projectRoot 'dist')).FullName.TrimEnd('\')
foreach ($name in @('DragonCompanion.iconcheck.exe', 'DragonCompanion.pixelcheck.exe', 'DragonCompanion.startup.exe')) {
    $path = Join-Path $distRoot $name
    if (Test-Path -LiteralPath $path) { $item = Get-Item -LiteralPath $path; if ([version]$item.VersionInfo.FileVersion -lt [version]($version + '.0')) { $retired.Add($item) } }
}

# Validate the final absolute paths and reject any linked folder before deletion.
foreach ($item in $retired) {
    $absolute = [System.IO.Path]::GetFullPath($item.FullName)
    $parent = Split-Path -Parent $absolute
    if (-not $absolute.StartsWith($workspaceRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or $parent -notin @($workspaceRoot, $releaseRoot, $distRoot)) { throw ('Out-of-scope retirement path: ' + $absolute) }
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { throw ('Linked artifact requires manual inspection: ' + $absolute) }
    if (($item -is [System.IO.DirectoryInfo]) -and (Get-ChildItem -LiteralPath $absolute -Recurse -Force | Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 } | Select-Object -First 1)) { throw ('Artifact contains a linked path: ' + $absolute) }
}
if (-not $Apply) { $retired | ForEach-Object FullName; Write-Output ('Validated ' + $retired.Count + ' old artifacts; no files removed.'); return }
foreach ($process in Get-Process -Name 'DragonCompanion*' -ErrorAction SilentlyContinue) {
    $executable = $process.Path
    if (-not $executable) { throw 'A companion process path cannot be verified; refusing to stop it' }
    $belongsToRetiredArtifact = $false
    foreach ($item in $retired) { if ((($item -is [System.IO.DirectoryInfo]) -and $executable.StartsWith($item.FullName.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) -or ((-not ($item -is [System.IO.DirectoryInfo])) -and $executable -eq $item.FullName)) { $belongsToRetiredArtifact = $true } }
    if ($belongsToRetiredArtifact) { Stop-Process -Id $process.Id -ErrorAction Stop; $process.WaitForExit(3000) | Out-Null }
}
$log = [System.Collections.Generic.List[string]]::new()
foreach ($item in $retired) {
    if (($item -is [System.IO.DirectoryInfo])) { Remove-Item -LiteralPath $item.FullName -Recurse -Force } else { Remove-Item -LiteralPath $item.FullName -Force }
    $log.Add('Removed ' + $item.FullName)
}
$log.Add('Current release retained: ' + $archivePath)
$log | Set-Content -LiteralPath (Join-Path $projectRoot ('tests/release-cleanup-v' + $version + '.txt')) -Encoding UTF8
$log

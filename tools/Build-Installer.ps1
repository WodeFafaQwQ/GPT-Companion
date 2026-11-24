param(
    [string]$CompilerPath = $env:INNO_SETUP_PATH,
    [string]$ExecutablePath,
    [string]$OutputDirectory,
    [switch]$TestBuild
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $projectRoot
$versionSource = Get-Content -LiteralPath (Join-Path $projectRoot 'src/AppVersion.cs') -Raw
$version = [regex]::Match($versionSource, 'Current\s*=\s*"(\d+\.\d+\.\d+)"').Groups[1].Value
if (-not $version) { throw 'Missing application version.' }
if (-not $CompilerPath) {
    $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($compiler) { $CompilerPath = $compiler.Source }
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath)) { throw 'Install Inno Setup 7 and set INNO_SETUP_PATH to ISCC.exe.' }
if (-not $ExecutablePath) { $ExecutablePath = Join-Path $projectRoot 'dist/DragonCompanion.exe' }
$ExecutablePath = (Resolve-Path -LiteralPath $ExecutablePath).Path
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($ExecutablePath).FileVersion -ne "$version.0") { throw 'Executable and installer versions differ.' }
if (-not $OutputDirectory) { $OutputDirectory = if ($TestBuild) { Join-Path $projectRoot "tests/installer-v$version" } else { $workspaceRoot } }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$payload = Join-Path $projectRoot "release/installer-payload-v$version"
New-Item -ItemType Directory -Path $payload -Force | Out-Null
Copy-Item -LiteralPath $ExecutablePath -Destination (Join-Path $payload 'DragonCompanion.exe') -Force
foreach ($name in @('README.md', '更新日志.txt', '词库说明.txt', 'RELEASE_NOTES.md')) {
    $content = Get-Content -LiteralPath (Join-Path $projectRoot $name) -Raw -Encoding UTF8
    # Documentation can contain local asset-provenance paths; they are not payload data.
    $content = [regex]::Replace($content, '(?i)C:[/\\]Users[/\\][^/\\\s`"<>]+', '<USERPROFILE>')
    [IO.File]::WriteAllText((Join-Path $payload $name), $content, [Text.UTF8Encoding]::new($false))
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets/vocabulary/vocabulary-data-source.zip') -Destination $payload -Force
$allowed = @('DragonCompanion.exe', 'README.md', '更新日志.txt', '词库说明.txt', 'RELEASE_NOTES.md', 'vocabulary-data-source.zip')
$files = @(Get-ChildItem -LiteralPath $payload -Force)
if ($files.Count -ne $allowed.Count -or @($files | Where-Object { $_.PSIsContainer -or $_.Name -notin $allowed }).Count) { throw 'Unexpected file in installer payload.' }
$compilerArgs = @('/Qp', "/DAppVersion=$version", "/DPayloadDir=$payload", "/DOutputDir=$OutputDirectory")
if ($TestBuild) { $compilerArgs += '/DTestBuild' }
$compilerArgs += (Join-Path $projectRoot 'installer/GPTCompanion.iss')
& $CompilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$name = if ($TestBuild) { "GC-v$version-InstallerTest.exe" } else { "GC v$version Setup.exe" }
$installer = Join-Path $OutputDirectory $name
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($installer).FileVersion.Trim() -ne "$version.0") { throw 'Compiled installer version mismatch.' }
$manifest = [ordered]@{ version = $version; test_build = [bool]$TestBuild; installer = $name; sha256 = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash; payload = @($files | ForEach-Object { [ordered]@{ file = $_.Name; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } }) }
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory "$name.manifest.json") -Encoding UTF8
Write-Output "Installer built: $installer"

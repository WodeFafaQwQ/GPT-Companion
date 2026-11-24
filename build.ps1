param([string]$OutputName = 'DragonCompanion.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$versionSource = Get-Content -LiteralPath (Join-Path $projectRoot 'src/AppVersion.cs') -Raw -Encoding UTF8
$releaseVersion = [regex]::Match($versionSource, 'Current\s*=\s*"(\d+\.\d+\.\d+)"').Groups[1].Value
if (-not $releaseVersion) { throw 'Missing release version in src/AppVersion.cs' }
$manifestPath = Join-Path $projectRoot 'app.manifest'
$manifestText = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8
$manifestText = [regex]::Replace($manifestText, '(<assemblyIdentity\s+version=")[^"]+("\s+name="WodeFafa\.DragonCompanion")', ('${1}' + $releaseVersion + '.0${2}'))
[System.IO.File]::WriteAllText($manifestPath, $manifestText, [System.Text.UTF8Encoding]::new($false))
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$outputPath = Join-Path (Join-Path $projectRoot 'dist') $OutputName
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'dist') -Force | Out-Null
# Generate and validate all native Windows icon sizes before invoking the compiler.
# A missing/transparent icon must fail the build instead of silently shipping a blank EXE.
& (Join-Path $projectRoot 'tools/New-AppIcon.ps1') -ProjectRoot $projectRoot
$iconPath = Join-Path $projectRoot 'assets/app.ico'
$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object FullName
$gac = Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL'
$uiAutomationRefs = @('UIAutomationClient', 'UIAutomationTypes', 'WindowsBase') | ForEach-Object {
    $assembly = Get-ChildItem -LiteralPath (Join-Path $gac $_) -Filter ($_.ToString() + '.dll') -Recurse -ErrorAction Stop | Select-Object -First 1
    if (-not $assembly) { throw "Required Windows UI Automation assembly missing: $_" }
    '/reference:' + $assembly.FullName
}
$compilerArgs = @('/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/utf8output', ('/out:' + $outputPath), ('/win32manifest:' + (Join-Path $projectRoot 'app.manifest')), '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll') + $uiAutomationRefs + @(('/resource:' + (Join-Path $projectRoot 'assets/anime.png') + ',anime.png'), ('/resource:' + (Join-Path $projectRoot 'assets/pixel.png') + ',pixel.png'))
foreach ($skinName in @('anime-daily-8','pixel-daily-8','anime-tasks-8','pixel-tasks-8','anime-walk','pixel-walk','anime-lift','pixel-lift','anime-rig','pixel-rig','anime-wings','pixel-wings','anime-pickup','pixel-pickup','anime-mania','pixel-mania','anime-cs2','pixel-cs2','osu-mania-icon','cs2-icon')) { $compilerArgs += ('/resource:' + (Join-Path $projectRoot ('assets/' + $skinName + '.png')) + ',' + $skinName + '.png') }
foreach ($style in @('pixel','anime')) { $compilerArgs += ('/resource:' + (Join-Path $projectRoot ('assets/' + $style + '-activity-model-v1.png')) + ',' + $style + '-activity-model.png') }
foreach ($style in @('pixel','anime')) { $compilerArgs += ('/resource:' + (Join-Path $projectRoot ('assets/' + $style + '-reminder-clean-v1.png')) + ',' + $style + '-reminder-clean.png') }
$compilerArgs += ('/win32icon:' + $iconPath)
$compilerArgs += ('/resource:' + $iconPath + ',app.ico')
$compilerArgs += ('/resource:' + (Join-Path $projectRoot 'assets/vocabulary/catalog.json.gz') + ',vocabulary.json.gz')
& $compilerPath @compilerArgs @sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
& (Join-Path $projectRoot 'tools/Test-AppIcon.ps1') -ExecutablePath $outputPath -IconPath $iconPath | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot '更新日志.txt') -Destination (Split-Path -Parent $outputPath) -Force
Write-Output "Built $outputPath"

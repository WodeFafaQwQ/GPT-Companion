param([Parameter(Mandatory)][string]$InstallerPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $projectRoot ('tests/installer-lifecycle-' + [Guid]::NewGuid().ToString('N'))
$installDir = Join-Path $testRoot 'installed'
$profileDir = Join-Path $testRoot 'synthetic-user-data'
$runPath = 'Software\Microsoft\Windows\CurrentVersion\Run'
$runName = 'DragonCompanion.InstallerTest'
$uninstallPath = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\GPTCompanion.Desktop.InstallerTest_is1'
$desktopLink = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'GPT 桌面伙伴安装测试.lnk'
$programsDir = Join-Path ([Environment]::GetFolderPath('Programs')) 'GPT 桌面伙伴安装测试'
$results = [Collections.Generic.List[string]]::new()
function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "FAIL $Message" }
    $results.Add("PASS $Message")
    Write-Output "PASS $Message"
}
function Invoke-Setup([string]$Suffix) {
    $args = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/DESKTOP=1',('/DIR="' + $installDir + '"'),('/LOG="' + (Join-Path $testRoot "$Suffix.log") + '"'))
    $process = Start-Process -FilePath $InstallerPath -ArgumentList $args -WindowStyle Hidden -PassThru -Wait
    Check ($process.ExitCode -eq 0) "$Suffix installer exits successfully"
}
function Invoke-Uninstall([string]$Suffix) {
    $uninstaller = Join-Path $installDir 'unins000.exe'
    $process = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="' + (Join-Path $testRoot "$Suffix.log") + '"')) -WindowStyle Hidden -PassThru -Wait
    Check ($process.ExitCode -eq 0) "$Suffix uninstaller exits successfully"
}
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($InstallerPath).ProductName -notlike '*安装测试*') {
    # Test packages have a separate identity so no real app registration is touched.
    $leaf = Split-Path $InstallerPath -Leaf
    if ($leaf -notmatch '^GC-v\d+\.\d+\.\d+-InstallerTest\.exe$') { throw 'Only the isolated test installer may be executed by this check.' }
}
$existing = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($uninstallPath)
if ($existing) { $existing.Dispose(); throw 'An older isolated installer test is still registered; inspect it first.' }
$runKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($runPath)
if ($null -ne $runKey.GetValue($runName)) { $runKey.Dispose(); throw 'The isolated test startup value already exists.' }
if ((Test-Path -LiteralPath $desktopLink) -or (Test-Path -LiteralPath $programsDir)) { $runKey.Dispose(); throw 'An isolated test shortcut already exists.' }
New-Item -ItemType Directory -Path $profileDir -Force | Out-Null
$profileFile = Join-Path $profileDir 'schedules.json'
[IO.File]::WriteAllText($profileFile, '{"test_only":true,"plan":"synthetic installer preservation fixture"}')
$profileHash = (Get-FileHash -LiteralPath $profileFile).Hash
try {
    $runKey.SetValue($runName, '"C:\old-installer-fixture\DragonCompanion.exe" --background')
    Invoke-Setup 'first-install'
    $installedExe = Join-Path $installDir 'DragonCompanion.exe'
    $versionInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($installedExe)
    $version = '{0}.{1}.{2}' -f $versionInfo.FileMajorPart, $versionInfo.FileMinorPart, $versionInfo.FileBuildPart
    $payload = Join-Path $projectRoot ('release/installer-payload-v' + $version)
    Check ((Get-FileHash -LiteralPath $installedExe).Hash -eq (Get-FileHash -LiteralPath (Join-Path $payload 'DragonCompanion.exe')).Hash) 'Installed executable exactly matches the reviewed payload'
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($uninstallPath)
    Check ($null -ne $key) 'Windows uninstall entry is registered for the isolated product'
    if ($key) { $key.Dispose() }
    Check ((Test-Path -LiteralPath $desktopLink) -and (Test-Path -LiteralPath (Join-Path $programsDir 'GPT 桌面伙伴.lnk'))) 'Desktop and Start menu shortcuts are created'
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($desktopLink)
    Check ($shortcut.TargetPath -eq $installedExe) 'Desktop shortcut targets the installed executable'
    [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) | Out-Null
    $expectedStartup = '"' + $installedExe + '" --background'
    Check ($runKey.GetValue($runName) -eq $expectedStartup) 'An enabled isolated startup entry is migrated to the installed copy'
    $extraFile = Join-Path $installDir 'personal-marker-test.json'
    [IO.File]::WriteAllText($extraFile, '{"untracked_fixture":true}')
    [IO.File]::WriteAllText((Join-Path $installDir 'README.md'), 'stale documentation fixture')
    Invoke-Setup 'overwrite-update'
    Check ((Get-Content -LiteralPath (Join-Path $installDir 'README.md') -Raw) -ne 'stale documentation fixture') 'Updating replaces the application documentation'
    Check (Test-Path -LiteralPath $extraFile) 'Updating preserves untracked user-created files'
    Check ((Get-FileHash -LiteralPath $profileFile).Hash -eq $profileHash) 'Updating preserves the external synthetic user profile'
    $otherStartup = '"C:\other-installer-fixture\DragonCompanion.exe" --background'
    $runKey.SetValue($runName, $otherStartup)
    Invoke-Uninstall 'uninstall-repointed-startup'
    Check (-not (Test-Path -LiteralPath $installedExe)) 'Uninstall removes the installed application executable'
    Check ((Test-Path -LiteralPath $extraFile) -and (Get-FileHash -LiteralPath $profileFile).Hash -eq $profileHash) 'Uninstall preserves untracked files and external profile data'
    Check (-not (Test-Path -LiteralPath $desktopLink) -and -not (Test-Path -LiteralPath $programsDir)) 'Uninstall removes only its own shortcuts'
    Check ($runKey.GetValue($runName) -eq $otherStartup) 'Uninstall preserves a startup entry repointed to a different application copy'
    $runKey.DeleteValue($runName, $false)
    Invoke-Setup 'reinstall-without-startup'
    Check ($null -eq $runKey.GetValue($runName)) 'A fresh install does not enable startup without consent'
    $runKey.SetValue($runName, $expectedStartup)
    Invoke-Uninstall 'uninstall-owned-startup'
    Check ($null -eq $runKey.GetValue($runName)) 'Uninstall removes a startup entry pointing to its own executable'
    Check ((Get-FileHash -LiteralPath $profileFile).Hash -eq $profileHash) 'Synthetic plan data remains unchanged after the full lifecycle'
}
finally {
    $testUninstaller = Join-Path $installDir 'unins000.exe'
    if ((Test-Path -LiteralPath $testUninstaller) -and (Test-Path -LiteralPath (Join-Path $installDir 'DragonCompanion.exe'))) {
        $cleanupProcess = Start-Process -FilePath $testUninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -PassThru -Wait
        if ($cleanupProcess.ExitCode -ne 0) { Write-Warning 'The isolated test registration needs cleanup.' }
    }
    $runKey.DeleteValue($runName, $false)
    $runKey.Dispose()
    $results | Set-Content -LiteralPath (Join-Path $testRoot 'test-results.txt') -Encoding UTF8
    Write-Output "Installer lifecycle results: $testRoot"
}

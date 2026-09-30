[CmdletBinding()]
param([string]$InstallerPath)
. (Join-Path $PSScriptRoot 'Build.Common.ps1')

[xml]$project = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'src\AppVolumeController\AppVolumeController.csproj')
$version = [string]$project.Project.PropertyGroup.Version
if (-not $InstallerPath) { $InstallerPath = Join-Path $RepositoryRoot "releases\AppVolumeController-$version-win-x64-Setup.exe" }
if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) { throw 'Build the installer before running this check.' }
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{8772141F-185B-43D4-9B82-FE433835C4B7}_is1'
if (Test-Path -LiteralPath $uninstallKey) { throw 'An installed copy already exists. This check will not replace an existing installation. Use a clean Windows test account instead.' }
if (Get-Process -Name AppVolumeController -ErrorAction SilentlyContinue) { throw 'Close App Volume Controller before testing the installer.' }

$testRoot = Join-Path $RepositoryRoot 'artifacts\installer-test'
Reset-ArtifactDirectory $testRoot
$installDirectory = Assert-ArtifactPath (Join-Path $testRoot 'installed')
$groupName = 'App Volume Controller Installer Test ' + [Guid]::NewGuid().ToString('N')
$startMenuFolder = Join-Path ([Environment]::GetFolderPath('Programs')) $groupName
$preferencesPath = Join-Path $env:LOCALAPPDATA 'AppVolumeController\preferences.json'
$beforePreferences = if (Test-Path -LiteralPath $preferencesPath) { (Get-FileHash -LiteralPath $preferencesPath).Hash } else { 'absent' }
$results = [Collections.Generic.List[string]]::new()
function Assert-Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $results.Add('PASS: ' + $Message)
}
$uninstalled = $false
try {
    $arguments = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TASKS="" /DIR="' + $installDirectory + '" /GROUP="' + $groupName + '" /LOG="' + (Join-Path $testRoot 'install.log') + '"'
    Invoke-CheckedProcess $InstallerPath $arguments
    $installedExe = Join-Path $installDirectory 'AppVolumeController.exe'
    Assert-Check (Test-Path -LiteralPath $installedExe) 'Installer creates the app in the requested per-user location'
    $registration = Get-ItemProperty -LiteralPath $uninstallKey
    Assert-Check ($registration.DisplayVersion -eq $version) 'Windows uninstall registration includes the release version'
    Assert-Check ($registration.InstallLocation.TrimEnd('\') -eq $installDirectory) 'Uninstall registration points to the test installation'

    $payload = Join-Path $RepositoryRoot 'artifacts\publish\win-x64'
    $payloadFiles = Get-ChildItem -LiteralPath $payload -Recurse -File
    foreach ($file in $payloadFiles) {
        $relative = $file.FullName.Substring($payload.Length + 1)
        $installed = Join-Path $installDirectory $relative
        if (-not (Test-Path -LiteralPath $installed) -or (Get-FileHash -LiteralPath $installed).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) { throw "Installed payload mismatch: $relative" }
    }
    Assert-Check $true "All $($payloadFiles.Count) installed payload files match the verified publish output"
    Assert-Check ((Test-Path -LiteralPath (Join-Path $installDirectory 'coreclr.dll')) -and (Test-Path -LiteralPath (Join-Path $installDirectory 'PresentationFramework.dll'))) 'The .NET and WPF runtimes are bundled'
    Assert-Check (-not (Get-ChildItem -LiteralPath $installDirectory -Recurse -File -Include '*.pdb','*.log','preferences.json','ui-smoke.png')) 'Package excludes debug symbols, test output, and personal preferences'

    $shortcutPath = Join-Path $startMenuFolder 'App Volume Controller.lnk'
    Assert-Check (Test-Path -LiteralPath $shortcutPath) 'Start menu shortcut is created'
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    Assert-Check ($shortcut.TargetPath -eq $installedExe -and $shortcut.IconLocation.StartsWith($installedExe, [StringComparison]::OrdinalIgnoreCase)) 'Shortcut opens the installed app and uses its volume icon'
    [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut) | Out-Null
    [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) | Out-Null

    Invoke-CheckedProcess $installedExe '--smoke-test'
    Assert-Check (Test-Path -LiteralPath (Join-Path $installDirectory 'ui-smoke.png')) 'The installed app starts, reads audio devices, renders, and closes'
    Move-Item -LiteralPath (Join-Path $installDirectory 'ui-smoke.png') -Destination (Join-Path $testRoot 'installed-ui.png')

    $reinstallArguments = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TASKS="" /DIR="' + $installDirectory + '" /GROUP="' + $groupName + '" /LOG="' + (Join-Path $testRoot 'reinstall.log') + '"'
    Invoke-CheckedProcess $InstallerPath $reinstallArguments
    Assert-Check ((Get-ItemProperty -LiteralPath $uninstallKey).InstallLocation.TrimEnd('\') -eq $installDirectory) 'Reinstall reuses the existing app registration and location'

    $uninstaller = Join-Path $installDirectory 'unins000.exe'
    Invoke-CheckedProcess $uninstaller ('/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="' + (Join-Path $testRoot 'uninstall.log') + '"')
    Assert-Check (-not (Test-Path -LiteralPath $installedExe)) 'Uninstaller removes the app executable'
    Assert-Check (-not (Test-Path -LiteralPath $uninstallKey)) 'Uninstaller removes its Windows registration'
    Assert-Check (-not (Test-Path -LiteralPath $shortcutPath)) 'Uninstaller removes its Start menu shortcut'
    $uninstalled = $true
    $afterPreferences = if (Test-Path -LiteralPath $preferencesPath) { (Get-FileHash -LiteralPath $preferencesPath).Hash } else { 'absent' }
    Assert-Check ($beforePreferences -eq $afterPreferences) 'Install, reinstall, smoke test, and uninstall leave existing preferences unchanged'
}
catch { $results.Add('FAIL: ' + $_.Exception.Message); throw }
finally {
    $results | Set-Content -LiteralPath (Join-Path $testRoot 'results.log') -Encoding utf8
    if (-not $uninstalled -and (Test-Path -LiteralPath (Join-Path $installDirectory 'unins000.exe'))) {
        # Only uninstall the exact test directory; never touch another installed copy.
        $location = (Get-ItemProperty -LiteralPath $uninstallKey -ErrorAction SilentlyContinue).InstallLocation
        if ($location -and $location.TrimEnd('\') -eq $installDirectory) {
            Invoke-CheckedProcess (Join-Path $installDirectory 'unins000.exe') '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
        }
    }
}
$results

[CmdletBinding()]
param([switch]$Installer, [switch]$Verify, [string]$InnoSetupPath)
. (Join-Path $PSScriptRoot 'Build.Common.ps1')

$project = Join-Path $RepositoryRoot 'src\AppVolumeController\AppVolumeController.csproj'
$publish = Join-Path $RepositoryRoot 'artifacts\publish\win-x64'
$releaseDirectory = Join-Path $RepositoryRoot 'releases'
$dotnet = Get-DotNetPath
[xml]$projectXml = Get-Content -LiteralPath $project
$version = [string]$projectXml.Project.PropertyGroup.Version
$fileVersion = [string]$projectXml.Project.PropertyGroup.FileVersion
if ($version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'Project Version is missing or not a valid release filename.' }
if ($Installer) { $compiler = Get-InnoSetupPath $InnoSetupPath }
$running = Get-Process -Name AppVolumeController -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($publish + '\', [StringComparison]::OrdinalIgnoreCase) }
if ($running) { throw 'Close the copy of App Volume Controller running from artifacts before rebuilding.' }
Reset-ArtifactDirectory $publish
& $dotnet publish $project -c Release -r win-x64 --self-contained true -o $publish -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

$testingGuide = (Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot 'docs\TESTING.txt')).Replace('{{VERSION}}', $version)
[IO.File]::WriteAllText((Join-Path $publish 'TESTING.txt'), $testingGuide)
Copy-Item -LiteralPath (Join-Path $RepositoryRoot 'licenses') -Destination $publish -Recurse
Copy-Item -LiteralPath (Join-Path $RepositoryRoot 'LICENSE') -Destination (Join-Path $publish 'LICENSE.txt')

# Ship the license/notices for exactly the runtime versions selected by publish.
$nugetPackages = $env:NUGET_PACKAGES
if (-not $nugetPackages) {
    $packageLocation = & $dotnet nuget locals global-packages --list
    if ($LASTEXITCODE -ne 0) { throw 'Could not locate the NuGet package cache.' }
    $nugetPackages = ($packageLocation -replace '^global-packages:\s*', '').Trim()
}
$dependencies = Get-Content -Raw -LiteralPath (Join-Path $publish 'AppVolumeController.deps.json') | ConvertFrom-Json
foreach ($name in $dependencies.libraries.PSObject.Properties.Name | Where-Object { $_ -like 'runtimepack.*' }) {
    $parts = $name.Substring('runtimepack.'.Length).Split('/')
    $package = Join-Path (Join-Path $nugetPackages $parts[0].ToLowerInvariant()) $parts[1]
    $destination = Join-Path $publish ('licenses\' + $parts[0])
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    $notices = Get-ChildItem -LiteralPath $package -File | Where-Object { $_.Name -like 'LICENSE*' -or $_.Name -like '*NOTICE*' }
    if (-not $notices) { throw "Runtime license files not found in $package" }
    foreach ($notice in $notices) { Copy-Item -LiteralPath $notice.FullName -Destination $destination }
}

if ($Verify) {
    $verification = Join-Path $RepositoryRoot 'artifacts\verification'
    Reset-ArtifactDirectory $verification
    $verificationApp = Join-Path $verification 'app'
    Copy-Item -LiteralPath $publish -Destination $verificationApp -Recurse
    $exe = Join-Path $verificationApp 'AppVolumeController.exe'
    foreach ($mode in @('--logic-test', '--ui-test', '--self-test', '--smoke-test')) {
        Write-Output "Running $mode in an isolated verification folder..."
        Invoke-CheckedProcess $exe $mode
    }
    Write-Output "Verification passed. Logs and renders: $verificationApp"
}

if ($Installer) {
    New-Item -ItemType Directory -Path $releaseDirectory -Force | Out-Null
    $compilerLog = Join-Path $RepositoryRoot 'artifacts\installer-build.log'
    $compilerArguments = @('/Q', "/DAppVersion=$version", "/DFileVersion=$fileVersion", "/DSourceDir=$publish", "/DRepoRoot=$RepositoryRoot", (Join-Path $RepositoryRoot 'installer\AppVolumeController.iss'))
    & $compiler @compilerArguments *> $compilerLog
    if ($LASTEXITCODE -ne 0) { Get-Content -LiteralPath $compilerLog | Select-Object -Last 30; throw 'Installer compilation failed.' }
    $setup = Join-Path $releaseDirectory "AppVolumeController-$version-win-x64-Setup.exe"
    $portable = Join-Path $releaseDirectory "AppVolumeController-$version-win-x64-Portable.zip"
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $portable -CompressionLevel Optimal -Force
    [IO.File]::WriteAllText((Join-Path $releaseDirectory 'TESTING.txt'), $testingGuide)
    Copy-Item -LiteralPath (Join-Path $publish 'HELP.html') -Destination $releaseDirectory -Force
    Copy-Item -LiteralPath (Join-Path $publish 'LICENSE.txt') -Destination $releaseDirectory -Force
    $hashLines = foreach ($path in @($setup, $portable, (Join-Path $releaseDirectory 'TESTING.txt'), (Join-Path $releaseDirectory 'HELP.html'), (Join-Path $releaseDirectory 'LICENSE.txt'))) {
        '{0}  {1}' -f (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant(), [IO.Path]::GetFileName($path)
    }
    $hashLines | Set-Content -LiteralPath (Join-Path $releaseDirectory 'SHA256SUMS.txt') -Encoding ascii
    Write-Output "Share this installer: $setup"
    Write-Output "Portable alternative: $portable"
}
else { Write-Output "Ready to run: $(Join-Path $publish 'AppVolumeController.exe')" }

$ErrorActionPreference = 'Stop'
$RepositoryRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))

function Get-DotNetPath {
    $privateSdk = Join-Path $env:LOCALAPPDATA 'AppVolumeControllerSDK\dotnet.exe'
    if (Test-Path -LiteralPath $privateSdk) { return $privateSdk }
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    throw 'Install the .NET 10 SDK to build the app.'
}

function Assert-ArtifactPath([string]$Path) {
    $resolved = [IO.Path]::GetFullPath($Path)
    $boundary = (Join-Path $RepositoryRoot 'artifacts').TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($boundary, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a path outside this project's artifacts folder: $resolved"
    }
    return $resolved
}

function Reset-ArtifactDirectory([string]$Path) {
    $resolved = Assert-ArtifactPath $Path
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
    New-Item -ItemType Directory -Path $resolved -Force | Out-Null
}

function Get-InnoSetupPath([string]$ExplicitPath) {
    $candidates = @(
        $ExplicitPath,
        $env:INNO_SETUP_PATH,
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Antigravity IDE\resources\app\node_modules\innosetup\bin\ISCC.exe')
    )
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { $candidates += $command.Source }
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return [IO.Path]::GetFullPath($candidate) }
    }
    throw 'Inno Setup 6.4+ is needed for installer builds. Install it from https://jrsoftware.org/isdl.php, or supply -InnoSetupPath to an existing ISCC.exe.'
}

function Invoke-CheckedProcess([string]$FilePath, [string]$Arguments, [int]$TimeoutSeconds = 60) {
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        # This is only the child started by this function; never terminate an existing user app.
        $process.Kill()
        throw "Process timed out: $FilePath $Arguments"
    }
    if ($process.ExitCode -ne 0) { throw "Process exited $($process.ExitCode): $FilePath $Arguments" }
}

[CmdletBinding()]
param([switch]$Installer, [switch]$Verify, [string]$InnoSetupPath)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'scripts\Build.ps1') @PSBoundParameters

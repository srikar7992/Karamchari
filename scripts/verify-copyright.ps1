<#
.SYNOPSIS
    Fails when any in-scope C# file is missing the Karamchari copyright header.
.DESCRIPTION
    CI gate counterpart to add-copyright-headers.ps1 (same scope via
    copyright-common.ps1). Lists every violating file and exits 1 when any are found.
    Fix locally with: ./scripts/add-copyright-headers.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

. (Join-Path $PSScriptRoot 'copyright-common.ps1')

$missing = @()
foreach ($file in Get-CopyrightCandidateFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if (-not $text.Contains('<copyright')) {
        $missing += $file.FullName
    }
}

if ($missing.Count -gt 0) {
    Write-Host "Files missing copyright header: $($missing.Count)"
    $missing | ForEach-Object { Write-Host "  $_" }
    Write-Host 'Fix: ./scripts/add-copyright-headers.ps1'
    exit 1
}

Write-Host 'Copyright headers: all files compliant.'
exit 0

<#
.SYNOPSIS
    Adds the Karamchari copyright header to every C# source file that lacks one.
.DESCRIPTION
    Scans src/Backend and tests/Backend for .cs files, skipping generated code
    (Migrations, *.Designer.cs, *ModelSnapshot.cs, *.g.cs, *.generated.cs) and build
    output (obj/bin/artifacts). Preserves each file's UTF-8 BOM state and uses LF
    line endings for the inserted header (.editorconfig enforces end_of_line = lf).
    Idempotent: files that already contain a <copyright> element are left untouched.

    Enforcement lives in scripts/verify-copyright.ps1 (run in CI).
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

. (Join-Path $PSScriptRoot 'copyright-common.ps1')

$added = 0
foreach ($file in Get-CopyrightCandidateFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text.Contains('<copyright')) { continue }

    $header = @(
        '// -----------------------------------------------------------------------',
        "// <copyright file=""$($file.Name)"" company=""Karamchari"">",
        '// Copyright (c) Karamchari.',
        '// All rights reserved.',
        '// </copyright>',
        '// -----------------------------------------------------------------------',
        '',
        ''
    ) -join "`n"

    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $encoding = New-Object System.Text.UTF8Encoding($hasBom)

    [System.IO.File]::WriteAllText($file.FullName, $header + $text, $encoding)
    $added++
}

Write-Host "Copyright headers added: $added"

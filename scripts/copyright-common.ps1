<#
.SYNOPSIS
    Shared file-selection logic for the copyright header scripts.
.DESCRIPTION
    Defines which C# files require the Karamchari copyright header. Generated code and
    build output are excluded. Both add-copyright-headers.ps1 and verify-copyright.ps1
    dot-source this file so the two scripts can never disagree about scope.
#>

function Get-CopyrightCandidateFiles {
    [CmdletBinding()]
    param()

    $roots = @('src/Backend', 'tests/Backend')

    foreach ($root in $roots) {
        Get-ChildItem -Path $root -Recurse -File -Filter '*.cs' | Where-Object {
            $_.FullName -notmatch '[\\/](obj|bin|artifacts|Migrations)[\\/]' -and
            $_.Name -notlike '*.Designer.cs' -and
            $_.Name -notlike '*ModelSnapshot.cs' -and
            $_.Name -notlike '*.g.cs' -and
            $_.Name -notlike '*.generated.cs'
        }
    }
}

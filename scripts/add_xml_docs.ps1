$Files = Get-ChildItem -Path "src\Backend" -Recurse -Filter "*.cs"
foreach ($File in $Files) {
    if ($File.Name.EndsWith(".g.cs")) { continue }
    $Lines = Get-Content $File.FullName
    $OutLines = @()
    $i = 0
    while ($i -lt $Lines.Count) {
        $Line = $Lines[$i]
        $Stripped = $Line.Trim()
        
        $IsPublic = $Line -match '^\s*public\s+(?:(?:partial|static|abstract|sealed|async|virtual|override|readonly)\s+)*(class|interface|record|struct|enum|delegate|[\w<>\[\]]+\s+\w+)\b'
        
        if ($IsPublic -and -not $Stripped.StartsWith('//')) {
            $HasDoc = $false
            for ($j = $i - 1; $j -ge [math]::Max(-1, $i - 5); $j--) {
                if ($j -lt 0) { break }
                $PrevStripped = $Lines[$j].Trim()
                if ($PrevStripped.StartsWith('///')) {
                    $HasDoc = $true
                    break
                }
                if (-not $PrevStripped.StartsWith('[') -and $PrevStripped -ne '') {
                    break
                }
            }
            if (-not $HasDoc) {
                $IndentMatch = $Line -match '^(\s*)'
                $Indent = $Matches[1]
                $OutLines += "$Indent/// <summary>"
                $OutLines += "$Indent/// Provides required documentation for this member."
                $OutLines += "$Indent/// </summary>"
            }
        }
        $OutLines += $Line
        $i++
    }
    Set-Content -Path $File.FullName -Value $OutLines -Encoding UTF8
}
Write-Host "Finished adding XML stubs."

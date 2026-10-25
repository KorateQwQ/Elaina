param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
Add-Type -LiteralPath (Join-Path $Tml 'Libraries/hjson/3.0.0/lib/netstandard1.3/Hjson.dll')
$files = @(Get-ChildItem -LiteralPath (Join-Path $repo 'Localization') -Filter '*.hjson' -Recurse)
$failures = @()
foreach ($file in $files) {
    try {
        [void][Hjson.HjsonValue]::Parse([IO.File]::ReadAllText($file.FullName))
    } catch {
        $failures += "$($file.Name): $($_.Exception.GetBaseException().Message)"
    }
}
if ($failures.Count) { throw ($failures -join "`n") }
Write-Output "PASS: $($files.Count) localization files parsed with tModLoader's Hjson.dll."

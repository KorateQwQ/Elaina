param([string]$Tml = 'C:/Program Files (x86)/Steam/steamapps/common/tModLoader', [string]$Label = 'after')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$out = Join-Path $repo '.vissandbox/constellation-raster'
New-Item -ItemType Directory -Force $out | Out-Null
$source = [IO.File]::ReadAllText("$repo/ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/ConstellationDrawing.cs")
$methods = @('At','Image','Line','Ring','Dot','CrossStar','StarTile','Legible','Measure','MeasureEm','Text','SlotLabelWidth','SlotLabel','LatinWidth','LatinText')
# Extract the production methods on every run, including new raster helpers when present.
foreach ($helper in @('RasterTile','RasterMask','DottedRing','SnapCenter','TextEm')) {
    if ($source -match "\b$helper\(") { $methods += $helper }
}
$body = ''
foreach ($name in $methods) {
    $match = [regex]::Match($source, '(?m)^    (?:internal|private) [^\r\n(=]*\b' + $name + '\(')
    if (!$match.Success) { throw "Missing production method $name" }
    $start = $match.Index; $brace = $source.IndexOf('{', $start); $semi = $source.IndexOf(';', $start)
    if ($source.Substring($start, $brace - $start).Contains('=>') -and $semi -lt $brace) { $end = $semi + 1 }
    else {
        $depth = 1; $end = $brace + 1
        while ($depth -gt 0) { if ($source[$end] -eq '{') { $depth++ }; if ($source[$end] -eq '}') { $depth-- }; $end++ }
    }
    $body += "`n" + $source.Substring($start, $end - $start)
}
$fixture = [IO.File]::ReadAllText("$PSScriptRoot/Preview.cs.txt").Replace('// PRODUCTION_METHODS', $body)
[IO.File]::WriteAllText("$out/Preview.cs", $fixture)
$escapedTml = [Security.SecurityElement]::Escape($Tml)
$escapedRepo = [Security.SecurityElement]::Escape($repo)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>0436</NoWarn></PropertyGroup>
  <ItemGroup>
    <Compile Include="Preview.cs" />
    <Compile Include="$escapedRepo/Tools/ConstellationRasterChecks/FontBake.cs.txt" />
    <Compile Include="$escapedRepo/Tools/ConstellationRasterChecks/Checks.cs.txt" />
    <Compile Include="$escapedRepo/ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/ConstellationFont.cs" />
    <Reference Include="$escapedTml/Libraries/FNA/1.0.0/FNA.dll" />
    <Reference Include="$escapedTml/Libraries/ReLogic/1.0.0/ReLogic.dll" />
    <None Include="$escapedTml/Libraries/Native/Windows/*.dll" Link="%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
"@
[IO.File]::WriteAllText("$out/Preview.csproj", $project)
dotnet run --project "$out/Preview.csproj" -c Release -- $repo $Label
if ($LASTEXITCODE -ne 0) { throw 'Raster checks failed' }

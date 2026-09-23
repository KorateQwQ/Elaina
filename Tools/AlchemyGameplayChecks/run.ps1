param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/alchemy-gameplay-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-CheckItem([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    $null = $items.AppendChild($node)
    return $node
}
$null = Add-CheckItem 'Reference' "$Tml/tModLoader.dll"
$reference = Add-CheckItem 'Reference' "$Tml/Libraries/**/*.dll"
$reference.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
foreach ($relative in @('AlchemyRecipe.cs','AlchemyIngredient.cs','UI/AlchemyCatalog.cs','UI/AlchemyNotebookSnapshot.cs','UI/AlchemyNotebookState.cs','item/AlchemyItem.cs','item/Curios/AshenFacsimileDust.cs','Gameplay/*.cs','Crafting/*.cs')) {
    $null = Add-CheckItem 'Compile' "$repo/ElainaModAlchemy/$relative"
}
$node = Add-CheckItem 'Compile' "$PSScriptRoot/VanillaChecks.cs.txt"
$node.SetAttribute('Link', 'Checks.cs')
$path = Join-Path $output 'Checks.csproj'
$project.Save($path)
foreach ($configuration in @('Release', 'Debug')) {
    dotnet run --project $path --configuration $configuration --verbosity quiet -- $repo
    if ($LASTEXITCODE -ne 0) { throw "Alchemy gameplay checks failed in $configuration." }
}

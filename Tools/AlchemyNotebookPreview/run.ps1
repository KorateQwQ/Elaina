param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/alchemy-state-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../../ElainaModAlchemy/UI/AlchemyCatalog.cs" />
    <Compile Include="../../ElainaModAlchemy/UI/AlchemyCatalogLayout.cs" />
    <Compile Include="../../ElainaModAlchemy/UI/AlchemyNotebookState.cs" />
    <Compile Include="../../ElainaModAlchemy/UI/AlchemyNotebookSnapshot.cs" />
    <Compile Include="../../Tools/AlchemyNotebookPreview/StateChecks.cs.txt" />
    <Compile Include="../../Localization/UIText.cs" />
    <Compile Include="../../Tools/LocalizationChecks/PreviewLocalization.cs.txt" />
    <Reference Include="__TML__/Libraries/hjson/3.0.0/lib/netstandard1.3/Hjson.dll" />
  </ItemGroup>
</Project>
'@
$projectPath = Join-Path $output 'StateChecks.csproj'
[IO.File]::WriteAllText($projectPath, $project.Replace('__TML__', $Tml), [Text.UTF8Encoding]::new($false))
dotnet run --project $projectPath -- $repo
if ($LASTEXITCODE -ne 0) { throw 'Alchemy notebook state checks failed.' }

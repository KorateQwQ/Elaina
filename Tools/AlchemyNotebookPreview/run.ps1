param()
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
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../../ElainaModAlchemy/UI/AlchemyCatalog.cs" />
    <Compile Include="../../ElainaModAlchemy/UI/AlchemyNotebookState.cs" />
    <Compile Include="../../ElainaModAlchemy/UI/AlchemyNotebookSnapshot.cs" />
    <Compile Include="../../Tools/AlchemyNotebookPreview/StateChecks.cs.txt" />
  </ItemGroup>
</Project>
'@
$projectPath = Join-Path $output 'StateChecks.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
dotnet run --project $projectPath
if ($LASTEXITCODE -ne 0) { throw 'Alchemy notebook state checks failed.' }

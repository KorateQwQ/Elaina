param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
# 扫帚冲刺拖尾第二轮：以人物整体为路径（C 第一轮 / E 整体气流带·已定稿，直接调用正式 DrawTrail / F 尾流扩散 / G 速度线），水平、斜下、转向、冲刺后冻结四种场景。
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/broom-dash-preview/body'
New-Item -ItemType Directory -Force -Path $output | Out-Null
python "$PSScriptRoot/prepare.py" $repo $output
if ($LASTEXITCODE -ne 0) { throw 'Texture preparation failed.' }
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"),
    @('Compile', "$repo/Mounts/BroomDashWindRenderer.cs"),
    @('Compile', "$PSScriptRoot/BodyTrail.cs.txt"),
    @('None', "$Tml/Libraries/Native/Windows/*.dll")
)) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$projectPath = Join-Path $output 'Body.csproj'; $project.Save($projectPath)
dotnet run --project $projectPath --configuration Release --verbosity quiet -- $output $repo
if ($LASTEXITCODE -ne 0) { throw 'Broom dash body trail preview failed.' }
python "$PSScriptRoot/compose_body.py" $output
if ($LASTEXITCODE -ne 0) { throw 'Options composition failed.' }

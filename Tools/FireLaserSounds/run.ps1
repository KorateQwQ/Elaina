param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Check)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $repo '.vissandbox/fire-laser-sounds'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$project = "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Reference Include=`"$Tml/Libraries/MP3Sharp/1.0.0.0/MP3Sharp.dll`" /><Compile Include=`"$PSScriptRoot/Program.cs.txt`" Link=`"Program.cs`" /></ItemGroup></Project>"
Set-Content -LiteralPath (Join-Path $work 'Sounds.csproj') -Value $project -Encoding UTF8
$arguments = @($repo)
if ($Check) { $arguments += '--check' }
dotnet run --project (Join-Path $work 'Sounds.csproj') --verbosity quiet -- @arguments
if ($LASTEXITCODE -ne 0) { throw 'Fire laser sound export failed.' }

param([string]$Tml = 'D:\Steam\steamapps\common\tModLoader', [switch]$Headers)
$ErrorActionPreference = 'Stop'
$notebookRepo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
python (Join-Path $PSScriptRoot 'prepare.py') --tml $Tml
if ($LASTEXITCODE -ne 0) { throw 'Notebook drawing extraction failed.' }
$notebookArgs = @($notebookRepo)
if ($Headers) { $notebookArgs += '--headers' }
dotnet run --project (Join-Path $notebookRepo '.vissandbox\notebook-preview\Preview.csproj') -- @notebookArgs
if ($LASTEXITCODE -ne 0) { throw 'Notebook checks or FNA rendering failed.' }

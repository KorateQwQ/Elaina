param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
# Each run has its own source copies and markers; never edit the user's production settings.
$output = Join-Path $repo ('.vissandbox/purification-dew/hotreload-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
foreach ($name in @('PurificationDewRenderer', 'PurificationDewVisualSettings')) {
    Copy-Item -LiteralPath "$repo/ElainaModAlchemy/Purification/$name.cs" -Destination $output
}
Copy-Item -LiteralPath "$PSScriptRoot/HotReloadProbe.cs.txt" -Destination "$output/Program.cs"
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><DebugType>portable</DebugType><Optimize>false</Optimize></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(@('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"), @('None', "$Tml/Libraries/Native/Windows/*.dll"))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') { $node.SetAttribute('Link', '%(Filename)%(Extension)'); $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest') }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$project.Save("$output/Probe.csproj")
$info = [Diagnostics.ProcessStartInfo]::new('dotnet')
$info.WorkingDirectory = $output; $info.UseShellExecute = $false; $info.CreateNoWindow = $true
$info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
$info.Environment['DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER'] = '1'
foreach ($argument in @('watch', '--non-interactive', '--project', "$output/Probe.csproj", 'run', '--no-launch-profile', '--',
    "$repo/Effects/Content", "$repo/../KL/Effects/Tex/Trail/LightTrail2.png", $output)) { $info.ArgumentList.Add($argument) }
$process = [Diagnostics.Process]::Start($info)
$stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
try {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while (!(Test-Path -LiteralPath "$output/ready.json")) {
        if ($process.HasExited -or $timer.Elapsed.TotalSeconds -gt 45) { throw 'Hot Reload probe failed to start; see logs in the output directory.' }
        Start-Sleep -Milliseconds 250
    }
    $before = Get-Content -LiteralPath "$output/ready.json" -Raw | ConvertFrom-Json
    $settings = [IO.File]::ReadAllText("$output/PurificationDewVisualSettings.cs")
    foreach ($change in @(@('SwaySpeed: 1.8f','SwaySpeed: .9f'), @('SwayAmplitude: .08f','SwayAmplitude: .16f'),
        @('ColorSpeed: .10f','ColorSpeed: .25f'), @('TaperExponent: .72f','TaperExponent: 1.2f'), @('Width: 24f','Width: 30f'))) {
        if (!$settings.Contains($change[0])) { throw "Probe's baseline no longer matches: $($change[0])" }
        $settings = $settings.Replace($change[0], $change[1])
    }
    [IO.File]::WriteAllText("$output/PurificationDewVisualSettings.cs", $settings)
    $timer.Restart()
    while (!(Test-Path -LiteralPath "$output/passed.json")) {
        if ($process.HasExited -or $timer.Elapsed.TotalSeconds -gt 30) { throw 'Hot Reload did not update the existing FNA Effect.' }
        Start-Sleep -Milliseconds 250
    }
    $after = Get-Content -LiteralPath "$output/passed.json" -Raw | ConvertFrom-Json
    if ($before.Process -ne $after.Process) { throw 'The process restarted instead of applying Hot Reload.' }
    Get-Content -LiteralPath "$output/passed.json"
    Write-Output "PASS: same process and Effect observed five method-body changes through actual .NET Hot Reload. $output"
} finally {
    if (!$process.HasExited) { $process.Kill($true) }
    $process.WaitForExit()
    [IO.File]::WriteAllText("$output/stdout.log", $stdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText("$output/stderr.log", $stderr.GetAwaiter().GetResult())
    $process.Dispose()
}

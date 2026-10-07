# Bloom Backbuffer Regression Probe

Run from PowerShell with the .NET 9 SDK installed:

```powershell
& ./Tools/BloomBackbufferPreview/run.ps1
```

Use `-TmlPath <installation>` for a different tModLoader installation. The
adjacent KL source project must exist. The script copies the host templates to
`.vissandbox/bloom-backbuffer-preview` and writes PNGs under its `out` directory.
The templates use `.txt` extensions so they are not compiled into the mod;
`Tools` and `.vissandbox` are already excluded by this mod's `build.txt`.

The host parses the current KL C# with Roslyn and compiles the actual requested
Bloom methods, sampling methods, RenderHelper target creation/switching, queue,
SpriteBatch state capture/restore, and effect bindings. It uses the installed
FNA/native libraries and KL's compiled Bloom/ReColor XNB shaders. Terraria's
device, camera, drawing helper and request input are minimally adapted. The
visual is a synthetic HDR glow over a fixed background, not a game screenshot.

`before-discard.png` disables only the new backbuffer preservation assignment
to reproduce the black background. The fixed path keeps it enabled and checks:

- Backbuffer background preservation at 2, 5 and 7 iterations.
- Multiple Bloom layers and exactly one callback invocation per request.
- Zero/nonzero Bloom strength and disabled post-processing.
- Active SpriteBatch restoration and callbacks that end their SpriteBatch.
- Usage, target, viewport and scissor restoration when a callback throws.
- The existing scene-target path.

Verified on 2026-10-07 with the installed FNA D3D11 backend. PNGs were opened
and visually checked. Lighting, projectile AI, world rendering, networking and
other mods are not simulated. Build/reload KL in tModLoader and verify Retro,
Color with water waves, and Color without water waves in the actual game.

Source-only compile commands, without packaging or replacing installed mods:

```powershell
dotnet build ../KL/KL.csproj -p:BuildMod=false -p:TargetFramework=net8.0 --no-restore -v:quiet -clp:ErrorsOnly
dotnet msbuild ./伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:quiet -clp:ErrorsOnly
```

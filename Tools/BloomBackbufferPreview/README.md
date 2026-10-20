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
- Reuse across repeated same-size resolution notifications, disposal on logical
  scene/backbuffer size changes, tiny pyramid sizes, recovery of a disposed level,
  and deferred unload disposal that cannot release a new target set.
- Ownership checks that leave Terraria's borrowed scene target alive, followed
  by a fresh Bloom draw after the lifecycle checks.

The lifetime probe changes the reported backbuffer dimensions and allocates
actual FNA targets. It does not reset the native swapchain, and cannot verify
window/fullscreen transitions in the game. On Windows the host retains a module
reference to the system `d3d11.dll` until process exit: without it, the installed
FNA3D backend's teardown crashed in `d3d11.dll_unloaded` after the disposal checks.
This workaround only affects the offline host; assertions and exit failures
remain enforced. No corresponding DLL pin is added to the mod.

On 2026-10-20, the five existing PNG outputs were byte-identical before and after
the target lifetime fix. `after-lifetime-rebuild.png` checks the recreated targets.
The two mods also compiled from source. This does not reproduce or resolve the
reported intermittent one-second in-game stall by itself.

Slow-frame diagnostics are now written by KL to `client.log` as `[RenderStall]`
when update/draw wall time reaches 100 ms or the draw-start interval reaches
200 ms, at most once per second outside the menu. They include Bloom time/pass
count, target allocation time/count, the last/slowest measured normal missile
cast or spawn, GC counts, resolution, lighting, focus and pause state. These are
CPU wall-clock intervals, including blocking inside the calls, not GPU timestamps.
The draw interval also includes time outside the update/draw hooks. No GPU
readback, forced GPU wait, or OS process polling runs inside the mod.

Verified on 2026-10-07 with the installed FNA D3D11 backend. PNGs were opened
and visually checked. Lighting, projectile AI, world rendering, networking and
other mods are not simulated. Build/reload KL in tModLoader and verify Retro,
Color with water waves, and Color without water waves in the actual game.

Source-only compile commands, without packaging or replacing installed mods:

```powershell
dotnet build ../KL/KL.csproj -p:BuildMod=false -p:TargetFramework=net8.0 --no-restore -v:quiet -clp:ErrorsOnly
dotnet msbuild ./伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:quiet -clp:ErrorsOnly
```

# Battle skill bar preview

The live SUI bar uses `ElainaSkillBar.cs`, `ElainaSkillSlot.cs`, and `ElainaSkillIcon.cs`.
`SkillBarDrawing.cs` is linked directly into the offline FNA host; it is not a copy.
The reference is `ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-battle-eight-skills.html`.

## Rebuild static artwork

Run `node Tools/SkillBarPreview/bake.cjs` with `playwright` and `sharp` on Node's module path.
Set `CHROME_PATH` when Chrome is not installed at its default Windows location.
The script reads the reference's final CSS and embedded rack image. It exports only
static decorations, with no skill icons, cooldowns, labels, or animation frames baked in.
The empty slot's four-point ornament is a small SVG shape.

The two PNGs under `ElainaModSkills/ElainaSkillUI/BattleBar` contain premultiplied RGBA
for the local tML rawimg pipeline. Do not premultiply them again when loading.
Rack: 1388 x 284, logical size 694 x 142, including 24px shadow padding on each side.
Ornaments: 1152 x 256, six 192 x 192 tiles at 2x resolution, followed by an AA line strip
and four 64 x 64 marker tiles (star core/glow, diamond core/glow) at x=16+64*n, y=192.
Tile order: resting frame, selected frame, selected fill/glow, empty ornament, resting
fill, hover fill. Combined RGBA storage is about 2.63 MiB without mipmaps.

Unselected slots draw only four 12px corner patches from the resting-frame tile.
The original complete tile remains available for the selected arrival outline.
Occupied, empty, and hovered slots retain their different frame opacities.

The moving highlight is evaluated at runtime from the CSS conic-gradient stops,
with a 2.8-second period and 224 cached contour segments. It creates no textures or
render targets per frame. Selection triggers a 450ms additive frame/glow pulse,
an expanding outline, and a main-star flare, peaking after 55ms. The icon stays
fixed. A 10x12px silver star sits 37.5px above the center, grows by up to 25% on arrival,
then remains fixed. A quieter 4x6px diamond sits 34.5px below the center and fades in
65ms later. Only their soft glows breathe (3.6-second period) after arrival.
The original two small SVG stars are removed from the selected-frame tile during baking.
GPU time was not benchmarked.

## Run FNA validation

```powershell
pwsh -NoProfile -File Tools/SkillBarPreview/run.ps1 -Tml 'D:\Steam\steamapps\common\tModLoader'
python Tools/SkillBarPreview/compose-arrival.py
```

The hidden FNA process exits after exporting PNGs under `.vissandbox/skillbar-preview/output`.
It covers 100% and 150% scale, occupied and empty selection, hover, no selection,
the last slot, different orbit phases, and a simulated cooldown/stack state using
KL's actual CDEffect and NotoSerifSC assets. The host uses the same drawing source and
real skill artwork. It does not simulate SUI input routing, player state, or game lifecycle.
The reference PNG is browser output; the output directory contains FNA output.
The optional Pillow composition script creates `selection-arrival.gif` from 60 FNA
frames (30fps, two selections over two seconds) and a six-phase PNG timeline.
Tools and scratch output are excluded from mod packaging. The `.cs.txt` host is copied
to the scratch directory by `run.ps1`, so it is excluded from the main C# compilation.

The existing skill cooldown shader, skill-specific callbacks, and stack labels remain
live in the game. They require in-game verification after rebuilding/reloading the mod.
There is no new UI hotkey: this replaces the existing always-enabled battle bar.

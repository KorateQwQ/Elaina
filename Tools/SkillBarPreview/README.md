# Battle skill bar preview

## Bar size setting

`BasicConfig.SkillBarScale` is a client setting from 0.5 to 1.0 in 0.05 steps,
defaulting to 1.0 for new and existing configurations. It resizes the SUI bar,
padding, gaps, slots, icons and all ornaments/cooldown/seal drawing. The wheel's
scale remains independent. Movement uses the resized bounds; the book/toast dock
follows the bar's right edge.

`pwsh -File Tools/SkillBarPreview/run.ps1 -ScaleOnly` renders 100%, 75% and 50%
bar sizes at 100% and 125% Terraria UI scales using the production drawing code,
including cooldown numbers, charge pips/badges and seal/release states. Outputs
are `seal-states-dark-ui<UI scale>-bar<bar scale>.png`. These are FNA offline
renders with fixture skill state. Live SUI input and settings-menu interaction
still need game verification.

`Tools/ResourceDisplayChecks/run.ps1` checks config JSON compatibility/round trips,
runtime clamping (including nonfinite values), resized production SUI properties,
restoring full size, movement/bounds at multiple UI scales and the book dock.

## Seal state (2026-10-19)

`SkillBarDrawing.DrawSealState` ports the four corner tethers, central diamond/keyhole,
denied shake, and 0.7-second release animation from `Tools/SkillBarSealPreview/index.html`.
The HTML reference is read-only and remains user-owned. There is no release floating text.
`ElainaSealedSkill` supplies transient gameplay state; rendering cannot grant a release.
`SkillSeal.fx` preserves the existing cooldown sector and adds the reference's lavender
grayscale tone. The shared KL shader is unchanged.

Recompile only this shader with `pwsh -File Tools/SkillBarPreview/compile-seal.ps1`.
`run.ps1` loads the resulting real XNB and the current shared drawing source. Additional
`seal-states-dark-*` and `seal-states-day-*` outputs cover sealed-ready, sealed-cooling,
denied, release at 0.06/0.25/0.50 seconds, unsealed-cooling, and unsealed-ready at 100/150%.
The English captions are fixture labels, not in-game floating text. The wheel preview
also covers a sealed skill and its center display. Actual mouse routing and live network
sockets are not exercised by these visual fixtures.

The live SUI bar uses `ElainaSkillBar.cs`, `ElainaSkillSlot.cs`, and `ElainaSkillIcon.cs`.
The bar ignores the mouse entirely. Holding the `MoveSkillBar` keybind (default K) centers it
on the cursor; the position is a mod-wide client preference (`SkillBarLayout.cs`, stored in
`ModConfigs/伊蕾娜_SkillBarLayout.json`), not per player.
The bar derives directly from SUI `BaseBody`, without KL's mouse drag controller;
the legacy book icon no longer forwards drag events to it. Positioning runs during
SUI's draw status update, where pointer and screen bounds share UI coordinates.
`Tools/ResourceDisplayChecks/run.ps1` also checks that mouse gestures cannot move it.
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

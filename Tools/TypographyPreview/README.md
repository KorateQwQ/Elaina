# HTML-style typography verification

Run `pwsh -NoProfile -File Tools/TypographyPreview/run.ps1` (optional `-Tml <installation>`).
The script extracts current drawing methods from ConstellationPreviewUI.cs and links
PreviewDrawing.cs and PreviewData.cs directly into an isolated FNA project. It uses
the actual KL NotoSerifSC and HarmonyOS_Sans_SC XNBs and the existing UI artwork.
Outputs: `.vissandbox/typography-preview/output/panel-1.0.png`, `panel-1.5.png`,
`help-1.0.png`, `help-1.5.png`. These are offline FNA renders with demo state, not game screenshots.

Eight-slot footer checks also emit `loadout-reference`, `loadout-occupied`,
`loadout-eighth`, and `loadout-hover` at both scales. The reference state has an
empty selected slot 01, ember in 02 and vortex in 03, matching the supplied crop.
The replay refreshes layout constants and footer drawing from the current source,
checks the actual XNBs for footer glyph coverage, and exercises moving equipment
to slot 08, preventing duplicates, clearing/re-equipping, and resetting all eight slots.

Font roles:
- Noto Serif SC Regular 400: main/chapter/detail/modal titles, revealed skill names,
  equip/awaken button, footer title, decorative Latin text and numbers.
- HarmonyOS Sans SC: descriptions, filters, status text, tooltips, hidden skill labels.
- Gelasio Regular 400: footer slot numbers and current target number; printable ASCII,
  baked at 48px, loaded from KL's FontManager (OFL source/license in KL/Fonts).
- Battle cooldown and stack counts: Noto Serif SC at nominal 22px and 14px.
- Legacy Elaina skill tooltip: overrides KL's new NameFont extension point. Other
  KL consumers retain their original default font.

The same font and letter spacing are used for text width, fitting and drawing.
Fitted labels scale both font size and letter spacing together to stay within bounds.
The HTML uses Georgia for Latin/numbers and Microsoft YaHei for body text; the current
implementation deliberately uses the already imported serif and sans assets, so these
two roles are stylistic approximations. The footer now uses the Georgia metrics-compatible
Gelasio alternative for numbers. Noto Serif SC titles match the HTML font family.

SUI input/lifecycle and the legacy tooltip's live layout still require game verification.
Rebuild/reload KL first, then Elaina. The star-map preview keeps its existing open key;
this change does not connect its demo state to player progression or saves.

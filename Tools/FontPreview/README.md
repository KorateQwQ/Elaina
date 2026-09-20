# Noto Serif SC integration probe

The output resource lives in `../KL/Fonts/NotoSerifSC.xnb`; KL exposes it through
`KL.Drawing.FontManager.NotoSerifSC`. No production UI font assignments are changed.

## Rebuild

Requires Python with `fonttools`, Windows/.NET Framework, and the existing
DynamicFontGenerator with its XNA/ReLogic pipeline DLLs.

```powershell
python -m pip install fonttools
pwsh -NoProfile -File Tools/FontPreview/build-font.ps1
```

Optional parameters: `-Font <NotoSerifSC-VF.ttf>`, `-Generator <DynamicFontGenerator.exe>`,
`-Python <python.exe>`. The installed source version/hash is recorded beside the XNB.
Upstream family: https://github.com/google/fonts/tree/main/ofl/notoserifsc
Bundled license: `../KL/Fonts/NotoSerifSC-OFL.txt` (SIL OFL 1.1).

The preparer fixes weight 400, uses a distinct temporary family name, and selects only
supported printable BMP characters. The old generator does not consume a private
GDI+ font collection, so the wrapper registers a temporary session font and removes it
on process exit. It does not copy fonts into Windows or change font registry entries.

## FNA verification

```powershell
pwsh -NoProfile -File Tools/FontPreview/run.ps1
```

Optional `-Tml <installation>` overrides the local installation path. A hidden FNA
window loads the actual XNB with ReLogic's DynamicSpriteFontReader, validates Chinese,
English, numbers and supported symbols, then exports 100%/150% comparison screenshots
under `.vissandbox/font-integration/preview/output`. It checks that unsupported star
symbols are not falsely advertised. This validates the resource, not tML asset lifecycle
or a full SUI input tree. Reload/rebuild KL before using the new FontManager field in game.

Tool C# files are stored as `.cs.txt` and compiled in `.vissandbox`, excluded from the
production mod compilation. Elaina's `buildIgnore` excludes Tools and scratch output.

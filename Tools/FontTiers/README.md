# 字体母版维护（保留旧 FontTiers 路径）

2026-11-04 已删除 KL 中 54 份旧小字号 XNB 及对应加载代码。技能面板的小字号资源、字符清单和重建入口现位于 [ConstellationRasterChecks](../ConstellationRasterChecks/README.md)，不会再读取旧字号文件。

本目录的 `build.ps1` 现在只重建裁剪后的 **NotoSerifSC 48px 母版**，保留用于新增母版未覆盖的字符；不会生成或恢复小字号 XNB。`describe.py` 和 `GenerateHost.cs.txt` 是该母版流程的辅助工具。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/FontTiers/build.ps1
```

母版流程仍需要旧字体生成器、Python、HarmonyOS Sans SC Regular TTF，以及改名为 KL 字族的 Noto Serif SC Regular / Gelasio TTF。通过 `-Generator`、`-HarmonyFont`、`-NotoFont`、`-GelasioFont` 指定实际文件；`describe.py` 中原版物品名来源也需匹配本地 Terraria 源码目录。原字体源、许可与描述保存在 KL 的 `Fonts` 目录。

此流程和技能面板小字号生成是独立的：一般重建面板资源只需现有母版 XNB，不需要运行旧字体生成器。新增字符时检查母版覆盖和 `Tools/ConstellationRasterChecks/charsets.json`，分别更新后再重建。

# 断裂收细版快照

保存于 2026-11-01，内容为启用连续追尾版之前的实际源码与 FNA 预览。

- `MagicMissile.cs.txt`：原弹幕与 TrailManager 生命周期接入。
- `MagicMissileTrailVisuals.cs.txt`：随机裂口逐步增加、完整横截面收细。
- `MagicMissileTrail.fx.txt`：该版 Shader 源码。
- `preview.png`：该版实际 FNA 输出。

当前正式实现默认采用连续追尾模式。无需覆盖这些快照即可对比旧版：设置 `MISSILE_EARLY_CRACKS=1` 后运行上级 `run.ps1`。正式 C# 的 `FracturedRelease` 开关也保留旧版绘制路径；本目录的 `.txt` 快照仅供恢复与查阅，不参与模组编译或打包。

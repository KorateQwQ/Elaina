# 炼金目录与 UI 资源约定

本文件适用于本目录及其子目录。修改炼金配方、材料目录、物品映射或图标时，必须一起检查 UI 资源；仅 C# 编译成功不能证明配方页面可打开。

- `UI/AlchemyDrawing.cs` 的 `Outline(path)` 会直接加载同名预制描边，不会在缺图时自动生成。新增原版材料必须有 `UI/Assets/Outlines/Item_<真实 ItemID>.png`；新增或改画模组图标必须同步对应的 `<图标名>_Pixel.png` 描边。
- 原版 ItemID 使用 `item/AlchemyCatalogItemTypes.cs` 与本机 tModLoader 常量解析，不凭记忆填写。材料目录、真实物品映射、中英文本地化、轮廓 PNG 和 `UI/Assets/Outlines/manifest.json` 必须一致。
- 新增原版材料后，从仓库根目录依次执行：
  1. `pwsh -File Tools/AlchemyPreview/run.ps1 -ExportMaterials`
  2. `python Tools/AlchemyPreview/bake_redesign.py`
  3. `python Tools/AlchemyPreview/bake_redesign.py --check`
  4. `pwsh -File Tools/AlchemyPreview/run.ps1 -Scene <相关场景>`，打开本次生成的截图，检查材料行与底栏，至少覆盖默认与 125% UI 缩放。
- 预览启动时会按正式目录检查全部材料与配方的轮廓资源，不能仅依靠本机已导出的 `material-sources` 文件列表判定资源齐全。四种职业药水可用 `bloodlust`、`starpower`、`focus`、`resonance` 场景。
- 描边沿用现有四邻域外扩一像素、白色预乘 RGBA 遮罩；动画按帧处理。不要用运行时生成轮廓代替漏交的资源。
- 生成工具和说明保留在 `Tools/AlchemyPreview`；正式 PNG 与清单必须位于可打包目录，不能只存在于被忽略的 `.vissandbox`。交付时说明离线预览结果及尚未完成的游戏内验证。

# 净土露滴 · 蓝色滴管

正式物品贴图取自 `ElainaModAlchemy/item/Untitled 10152026 050309.png` 左下角的蓝色滴管，保留分离的上部组件。截取区域为 `(0, 32, 36, 64)`，按透明边界裁为 **27×28**，不缩放、不改色。`AlchemyCatalog` 的尺寸与正式贴图一致。

重建命令（仓库根目录）：

```powershell
python Tools/PotionPixel/GeneratePurificationDewPixel.py
python Tools/AlchemyPreview/prepare_mystery_pixels.py
pwsh -File Tools/AlchemyPreview/run.ps1 -ExportMaterials
python Tools/AlchemyPreview/bake_redesign.py
python Tools/AlchemyPreview/bake_redesign.py --check
pwsh -File Tools/AlchemyPreview/run.ps1 -Scene vanilla-materials
pwsh -File Tools/AlchemyPreview/run.ps1 -Scene recipe-isolation-ready
```

正式 PNG 位于 `ElainaModAlchemy/item/ExampleAssets/PurificationDew_Pixel.png`，未学习剪影位于 `UI/Assets/MysteryPixel`，预制描边与尺寸清单位于 `UI/Assets/Outlines`。批量像素生成器也调用新提取入口。旧的 `GeneratePurificationDewUprightPixel.py` 只保留历史设计输出，不再覆盖正式贴图。

配方卷轴由肉后树妖出售，沿用卷轴基础价格 5 金。炼金 8 级时在手记中消耗 1 张学习；原合成材料为月露×1、净化粉×10、太阳花×1，产出×1。本次只补充配方获取和替换贴图，净化使用效果仍待实现。

FNA 离线预览覆盖 100% 和 125% UI 缩放，不替代游戏中的商店交互与角色存档验证。

验证记录（2026-10-22）：模组编译通过（0 错误）；正式资源检查通过（65 张描边）；药水专项检查通过，其中配方卷轴包含 128 项注册、研究、存档、商店与掉落检查。已打开本次 `vanilla-materials` 的 100%／125% FNA 截图，以及 `recipe-isolation-ready` 的 125% 截图，确认新图标、材料行、底栏和研究卷轴显示。全量物品检查被现有 `RevealingDust pixel asset has catalog resolution` 尺寸问题阻断；本次未修改显迹尘。游戏内购买、研究和存档操作尚未人工验证。

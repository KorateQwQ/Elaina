# 炼金物品像素图约定

本目录的脚本手绘炼金物品的像素贴图（整数像素网格，无抗锯齿、无半透明）。新画或重画贴图时遵守以下约定。

## 外轮廓必须纯黑

物品外轮廓一律使用纯黑 `#000000`。手记纸卡上不画描边光环，深紫灰外轮廓（`#51415f`、`#332f43` 等）在浅紫纸面上看不清形状。

- 外轮廓指与透明像素或画布边缘四邻接的不透明像素。
- 内部线条（瓶塞与瓶口的分隔、蝴蝶结的环线等）可以继续用深紫灰，避免内部过重。
- 推荐做法：网格画完后统一后处理，不要在每个部件里手填黑色。新脚本使用 `pixel_common.py` 的 `blacken_silhouette()`，它把所有外缘像素换成调色板符号 `O`（`#000000`）。

## 不画与物品分离的装饰

物品可以丢在世界里，掉落物会连同整张贴图一起绘制、旋转。所以贴图外不要画飘着的星芒、闪光、热气、烟雾、香气卷等装饰，否则它们会跟着掉落物一起转。

- 装饰只能画在物品本身之内，例如瓶中的星点、药液里的气泡。
- 确实需要特效时，在绘制代码里额外添加（如手记 UI 或 `PostDrawInWorld`），不要烘进贴图。
- 检查方法：除了属于物品本身的部件（如灰发染剂旁的梳子），不透明像素应当四邻接连成一块。

## 先出预览，确认后再写入正式文件

- 脚本默认只写 `Tools/PotionPixel/output/`：`*_Pixel_New.png`（原尺寸）、`*_Compare.png`（新旧对比，深浅背景，多倍率）等预览。
- `--apply` 才写入正式贴图：`ElainaModAlchemy/item/ExampleAssets/*_Pixel.png`、`UI/Assets/Outlines/*_Pixel.png` 及可编辑的 `.pixel.json`。贴图要等确认后才能 `--apply`。
- 写入正式贴图后，运行 `python Tools/AlchemyPreview/prepare_mystery_pixels.py` 更新未研究问号图，并让旧的批量生成器（如 `Tools/AlchemyPlan/pixels.py`、`GenerateStarAndMoonPotionPixels.py`）转调新脚本，避免重建时退回旧图。

## 其他

- 画布尺寸须与 `AlchemyCatalog.cs` 中条目的 `PixelWidth` / `PixelHeight` 一致；图形不必填满画布。
- 瓶身外轮廓应左右对称（光影可以不对称）；`GenerateMoonDewPixel.py` 内有对称断言可参考。

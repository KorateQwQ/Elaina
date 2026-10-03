# 净土露滴 · 已接入竖直手持版

当前正式物品贴图为用户确认的 **19×34、17 色**竖版，依据 `Tools/PotionBottleDesigns/output/round1/palettes/PurificationDew_Iridescent.svg` 手绘。珍珠圆顶、金色套环与渐细玻璃管共用 x=9 中轴；没有下方独立露滴。管内药液为水平、左右镜像的蓝紫粉色带，上部玻璃与珍珠保留左上受光。

运行 `python Tools/PotionPixel/GeneratePurificationDewUprightPixel.py` 重建。旧入口 `GeneratePurificationDewPixel.py` 也转调此生成器，避免重新运行后恢复被替换的斜版。

- `ElainaModAlchemy/item/ExampleAssets/PurificationDew_Pixel.png`：正式物品贴图。
- `Tools/PotionBottleDesigns/output/round1/pixel/PurificationDew_Upright.png` 及 `PurificationDew.png`：相同设计图。
- 同目录两个 `.pixel.json`：相同的可编辑字符网格。
- `Tools/PotionPixel/preview/PurificationDew_Upright_x8.png` 和 `PurificationDew_Pixel_x8.png`：8 倍最近邻预览。
- 像素稿目录 `PurificationDew_Upright-review.png` 和 `PurificationDew-review.png`：原尺寸、2×、4×、8×与深浅背景检查。

`PurificationDew.EntryId = "isolation"` 经 `AlchemyItem.Texture → AlchemyCatalog.PixelPath` 加载这张贴图；目录尺寸同步为 19×34。两套批量像素生成器调用已确认竖版。此次仅接入贴图和尺寸，没有新增物品使用行为。彩铅手记图与剪影也已通过 `Tools/PotionBottleDesigns/apply_notebook_potions.py` 保留原虹彩 SVG 的斜向构图和装饰露滴，竖直无露滴只用于手持物品像素图；参考 SVG 仍保留在 round1/palettes；历史比较图只作为迭代记录。

像素稿的 `ROWS` 是重建源，非空行均为连续实心像素段。离线预览未模拟游戏物品栏、手持位置或光照。

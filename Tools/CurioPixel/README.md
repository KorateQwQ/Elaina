# 奇物像素稿

## 以太滴管像素稿

`python Tools/CurioPixel/GenerateAetherDropper.py` 重建以太滴管正式物品贴图。参照净土露滴的直立轮廓，在 19×34 原生像素网格上绘制紫色胶头、水平金色套环和玻璃管；微光只保留在管内，没有滴出的液体、悬挂水滴或外围散点。透明背景，Alpha 仅 0/255。

`AetherDropper/` 保存 PNG、可编辑调色板字符网格、修改前备份、8× 放大图及深浅背景 1×/2×/8× 对照。预览左侧为旧稿，中间为直立以太滴管，右侧为净土露滴参考；这是离线像素检查，不是游戏截图。生成入口 `Tools/AlchemyPlan/pixels.py` 复用此生成器，目录与计划尺寸同步为 19×34。原 SVG 与彩铅图继续独立保留。

## 灰赝尘像素稿

`python Tools/CurioPixel/GenerateAshenFacsimileDust.py` 重建已确认的灰赝尘像素贴图。需要 Python 和 Pillow，预览与可编辑稿输出在 `AshenFacsimileDust/`，同时更新正式贴图 `ElainaModAlchemy/item/ExampleAssets/AshenFacsimileDust_Pixel.png`。

- `AshenFacsimileDust_Pixel.png`：32×32、12 个可见颜色、RGBA、透明背景；像素只有 0/255 两种 Alpha。
- `AshenFacsimileDust_Review.png`：深浅背景的 1×、2×、8× 最近邻预览，不是游戏截图。
- `AshenFacsimileDust_Pixel.json`：导出的调色板与逐行字符网格。
- `metrics.json`：尺寸、颜色数、Alpha 与可见范围。

源稿是已确认的 `ElainaModAlchemy/item/ExampleAssets/AshenFacsimileDust.svg`。像素由生成器中的整数坐标图层重绘，保留灰紫布袋、粉色束绳、白色四角星和右侧粉尘堆；没有将 SVG 缩小采样。修改造型时编辑生成器的 `build_sprite()` 与 `PALETTE`。

顶部粉尘使用低矮、圆润的颗粒簇；右侧粉堆采用连续缓坡和浅灰紫亮面。袋口填满布料色，布袋在阴影和绳结绘制后补齐四邻域外边界，避免阶梯转折处出现描边断续。独立于物品的右上星芒和外围散点均已移除，布袋上的星形标记保留。

已接入普通物品绘制：`AshenFacsimileDust.EntryId = "mimic"` 通过共同基类读取目录的 PixelPath，物品尺寸随目录同步为 32×32。批量像素生成器 `Tools/AlchemyPixel/BuildAlchemyPixels.py` 的 dust() 复用本生成器，避免重建时退回旧版。原 SVG 与炼金手记彩铅图独立保留。Tools 目录由模组构建排除。

## 瓶中雨像素稿

`python Tools/CurioPixel/GenerateBottledRain.py` 重建已确认的正式贴图 `ElainaModAlchemy/item/ExampleAssets/BottledRain_Pixel.png`，以及 `BottledRain/` 下的像素稿、调色板字符网格 JSON、深浅背景 1×/2×/8× 预览与尺寸检查信息。原生尺寸 31×38，16 个可见颜色，Alpha 仅 0/255。

参照已确认的瓶中雨 SVG 和彩铅版，手绘梨形玻璃瓶、紫色封口、粉色封带、三瓣小雨云、三颗错落水滴及浅层积水。外围没有星芒或散点，轮廓为连通的闭合像素边缘。所有形状直接在整数坐标上绘制，保留内部金色小星印；没有缩小 SVG 采样。通过 `build_sprite()` 调整图层。

瓶中雨已通过 `EntryId = "rain"` 接入目录 PixelPath，物品尺寸由共同基类读取目录的 31×38。`Tools/AlchemyPlan/pixels.py` 的 rain() 复用本生成器，确保完整资产重建保留定稿。不影响已接入的灰赝尘和炼金 UI 彩铅图。预览为离线像素检查，不是游戏截图。

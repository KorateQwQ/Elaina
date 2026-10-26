# 忘却之书像素稿

`python Tools/OblivionBookPixel/GenerateBookOfOblivion.py` 重建预览与可编辑稿；加 `--apply` 时同时写入正式物品贴图 `Items/消耗品/BookOfOblivion.png`。需要 Python 和 Pillow。

忘却之书是用于重置技能等级的消耗品，目前只有图标，尚无物品代码。图标为 32×32、14 个可见颜色、RGBA 透明背景，Alpha 仅 0/255。

- 灰紫色厚书，左侧书脊带两道银箍，前缘上下两角包银，右下露出书页与粉色书签丝带。
- 封面中央是发亮的闭眼法印（下垂眼睑加四根睫毛），外围压纹内框在右下角磨损断开，表示“被遗忘”。没有独立于书本的外围散点。
- 物品类命名为 `BookOfOblivion` 且放在 `Items/消耗品/` 时可直接按默认路径加载此贴图。

输出：

- `BookOfOblivion_Pixel.png`：与正式贴图相同的 32×32 原图。
- `BookOfOblivion_Review.png`：深浅背景的 1×、2×、8× 最近邻预览，不是游戏截图。
- `BookOfOblivion_Pixel.json`：调色板与逐行字符网格。
- `metrics.json`：尺寸、颜色数、Alpha 与可见范围。

修改造型时编辑生成器的 `build_grid()` 与 `PALETTE`。Tools 目录由模组构建排除。

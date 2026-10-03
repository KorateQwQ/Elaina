# 集中药水像素稿

参考 `Tools/PotionBottleDesigns/output/round1/svg/ConcentrationPotion.svg` 的材质和标记，按用户要求改为**竖直试管**，手绘为 **19×38、16 色**透明背景像素图。软木塞、瓶口、平行管壁和准星纸签共用垂直中轴，底部以对称阶梯收成圆底；保留暖色软木塞、浅紫玻璃瓶口、水平绿色液面和两组刻度。受光来自左上，不使用抗锯齿或旋转斜版图片采样。

竖版已获用户确认并接入物品贴图。运行 `python Tools/PotionPixel/GenerateConcentrationPotionPixel.py` 重建：

- `Tools/PotionBottleDesigns/output/round1/pixel/ConcentrationPotion.png`：原尺寸像素图。
- 同目录 `ConcentrationPotion.pixel.json`：可编辑字符网格和调色板。
- `Tools/PotionPixel/preview/ConcentrationPotion_Study_x8.png`：8 倍最近邻预览。
- `ElainaModAlchemy/item/ExampleAssets/ConcentrationPotion_Pixel.png`：正式物品贴图；目录尺寸为 19×38。

生成器的 `ROWS` 定义连续轮廓和材质色块，`LABEL` 定义轮廓内部的纸签与准星。`ConcentrationPotion-review.png` 展示当前竖直版在深浅背景下的原尺寸、2×、4×、8×；`ConcentrationPotion-source.png` 保留此前渲染的斜向 SVG 参考，不是当前像素图。

正式物品通过 `ConcentrationPotion.EntryId = "focus"`、`AlchemyItem.Texture` 和目录的 `PixelPath` 加载此贴图。两套批量像素生成器也调用当前竖版。彩铅图与手记剪影也已通过 `Tools/PotionBottleDesigns/apply_notebook_potions.py` 同步为原 SVG 的斜向试管；竖直方向只用于物品像素图。像素预览为离线图片，未模拟游戏物品栏和光照。

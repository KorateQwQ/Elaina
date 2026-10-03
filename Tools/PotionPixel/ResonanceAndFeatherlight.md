# 共鸣与轻羽 · 已接入像素贴图

依据 `Tools/PotionBottleDesigns/output/round1/svg` 中的同名 SVG 手绘，两张均已获用户确认并接入正式物品。运行 `python Tools/PotionPixel/GenerateResonanceAndFeatherlightPixels.py` 重建正式 PNG、设计副本与可编辑像素网格。

- **共鸣药水**：31×39、16 色。圆肚、粉紫药液、中心光点、对称双层声波，颈部保留紫色丝带和金色挂饰。瓶身用逐行整数边界构成，内部色块与高光独立绘制。
- **轻羽药水**：19×38、17 色。沿用用户确认的集中药水竖直管身，改为浅蓝药液；内部是一枚象牙白羽毛，带斜向羽轴和两段短羽枝。

输出到 `Tools/PotionBottleDesigns/output/round1/pixel/`：`ResonancePotion.png`、`FeatherlightPotion.png`，各自的 `.pixel.json` 可编辑字符网格。`-review.png` 是深浅背景、原尺寸和整数倍放大对照，`-source.png` 是原始 SVG 参考渲染。

同时更新 `ElainaModAlchemy/item/ExampleAssets/ResonancePotion_Pixel.png` 和 `FeatherlightPotion_Pixel.png`，经物品 `EntryId`、`AlchemyItem.Texture` 和目录 `PixelPath` 加载；目录尺寸分别为 31×39、19×38。两套批量像素生成器也调用此处的新版本。彩铅手记图和未研究剪影也已通过 `Tools/PotionBottleDesigns/apply_notebook_potions.py` 更新为共鸣圆瓶、轻羽斜向试管；轻羽物品像素图保持竖直。

8 倍预览存于 `Tools/PotionPixel/preview/*_Study_x8.png`，两张合览为 `Resonance_Featherlight-review.png`。原图 RGBA，所有可见像素不透明；不使用旋转采样、模糊或抗锯齿。离线贴图预览不等于游戏截图。

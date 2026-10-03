# 星力三角瓶与月露圆瓶

星力药水采用 `Tools/PotionBottleDesigns/output/round1/svg/StarPowerPotion.svg` 的三角烧瓶；月露按手记中的**月露合剂 MoonDewElixir**处理，参考其已确认的圆瓶 SVG，不涉及材料栏的月露 MoonDew。

- 星力：31×36、15 色；平行瓶颈、对称三角瓶身、深蓝药液、一大一小金色星芒、侧丝带与挂饰。
- 月露合剂：33×40、15 色；圆肚、紫色药液、双环粉色蝴蝶结与飘带、星点和气泡。

运行 `python Tools/PotionPixel/GenerateStarAndMoonPotionPixels.py` 重建正式 `ExampleAssets/*_Pixel.png`、`Tools/PotionBottleDesigns/output/round1/pixel/` 中的同名 PNG 与可编辑 `.pixel.json`，以及 `Tools/PotionPixel/preview/*_Pixel_x8.png`。绘图为整数像素簇，无 SVG 缩小采样、半透明边缘或抗锯齿。

目录 `AlchemyCatalog` 的尺寸与新 PNG 一致；月露合剂的 `SetDefaults` 改为读取目录尺寸，饮用和魔力补给逻辑保留。两套批量像素生成器已指向新稿，且保留此前确认的嗜血菱形稿，避免重新生成时退回旧瓶型。

星力手记图运行 `python Tools/PotionBottleDesigns/apply_starpower.py` 重建：把 round1 的三角 SVG 和彩铅 SVG 接入 ExampleAssets，渲染 100×100 原始彩铅 PNG，再使用公共预乘函数生成 UI 副本和同 Alpha 的未研究剪影。`python Tools/AlchemyPreview/prepare_pencil.py --check` 检查所有彩铅副本。月露合剂的原始 SVG、彩铅 UI 与剪影保留已有圆瓶。

`StarPowerPotion-review.png`、`MoonDewElixir-review.png` 是深浅背景的原尺寸与倍数放大检查。`Tools/PotionPixel/preview/StarPower_MoonDewElixir-review.png` 是两张合览。FNA UI 检查使用 `Tools/AlchemyPreview/run.ps1` 的 `level-max`、`new` 场景，在 720p、100% 和 125% 缩放下检查三角彩铅图及剪影；这些是离线预览，不是游戏截图。

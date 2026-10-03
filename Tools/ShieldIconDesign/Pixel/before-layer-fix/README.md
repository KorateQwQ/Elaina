# 伊蕾娜魔法护盾 · 像素图标 V2

- `MagicBarrierSkill.png`：56×56，11 个可见 RGBA 色阶，透明背景；边缘在最终像素网格上绘制，无抗锯齿或模糊。主体有效包围盒为 `(5, 3)–(51, 52)`，右、下坐标不含。
- `MagicBarrierSkill_Mask.png`：56×56，青蓝辉光层，与主体逐像素对齐。仅对亮边与星光产生柔和辉光，不对中心薄膜整块发光。
- `Preview.png`：主体、遮罩、合成示意的 4 倍最近邻放大，深浅背景原尺寸检查，以及现有技能图标对照。
- `Comparison.png`：第一版与第二版的 4 倍最近邻放大、深浅背景原尺寸对照。
- `v1/`：第一版的贴图、遮罩、预览与生成脚本快照，供比较或恢复。
- `generate.py`：可编辑的整数坐标、调色板及独立辉光生成器；只依赖 Pillow，运行 `python Tools/ShieldIconDesign/Pixel/generate.py` 可重建输出。
- `metrics.json`：导出尺寸、可见颜色数、alpha 层级及包围盒。

第二版直接基于像素图优化，不再参考或读取 SVG。本次按用户提供的像素轮廓调整右上缺口：上方小凹口与下方较宽的碗形凹口贴近排列，交接处保留尖角，下方弧线向右回升。擦除范围分别为 `(33, 7, 43, 17)` 和 `(35, 15, 51, 29)`。中间最大的星芒改为 7×9 的实心菱形星，四端收尖，旁边两颗小十字星衬托。辉光遮罩同步生成。

系列参考来自 `ElainaModSkills/Skills` 下的 WaterBallSkill、FireBurstSkill 和 MagicMissileSkill：沿用 56×56 画布、明亮像素主体及同名 `_Mask.png` 分层约定。遮罩允许连续 alpha 和柔光，这是与清晰像素主体分开的光效层。

两张交付贴图的 RGB 均已预乘 alpha，与项目本地 tML rawimg / AlphaBlend 资源处理方式一致（例如 FireBurstSkill 及相关 UI 资源）。不要再次预乘。普通图片软件可能将这些半透明像素显示得稍暗；预览图采用正常透明度的源图合成，展示预期色彩。辉光示意采用“遮罩在下、主体在上”的普通 alpha 合成，实际效果还取决于游戏的混合方式与绘制强度。

当前只交付设计资产，没有替换 `ElainaModSkills/Skills/MagicBarrier/MagicBarrierSkill.png`，没有改运行时引用。`ElainaSkill.ExtraIcon` 按 `SkillTexturePath + "_Mask"` 查找光效层；目前 `PostDrawSkillIcon` 中绘制 ExtraIcon 的代码被注释，后续接入时需决定光效绘制位置。此处预览为离线合成，不是游戏截图。

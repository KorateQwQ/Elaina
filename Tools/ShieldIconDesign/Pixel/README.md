# 伊蕾娜魔法护盾 · 像素图标

所有贴图均为 56×56。本次保持缺口、星芒和像素位置，按项目要求修正图标的组成与导出流程。

## 合成流程

纯白像素原图（RGB 固定 255，仅 alpha 表达明暗）＋青蓝辉光层 → 合成 Skill → 最后预乘 alpha。

生成器使用普通 source-over：辉光在下，纯白原图在上。两层输入都为 straight alpha。合成后仅最终游戏贴图预乘一次，不要将最终贴图再与遮罩叠加。

| 文件 | 用途 | alpha 格式 |
| --- | --- | --- |
| `MagicBarrierSkill_Original.png` | 纯白像素原图，11 个非零 alpha 层级；透明背景，不模糊、不抗锯齿 | Straight |
| `MagicBarrierSkill_Mask.png` | 独立的青蓝辉光层，与原图对齐；柔光只在这一层生成 | Straight |
| `MagicBarrierSkill_Composite.png` | 原图与辉光合成后的正常 PNG，便于编辑与普通图片软件查看 | Straight |
| `MagicBarrierSkill.png` | 最终可供项目加载的技能图标，已含主体和辉光 | Premultiplied |

最终图标 RGB 按 `RGB × alpha / 255` 编码，alpha 保持不变，与项目本地 tML rawimg / AlphaBlend 管线一致。普通图片软件按 straight alpha 显示时可能将它显示得偏暗，因此查看效果请使用 `MagicBarrierSkill_Composite.png` 或 `Preview.png`。原图和辉光图是编辑、合成用的源层，不是两个需要在游戏里继续叠加的预乘贴图。

## 造型与预览

不再参考或读取 SVG。右上沿用用户给出的像素缺口轮廓：小凹口与较宽的下方凹口相接，交接处保留尖角，下方圆弧向右回升。擦除范围为 `(33, 7, 43, 17)` 与 `(35, 15, 51, 29)`。中间为 7×9 的菱形星芒，旁边两颗小十字星衬托。

- `Preview.png`：白色原图、独立辉光、最终合成图的 4 倍最近邻预览；另含深浅背景的原尺寸效果和项目图标对照。
- `Comparison.png`：此前只导出主体的结果与当前含辉光的最终图标对照。
- `generate.py`：可编辑的整数坐标与 alpha 调色板，只依赖 Pillow。执行 `python Tools/ShieldIconDesign/Pixel/generate.py` 重建全部产物。
- `metrics.json`：尺寸、可见 RGBA 组合数、alpha 层级、包围盒、编码方式。
- `v1/`、`before-reference/`、`before-layer-fix/`：历史快照，保留当时的脚本和文件语义，不用作当前交付。

已核对导出的原图所有 RGB 均为纯白，重新读取两份源层能精确重建合成 PNG，且最终图标与该合成结果预乘一次的值完全一致。

当前产物在设计目录中，没有替换游戏现有的 `ElainaModSkills/Skills/MagicBarrier/MagicBarrierSkill.png`。预览是离线合成，不是游戏截图。

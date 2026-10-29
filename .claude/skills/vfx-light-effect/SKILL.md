---
name: vfx-light-effect
description: 伊蕾娜模组的光效/命中爆点/粒子特效制作经验：贴图预乘、AlphaBlend 覆盖度混合、单层纯色 + HDR 靠 Bloom 出光、KLBasicDust 承载与同层 Bloom、离线 FNA 预览对照流程。制作或调整技能命中特效、爆点、火花、发光粒子、Bloom 光感时使用。
---

# 光效制作经验

范例实现：魔法飞弹命中特效（`ElainaModSkills/Skills/MagicMissile/MagicMissileHitVisuals.cs`、`MagicMissileHitDust.cs`，预览 `Tools/MagicMissileHitPreview/`）。该特效的具体参数只记在它自己的 README 里，不要照搬到其他特效。

## 流程

- 用户给参考图时，先按层拆解说清楚各层的作用，再动手。
- 先离线预览，用户确认后才写正式代码。预览直接编译正式的绘制文件，保证预览即游戏画面。
- 预览同时出白天（亮背景）和夜晚，并给 2 倍局部；只看缩小的整图容易误判"纯色块"或"白团"。
- 每轮先自己看图找问题；某一层出问题时单独渲染各层来定位，不要凭猜测调参。
- 优先用现成贴图（KL 素材很多）。确需新贴图时按 `Tools/FixedTextureBake` 流程烘焙，不在运行时生成。

## 贴图与预乘

- 用户的贴图基本都已预乘（KL `ShockDust`/`ShockBlackDust` 例外）。不确定时逐像素检查"RGB 是否 ≤ α"。
- 打包后 PNG 转 rawimg，游戏加载时不再预乘。离线预览读 PNG 只把 α=0 的像素清零，不要 `Color.FromNonPremultiplied`，否则比游戏暗、误导调参。
- 带大片低透明柔光边的贴图（如 `ShotLineSPA`）多份叠加会糊成白雾：用源矩形只取亮的部分，光晕交给 Bloom。

## 混合

- 用 AlphaBlend，顶点色 alpha 作为覆盖度：叠加亮色的同时压掉一部分背景，白天不会被叠成纯白。实心形状覆盖度高，细线居中，小光点低。
- 不要以 alpha 0 纯叠加为主（白天会叠成纯白）。
- 大面积柔光（闪光、光环）覆盖度用 0，否则亮背景上压出灰圈。
- 淡出系数要同时乘到亮度和覆盖度上；只降亮度会剩下遮挡，在亮背景上发灰。
- 用户否决过"两图层暗底"和逐粒子暗底，除非用户要求不再提。
- `BlendState.Additive` 在 KL Bloom 捕获中会变成 `SourceAlpha, One`，对预乘贴图等于 α 乘两次、边缘变暗变窄。

## 光感

- 光感靠"一层纯色 + 明显超过阈值的 HDR 倍率"让 Bloom 晕出，中心自然过曝泛白、边缘保留颜色。HDR 用 KL `ReColorEffect` 设置，Immediate 模式下每次绘制前单独设置。
- 不要为了渐变叠白芯/浅色等多层。
- 倍率只略高于阈值时几乎不出 Bloom，实心剪影会成没有光感的纯色块；倍率过高或多个实心形状叠在同一点会冲成白团。

## 承载与节奏

- 一次完整特效用一颗 `KLBasicDust` 承载：生命周期独立于弹幕，在 `SubmitBloomDrawRequest` 里与弹幕同层请求 Bloom（按 order 排前后）。不要用 `KLBasicDust.SpawnDust`，其立即 Update 会让首帧跳过闪光。
- 触发频繁的普通攻击特效要小、短、散得开（飘散粒子初速要够，否则挤在命中点）；分量和持续时间留给低频的重击。

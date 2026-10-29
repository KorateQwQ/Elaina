# 魔法飞弹命中特效预览

对比改版前 `MagicMissile.OnKill` 的命中粒子与正式命中特效。正式特效的构成与绘制在 `ElainaModSkills/Skills/MagicMissile/MagicMissileHitVisuals.cs`，游戏中的 `MagicMissileHitDust` 与本预览共用这份代码。Bloom 管线从 KL 源码编译（同 `Tools/FireTornadoLookPreview`），靶子用原版僵尸 `NPC_3.xnb`。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/MagicMissileHitPreview/run.ps1
```

输出在 `.vissandbox/magic-missile-hit/out`：

- `day-compare.png` / `night-compare.png`：三行依次是旧版、普通命中、第五击花印爆发，列为命中后第 1/2/3/5/8/12/20/35/55 帧和连发第 12 帧。连发每 3 帧一发；花印行为 4 发普通命中加第 5 发爆发。
- `day-hit.gif` / `night-hit.gif`：单发命中（80 帧），`*-burst.gif`：连发（60 帧）。泳道并排，每帧 20 ms，接近游戏速度。
- `day-hit-zoom.png` / `night-hit-zoom.png`：普通命中第 2/3/5/8 帧的 2 倍局部，接近游戏常用缩放，便于对照游戏截图。
- `frames/`：全宽逐帧原图，`layout.txt` 记录各泳道命中点。
- `day-trail.gif` / `night-trail.gif`：正式拖尾与命中组合，三列依次为直线普通命中、弯曲普通命中、花印中心偏离接触点 45 px。输入故意让上一帧拖尾头部距离接触点 30 px，调用正式 `FinishPath` 接上末段。
- `*-trail-sheet.png`：上述效果第 1/4/8/12/16/21/27 帧，观察整条变细、裂开和淡出；`*-trail-only.png` 为第 15 帧移除爆炸后的拖尾。
- `*-width-1x.png` / `*-width-2x.png`：只画拖尾的真实矩阵缩放检查，左右为两个噪声种子，上下依次为飞行中、消亡首帧、消亡进度 40%/60%/80%/97%；无爆炸遮挡，用于检查首帧宽度连续与最后的细线。
- `*-trail-zoom.gif`：直线普通命中的 2 倍图片放大，便于查看细线；这是输出图缩放，不是游戏相机缩放测试。

## 构成

- 旧版：按改版前 `OnKill` 参数复刻 `ShockBlackDust`/`BurstPoint`/`ShockDust`/`LineSparkle`，含 `SpawnDustsCircle` 的随机顺序、生成时的额外 Update 和各粒子的混合模式；与当时游戏一致，不进入 Bloom。
- 多刺爆点：KL `BurstDust`（2×4 帧，128×64，尖刺朝 +X，从伸出、完整多刺到碎裂），普通命中朝穿透方向，主爆点加一侧镜像的副爆点；第五击为一圈爆点，根部离开中心 18 像素以免叠成实心。只画一层纯粉色，倍率 1.6～1.8 明显超过阈值，由 Bloom 晕出光感，中心过曝泛白、尖端保留粉色（参考特效的做法）。倍率约 1.1 时几乎不触发 Bloom，会变成没有光感的纯色块；不要再叠白芯等多层渐变。
- 细长火花：`ShotLineSPA` 针线头部快速射出、尾部随后收拢，另有短火花。只取贴图中间 17 像素的亮线带：上下的大片低透明柔光叠多了会糊成一团。
- 星点飘曳：顺速度拉长的 `GlowDot`，减速后叠加缓慢正弦漂移与轻微上浮。普通命中触发频繁，星点细小（4～7 像素，少数 8～10）、7 颗、持续 20～38 帧，初速 3.5～7 散开以免挤在命中点；第五击 14 颗、5～8 像素（少数 8～11）、持续 28～48 帧、初速 4～8。
- 第五击另有 164 像素溅射半径的 `BurstCircle` 光环与 `Flo_1~5` 花瓣散开。
- 不需要新贴图，用到的贴图均已预乘。

## 混合

全部用 AlphaBlend，顶点色 alpha 为覆盖度：叠加亮色的同时压掉一部分背景，白天不至于被叠成纯白。爆点 200、细线/火花 120、星点 100、小柔光 60、花瓣 150；闪光与光环是大面积柔光，用 0，否则亮背景上会压出灰圈。淡出系数同时作用于亮度与覆盖度，否则颜色淡出后只剩遮挡、发灰。亮度经 ReColor HDR 进入 `Projectiles.After` 层 Bloom（强度 1、3 次迭代，与 `BloomConfig` 默认相同）。

曾试过两图层暗底（先以颜色/透明度取最大值的混合画暗底剪影，再纯叠加亮层），白天保色更好，但暗影为描边而放大后显得比粒子大一圈，未采用。

## 贴图加载

打包后的 tModLoader 会把 PNG 转为 rawimg（`ImageIO.ToRaw`），`RawImgReader` 原样上传、不预乘，只把 alpha 为 0 的像素清零。本预览的 `Png()` 按此加载。早期版本曾在加载时再乘一次 alpha，比游戏偏暗。

场景只是色块，不模拟光照、实际碰撞、完整飞弹头部、镜头缩放和其他图层，最终效果以游戏内为准。新增的拖尾场景直接链接正式 `MagicMissileTrailVisuals`，并使用 KL `VertexDraw.xnb`、`LightTrail.png` 和同一 Bloom 管线；普通命中和花印爆发直接链接正式 `MagicMissileHitVisuals`。

## 拖尾消亡与断口回归

`MagicMissile.OnKill` 用接触点（撞墙）或当前 `Projectile.Center` 补齐最后一次 `DrawBehind` 采样，并刷新同一个 TrailManager handle。两次提交都关闭 `shrinkPathOnExpire`：TrailManager 的线性缩短会在半程就砍掉一半长度，而截面消融要到后段才完成，看起来像拖尾“变短而消失”。改由 `MagicMissileTrailVisuals.BuildGeometry` 按 `1 − LengthShrink × age²`（默认 0.45）只截掉尾端几何：保留部分的顶点与 U 完全不动。曾先截路径再按新点数重算 U，导致每次截断越过采样点时噪声图案整体跳动；`RunNoise` 检查缩短时保留几何与 U 不变。Shader 的 `TrailAge = 0.12 + 1.25 × age`，收缩最快的部位约 40% 寿命归零、最慢的约 70%，此时长度仍保留约八成，消失由断续消融主导。路径长度上限仍为 320 px。

预览曾在加载后立即释放 PerlinX 纹理，绘制时噪声为空、拖尾只会整体均匀收细，与游戏不符；现在与游戏一样保持纹理存活。

消亡由 PerlinX 驱动局部截面收缩，不预设五个断点，也不叠加亮度淡出。正式运行时只绑定已经打包的 `PerlinX` 纹理，不调用 `Texture2D.GetData`，不生成新贴图；专用 `MagicMissileTrail.fx` 在像素阶段采样噪声。每发固定一个随机 UV 偏移，沿轨迹 U 坐标取得连续的局部收缩倍率。局部宽度由 Shader 计算为 `原始宽度 × pow(max(0, 1 − age × rate), exponent)`，快速收缩的部位先归零，两侧形成连续尖端；较慢部位保留长短不一的细线。颜色、HDR、alpha 和 UV 不随消亡进度变化，Bloom 由发光截面减少而自然减弱。

飞行和消亡使用同一绘制器。正常飞行按 KL 原有逐点法线、6→2 半宽、颜色、UV 和三角形顺序绘制，并明确传入 `GameViewMatrix.TransformationMatrix.M11` 作为旧有线宽缩放。消亡后沿原网格两侧边插值细分至约 1.5 px，首帧不改变外轮廓，随后由 `MagicMissileTrail.fx` 只收缩横截面。顶点数组复用。持续 22 个游戏更新，暂停沿用 TrailManager 行为，不在绘制回调累积时间。

撞墙时原版在 `OnTileCollide` 之后才按“沿墙滑动”的修正速度移动再 `Kill`，消亡位置会沿墙偏出一截，末段拐成 L 形。`OnTileCollide` 用 `MagicMissileTrailVisuals.WallContact` 按被挡住轴的行进比例沿入射方向找回接触点，拖尾终点、命中特效位置与角度都用它（撞墙后的速度可能已归零，不能再取 `velocity.ToRotation()`）。旧版曾在 `FinishPath` 里“拉直头部”，但取的是前进方向，把头部几段放到了接触点前方，形成穿出弹幕的长直线，已删除。

命中爆点比拖尾短（约 13 帧对 22 帧），之后拖尾端口裸露。消亡期间 `DrawHead` 在路径头部画 KL `T_Vfx_Stamp_HitSword_22`（2×2 图集，每发按种子固定一帧和角度），由 `MagicMissileTrail.fx` 的 `Head` 通道消融：消融场 = 半径（按帧内可见范围约 0.75 归一化）与 KL `Noise/5`（胞状噪声，缩放 1.8、反相）各半，外圈先被蚀去、暗块所在的碎块最后消失，爆点被切成分离的圆润碎块，前沿提亮成粉色。曾对照 PerlinX 0.8～3.2（颗粒或糊状）、windnoise 居中/平铺（收成光球或撕成长条），最终选此。头部在拖尾寿命的前 72% 内匀速消失（与截面收缩最慢的部位同时归零）：开始画到 1.4 倍以盖过命中爆点，尺寸、HDR（1.9→1.5）与消融同时推进。消融值与可见亮度的关系极不均匀（低值只蚀去外圈淡晕，且随噪声差异很大），所以按 `head_curve.py` 复刻 Head 通道测得的反查表 `DissolveByVisibleLoss` 取消融值，使尺寸² × HDR × 剩余亮度随时间线性下降（脚本末尾自检，偏差约 2%）。脚本默认参数即正式设置；改贴图、噪声、缩放、反相或 Head 通道的消融场后需重跑 `python -I Tools/MagicMissileHitPreview/head_curve.py ../KL` 并更新该表。飞行中由飞弹本体遮挡，不画。头部与拖尾一样用 `DrawUserPrimitives` 画自带顶点着色器的四边形，不经 SpriteBatch：`Head` 通道最初只有像素着色器，预览中正常，游戏中紧接拖尾的自定义顶点着色器绘制时沿用了不确定的顶点着色器，头部完全不显示且不报错。`TrailChecks` 会检查 `pass Head` 绑定了顶点着色器。改 FX 后运行 `compile.ps1` 重新生成正式 XNB。`*-head-dissolve.png` 为无命中特效遮挡的 3 倍消融序列。`trail-option-shrink.gif` / `trail-option-noshrink.gif` 对照正式的尾端缩短 45%² 与完全不缩短（`LengthShrink` = 0）两种消亡方案，上白天下夜晚、按游戏速度；`*-slow.gif` 为直线泳道 0.4 倍速 2 倍放大。

普通命中补 9 帧的小反向爆点与 6 帧中心柔光；第五击在真实接触位置单独补这一小组效果，避免大型敌人中心与消亡位置错开时又露出断口。全部复用已有正式贴图，没有运行时生成固定纹理。

`run.ps1` 运行 `TrailChecks.cs.txt`：检查末段连接、原快照不变、320 px 上限、重复点、出生即消亡边界及生命周期接线；加载真实 PerlinX 后验证 16 个种子的局部宽度单调收缩并最终归零，颜色/HDR/alpha/UV 始终不变、中心线不动，以及 1×/2× 下飞行宽度与原 KL 算法一致。渲染检查也要求 Shader 的 `shouldClip` 保持关闭。

源码编译（不打包、不要求退出正在运行的游戏）：

```powershell
dotnet build 伊蕾娜.csproj --no-restore -t:Compile -p:BuildProjectReferences=false -v:q
```

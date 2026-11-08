# 魔法飞弹命中特效预览

预览正式的连续追尾收细版及命中特效。正式命中特效的构成与绘制在 `ElainaModSkills/Skills/MagicMissile/MagicMissileHitVisuals.cs`，游戏中的 `MagicMissileHitDust` 与本预览共用这份代码。Bloom 管线从 KL 源码编译（同 `Tools/FireTornadoLookPreview`），靶子用原版僵尸 `NPC_3.xnb`。

## 当前默认：连续追尾收细

整个截面（包括亮芯）从原宽度线性压缩到零，保持发光材质。

极细斜线使用基于屏幕导数的最小采样宽度，并按实际宽度 / 采样宽度补偿覆盖率，避免最后几帧因亮芯窄于一像素而被采成虚线。

死亡时 `BeginRelease` 保存完整轨迹与实际入射速度（换算为每游戏 tick 的位移，包含 extraUpdates）。撞墙使用 `OnTileCollide` 的旧速度，避免墙体把速度改成滑动或零；其他死亡使用最后一次 AI 的飞行速度。生命周期取 `ceil(轨迹长度 / 速度)`，尾端每 tick 沿原路径向死亡点前进该速度，材质坐标也按相同弧长前移，不只是裁掉尾端。头部固定在死亡位置，保留曲线，不向墙后延伸。零速弹幕保持原路径、在 22 tick 内只收细。由 TrailManager 的游戏 tick 推进，重复绘制不累计推进，游戏暂停仍沿用 TrailManager 规则。

输出包含 30 px/tick 主预览、12 / 65 px/tick 速度检查、宽度图和逐帧图。场景覆盖直线、弯曲轨迹、普通命中和花印命中；这是独立 FNA 预览，不是游戏实拍。`RunFollowing` 检查速度与到达时刻、弯曲/极短/重复点路径、零速、不越过死亡点、材质弧长推进和重复绘制稳定；GPU 检查收尾时只有一条连续亮线。

旧断裂方案的源码与图片快照仅存放于 `Variants/Fractured`，不参与游戏和预览构建。设置 `$env:MISSILE_LEGACY_HITS = '1'` 可运行下文的旧全量命中特效比较。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/MagicMissileHitPreview/run.ps1
```

输出在 `.vissandbox/magic-missile-hit/out`。以下列表对应 `MISSILE_LEGACY_HITS=1` 的全量命中特效比较：

- `day-compare.png` / `night-compare.png`：三行依次是旧版、普通命中、第五击花印爆发，列为命中后第 1/2/3/5/8/12/20/35/55 帧和连发第 12 帧。连发每 3 帧一发；花印行为 4 发普通命中加第 5 发爆发。
- `day-hit.gif` / `night-hit.gif`：单发命中（80 帧），`*-burst.gif`：连发（60 帧）。泳道并排，每帧 20 ms，接近游戏速度。
- `day-hit-zoom.png` / `night-hit-zoom.png`：普通命中第 2/3/5/8 帧的 2 倍局部，接近游戏常用缩放，便于对照游戏截图。
- `frames/`：全宽逐帧原图，`layout.txt` 记录各泳道命中点。
- `day-trail.gif` / `night-trail.gif`：正式拖尾与命中组合，三列依次为直线普通命中、弯曲普通命中、花印中心偏离接触点 45 px。输入故意让上一帧拖尾头部距离接触点 30 px，调用正式 `FinishPath` 接上末段。
- `*-trail-sheet.png`：上述效果第 1/4/8/12/16/21/27 帧，观察拖尾连续收细与淡出；`*-trail-only.png` 为第 15 帧移除爆炸后的拖尾。
- `*-width-1x.png` / `*-width-2x.png`：只画拖尾的真实矩阵缩放检查，左右为两个随机种子，上下为飞行与消亡阶段；无爆炸遮挡，用于检查连续收细与最后的细线。
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

## 拖尾消亡回归

`MagicMissile.OnKill` 用接触点（撞墙）或当前 `Projectile.Center` 补齐最后一次 `DrawBehind` 采样，并刷新同一个 TrailManager handle。提交时关闭 `shrinkPathOnExpire`，由拖尾沿原路径追至死亡点；路径长度上限为 320 px。

拖尾整体按 `1 - age` 连续收细，横向纹理坐标同步压缩，因此亮芯也随截面变窄。颜色、HDR、alpha 和轴向 UV 不随消亡淡出；Bloom 随发光截面变小自然减弱。

飞行和消亡使用同一绘制器。正常飞行按 KL 原有逐点法线、6→2 半宽、颜色、UV 和三角形顺序绘制，并明确传入 `GameViewMatrix.TransformationMatrix.M11` 作为旧有线宽缩放。消亡后沿原网格两侧边插值细分至约 1.5 px，首帧不改变外轮廓，随后由 `MagicMissileTrail.fx` 只收缩横截面。顶点数组复用。持续 22 个游戏更新，暂停沿用 TrailManager 行为，不在绘制回调累积时间。

撞墙时原版在 `OnTileCollide` 之后才按“沿墙滑动”的修正速度移动再 `Kill`，消亡位置会沿墙偏出一截，末段拐成 L 形。`OnTileCollide` 用 `MagicMissileTrailVisuals.WallContact` 按被挡住轴的行进比例沿入射方向找回接触点，拖尾终点、命中特效位置与角度都用它（撞墙后的速度可能已归零，不能再取 `velocity.ToRotation()`）。旧版曾在 `FinishPath` 里“拉直头部”，但取的是前进方向，把头部几段放到了接触点前方，形成穿出弹幕的长直线，已删除。

命中爆点比拖尾短，之后拖尾端口裸露。消亡期间 `DrawHead` 在路径头部画 KL `T_Vfx_Stamp_HitSword_22`（2×2 图集，每发按种子固定一帧和角度），由 `MagicMissileTrail.fx` 的 `Head` 通道消融：消融场由半径项和 KL `Noise/5`（胞状噪声，缩放 1.8、反相）各占一半，外圈先消失，前沿提亮成粉色。头部开始时放大以盖过命中爆点，尺寸、HDR 与消融同步推进；反查表 `DissolveByVisibleLoss` 由 `head_curve.py` 按可见亮度测得，使尺寸² × HDR × 剩余亮度随时间近似线性下降。改贴图、噪声、缩放、反相或 Head 通道消融场后，需重跑脚本并更新反查表。飞行中由飞弹本体遮挡头部。拖尾头部使用 `DrawUserPrimitives` 绘制自带顶点着色器的四边形；`TrailChecks` 会检查 `Head` pass 绑定了顶点着色器。改 FX 后运行 `compile.ps1` 重新生成正式 XNB。`*-head-dissolve.png` 为无命中特效遮挡的消融序列；`*-slow.gif` 为直线泳道 0.4 倍速 2 倍放大。

普通命中补 9 帧的小反向爆点与 6 帧中心柔光；第五击在真实接触位置单独补这一小组效果，避免大型敌人中心与消亡位置错开时又露出断口。全部复用已有正式贴图，没有运行时生成固定纹理。

`run.ps1` 运行 `TrailChecks.cs.txt`：检查末段连接、原快照不变、320 px 上限、重复点、出生即消亡边界及生命周期接线；几何检查颜色/HDR/alpha/UV 始终不变、中心线不动，以及 1×/2× 下飞行宽度与原 KL 算法一致。跟随检查覆盖正常速度、零速、弯曲路径、材质弧长推进和重复绘制稳定。

源码编译（不打包、不要求退出正在运行的游戏）：

```powershell
dotnet build 伊蕾娜.csproj --no-restore -t:Compile -p:BuildProjectReferences=false -v:q
```

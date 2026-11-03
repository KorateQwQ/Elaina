# 火焰射线 FNA 离线预览

当前预览入口与游戏一致，`DrawVisual2` 调用 `FireLaserBeamVisual`：保留 `Eff_Trail_1679` / `Eff_Trail_2014` 两层持续射线及 `wind3` 火焰头。下文的法球、缎带和圆柱火焰说明对应保留的旧 `DrawVisual`，当前入口不调用它们。

入场在 24 个游戏 tick 内完成：先由零宽度展开，第 10 tick 达到 2.2 倍，保持 4 tick 后回落到原宽度。入场独立参数为 EntranceGrowTicks、EntrancePeakTick、EntrancePeakHoldTicks、EntrancePeakWidth。结束时伤害弹幕立即 Kill，通过 KL TrailManager 留下 42 tick 的世界坐标快照，复用 `Projectiles.After` Bloom；`FireLaserRelease.fx` 使用正式 `PerlinX`，按不同位置的噪声先收细再断成残余线段。噪声坐标固定，纹理仅按原速度的 30% 继续流动，头部按独立的 ReleaseHeadShrinkTicks 收缩。`LineSparkle` 从中心线的独立随机位置沿射线喷射方向前进，随机向两侧张开；少量局部聚集形成疏密变化，不绑定已结束的弹幕。游戏暂停时残影按 TrailManager 的游戏时间暂停，世界卸载时一并清理。

调参入口：`FireLaserBeamVisual.OpeningWidth`（入场曲线）、`ReleaseTicks` / `ReleaseShrink`（退场）、`ReleaseSparks`（散逸粒子），持续态贴图、颜色及宽度在 `Draw` 中。

退场细调集中在 `FireLaserBeamVisual` 文件顶部，修改这些 C# 参数即可，无需再次修改 FX：

- `ReleaseLineShrinkTicks`：线体收缩和断裂的时长，越小越快。
- `ReleaseLineShrinkPower = 2`：线体按 `1 - (1 - t)^power` 收缩，`t` 是线体时长内的归一化进度。1 为匀速，2 为先快后慢，越大前段收得越快；默认半程完成 75% 的基础收缩，局部噪声仍可让部分位置提前断开。
- `ReleaseHeadShrinkTicks = 33`：头部收缩时长；保持大于线体时长，使线体先消失，头部稍晚收完遮住端口。两者均为游戏 tick（60 tick = 1 秒），`ReleaseTicks = 42` 是残影托管的总时长。
- `ReleaseNoisePeriod = 1300`：长射线按世界像素计算噪声周期；短射线至少覆盖一个完整周期，避免近距离退场只收细、不形成多段断裂。它是噪声周期，不是每条残线的精确长度。
- 默认预览会用正式 `PerlinX` 的双线性采样检查 60、120、180、450、940、1500px 射线：退场至少两帧存在三段宽度尚未归零、长度至少两像素的残线，并导出 `exit-distance-*` 供检查实际 Bloom 与头部叠加后的效果。
- `ReleaseMaxShrinkRate = 1.25`：越接近 1，各处越同步收细、越晚断开；越大越早产生缺口。整体收缩速度请调 `ReleaseLineShrinkTicks`。
- `ReleaseSparkSpacing = 24`：平均每多少世界像素生成一颗；越小越多，总数限制为 6–80。位置独立随机，35% 在随机簇附近聚集，两侧不再成对。
- `ReleaseSparkLengthScale = (0.4, 0.9)`：亮线长度倍率范围，越大越长；寿命随机为 28–46 tick。
- `ReleaseSparkForwardSpeed = (3.5, 6)`：沿射线前进的初速度范围。
- `ReleaseSparkSideSpeed = (0.6, 1.6)`：向两侧散开的初速度范围；相对于前进速度越大，散开角度越大。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/FireLaserPreview/run.ps1 -Dotnet C:/Users/PC/.dotnet/dotnet.exe
```

脚本先用 `compile.ps1` 调用同级 `KL/Effects/XNBCompiler.exe`，把正式源 `Effects/Content/FireLaserRibbon.fx`、`FireLaserBody.fx`、`FireLaserRelease.fx` 编译并写回同名 `.xnb`（`-SkipShader` 跳过）。随后从源码提取并一起编译：

- `FireLaser.cs` 的 `DrawVisual2`（游戏中 Bloom 请求的回调），以及保留的 `DrawVisual`、`DrawRibbons`、`DrawLaser`、`DrawMagicBall`、`DrawMuzzleFlare`；
- KL 的 `LayerDrawRequestSystem`、Bloom 捕获/合成、`TrailEffect`、`CommonMagicEffect`、`VertexDrawEffect`、`QuickConePoints`、`DrawInWorld`；
- 正式的 `FireLaserBeamVisual.cs`、`FireLaserVisuals.cs`、`FireLaserRibbonRenderer.cs`、`FireLaserBodyRenderer.cs`，以及 KL `LineSparkle` 的实际绘制与 Bloom 请求。粒子位置按 `.92` 阻尼和生成时额外一次 Update 回放，不模拟真实 Dust 池。

默认额外导出 `entry-*` / `exit-*`，检查早松手、爆发峰值和稳定态转退场的逐像素连续性；包含短射线、左向和白天退场。完整入退场动图：

```powershell
$env:FIRE_TRANSITIONS = '1'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/FireLaserPreview/run.ps1 -SkipShader
Remove-Item Env:FIRE_TRANSITIONS
python Tools/FireLaserPreview/compose_transitions.py
```

输出 `fire-laser-transitions.gif`、`entry-sheet.png`、`exit-sheet.png`。动图包含 0.4 秒入场、短暂保持和 0.7 秒退场，是 FNA 离线预览，不是游戏截图。

退场光感：`FireLaserRelease.fx` 保留持续态的 HDR 颜色和贴图透明度，仅用固定噪声让局部宽度归零；不再按退场进度整体淡化 RGB / alpha。火焰头同样只收缩截面，剩余部分保持发光。默认预览额外比较第 28 tick 的 Bloom 开关画面，关闭粒子并避开头部区域，检查残余线段仍产生辉光；输出 `exit-light-check.png` 和 `exit-light-check-no-bloom.png`。

按游戏中的 `Projectiles.After` Bloom 请求绘制，没有另写模拟 Bloom。输出位于 `.vissandbox/fire-laser-preview/out`：白天/夜晚第 30/60/75 帧、杖尖 2 倍局部（白天/夜晚）、火柱中段 2 倍局部（`body-zoom2`、`body-zoom2-day`）、左向、短射线（180px）、关闭 Bloom，以及 `flow-60`～`flow-63` 连续帧。设置环境变量 `FIRE_FRAMES=1` 运行时改为输出第 60～119 帧到 `out/frames`，再运行 `python -I Tools/FireLaserPreview/make_gif.py` 合成实时速度的 `fire-laser-day.gif`。

## 绘制组成

保留原有的杖尖水波法球与旋转圆环、头部锥形火焰、后坐力风场，以及两条绕火柱旋转的缎带（一橙一黑）。缎带与水球飞行水带一样按空间深度拆成两次：转到后方的部分在法球和主体之前绘制，转到前方的部分在之后绘制；近处更宽更亮。缎带从法阵的大半径收拢到略大于火柱外壳的半径，整条射线绕 1.75 圈。橙带倍率超过 Bloom 阈值自带光晕；黑带本体不发光，用 `PerlinX` 低频少量消融，消融边以高倍率橙黄色绘制。

火柱主体（2026-10-30 重构，替换原先两层 `CommonMagicEffect` 噪声条）：`FireLaserBodyRenderer` 沿射线画一个四边形，`FireLaserBody.fx` 把它投影成横躺的圆柱，叠层照搬火焰龙卷：

- 火焰内芯：`水波` 噪声经 `FireTornadoFireRamp` 色带着色（中心提亮、侧面压暗），叠 `wind1` 顺轴火纹，再叠 `cellnoise` HDR 高亮碎片（向中心集中）。
- 暗色外壳（半径略大于内芯）：低覆盖度的 `Eff_Noise_705` 暗色条带，加高频 `PerlinX` 细碎焦黑石块；石块烧边比条带宽，碎块小也能看出亮边。暗部刻意保持细碎，不做大片覆盖层。
- 各层以不同的绕轴旋转、沿轴流动和螺旋倾斜运动；上下轮廓由 `Eff_Noise_20` 起伏成火舌状；起点从法阵内收拢燃起，末端逐渐烧尽并接入头部锥形。

杖尖爆点（`DrawMuzzleFlare`）：法阵前方一个 `glo_0281` 放射亮芯，加 7 条 `ShotLine` 以不同角度、长度、粗细穿过中心（中心沿线偏移，两端尖刺长短不一），每 3 帧按哈希重新排布一次形成闪动，持续盖住火柱从法阵伸出的起点。AlphaBlend + `ReColorEffect` HDR，顶点 alpha 作覆盖度。

KL Bloom 会把亮度超过 1 的像素整体提取（不只是超出部分），火柱只有约 50px 粗，大面积超阈值会把暗部全部晕成橙色。因此底色与火纹限制在 1 以内，发光只留给高亮碎片与烧边，烧边倍率也比龙卷低。调参入口是 `FireLaserBodyRenderer.Draw` 开头的局部变量。

## 2026-10-30 失效原因

`DrawMagicBall` 曾给 `MagicMissileEffect` 写 `clipValue2`、`Edge`、`EdgeColor`、`imageColor` 等参数（该 Effect 实际从未被应用）。魔法飞弹后来重写了这个 shader，这些参数不再存在，`Parameters[...]` 返回 null，每帧抛出 NullReferenceException，射线绘制整体中断。已删除这些写入。另外旧版在原版弹幕批次里直接绘制，HDR 颜色（×2.5～×5.5）被截断成白色且没有光晕；现改为 `LayerDrawRequestSystem.RequestBloom`。

预览只模拟固定的杖尖位置（玩家中心取杖尖后方 40px）和相机，不含角色姿势、地形碰撞截短、命中粒子与光照，最终效果以游戏内为准。

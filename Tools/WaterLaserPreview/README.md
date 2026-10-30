# 水射线 FNA 离线预览

## 清透贴图（2026-10-22）

新增独立的 `ElainaModSkills/Skills/Water/Eff_Splash_4001_Clear.png`（水花，1024×1024）和 `Eff_Splash_4002_Clear.png`（圆环泡沫，512×512）。水射线正式绘制及粒子注册已接入清透版，原 PNG 保留供其他效果使用。预览器默认直接使用正式资源路径，仅 `-ParticleVariant Original` 会切回原贴图做对比。

新版本逐像素把灰色暗部映射到浅青蓝，泡沫提亮至蓝白，降低暗部不透明度。沿用全部动画帧的位置、尺寸和透明轮廓，白色浪尖保留；没有新增辉光或修改混合方式。`make_clear_textures.py` 使用 Pillow 生成资源，并检查原文件 SHA-256、输出尺寸、透明像素分布及不透明度上限。输出和检查记录在 `Tools/WaterLaserPreview/output/clear-textures/`。

```powershell
python ./Tools/WaterLaserPreview/make_clear_textures.py
& ./Tools/WaterLaserPreview/run.ps1 -Dotnet C:/Users/PC/.dotnet/dotnet.exe -ParticleVariant Original
& ./Tools/WaterLaserPreview/run.ps1 -Dotnet C:/Users/PC/.dotnet/dotnet.exe -ParticleVariant Clear
python ./Tools/WaterLaserPreview/make_clear_textures.py --fna-comparison
```

两次 FNA 输出分别位于 `.vissandbox/water-laser-preview/out` 与 `out-clear`。`atlas-comparison.png` 展示贴图动画抽帧，`fna-comparison.png` 展示实际共享绘制代码的终点裁剪对比。已查看清透版白天第 18/60 帧、夜晚第 60 帧；两套完整预览均通过既有 Bloom、生命周期及附着断言。未进行游戏实机验证。

## 水球爆炸水花（2026-10-30）

水球术爆炸原本用 KL `WaterDust`/`WaterDust1`/`WaterDust2`：灰白剪影贴图 + 白色顶点色 + 叠加混合，画面是纯白的。现改为 `WaterBall.SpawnBurstParticles`，复用 `WaterLaserDust` 的清透水花（不含泡沫环），与水流术同材质、同层、同混合；`BubbleDust` 气泡保留并改为浅蓝着色。发射时的 `OnShoot` 水花按 `LaunchBurstScale`（0.6 倍半径）复用锥形爆炸。

预览提取正式的 `SpawnBurstParticles` 渲染 `ball-{circle|cone|launch}-*`，水球只有蓄满（半径 200）才能发射，因此只渲染这一种尺寸：第 1/6/12/20 帧白天、第 6 帧夜晚、2 倍局部，以及开关 Bloom 对比和第 30 帧清空检查。金色虚线圈只标出水球半径；锥形爆炸叠加的红框是 `Colliding` 的 T 形判定，水花按它分为窄而长的中间突刺与短而宽的两侧。KL 气泡和径向扭曲不在预览中。

动态预览：`& ./Tools/WaterLaserPreview/run.ps1 -BallFrames` 额外输出第 0–32 帧连续帧到 `out-clear/frames/`，再运行 `python ./Tools/WaterLaserPreview/make_ball_gif.py` 合成 `water-ball-burst.gif`（约实际速度，每帧 20ms）和 `water-ball-burst-slow.gif`（3 倍慢放）。GIF 只含水花粒子，不含水球本体、蓄力水流、气泡和屏幕扭曲。

## 运行与验证

运行：`& ./Tools/WaterLaserPreview/run.ps1`。默认使用项目 tModLoader.targets 对应的 `D:/Steam/steamapps/common/tModLoader`，也可传入 `-TmlPath`。

此预览工具使用 .NET 10 SDK；如果 PATH 里的 SDK 较旧，本机可运行 `& ./Tools/WaterLaserPreview/run.ps1 -Dotnet C:/Users/PC/.dotnet/dotnet.exe`。

射线纹理滚动速度为 `12.6`，是水球吸水 `2.1` 的 6 倍（每秒约 2268 个世界像素）。`flow-60.png` 至 `flow-65.png` 是连续 60 Hz 帧，用于检查高速水纹向末端移动；水球吸水速度与技能逻辑不变。

脚本默认在 `.vissandbox/water-laser-preview/out-clear` 输出 PNG。每次运行重新读取实际 `WaterLaserVisuals.cs`、`WaterLaserFlowRenderer.cs`、`WaterLaserDust.cs`，提取 WaterLaser 的粒子生成、请求注册和绘制方法，以及 KL 的粒子生命周期、分层队列、Bloom 捕获和合成代码。使用项目 PNG、本模组 `WaterChargeFlow.xnb` 和 KL 编译后的 XNB。没有另写一套模拟 Bloom。

射线与水花现共用 `Projectiles.After`：主体 order 为 `0`，水花为 `100`。预览直接使用 KL 当前的粒子提交方法，复现弹幕绘制后、合成前的收集时机；断言粒子只申请这一组，并且随后 `Dust.PreDraw` 不重复提交。停止施法后的独立水花仍按帧收集，不依赖射线存活。

`wand-*` 帧额外提取 `Action_WaterLaser` 的星芒/拖尾覆盖方法、`ElainaAction` 的默认方法与拖尾分派、完整 `ElainaTrailVisualStyle`，使用实际 KL 星芒贴图与 TrailEffect。仅适配 PlayerDrawSet/DrawData 存储和固定的杖尖运动轨迹。按本机 Main 的 `DrawProjectiles` → `DrawPlayers_AfterProjectiles` 顺序，在射线/水花之后绘制动作光效。覆盖引导第 0 帧、转向、左向、1.35 倍缩放、松手后 1/10/20 帧及其他动作的默认紫色；检查水射线第 0 帧显示蓝色、收势亮度递减到零，默认动作仍保留紫色及原有淡入。

验证状态：6/18/36/60 帧、夜晚 60/90/120 帧、白天/夜晚、左向/上向、弯曲、短距离、1.35 倍缩放、直接绘制到 backbuffer、关闭 Bloom、仅粒子 Bloom、停止后 8/30 帧。射线沿用分层队列，但材质现在是正常透明混合的蓝色水体和白沫；断言开启 Bloom 不会产生大片光晕或水花光团，停止后粒子能全部消失。

移动附着回归按玩家 → 弹幕生成粒子 → Dust 更新的顺序运行，覆盖生成当帧、同帧重复更新、水平/垂直/反向移动、oldPosition 重置、owner 失活后重新激活。直接使用射线生成方法，比较静止与 24px/tick 左右移动、18px/tick 上移、斜向飞行时的杖尖粒子局部坐标，输出 moving-right-up、moving-left-up、moving-diagonal 帧。修复前零速粒子在 owner 移动 24px 的生成当帧会错误多移 24px，之后同帧 Dust 更新还会重复加一次；修复后只消费粒子保存的附着位置差。

这里模拟的是固定轨迹、相机和杖尖标记，未运行玩家动作、碰撞、敌人受击、动态环境光照、联机同步或游戏完整绘制顺序。因此这些 PNG 是 FNA 离线预览，不是游戏截图；实际角色姿势与杖尖的相对位置仍需在游戏里确认。

源代码检查可执行 `dotnet msbuild 伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:q`，不触发向正在运行的游戏打包。工具源码保留为 `.cs.txt`，生成的 `.cs` 位于默认编译排除的隐藏目录；`build.txt` 已排除 `Tools` 与 `.vissandbox`。

素材来源：水花 `Eff_Splash_4001.png`（2 列变体、6 行动画）、泡沫 `Eff_Splash_4002.png`（4×4 动画）均来自伊蕾娜水魔法目录。射线改用与水球吸水一致的 `Eff_Splash_322_288.png`、`WaterChargeFlow.xnb`、KL 的 `Eff_Noise_DHL_01` 与 `LightTrail`，保持相同的浅蓝主体、白沫提取阈值及透明混合。纹理按实际路径每 180px 重复，向射线末端滚动；不会因为碰撞截短而拉伸纹理。`WaterLaserGlow.png` 仍保留供原有杖尖绘制方法使用，该方法当前未被射线调用。

本次仅改绘制：`WaterLaser.cs` 和 `WaterLaserDust.cs` 的轨迹、碰撞、伤害、耗魔、粒子生成与生命周期均保持原样，水球吸水绘制也未修改。

# 水射线 FNA 离线预览

运行：`& ./Tools/WaterLaserPreview/run.ps1`。默认使用项目 tModLoader.targets 对应的 `D:/Steam/steamapps/common/tModLoader`，也可传入 `-TmlPath`。

脚本在 `.vissandbox/water-laser-preview/out` 输出 PNG。每次运行重新读取实际 `WaterLaserVisuals.cs`、`WaterLaserDust.cs`，提取 WaterLaser 的粒子生成、请求注册和绘制方法，以及 KL 的粒子生命周期、TrailEffect、分层队列、Bloom 捕获和合成代码。使用项目原始 PNG 和 KL 编译后的 XNB。没有另写一套模拟 Bloom。

验证状态：6/18/36/60 帧、白天/夜晚、左向/上向、弯曲、短距离、1.35 倍缩放、直接绘制到 backbuffer、关闭 Bloom、仅粒子 Bloom、停止后 8/30 帧。断言 Bloom 产生亮度增量，停止后粒子能全部消失。

移动附着回归按玩家 → 弹幕生成粒子 → Dust 更新的顺序运行，覆盖生成当帧、同帧重复更新、水平/垂直/反向移动、oldPosition 重置、owner 失活后重新激活。直接使用射线生成方法，比较静止与 24px/tick 左右移动、18px/tick 上移、斜向飞行时的杖尖粒子局部坐标，输出 moving-right-up、moving-left-up、moving-diagonal 帧。修复前零速粒子在 owner 移动 24px 的生成当帧会错误多移 24px，之后同帧 Dust 更新还会重复加一次；修复后只消费粒子保存的附着位置差。

这里模拟的是固定轨迹、相机和杖尖标记，未运行玩家动作、碰撞、敌人受击、动态环境光照、联机同步或游戏完整绘制顺序。因此这些 PNG 是 FNA 离线预览，不是游戏截图；实际角色姿势与杖尖的相对位置仍需在游戏里确认。

源代码检查可执行 `dotnet msbuild 伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:q`，不触发向正在运行的游戏打包。工具源码保留为 `.cs.txt`，生成的 `.cs` 位于默认编译排除的隐藏目录；`build.txt` 已排除 `Tools` 与 `.vissandbox`。

素材来源：水花 `Eff_Splash_4001.png`（2 列变体、6 行动画）、泡沫 `Eff_Splash_4002.png`（4×4 动画）均来自伊蕾娜水魔法目录。`WaterLaserGlow.png` 由 `make_glow.py` 生成，是边缘归零的 128×128 径向衰减图，使用加色混合，不需要新 shader。射线继续使用 KL 的原噪声和流动贴图。

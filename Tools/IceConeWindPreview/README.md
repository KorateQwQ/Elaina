# 冰锥发射气流 FNA 离线预览

运行 `& ./Tools/IceConeWindPreview/run.ps1`，输出到 `.vissandbox/ice-cone-wind-preview/out`。
工程直接链接正式 `IceConeWindDrawing.cs`、`IceConeWindDust.cs` 与 KL 的 `LayerDrawRequestSystem.cs`。
加载正式 `Mounts/Assets/BroomDashWind.png` 和 `Effects/Content/FireBurstFlame.xnb` 的 `WindRing` 通道。
不生成或修改正式资源。工具源为 `.cs.txt`，工具与临时工程均在打包排除目录。

冰锥后撤最后一帧原有的 BurstCircle 已替换为 IceConeWindDust；位置沿用原环的前方 50 像素，
方向直接读取 forwardVelocity，避免使用后撤时反向的速度或停顿帧的零速度。
粒子独立存活 16 tick，半径从 30 扩散至 66，带宽从 18 收细到 0，整体淡出。
前后面由深度正负固定，缺口与纹理相位旋转；内面蓝色、外面黑色，采用预乘 AlphaBlend。
时间继承 KLBasicDust 的游戏粒子生命周期，绘制不推进计时。

借用 KL 的粒子收集入口 SubmitBloomDrawRequest，但两面都调用普通 Request：

- 蓝色后半圈：Projectiles.After，TrailOrder；当前 KL 先执行普通请求，再合成同层冰锥 Bloom 主体。
- 黑色前半圈：Dust.After，DustOrder；覆盖已经合成的主体。

当前 KL FlushLayerRequests 为普通回调准备活动 SpriteBatch；本工具复现此边界，检查回调后仍能 End。
没有使用旧炎爆预览文档中“Dust.After 无活动批次”的过时假设。

输出与检查：

- `stages.png`：每行依次为年龄 1、3、6、10、14、16 tick；上两行亮背景、下两行暗背景，含左右发射。
- `multiple-125.png`：三个独立粒子，非零相机坐标、125% 缩放。
- 8 个发射方向 × 8 个缺口相位，分别渲染两面并检查颜色、深度半区与覆盖度。
- 普通前后请求、无 Bloom 请求、白条遮挡、到期、回调登记后失效、Dust 槽位复用、服务器不生成粒子。

白条是遮挡探针，不是冰锥模型。预览适配 Main、Dust 创建、资源获取和基础生命周期数据；
真实 Dust 回调、请求分组及网格绘制直接使用正式源码。它不模拟完整粒子更新、冰锥 AI、世界光照、
模型 Bloom、碰撞与联机。游戏内与冰锥模型的最终搭配需要实测。

2026-11-08：正式 C# 编译通过，FNA 检查通过，已查看本次输出的阶段图与多粒子图。

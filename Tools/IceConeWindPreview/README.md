# 冰锥发射气流 FNA 离线预览

运行 `& ./Tools/IceConeWindPreview/run.ps1`，输出到 `.vissandbox/ice-cone-wind-preview/out`。
工程直接链接正式 `IceConeWindDrawing.cs`、`IceConeWindDust.cs` 与 KL 的 `LayerDrawRequestSystem.cs`。
加载正式 `Mounts/Assets/BroomDashWind.png` 和 `Effects/Content/FireBurstFlame.xnb` 的 `WindRing` 通道。
不生成或修改正式资源。工具源为 `.cs.txt`，工具与临时工程均在打包排除目录。

冰锥后撤最后一帧原有的 BurstCircle 已替换为 IceConeWindDust；位置沿用原环的前方 50 像素，
方向直接读取 forwardVelocity，避免使用后撤时反向的速度或停顿帧的零速度。
粒子独立存活 16 tick，半径从 40 扩散至 86，带宽从 18 收细到 0。
前后面由深度正负固定，缺口与纹理相位旋转；内面蓝色、外面白色，采用预乘 AlphaBlend。
蓝面 RGB 与 Alpha 同乘生命周期淡出；白面保持 .95 白色、Alpha 为 1，随带宽收细消失。
时间继承 KLBasicDust 的游戏粒子生命周期，绘制不推进计时。

借用 KL 的粒子收集入口 SubmitBloomDrawRequest，但两面都调用普通 Request：

- 蓝色后半圈：Projectiles.After，TrailOrder；当前 KL 先执行普通请求，再合成同层冰锥 Bloom 主体。
- 白色前半圈：Dust.After，DustOrder；覆盖已经合成的主体。

当前 KL FlushLayerRequests 为普通回调准备活动 SpriteBatch；本工具复现此边界，检查回调后仍能 End。
没有使用旧炎爆预览文档中“Dust.After 无活动批次”的过时假设。

输出与检查：

- `stages.png`：每行依次为年龄 1、3、6、10、14、16 tick；上两行亮背景、下两行暗背景，含左右发射。
- `multiple-125.png`：三个独立粒子，非零相机坐标、125% 缩放。
- `overlap-stages.png`：列为年龄 1、4、8、12、15 tick，行为 1、3、6 个重合气流；直接在不透明暗背景绘制两面。
- `rear-age*-copies*.png`：蓝色后半圈透明底原始输出；检查 RGB 不超过色调乘覆盖度，容许每层 1 个色阶再加 1 的 8 位混合舍入误差。
- 8 个发射方向 × 8 个缺口相位，分别渲染两面并检查颜色、深度半区与覆盖度。
- 普通前后请求、无 Bloom 请求、白条遮挡、到期、回调登记后失效、Dust 槽位复用、服务器不生成粒子。

白条是遮挡探针，不是冰锥模型。预览适配 Main、Dust 创建、资源获取和基础生命周期数据；
真实 Dust 回调、请求分组及网格绘制直接使用正式源码。它不模拟完整粒子更新、冰锥 AI、世界光照、
模型 Bloom、碰撞与联机。游戏内与冰锥模型的最终搭配需要实测。

使用 `Game.RunOneFrame()` 后台导出，在初始化、加载、绘制及每次保存时检查 SDL 隐藏状态。

2026-11-14：修复蓝面只降低 Alpha、RGB 不同步淡出的问题；旧实现在重叠时累计亮度并变白。
Shader `WindRing` 已将纹理遮罩乘入 RGBA，所以保留 AlphaBlend，修正顶点 RGB 的生命周期预乘。
`& ./Tools/IceConeWindPreview/run.ps1 -LegacyAlpha` 仅在临时目录还原旧错误表达式，输出到 `legacy-alpha`，
**预期以预乘颜色检查失败退出**，证明检查能捕获旧缺陷；不修改正式源码。
正常入口检查通过。已查看阶段图、重叠对照及完整冰凌锁 FNA 抽帧，正式 C# 编译通过。
两套输出生成后，运行 `python Tools/IceConeWindPreview/compare.py` 生成 `comparison.png`：
第 8 tick 的 1 层/6 层对照，使用不透明底截图裁剪并以最近邻放大 2 倍，不再次做 Alpha 合成。
游戏包重载后的实机外观尚未验证。

## 2026-11-18：冷启动随机发白、炎爆风圈黄黑夹杂

两种风圈共用 `FireBurstFlame/WindRing`。旧像素 Shader 只有一个 uniform：
`MaskFromRed`，正常 Alpha 遮罩模式始终传 0。FNA3D 的 D3D11/MojoShader 后端创建
GPU 常量缓冲时没有提供初始数据，CPU 侧缓存却清零；上传函数仅在两份 CPU 参数
比较发生变化时更新 GPU。因此首次参数全零时可能一直不上传，遮罩读取 GPU 残留值。
这也解释了为什么仅切换其他 Effect 或修正透明度不能解决每次启动的随机性。

正式修复改为 `WindMaskWeights`：Alpha / RGB 最大值的权重分别为 `(1, 0)` / `(0, 1)`，
始终含非零值，保证首次上传。Shader 以 dot 计算遮罩，两个正式调用点同步修改，
重新编译 `FireBurstFlame.xnb`；颜色、几何、生命周期及 Bloom 参数保持原值。
正常 Alpha 模式输出与修改前正常基线逐像素相同。

```powershell
& ./Tools/FireBurstPreview/compile.ps1
& ./Tools/IceConeWindPreview/run.ps1 -ColdStart
& ./Tools/IceConeWindPreview/run.ps1 -ShaderState
& ./Tools/IceConeWindPreview/run.ps1
& ./Tools/FireBurstPreview/run.ps1 -SkipShader
```

`ColdStartChecks.cs.txt` 是仅供离线预览使用的 D3D11 故障注入：每组重新加载 Effect，
先用零参数和屏幕外三角形绑定像素常量缓冲，通过 `FNA3D_GetSysRendererEXT` 获取
当前预览的 D3D11 context，再以 Map/Unmap 向该缓冲写入 0、8、32、−4，保持
MojoShader 的 CPU 缓存为零。之后直接调用正式冰/火风圈绘制代码。
它模拟未初始化 GPU 数据，不修改运行中的游戏、驱动或 FNA DLL；其他后端直接报错。

旧实现冰圈有 983/984/930 个像素偏离零基线，火圈有 3364/3392/3314 个；
画面重现蓝面发白、黑面出现黄色纹路。修复后八组均为零差异。
输出 `cold-{ice,fire}-{0,8,32,-4}.png`；本次对照留存为 `out/cold-start-comparison.png`，
三列依次为正常基线、旧实现在残留值 8 下的异常、修复后在相同残留值下的正常结果。
故障注入图只验证风圈本身，不含火柱、粒子和 Bloom；完整炎爆另由 FireBurstPreview 检查。

`-ShaderState` 检查共享 Apply/Rubble/DomainRing 绘制及另外 117 个 Effect pass 的切换。
切换其他 Effect 的部分只 Apply，不代表模拟了这些效果的全部实际绘制。
正常冰圈的方向/相位、重叠、遮挡回归，以及炎爆完整 Bloom 预览通过，已打开生成图检查。
正式 C# 编译和 Shader 编译通过；完整构建在打包阶段因游戏占用报 TML003，
尚未更新正在运行的游戏包，也未进行重载后反复启动游戏的实机验证。

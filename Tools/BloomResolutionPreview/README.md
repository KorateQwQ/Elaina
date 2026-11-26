# Bloom 分辨率与采样检查

## 2026-11-10：Bloom 性能回归与优化

在本机 RTX 5080 / FNA D3D11 上重放正式管线，确认 2026-11-05 改动存在性能回归。该提交除了更换采样器，也修正了视口、源纹素间隔与绘制矩形；但控制变量测试表明主要增量来自 FP32 RGBA 贴图上的 25 次线性过滤：仅把优化前管线的 LinearClamp 改为 PointClamp，4K 单层耗时即从 5.005 ms 降到 3.198 ms。此诊断仅在隔离源码中运行，不能作为正式画质修复。

| 4K 场景 | 旧版 `47b4b52` | 优化前 `8b2b457` | 本次优化 |
| --- | ---: | ---: | ---: |
| 单层 Bloom | 3.306 ms | 5.005 ms | 3.044 ms |
| 两次独立 Bloom 合成 | 5.387 ms | 9.692 ms | 4.775 ms |

数据是 CPU 提交 + GPU 完成等待 + 固定单像素读回的中位数，含约 1.2 ms 的 4K 空场景/读回成本；不是 GPU timestamp、游戏帧率或利用率预测。每项预热后测 90 次，三轮轮换顺序；后台游戏、GPU 频率与读回成本仍可能影响绝对值。1080p 单层从 1.405 ms 降到 0.838 ms，两次从 2.472 ms 降到 1.345 ms。原始结果保存于 `performance-2026-11-10.json`。

正式改动位于同级 KL：

- 保留 LinearClamp、原分辨率链和迭代数；新增九次读取的滤波 pass，近似原来四舍五入的 5×5 权重，保留总权重 0.9997。双线性合并在非整数纹素相位下与原 25 次读取并非逐像素等价，按实际截图检查差异。
- 只把 BloomDownSample/BloomUpSample 改为 HalfVector4，捕获与通用场景目标仍为 Vector4；尺寸、格式、设备与生命周期均检查。
- 场景路径少一次无用全屏拷贝，backbuffer 路径少两次；保留原始捕获的预乘颜色和覆盖度。
- 公共 GaussianBlur/GaussianBlurTwice 的原签名和旧 pass 保留，快速 pass 仅由已设置 LinearClamp 的 Bloom 管线使用。FX 和正式 XNB 同步更新。

复现命令（PowerShell，仓库根目录）：

```powershell
& Tools/BloomResolutionPreview/compile.ps1
& Tools/BloomResolutionPreview/run.ps1 -Label perf-legacy -BaselineRevision 47b4b52 -Benchmark
& Tools/BloomResolutionPreview/run.ps1 -Label perf-before -BaselineRevision 8b2b457 -Benchmark
& Tools/BloomResolutionPreview/run.ps1 -Label perf-point-diagnostic -BaselineRevision 8b2b457 -Benchmark -DiagnosticPointSampler
& Tools/BloomResolutionPreview/run.ps1 -Label perf-after-half -Benchmark
& Tools/BloomResolutionPreview/run.ps1 -Label visual-before -BaselineRevision 8b2b457 -Check
& Tools/BloomResolutionPreview/run.ps1 -Label visual-after -Check
python Tools/BloomResolutionPreview/compare-performance.py
& Tools/BloomBackbufferPreview/run.ps1
& Tools/FireLaserPreview/run.ps1 -Dotnet C:/Users/PC/.dotnet/dotnet.exe -SkipShader
```

旧版源码使用保留的原始 shader pass，所以可与新版共用当前 XNB；隔离源码从 Git 读取，不回滚工作区。测试同步移除了飞弹夹具对已删除 MagicMissileTrailNoise 的依赖，实际拖尾仍从当前源码提取。

验证：1366×768、1080p、1440p、4K 与 125% 变换通过；移动点光源的能量比最大 1.0074；Point/Linear 世界采样器对 Bloom 输出无影响。23 张原生尺寸截图对照中，发光区域平均 RGB 通道差最大 0.756/255，单通道极值最大 7/255。已打开十字星、火焰粒子、飞弹拖尾对照图，以及火焰射线和双层合成输出，未见回归到马赛克或断裂光晕。输出是 FNA 离线预览。

Backbuffer 的 2/5/7 层、强度、开关、批次恢复、异常恢复和目标生命周期检查通过。真实 FireLaser.DrawVisual2 的白天/夜晚、短射线、左向、入退场连续性与后段 Bloom 检查通过。KL Release 构建与伊蕾娜 Release 源码编译通过；需先构建同配置 KL，再编译消费者，旧的 KL Release 引用会报无关的元素伤害和拖尾参数错误。没有替换或重载游戏中的模组，游戏内 GPU 利用率仍待重载 KL 后验证。

以下为此前采样画质修复的历史记录。

2026-11-05：用户在 1920×1080 下报告火焰粒子的光晕呈格子、多重射击十字星出现横竖条带、拖尾细且失去柔光。本工具复用 `BloomBackbufferPreview` 的 Roslyn 抽取器，直接编译当前 KL 的捕获、降采样、重建、合成与请求队列，并加载正式 Bloom/ReColor XNB。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/BloomResolutionPreview/run.ps1 -Label after -Check
```

可用 `-TmlPath`、`-Dotnet` 指定安装和 SDK；模板目标为 .NET 10。生成工程、全尺寸 PNG、未缩放局部及检查数据在 `.vissandbox/bloom-resolution/after`。工具与生成目录均排除编译/打包。

## 场景来源

- 十字星：每次抽取 `MagicMissleSpawner.DrawStartStar`，加载正式 ShotLine、voronoiOrigin、FuzzyEdge。
- 火焰与细线粒子：抽取 KL `FireDust`、`LineSparkle` 的字段、绘制及请求方法，使用正式 PNG；Dust 的位置、寿命与尺寸为固定夹具，不读真实粒子池。
- 飞弹拖尾：抽取 `MagicMissile.DrawManagedTrail`、`BeginTrailBatch`，编译完整 `MagicMissileTrailVisuals` / `MagicMissileTrailNoise`，加载正式 LightTrail、PerlinX、MagicMissileTrail XNB。固定弯曲路径代替 AI。
- 一像素线与点：独立 HDR 测试输入，检查后处理采样、光能和位置稳定性。临时白像素只在离线宿主生成。

覆盖 1366×768、1920×1080、2560×1440、3840×2160；1920×1080 的星、火焰和拖尾另覆盖真实 125% 画面变换。截图使用原生像素，非 UI 缩放。镜头、角色、光照、网络与其他模组没有模拟。

## 回归与修改前对照

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/BloomResolutionPreview/run.ps1 -Label before -BaselineRevision 47b4b523f834facc888c0494a1c2cd4c58a7f420
python Tools/BloomResolutionPreview/compose.py
```

基线只把指定 KL Git 版本的五份管线源码读入隔离目录，不切换工作区。消费者和素材与修改后相同；本次 Bloom FX/XNB 没有修改。`comparison-1080p.png` 按原生尺寸拼接局部，左旧右新；不通过二次模糊或缩放制作效果。

检查要求：固定光源的结果不随 `Main.DefaultSamplerState` 的 Point/Linear 改变；光点移动 8 个整数相位时的积分光能比 ≤1.15；辅助采样器恢复；逻辑场景和 backbuffer 不同尺寸时，Bloom 强度 0 的本体与关闭 Bloom 的本体 RGB 差异 ≤1。旧代码在四个分辨率的采样器检查上均失败。

基线数据（后处理之前的输入相同，调用方 s1 固定为 PointWrap）：1920×1080 的最大红通道采样差为 0.290657，修改后为 0；1366×768 的光点相位积分比从 5.5793 降至 1.0095。新路径按真实源纹素连续滤波，保留了旧路径跳采样时丢掉的细线能量，不能把亮度恢复解释成单独调高 HDR/Bloom 强度。

## 验证边界

`BloomBackbufferPreview` 的 2/5/7 层、开关、强度、多层合成、活跃批次、异常恢复、目标生命周期检查通过，补充了 s1 采样器恢复。`LineSparklePreview` 的共享层和消亡检查通过；`FireLaserPreview -SkipShader` 的真实火焰射线、入退场连续性、短射线残余段及后段光晕检查通过。

旧 `ManagedTrailPreview` 综合工具目前未通过：夹具仍按旧飞弹拖尾字段和接口构造，缺 `MagicMissileTrailVisuals` 等依赖。本次仅同步其 Bloom helper 抽取列表；没有改写或放宽其断言，也不把它记为通过。当前正式飞弹拖尾由本工具的直接抽取与完整绘制器覆盖。

KL 和伊蕾娜正式 C# 分别通过 `dotnet msbuild <项目>.csproj -t:Compile -p:Configuration=Release -p:BuildProjectReferences=false`。未打包、替换或重载运行中的模组；游戏内还需重载 KL 后确认 1080p 的实际战斗画面，以及 Color/Retro 光照路径。固定贴图无新增，Bloom RenderTarget 数量和尺寸分配策略未增加。

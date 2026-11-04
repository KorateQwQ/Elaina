# Bloom 分辨率与采样检查

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

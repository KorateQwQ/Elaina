# 瓶中雨正式 Shader 与 FNA 预览

Shader 已通过 `ElainaModAlchemy/Rain/BottledRainCloud.PreDraw` 接入游戏，默认使用体积云。`RainCloudShaderRenderer` 由游戏和本预览共同编译使用，`RainCloudVisualSystem` 在绘制线程首次加载后共享一份材质及噪声，卸载时释放自有资源；专用服务器跳过视觉资源。材质初始化失败时记录一次错误并回退原贴图云。原 `RainCloudDrawing` 的贴图分支及 `Tools/BottledRainPreview/` 保留用于对照；`run.ps1` 前后核对植物逻辑和旧预览源码 SHA-256。

## 运行

从仓库根目录执行：

```powershell
pwsh -File Tools/BottledRainShaderPreview/run.ps1
pwsh -File Tools/AlchemyItemChecks/run.ps1 -RainOnly
dotnet build 伊蕾娜.csproj -p:BuildProjectReferences=false -v:quiet
python Tools/BottledRainShaderPreview/check-package.py
```

默认使用当前 tModLoader 安装中的 FNA 与原生库，以及同级 `KL/Effects/XNBCompiler.exe` 工具链。可用 `-Tml` 和 `-CompilerDirectory` 指定其他安装位置。KL 只提供本机离线编译器，预览本身不引用 KL 程序集。

唯一正式源为 `Effects/Content/BottledRainCloud.fx`。脚本复制到 `.vissandbox/bottled-rain-shader/production-compiler/Content/`，移除上次暂存 XNB，编译后写回正式 `Effects/Content/BottledRainCloud.xnb`；预览直接加载正式资源。原型的 `CloudVolume.fx` 已迁移，不再维护两份材质。预览窗口默认隐藏，导出后退出，不启动或操作游戏。完整 build 更新 `tModLoader/Mods/伊蕾娜.tmod`，重新加载模组后投掷瓶中雨可查看实际效果。

## 绘制契约

- `BottledRainCloud.fx` / technique `CloudVolume` / pass `RenderCloud`，兼容当前 XNA/FNA 的 `ps_3_0`。像素着色器使用 SpriteBatch 自带顶点着色器，不用于裸顶点条带。
- 云体是若干平滑合并的三维椭球密度场，叠加两层流动三维值噪声；每像素沿深度采样 16 步，累积消光与透光，并沿光照方向追加密度采样形成体积阴影。
- s0 为独立白色 1×1 纹理，用完整 0–1 UV 绘制 360×140 的云体区域。不可把 1×1000 MagicPixel 的单像素 UV 直接作为云 shader 的 UV。
- s1 为 C# 固定种子生成的 256×256 噪声，`LinearWrap`；无需外部噪声图片。R 通道保留随机标量，shader 从相邻 Z 切片采样并插值得到连续三维噪声；G 通道在上传前一次性合成三层平滑二维噪声，用于消融。没有逐帧重建纹理。
- uniforms：`Time` 为弹幕年龄 / 60 的秒数，`Opacity` 负责末尾淡出，`Density=1.2`，`FlowSpeed=0.28`。`Reveal` 为显形阈值，`RevealGlow` 控制描边亮度。蓝紫暗部与冷白亮部在 shader 中定义。
- 出现阶段固定云体位置和大小，用约 132 tick（2.2 秒）的噪声消融显形。创建纹理时同时统计覆盖区的消融噪声分布，生成 129 项阈值表，使显形面积平稳推进，避免大量灰阶聚集导致中段突然铺开。前 42 tick 轻柔淡入；消融边界加宽羽化，蓝白细描边和淡紫柔光降低峰值并渐入渐出。Shader 版本的雨丝在 54～126 tick 逐渐落下，旧贴图回退的节奏保持不变。两次密度采样共用一个内层循环体，保持原有采样次数与体积光照，同时满足 ps_3_0 的静态指令槽限制。
- 输出为预乘 RGBA，使用 `BlendState.AlphaBlend` 和 `DepthStencilState.None`，沿用调用者 Rasterizer/Scissor 与完整变换矩阵。绘制前提交原批次，结束后恢复原 Sort/Blend/Sampler/Depth/Rasterizer/Effect/Matrix、s0/s1 纹理和采样器、Scissor。按变换后的云体包围盒跳过屏幕外云；零透明度直接跳过。没有额外屏幕采样、全屏 Bloom 或运行时 RenderTarget。
- 雨丝、水花、碎片和星点直接链接 `RainCloudDrawing.cs`；启用 Shader 时传 null 只跳过旧云层，再由共享 Renderer 绘制体积云。地形、人物与雨落距离仍是预览夹具。

## 输出与验证

输出目录：`.vissandbox/bottled-rain-shader/`。

- `shader-cloud.gif`：120 帧、6 秒的实际 FNA shader 绘制动画。
- `original-vs-shader.png`：左侧为保留的旧贴图效果，右侧为当前正式 Shader。
- `shader-sequence.png`：出现、完整雨云、亮背景、消散阶段。
- `shader-125.png`：125% 缩放。
- `dissolve-reveal-soft.gif`：包含缓慢出现过程的 5 秒 FNA 动画。
- `dissolve-sequence.png`：24、54、90、132 tick 的消融显形阶段。
- `reveal-timing.png`：从 0 到 132 tick、间隔 12 tick 的云体帧条。
- `plant-growth-indicators.gif`：雨幕内植物的向上增益箭头；屋顶遮挡的植物无标记。

2026-10-24：正式 FX 编译与模组构建通过。FNA 直接运行正式 Renderer 和正式 XNB，已查看多时间点、明暗背景和 125% 输出；12 组像素/状态检查覆盖 Deferred/Immediate、Alpha/Additive、缩放平移和重力翻转，并验证绘制前后哨兵精灵未受影响、s1 与裁剪恢复、屏幕外与零透明跳过。40 项真实引擎玩法检查通过，包含客户端图标的范围、遮挡、多格植物/重叠雨云去重、采集与过期清除、世界切换及不写图块。只读包检查校验 TMOD SHA-1、包内 Shader 与正式 XNB 字节相同、模组 DLL 与本次构建相同，以及 Tools/.vissandbox 未打包。尚未实际操作游戏验证视觉、联机和其他模组的绘制钩子。

## 植物影响标记

`RainPlantIndicatorSystem` 每 10 tick 从客户端同步后的雨云与图块收集影响对象；与服务器浇灌共用 `RainPlantGrowth.VisitCoveredPlants` 的范围、实心遮挡、液面和有效植物判定。多个雨云覆盖同一株只显示一个图标，树苗/南瓜按单株处理，连续竹子、藤蔓、仙人掌等茎段合并。植物被采集或长成树后立即清除标记；最后一朵云离开/消失后约 0.3 秒内淡出，切换世界清空缓存。保留当前服务器每 10 tick 的浇灌频率。

图标用共享的 `RainPlantIndicatorDrawing` 绘制浅青色像素箭头，带暗色轮廓、轻微上浮和呼吸；复用 MagicPixel 单像素采样，不新增图标贴图、Shader、Buff 或网络包。通过 `PostDrawTiles` 绘制并沿用游戏缩放/相机变换；专用服务器不运行标记扫描。FNA 直接链接正式箭头绘制代码，演示中的植物位置/遮挡仍为场景夹具，真实判定另由上述引擎检查覆盖。

## 独立性能测量

下列结果为正式接入时、加入消融显形之前的测量，尚未对消融版重新计时。

```powershell
pwsh -File Tools/BottledRainShaderPreview/run.ps1 -Benchmark
```

2026-10-24 正式 Renderer 复测：FNA3D 日志报告 NVIDIA GeForce RTX 5080 / D3D11。1920×1080 离屏目标，单朵 Shader 覆盖 360×140 像素。每种情景预热后采样 160 次，四轮轮换顺序；所有云完整显示并重叠，同样绘制雨丝、水花与星点。计时包括 CPU 提交、GPU 完成及一像素同步读回，因此属于独立绘制往返耗时，**不是纯 GPU 时间，也不是游戏帧时间或 FPS**。计时区域不包含文件导出。

| 同屏数量 | 原贴图版中位数 | Shader 版中位数 | 两者差值 |
| --- | --- | --- | --- |
| 1 | 0.355 ms | 0.385 ms | +0.031 ms |
| 4 | 0.395 ms | 0.484 ms | +0.089 ms |
| 8 | 0.480 ms | 0.610 ms | +0.130 ms |
| 16 | 0.538 ms | 0.856 ms | +0.318 ms |

空场景同步读回中位数为 0.331 ms，已包含在两栏中；不能把整栏数字当成云自身的 GPU 消耗。单朵云在 150%/200% 绘制缩放时，相比原贴图版增加约 0.076/0.094 ms。相差几百分之一毫秒的结果仅用于量级判断，会受同步、驱动和后台负载影响。

原始结果保存到 `.vissandbox/bottled-rain-shader/benchmark.json`。FNA 的 `GraphicsAdapter.Description` 在本机返回显示器名称，JSON 将其记为 `Display`；实际 GPU 型号以启动时的 FNA3D 日志为准。这些数据不能外推到核显或其他独显，正式接入后仍需在真实游戏场景测量。

## 游戏运行时的构建

运行中的游戏可能占用 `Mods/伊蕾娜.tmod`，外部构建会报 TML003。不要关闭用户的游戏或强行覆盖该文件。执行 `pwsh -File Tools/BottledRainShaderPreview/build-staged.ps1` 可使用隔离的 tML 存储目录生成并校验 `.vissandbox/bottled-rain-shader/staging/Mods/伊蕾娜.tmod`，不写入真实世界/角色/配置。用户在游戏的开发模组界面“构建并重新加载”后，正式源码和 XNB 才会应用到当前游戏；暂存包不会自动替换正在运行的版本。


## 固定资源预制（2026-10-25）

雨云现在从正式资源加载 `Effects/Tex/Baked/WhitePixel.png` 与 `ElainaModAlchemy/Rain/Assets/CloudNoise.png`，不再运行时生成白像素、随机噪声或平滑 G 通道。129 项消融阈值也由同一离线工具生成到 `RainCloudBakedData.cs`。生成算法和参数保留在 `Tools/FixedTextureBake`，使用 `run.ps1 -Check` 验证。

`RainCloudShaderRenderer` 借用资源 Effect/纹理，不再 Clone/Dispose GPU 资源或反射读取 SpriteBatch。弹幕绘制后调用 KL `EndBeginDraw()`；预览传入明确矩阵并结束自己的批次。上文初始化噪声、反射保存任意状态和 12 项旧状态检查描述为历史实现，已被此方案替代。

已对照本次更改前后新生成的出现、完整、明亮背景、消散、125% 图及帧条，输出像素完全一致；包内 rawimg 与原运行时算法的 RGBA 字节一致。尚未在实际游戏中验证本次资源替换。

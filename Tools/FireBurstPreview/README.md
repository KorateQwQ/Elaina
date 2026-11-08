# 炎爆、法阵与气流环 FNA 离线预览

运行：

```powershell
& ./Tools/FireBurstPreview/run.ps1
```

脚本先用 `compile.ps1` 调用相邻 `KL/Effects/XNBCompiler.exe`，把正式源 `Effects/Content/FireBurstFlame.fx` 编译并写回同名 `.xnb`（`-SkipShader` 跳过）。
默认使用本机 .NET 10、FNA D3D11 和相邻 KL 项目；可用 `-Dotnet`、`-TmlPath` 覆盖运行时路径。
输出目录：`.vissandbox/fire-burst-preview/out`。

Bloom 参数默认从 `KL/Configs/BloomConfig.cs` 提取实际 `SetBloomInfo` 调用，当前强度 1、采样层数 3。
此前预览写死了 5 层，因此火柱周围比游戏多出更大范围的淡辉光；已修正预览默认值，不改动游戏配置。
`bloom-config-vs-five.png` 为同一帧、同一分辨率的对照：左侧当前游戏配置（3 层），右侧 5 层。
`comparison-bloom5-f36.png` 仅用于参数比较，其他常规输出使用游戏配置；预览仍不模拟游戏的完整背景、分辨率和后续屏幕特效。

每次运行从当前 `FireBurstProj.cs` 提取实际阶段属性、默认值、AI、伤害开关和所有绘制方法，
从 KL 提取实际插值、纹理绘制、CommonMagic 参数绑定和 Bloom 管线，加载正式 PNG/XNB（含 `FireBurstFlame.xnb`）。
爆发火柱的着色：流纹贴图 `Trail/Eff_Trail_2014` × 包络的亮度在 0.06 / 0.15 / 0.42 三个阈值之间连续插值颜色，从外到内 暗红 → 红 → 黄，
颜色再乘贴图亮度，黄层超过 Bloom 阈值晕出白芯；透明度乘 1/外层阈值，轮廓内不透明。
曾对比过三层硬分层、色带取色和多种流纹纹理，最终保留原光柱形状与放大变细的曲线，只改为平滑分层。
流纹贴图原为 `光柱`：其两侧暗纹使轮廓内侧成为宽而破碎的暗褐边，顶端包络弱而贴图亮，外层成片饱和橙色，故换为亮度更均匀的 `Eff_Trail_2014`。
火柱包络原为 `lightMask` 贴图，现由 shader 按公式计算（拟合 `lightMask`，参数在 `DrawVisual` 开头），分辨率无关；
shader 另按屏幕像素宽度给轮廓做软边，并用三次 B 样条采样被放大的流纹贴图（双线性放大后的格子会在颜色分层上显出台阶）。渲染前把 1～3 号采样槽设为 `PointClamp`，模拟游戏中其他绘制的残留状态：
`DrawBlackDust` 原为 `lightMask` 裁切的黑烟，现为 `FireBurstFlame.fx` 的 Rubble 通道，做法同火焰龙卷的石块：
`PerlinX` 高于阈值的岛状区域为碎块，阈值附近为 HDR 橙色烧边；阈值随包络降低（中心碎块多），随时间叠加消融量，
前 8 帧从烧边中成形、末 15 帧烧尽。只用到 `PerlinX`，1 号槽同样显式设为线性采样。
`day-edge-x3-f33.png` 为爆发第 3 帧火焰顶端轮廓的 3 倍放大，用于检查锯齿。
星芒保留原有亮度脉动；光环的强度前 10 帧由 5 渐变至 2.5，随后保持 2.5，
再叠加原有脉动，不再于末段降低至零。脉动提取实际 `DrawManager.FrameTime`。
复用 `Tools/BloomBackbufferPreview` 的管线编译器；没有另写星芒、火柱或黑烟的视觉实现。
适配部分为弹幕状态、资源加载、纹理矩形与原点、相机和地面色块（在弹幕层之前绘制）。
星芒阶段为年龄 1–30 帧，爆发阶段为 31–60 帧；两者属于同一个弹幕，使用同步的 `ai[0]` 计时。

- `star-bloom-f06.png` / `star-no-bloom-f06.png`：星芒 Bloom 开关对比。
- `night-bloom-f36.png` / `night-no-bloom-f36.png`：爆发 Bloom 开关对比。
- `day-star-f06.png` / `day-bloom-f36.png`：两阶段的明亮背景。
- `night-bloom-f01/f15/f29/f30/f31/f42/f50/f59.png`：前摇、阶段交界和爆发淡出。
- `multiple.png`：两个炎爆共享同层 Bloom，每个回调只绘制一次。
- `multiple-stars.png`：两个星芒共享同层 Bloom。
- `invalidated.png` / `invalidated-star.png`：登记后失效的弹幕不再绘制。

检查星芒 `OverPlayers.After` 与爆发 `Projectiles.After` 的阶段切换、普通绘制禁用、
Bloom 开关亮度差及每个实例的绘制次数。
逐帧比较星芒 1–30 帧的 Bloom 开关，确保前摇末段仍有光晕；`day-star-f30.png` 检查白天下的最后一帧。
执行实际 AI 检查前 30 帧无伤害、爆发事件仅触发一次、服务器跳过视觉事件、同步年龄恢复及光照数值有限。
爆发粒子：阶段检查中用事件计数桩；渲染时提取 KL `FireDust`/`FireDust2`/`LineSparkle` 与伊蕾娜 `FireBurstFlameDust` 的正式类（`LineSparkle` 走正式的 Bloom 请求）与 `KLBasicDust` 的正式生成方法，
运行 `FireBurstProj` 的正式 `SpawnBurstEffects`（固定种子），按游戏顺序推进到当前帧，并按原版 `Main.DrawDust` 的状态（AlphaBlend + PointClamp）在弹幕层之后绘制；
`ShockDust` 只生成不绘制。`FireBurstFlameDust.png`、KL `FireDust2_Premultiplied.png` 分别是 KL `Eff_Fire_10`、`FireDust2`（直通 alpha）的预乘版，由 `premultiply_textures.py` 生成，`--check` 校验一致。`particles-sheet.png` 为爆发第 1/3/6/10/15/22/30/40 帧（上夜下昼，第 30 帧后弹幕已消失，只剩残渣粒子）；`impact-f33/f36.png` 为落点附近冲击粒子局部。径向模糊未模拟；背景和地面是预览色块，不模拟独立粒子绘制、
径向模糊、世界光照、碰撞、真实联机或完整游戏绘制生命周期。
这些是 FNA 离线预览图，最终游戏内观感仍需实测。

工具源码使用 `.cs.txt`，临时工程在 `.vissandbox`；两处均由 `build.txt` 排除打包。

## 底部法阵与双面气流条带（2026-11-01）

炎爆前摇时在落点描绘橙红法阵，随爆发淡出，仍复用 `RecallPortalDrawing`。星芒外圈改用多重射击球体起始光圈的 `CircleRingEffect` Shader、`noi_1.png` 和 KL `cellnoise.png`，替换原低分辨率渐变圈贴图。
爆发最初 8 帧在火柱前景叠加预制 `GlowDot.png` 暖色大光晕，随后平滑退去，以遮掩主体刚展开时的贴图轮廓。
此处为原设计意图；2026-11-08 已改用火柱自身峰值 ×3 的 Bloom，正式绘制已移除光斑，见下文。
法阵始终位于 `Projectiles.After` 的 `TrailOrder`，火柱在其上方，原星芒仍在 `OverPlayers.After`。
气流环沿用冲刺效果的开口椭圆思路，两圈相隔 6 tick 从底部扩张、旋转并上升，
每圈是一条绕火柱弯曲的竖直条带，宽度沿世界竖直方向展开，再按俯视角投影。
朝向火柱的内表面为 HDR 橙红，背向火柱的外表面为黑色。
以观察方向固定两个半圆：z<0 为屏幕上半圈/橙色内面，z>0 为屏幕下半圈/黑色外面。
旋转仅改变缺口与纹理相位，条带与固定半圆求交后绘制，内外面的空间分界不会跟着旋转。
上下半圈使用独立顶点和统一面色，颜色不会沿宽度或跨侧缘插值。
两面共享同一贴图遮罩，使用 AlphaBlend；橙色后半圈单独以 `TrailOrder + 1` 提交到 `Projectiles.After` 的 Bloom 请求，早于所有炎爆主体。
这里的多个请求只在同一个捕获目标内排序，法阵、橙色后圈、火柱和火焰粒子共同使用一次 Bloom，并未为后圈增加 Bloom 层。
黑色前半圈单独作为普通请求提交到 `Dust.After`，在火柱、碎石、火焰粒子与 Bloom 都合成完毕后遮挡最终场景。
黑面不再进入火柱的 HDR 目标：过曝 RGB 即使乘上半透明黑面的遮挡仍可能大于 1，后续粒子和 Bloom 也会重新盖亮它。
此前把“内外”误解为条带宽度上的两条色带，现已移除那种径向双色构形。
时间全部读取弹幕的同步年龄；绘制不递增时间，也不依赖当前绘制次数。

游戏调参入口在 `FireBurstProj.DrawAirRings` 方法开头：

| 参数 | 用途 |
| --- | --- |
| `texturePath` | 目前复用正式 `Mounts/Assets/BroomDashWind.png`，后续可换其他已打包贴图 |
| `uvScale` | 当前 `(2, 1)`；X 为沿环重复倍率，Y 为横截面采样倍率，数值越大纹理越密 |
| `uvOffset` | UV 平移；Y 的缩放以横截面中点 0.5 为中心 |
| `maskFromRed` | 0 取 Alpha，适合当前直通 Alpha 气流 PNG；1 取 RGB 最大值，适合灰度遮罩 |
| `innerColor` / `outerColor` | 朝火柱的橙红内表面与背向火柱的黑色外表面，W 为覆盖度；当前 W 均为 1 |
| 两次 `Draw` 的参数 | 起止半径、升高速度、起始角度；第二圈的 `time - 6` 控制错峰 |

环的寿命、俯视投影比例、条带宽度、开口和转速集中在 `FireBurstWindDrawing.Draw`。
条带宽度用 `SmoothStep(28f, 0f, progress)` 在单圈的 24 tick 寿命内平滑收细到零；两圈各按自己的年龄计算，第二圈仍延迟 6 tick。当前固定不透明度的设置保留。
这些是源码调参入口，修改后需使用现有 C# 调试流程或重新构建/加载模组；本工具不提供游戏内热重载。
固定贴图直接请求现有正式资产，不在游戏中生成纹理。

预览直接编译当前正式环几何、圆环 Shader 与回城法阵绘制代码，加载 KL `TexturedRing.xnb`、`VertexDraw.xnb` 和本次编译的 `FireBurstFlame.xnb` 的 `WindRing` 通道。
新增输出：

- `circle-rings-stages.png`：年龄 15 / 33 / 42 / 52，原尺寸底部裁切，上排夜晚、下排白天。
- `rings-day-no-bloom-f36.png` / `rings-day-bloom-f36.png`：只画气流环，验证黑色遮挡及内侧高亮。
- `rings-day-no-bloom-f42.png`：第二圈出现后的独立环效果。
- `ring-inner-face.png` / `ring-outer-face.png`：分开显示橙红内表面、黑色外表面。
- `ring-rotation-depth.png`：固定观察方向下 8 个旋转相位，白色竖条代表火柱；橙色上半圈在其后，黑色下半圈在其前。
- `foreground-hdr-probe-False.png` / `foreground-hdr-probe-True.png`：在粒子顺序之后插入 8 倍 HDR 白柱，验证最终黑面在关闭/开启 Bloom 时都能遮挡。
- `day-zoom125-f40.png`：125% 世界缩放与非零相机位置。

检查还包含每实例每帧一次法阵回调、前摇/爆发的层切换、失效弹幕、环到期、明亮背景下两面像素覆盖。
两面分别渲染时，内面必须只有火光色、外面必须只有黑色遮挡；旧的径向双色实现无法通过此回归。
固定半圈检查覆盖 16 个旋转相位 × 4 个年龄 × 2 个面，逐像素检查空间半圈和颜色。
HDR 遮挡回归在修复前实测失败：后绘高亮白柱穿过黑面，遮挡像素为 0；分离前景请求后通过。
检查登记层、每实例前后圈回调次数与多炎爆叠放，避免按单个弹幕交错绘制前后圈。
按原版完整调用边界回放：`OverPlayers` 在 `DrawDust` 之前；`DrawDust` 完成 Begin/End 后，再执行普通 `Dust.After` 请求。
当前 KL 在每个请求前确保活动批次，并由正式 `FlushLayerRequests` 包装保存/恢复外部批次状态；前景回调结束自身批次。预览提取该正式包装，检查整个 Dust.After 层结束后批次关闭、后续请求可以正常 Begin。
此前预览错误地在原版粒子的活动批次内执行 `Dust.After`，遗漏了真实游戏的崩溃。
改正预览后，旧实现已复现 `End was called, but Begin has not yet been called`；补齐前景批次所有权后通过。
同类调用点 `WaterLaserDust` 和 `ElainaTrailVisualStyle` 已自行配对 Begin/End，本次未修改公共绘制管线。
已查看本次生成的明暗背景、多阶段和独立气流环；正式 C# 编译及 FNA 检查通过。
没有在运行中的 Terraria 内验证材质、UV 或实际场景遮挡，后续仍由游戏内调试定稿。

## 爆点光晕修复与 GIF 对照（2026-11-08）

原 `1 - SmoothStep(0, 8, time - 1)` 把 tick 当成归一化进度，爆发第 1 tick 强度为 1，第 2 tick 起变为 -7，实际只画一帧。
修复阶段曾用 `CheckImpactGlow` 从正式绘制捕获提交强度，对照白天/夜晚、有/无光晕的逐帧图；旧实现实测无法通过 8 帧连续淡出断言。选定火柱增亮后，该检查改为下文的 `CheckBodyGlow`。
修复为 `SmoothStep(1, 0, (time - 1) / 8)`，第 1～8 tick 的强度依次为 1、0.9570、0.8438、0.6836、0.5、0.3164、0.1562、0.0430，第 9 tick 归零。
RGB 与 alpha 同步衰减，防止低亮度末段仍保留高覆盖度。

原光斑的 128×128 预乘贴图放大至约 922×1075 世界像素，中心在爆点上方 150 像素；它是固定椭圆覆盖，并不匹配火柱轮廓。
贴图对照方案改为约 461×538、中心上移 100 像素，收束在根部，保留火柱上半部本身的轮廓。仍加载正式 `GlowDot`，没有运行时生成固定纹理；该方案现仅保留在预览工具中。

三列依次为缩小后的历史贴图光晕、无光斑且火柱峰值 ×2、无光斑且峰值 ×3。用户已选定右列，正式 `DrawVisual` 直接给 `BandOuter/Mid/Inner` 的 RGB 乘以 `1 + 2 * SmoothStep(1, 0, (time - 1) / 8)`，alpha 不变；第 9 tick 起倍率为 1，不增强碎石、粒子或全局 Bloom。
正式 `DrawImpactGlow` 及其调用已删除。普通检查直接抽取正式色带增亮代码，不注入额外倍率；`CheckBodyGlow` 读取实际写入 Shader 的三段颜色，覆盖白天/夜晚爆发前 10 tick 的 RGB 倍率与 alpha。
显式 `-GlowComparison` 模式才替换峰值参数并启用工具内的历史光斑夹具。三列使用相同随机种子、阶段、正式 Shader、当前 KL Bloom 配置和批次包装，避免正式代码已乘 ×3 后预览再次叠乘。

```powershell
& ./Tools/FireBurstPreview/run.ps1 -SkipShader
& ./Tools/FireBurstPreview/run.ps1 -SkipShader -GlowComparison
python Tools/FireBurstPreview/glow_comparison.py
```

- `body-glow-day/night-t01` 至 `t10`：正式 ×3 方案的前 10 tick 原生帧；旧的 `impact-*.png` 属于贴图修复阶段留档。
- `glow-comparison/{day,night}/{texture,body2,body3}/f01.png` 至 `f78.png`：完整 FNA 原始帧；年龄 60 起停止弹幕绘制，继续模拟残余粒子。
- `glow-comparison-day/night-normal.gif`：三列同步、正常 60 tick/s，GIF 以交替 10/20 ms 延时保持总时长。
- `glow-comparison-day/night-slow.gif`：前摇末段至爆发第 15 tick，四分之一速度。
- `glow-comparison-day/night-sheet.png`：爆发第 1、3、6 tick 的静态对照。

GIF 将原生帧裁切缩小用于并排比较，原尺寸 PNG 可单独检查细节。背景与地面仍为色块，不能代替真实地图光照和游戏内观感。
选定 ×3 后，普通 FNA 套件及正式 C# Compile 通过；白天/夜晚爆发前 10 tick 共 20 张正式输出与用户选中的 ×3 候选逐像素一致。
已尝试完整构建，C# 构建成功，但打包被运行中的 tModLoader 占用阻止（TML003）；尚未重载模组或做游戏内验证。

## 炎爆领域范围圈（`-Domain`）

```powershell
& ./Tools/FireBurstPreview/run.ps1 -Domain
```

按 1920×1080 渲染（领域半径 `DomainRadius` 须与 `FireBurstDomain.Radius` 一致，当前 1400），场景为地面、敌人剪影与玩家位置色块；领域中心降一次正式炎爆，之后每 4 帧锁定一名敌人、同一敌人每 75 帧重锁，与 `FireBurstDomain` 调度一致。炎爆绘制、粒子与 Bloom 均为正式代码，粒子按各炎爆自己的年龄推进（只模拟当前一轮）。

范围圈为正式 `FireBurstDomainRingDrawing.cs` + `FireBurstFlame.fx` 的 `DomainRing` 通道，`DomainRingAdapter.cs.txt` 等同 `FireBurstDomain.DrawRing`。只显示领域边界：沿半径 1000 的圆燃烧一圈火环，火舌沿半径向外舔出。着色同火柱（火势 = 包络 × 噪声，0.06/0.15/0.42 三阈值 暗红 → 红 → 黄，黄层由 Bloom 晕出白芯）；噪声为可平铺的 `PerlinX`，沿火舌方向拉长并向外流动，根部连续燃烧，噪声决定火舌长短。展开 24 帧内火环从中心扩到边界，火舌 30 帧内长到全长；结束前 40 帧火势减弱熄灭。可调参数在 `FireBurstDomainRingDrawing.Draw` 开头。

定稿过程（2026-11-08）：否决显迹尘式火星飞散（粉尘外观来自"尘"本身）与地面大法阵；火环比较了火舌向外 / 向上，用户选向外，并从两档细淡程度中选定"细一档"（火舌 80 像素、根部 0.6、原亮度；更淡一档为 60 像素、0.5、亮度 ×0.7）。不要用 `Eff_Trail_2014` 沿圆周平铺：它的横向是单条流纹截面，会断成一段段火块。

输出 `out/domain/`：`Ring-spread-sequence.png`（6/14/24/40/80/130 帧）、`Ring-steady-{night,day}-{center,offset,high}.png`（offset 为玩家右移 半径−500 像素，可见右侧边界；high 再上移到可见顶部弧）、`Ring-ending-night-offset.png`、`Ring-domain.gif`（0～240 帧）。

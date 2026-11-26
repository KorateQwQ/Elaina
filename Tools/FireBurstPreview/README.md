# 炎爆、法阵与气流环 FNA 离线预览

## 施法 NonPremultiplied 调整（2026-11-10）

施法火核、星芒、轨道环、聚拢火星、落地爆燃与散开星光改为 NonPremultiplied。此前 AlphaBlend 配合零/低覆盖度及高 HDR 产生加色叠加，亮背景会爆白。新增正式 `Effects/Content/FireBurstCast.fx/.xnb`，对现有预乘 PNG 先采样、再还原直通 RGB；贴图自身灰黑亮度转为覆盖度，避免线状火星留下暗边。淡入淡出仅改变 Alpha，降低火核及火星倍率，保留同层 Bloom。魔杖拖尾通过可选 `blendState: 2` 接入，公共样式默认值不变。固定纹理继续加载已有打包资源，无运行时贴图生成。

重建 Shader 与预览：`pwsh -File Tools/FireBurstPreview/run.ps1 -Cast`；仅重跑预览加 `-SkipShader`。曝光检查覆盖昼夜背景与 1×/1.25×/2×，第 30 设计帧火核区域须保留足够橙黄色像素、不能大面积近白。旧版 2× 夜景有 6046 个近白像素，超过上限 480；修复后六种组合均为 0。保留压缩后转白及爆燃白闪，不用该曝光阈值限制它们。原有交接、拖尾与径向星光回归一并执行。

已查看本次 FNA 离线昼夜时序、压缩/坠落/爆燃及尾部淡出；C# 编译和 Shader 编译通过。输出 `out/cast/exposure-day-night.png`（左昼右夜）及 `cast.gif`。尚未进行游戏内重载验证或 `.tmod` 打包。

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

按 1920×1080 渲染（领域半径从 `FireBurstDomain.Radius` 抽取），场景为地面、敌人剪影与玩家位置色块；领域展开时中心不再降下炎爆（由施法爆燃标示位置，见 `-Cast`），每 4 帧锁定一名敌人、同一敌人每 75 帧重锁。炎爆绘制、粒子与 Bloom 均为正式代码，粒子按各炎爆自己的年龄推进（只模拟当前一轮）。

范围圈为正式 `FireBurstDomainRingDrawing.cs` + `FireBurstFlame.fx` 的 `DomainRing` 通道，`DomainRingAdapter.cs.txt` 等同 `FireBurstDomain.DrawRing`。只显示领域边界：沿半径 1000 的圆燃烧一圈火环，火舌沿半径向外舔出。着色同火柱（火势 = 包络 × 噪声，0.06/0.15/0.42 三阈值 暗红 → 红 → 黄，黄层由 Bloom 晕出白芯）；噪声为可平铺的 `PerlinX`，沿火舌方向拉长并向外流动，根部连续燃烧，噪声决定火舌长短。展开 24 帧内火环从中心扩到边界，火舌 30 帧内长到全长；结束前 40 帧火势减弱熄灭。可调参数在 `FireBurstDomainRingDrawing.Draw` 开头。

定稿过程（2026-11-08）：否决显迹尘式火星飞散（粉尘外观来自"尘"本身）与地面大法阵；火环比较了火舌向外 / 向上，用户选向外，并从两档细淡程度中选定"细一档"（火舌 80 像素、根部 0.6、原亮度；更淡一档为 60 像素、0.5、亮度 ×0.7）。不要用 `Eff_Trail_2014` 沿圆周平铺：它的横向是单条流纹截面，会断成一段段火块。

输出 `out/domain/`：`Ring-spread-sequence.png`（6/14/24/40/80/130 帧）、`Ring-steady-{night,day}-{center,offset,high}.png`（offset 为玩家右移 半径−500 像素，可见右侧边界；high 再上移到可见顶部弧）、`Ring-ending-night-offset.png`、`Ring-domain.gif`（0～240 帧）。

## 炎爆领域施法演出（`-Cast`）

```powershell
& ./Tools/FireBurstPreview/run.ps1 -SkipShader -Cast
```

方案见 `Tools/FireBurstCastPreview`（HTML 方案稿，用户选定方案 C）。正式实现：

- `FireBurstCastVisuals.cs`：只依赖 XNA 的时间轴、火种运动与全部绘制（杖尖聚火、天空火种与双火环、坠落拖尾、爆燃的白闪/冲击波/余热/径向星光），预览直接编译此文件。
- `Action_FireBurst.cs`：手臂姿势与时间推进，在 `Timeline.Impact` 帧各端生成爆燃、拥有者生成领域；`AnimationSpeed` 缩放整段动作（总帧数随动作同步）。
  - 落点：火种悬停点、落点与领域中心在爆燃前每帧跟随玩家（落点在玩家脚下，不贴地面）；坠落时天空点不锁定，整条火种轨迹以玩家为参考系（2026-11-11 起，见文末）；`Timeline.Impact` 那一帧玩家所在位置即领域中心。
  - 魔杖拖尾（`BuildWandTrail`）：不用基类的世界坐标历史，按 `ArmAngle` 回溯 `WandTrailTicks` 设计帧、以当前肩部为圆心重建（返回相对肩部的偏移）；玩家移动时弧线不变形。按时间等距密集采样（96 点），起手只覆盖已经发生的时间，点序号表示相对年龄；宽度取年龄衰减与尾端剩余弧长收尖的较小值，透明度仍按年龄衰减，挥过的部分逐渐消失；不能按弧长重采样，否则整条弧挥完后保持原样、到窗口末尾才突然消失。手臂停住时的重合点沿轨迹错开 0.001 像素，避免法线无效。`--cast` 同时检查挥杖后挥过的那段随时间移向序列尾部。指向火种的角度必须以玩家当前中心为参照：爆燃后 `center` 不再更新，用它会让回溯的挥杖在爆燃后移动时转向。`--cast` 开头的 `CheckWandTrailFollowsPlayer` 检查这一点（旧实现偏差约 90 像素，检查失败）。
  - 火种拖尾：`SeedTrailPath` 沿轨迹回溯（升空 4、坠落 3 设计帧），`DrawTaperTrail` 按弧长盖柔光点、从头到尾收细；不再用整根等宽的线条贴图。
- `FireBurstImpactDust.cs`：一颗粒子承载一次爆燃，十字星和点状星光从爆燃时锁定的领域圆心向四周散开。
- `FireBurstDomain.cs`：不再在中心降下第一道炎爆；快速施放时（`PlayImpact`）由领域自己播放爆燃。

`CastAdapter.cs.txt` 等同 `Action_FireBurst.SubmitExtraVisuals` 与 `FireBurstImpactDust` 的绘制请求；手臂姿势（`ArmAngle` 等）与领域半径由 `Preview.cs` 从正式源码抽取。玩家视为静止、面朝右，`ArmCenter` 省略了手臂贴图偏移；玩家与魔杖是色块和贴图示意。魔杖拖尾直接提取正式 `BuildWandTrail`、`BuildWandTrailWidths`、`ElainaTrailVisualStyle` 与 KL `TrailEffect`，加载 `LightTrail.png`、`VertexDraw.xnb`，通过正式 Bloom 队列绘制。

输出 `out/cast/`：`sequence-near-{night,day}.png`（缩放 2，4/10/14/18/24/30/36/39/41/43/44/46 帧）、`sequence-full-night.png`（缩放 1，40～120 帧）、原尺寸关键帧 `near-*-f30/f41/f45.png`、Bloom 对照 `near-night-f30-no-bloom.png`、`cast.gif`（0～110 帧，缩放 1.5）。

调参记录（2026-11-09）：火环与冲击波起初用 GlowDot 竖切片拼平头线段，相接处要么叠出横向亮痕、要么在椭圆两端露出暗点，改为沿弧长等距盖柔光点；爆燃白闪原 3 帧冲顶、10 帧退去在白天吞掉整个法阵，缩短为 2/6 帧并降低倍率；坠落拖尾减细减短，避免像光柱。

火核交接（2026-11-09）：火种原先固定从 12 像素起步、升空期间不变，而杖尖火核离杖前约 17.5 像素（`WandCoreSize` 的脉动正处波谷），离杖那一帧缩小约 31%，同时杖尖施法光效从 0.4 倍尺寸直接出现，像火核原地缩小。现 `SeedSize` 从 `WandCoreSize(Launch)` 接续并在 Launch～Compress 平滑长到最大值，施法光效的尺寸与亮度从零淡入（4 设计帧）。`CheckSeedHandover` 按动画速度 0.5 的半帧步长检查 Launch～Fall 间火种尺寸的单步变化不超过 10%（旧实现交接帧 31%）。`sequence-handover-night.png` 为第 9～22 帧的交接序列。


## 魔杖拖尾收尖修复（2026-11-10）

起手不足 14 设计帧时，旧采样把超出零帧的历史全部截到起点，末段收细挤在约 0.1 像素以内，形成平截口。现用 `min(t, WandTrailTicks)` 铺满有效历史。悬停后下挥也有重复尾点，因此 `BuildWandTrailWidths` 在年龄衰减之外增加最后 12 世界像素的线性收尖上限，末点半宽严格归零；点序列仍按时间采样，没有改为按弧长重采样。

KL `TrailEffect` 与 `ElainaTrailVisualStyle.DrawTrail` 增加逐点半宽重载，保留原有重载签名与默认行为；本次仅炎爆启用。固定资源仍使用既有 `LightTrail` 和 `VertexDraw`，没有新增运行时纹理生成。

验证入口仍为 `pwsh -File Tools/FireBurstPreview/run.ps1 -Cast -SkipShader`。新增起手半帧步长、左右朝向、向下瞄准和悬停后下挥的检查；原有随玩家移动及随年龄衰减的回归仍运行。预览现使用真实拖尾 Shader，不再用细折线代替。输出 `cast/wand-tail-detail.png`（左起手、右下挥）和 `cast/wand-tail-scales.png`（上昼下夜，列为 1、1.25、2 倍世界缩放），均为原生像素裁切。

本次 FNA 离线预览与 KL、伊蕾娜完整 C# 编译通过；尚未进行游戏内重载与实战验证，使用时需要同步重新构建并加载两个模组。


## 施法法阵移除与结尾径向星光（2026-11-10）

按用户要求移除 `Action_FireBurst` 的准备阶段法阵请求，以及 `FireBurstImpactDust` 在爆燃后的法阵外涨、淡出和对应绘制方法。天空火种与轨道环、领域范围火环仍沿用现有绘制；本节替代上述施法方案中有关地面法阵的历史说明。

结尾原有贴地线状火花、上升余烬替换为同一批径向星光：24 颗十字星与 48 颗点状星光，从爆燃时锁定的领域圆心出发，在完整圆周内分扇区随机取方向，速度为每帧 5～14 像素并按 0.965 阻尼减速，寿命为 38～76 帧。粒子保持径向，缩小并淡出；不受重力或上飘逻辑影响。使用已打包的 `Star4`、`GlowDot` 贴图。

`--cast` 直接绘制正式 `DrawImpactParticles`，新增 `CheckImpactParticles` 覆盖 8 个随机种子的同一发射圆心、两种星光的四象限分布、径向外移与生命周期。输出 `impact-stars-{night,day}.png` 为完整结尾，`impact-particles-{night,day}-f48/f59/f79.png` 为关闭闪光、冲击波和余热后的粒子检查，`impact-stars-detail.png` 为完整结尾的原生像素裁切。已查看明暗背景和起手无地面法阵的 FNA 离线预览；相关回归与伊蕾娜完整 C# 编译通过，尚未在运行中的游戏内验证。

## 扩散初期的火圈 Bloom（2026-11-10）

扩散和常驻原本使用同一 `RequestBloom` 请求及 `DomainRing` 通道。初燃时 `RingStrength = age / 10` 降低火势，着色落在橙红层；外层 HDR 色 `(2, 0.46, 0.06)` 的感知亮度为 0.878，低于 KL `DrawOverflowColor` 的阈值 1。红通道超过 1 不代表能触发 Bloom，因而会出现亮边清晰但没有光晕的早期帧。

`FireBurstDomainRingDrawing.Draw` 现在仅在前 10 帧把 RGB 乘上从 1.6 平滑降到 1 的初燃补偿，覆盖度、火势包络、扩散半径和火舌长度不变。第 10 帧后恢复原有颜色参数，不调整公共 Bloom 或 Shader。

验证：

```powershell
& ./Tools/FireBurstPreview/run.ps1 -RingBloom -SkipShader
```

该入口只绘制正式领域火环，排除爆燃白闪、冲击波、粒子与后续炎爆的贡献。检查年龄 2/4/6/10/12/20/24/40/300、昼夜背景、1×/1.25× 世界缩放，以及结束后的不可见性。每帧对比 Bloom 开关，沿环内侧 24 世界像素取 41 个样本，要求至少 30 个样本有可见光晕、平均红通道增量不少于 5。

修复前第 2 帧为 0/41，关闭补偿的旧实现会使新增检查失败；修复后四种背景与缩放组合为 40～41/41。年龄 12/20/24/40/300 的全部开关对照与修复前逐像素一致。已查看实际 FNA 明暗背景与早期/常驻局部图，完整 C# 编译通过，尚未游戏内重载验证。

输出 `out/ring-bloom/bloom-off-on.png`；本次留存的 `before-after.png` 左为修复前、右为修复后。原生像素比较脚本：`python Tools/FireBurstPreview/compose_ring_bloom.py <out> [修复前截图目录]`，指定修复前目录时同时验证初燃结束后的逐像素一致性。


## 2026-11-11 删除脚底横向余热

删除正式 `FireBurstCastVisuals.DrawImpact` 中两层横向拉伸的 GlowDot（420×46、220×22）及对应余热参数。此前移除地面法阵并未删除这两层，截图中的橙色长光带来自这里。上文余热描述保留为历史记录。

运行 `pwsh -File Tools/FireBurstPreview/run.ps1 -ImpactOnly -SkipShader`，直接以当前正式 DrawImpact 和 KL Bloom 绘制爆燃第 44/46/50/56/64 设计帧的昼夜预览，输出 `out/cast/impact-only-{day,night}-f*.png`；已目视确认脚底橙色长光带消失。该入口仅用于爆燃区域视觉核对，不代表完整施法回归通过。脚本复制源码后刷新时间戳，避免本机改过预览后因增量构建复用旧程序。

伊蕾娜 C# 编译通过（0 错误）；未进行游戏内重载验证。

## 火种离杖闪到天空（绘制比 Update 快一帧）

KL `ActionModPlayer.PostUpdate` 先以第 N 帧调用 `Update`，再推进帧号；绘制（`SubmitExtraVisuals`）取到的是 N+1。绘制时间第一次到达 `Launch` 时，`Update` 还没记录离杖点，旧 `Seed(t)` 以 `launched` 标记判断，返回天空悬停点：这一帧火种出现在约 170 像素外，下一帧回到杖尖再正常升空。现 `Seed(t)` 按时间判断离杖，离杖点未记录时用当前杖尖；同理绘制时间先到 `Impact`、爆燃尚未生成的那一帧，火种停在落点多画一帧而不是留空。

预览改为与游戏相同的顺序：绘制设计帧 t 时只 `Advance` 到 t − 0.5（`AdvanceLikeGame`）。`CheckLaunchTicks` 逐游戏帧检查 Launch − 3 ～ Arrive、三种朝向下火种每帧位移不超过 30 像素；临时恢复旧 `Seed` 时该检查以 172 像素跳变失败。逐帧查看：`run.ps1 -SkipShader -LaunchTicks`，输出 `out/cast/sequence-launch-ticks-night.png`。

2026-11-11：`-Cast` 中除曝光检查外全部通过。曝光检查（第 30 帧、夜景、缩放 1，近白 571 > 120）在恢复旧推进顺序后同样失败，与本次修复无关，尚未处理。尚未在游戏内重载验证。

## 火种轨迹以玩家为参考系

旧实现在 `Timeline.Fall` 把天空点锁定在世界坐标、落点继续跟随玩家，玩家移动时坠落轨迹会歪斜。现 `Sky()` 始终为玩家中心 + `skyOffset`，离杖点、悬停点、落点都相对玩家，移动时整条轨迹平移、形状不变；火种拖尾按同一函数回溯，也随玩家平移。

`CheckSeedFollowsPlayer`（`-Cast` 与 `-LaunchTicks` 都会运行）：两种朝向 × 水平/斜向匀速移动，离杖到爆燃前每个游戏帧火种相对玩家的位置须与静止施放一致（误差 ≤ 0.01 像素）。临时恢复锁定写法时该检查以第 42 帧偏离 39.4 像素失败。适配器 `MoveOwner` 对应 `TrackOwner`。

2026-11-11：`-Cast` 中除既有的曝光检查外全部通过；伊蕾娜 C# 编译无错误。尚未在游戏内重载验证。

## 2026-11-18：风圈冷启动随机过曝

共用 `WindRing` 的旧 `MaskFromRed = 0` 会触发 D3D11 后端全零常量首次未上传的问题，
残留 GPU 值放大遮罩，引起橙色过曝、深色前半圈黄黑夹杂；冰锥同样受影响。
现改用总和为 1 的 `WindMaskWeights`，两处调用与正式 XNB 已同步，原配色和 Bloom 参数未调整。
确定性故障注入、旧实现失败数据和运行入口见 `Tools/IceConeWindPreview/README.md` 的冷启动章节。
本工具普通入口（`-SkipShader`）完整预览及回归通过；正常冰/火风圈与旧正常基线逐像素一致。
正式 C# 编译通过，完整打包被游戏占用阻止（TML003），仍需在模组重建与重载后验证游戏表现。

## 2026-11-18：魔杖拖尾透明度与收尾

正式动作与 CastAdapter 的杖尾统一使用预乘 Alpha 混合，整体透明度与尾部透明度各作用一次。抽取器同步读取正式 `SmoothOpacity` / `DrawTrailOpacity`；保持既有姿势回溯及逐点宽度。

新增独立检查入口 `run.ps1 -SkipShader -WandTrail`：运行移动跟随、起手尾尖、停杖收尖检查，并导出第 1/4/10/18/36/40/44 设计帧的昼夜、1/1.25/2 倍缩放，共 42 张隐藏 FNA 图片（`out/cast/trail-check-*.png`）。本次全部通过并查看原尺寸局部图。

完整 `-Cast` 的既有火核曝光断言仍失败（夜景 1 倍、571 > 120），没有跳过或放宽该入口的断言。杖尾专项通过不能代替完整施法回归通过。通用拖尾的透明度、采样与动作中断回归见 `../ActionTrailPreview/README.md`。

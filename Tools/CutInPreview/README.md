# 奥义特写（Cut-In）

施放炎爆领域时，本地玩家屏幕上出现一条斜向特写带，伊蕾娜转头抬眼、举杖，挥杖下指时切到杖头指向镜头的定格，随后收拢退场。

## 文件

| 用途 | 位置 |
| --- | --- |
| 时间轴与播放状态（绘制阶段独立时钟 `ConstellationUIClock`，暂停时不推进） | `ElainaModSkills/CutIn/ElainaCutIn.cs` |
| 绘制（只依赖 FNA，可离线链接） | `ElainaModSkills/CutIn/ElainaCutInDrawing.cs` |
| 游戏接入：贴图加载、界面层、玩家位置锁定 | `ElainaModSkills/CutIn/ElainaCutInSystem.cs` |
| 触发：施法开始时 `ElainaCutIn.Play()`（只此一处，时长固定） | `ElainaActions/Action_FireBurst.cs` |
| 人像帧（预乘、四边渐隐，按源帧号 CutIn_01..21，960×540；22-28 与 21 相同已去重） | `ElainaModSkills/CutIn/Frames/` |
| 原始抠图帧（不打包，`build.txt` buildIgnore） | `ElainaModSkills/CutIn/png/frame-001..028.png` |
| 帧烘焙与校验 | `Tools/CutInBake/bake.py`、`Tools/CutInBake/manifest.json` |
| HTML 方案稿（构图、节奏对比，非验收依据） | `Tools/CutInPreview/index.html`，launch.json「奥义特写预览」，打开 `/Tools/CutInPreview/` |
| FNA 离线预览（正式时间轴 + 正式绘制） | `Tools/CutInPreview/run.ps1`、`Preview.cs.txt` |

## 设计要点

- **不铺全屏**：源帧只有 960×540，全屏铺满在 1440p 要放大 2.67 倍、明显发糊。人像放大倍率 = `BaseScale`（1.3）× `Main.UIScale`，带宽 = 300 源像素 × 倍率。
- **避开玩家**：玩家在屏幕中心，带子左低右高（13°），下沿放在玩家头顶上方 `PlayerClearance × UI 缩放` 处；位置在每次播放的第一次绘制时锁定。
- **最上层**：`ModifyInterfaceLayers` 插在 `Vanilla: Cursor` 之前，高于全部界面，鼠标指针仍在最上面。KL `LayerDrawRequestSystem` 的层都在世界绘制阶段（最高 `OverWiresUI`/`InfernoRings`），没有界面之上的请求层，所以直接插入界面层。界面层没有 KL Bloom，光晕用顶点色柔边近似。
- **帧选择**（2026-11-12 起）：源帧已全部修成闭口，按 `ElainaCutIn.Frames` 顺序播放 1-21：1-5 转头抬眼、6-13 举杖、14 挥杖指向镜头（`StrikeFrame`，冲击）、15-20 收势、21 定格。源帧 22-28 与 21 逐像素相同，烘焙时只输出一次。
- **人像锚点**：以眼睛为准。带宽 `BandSourceWidth` 200 时带内只有锚点上下各 100 源像素，放不下杖头：冲击前锚点 (470, 180)（1-13 帧眼睛 y 108-212），冲击后 (470, 240)（14-21 帧眼睛 y 184-238），切换在冲击闪白下。杖头星芒超出带子时沿带法向收进带内。改带宽或倾角需重新核对眼睛是否留在带内。
- **固定时长**（2026-11-12 用户要求，不再用动作限定动画）：施法开始时播放，前段播完立即冲击，冲击后 `StrikeHold` 开始退场，退场 `ExitDuration`；与炎爆时间轴无关。节奏参数全部在 `ElainaCutIn.cs` 顶部（`IntroStart`、`IntroFrames`、`StrikeFrameDuration`、`StrikeHold`、`ExitDuration`）。
- **贴图**：人像帧为离线烘焙的预乘 PNG（tML 打包不再预乘）；星芒、细圆环、带边亮线复用 `UICore/Assets/StarFlare`、`ThinRing`、`Recall/Assets/PortalLine`、`Effects/Tex/Baked/WhitePixel`。渐变底、速度线、光晕都用顶点色绘制，运行时不生成任何贴图。
- **计时与暂停**：每次绘制从 `ConstellationUIClock.Sample()` 取真实间隔累加，不读 `GameTime`、`Main.gameTimeCache` 或游戏 tick；`Main.gamePaused` 时只消耗采样不累计，画面定格；失焦、F11 隐藏界面、回到菜单由时钟采样作废排除。绘制结果只取决于传入的时间。

## 验证

```powershell
python Tools/CutInBake/bake.py --check
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/CutInPreview/run.ps1
```

`run.ps1` 先跑时间轴与计时检查（各帧段、冲击时刻、真实间隔推进、暂停不推进不结束、零间隔与重复缓存帧、极小间隔无最小值、负间隔忽略、固定时长结束、暂停交替），再用正式绘制代码在 1920×1080（UI 1.0）、2560×1440（UI 1.25）、3840×2160（UI 1.5）各输出 9 个时刻的原生尺寸 PNG 到 `.vissandbox/cut-in-preview/`。背景为游戏截图，玩家头顶按屏幕中心估算。

2026-11-11：烘焙校验 12 帧通过；时间轴检查 6 项通过；已查看三种分辨率的分镜与 1440p 原生裁切。C# 编译无错误（游戏运行中，打包被 TML003 拦截）。尚未在游戏内验证：真实界面层叠、镜头缩放下的玩家位置、异步加载首帧、联机下只在施法者本地播放。

2026-11-12：改为固定时长并在暂停时定格；时间轴与计时检查 11 项通过，C# 编译无错误。尚未在游戏内验证暂停菜单下的定格。


2026-11-12（全部帧）：烘焙 28 帧去重为 21 张（10 MB PNG，约 43 MB 显存）；时间轴检查 13 项通过（含按序播放全部列出帧）；已查看 1440p 各时刻眼睛均在带内。C# 编译无错误，尚未在游戏内验证。
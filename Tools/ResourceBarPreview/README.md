# 花蔓资源条

`bars.html` 是用户确认的视觉来源。正式入口是 `ElainaAttribute/ElainaPlayerResourceDisplaySet.cs`，
共享绘制与状态分别在 `ResourceBarDrawing.cs`、`ResourceBarState.cs`。
采用 A 款：生命条 262×8、魔力条 262×7，顶部 y=18/46，右侧预留 36 UI 像素。
生命花蔓在右，魔力花蔓在左；无前沿大星光。保留普通魔力 / 独立魔力读取；悬停不显示读数，数值改由设置开关在条上显示。

## 头像 · 等级 · 经验

`bars.html` 的“A+ 头像 · 等级”卡片是确认稿，默认等级样式为花饰（银胶囊 / 星芒 / 星徽仅作对比，未接入游戏）。
正式绘制为 `ElainaAttribute/PortraitHudDrawing.cs`，等级动画与升级星屑为 `PortraitHudState.cs`，同样被 FNA 预览直接链接。

头像源图为本目录 `PortraitSource.png`（用户提供的 100×100 透明 PNG），经 `bake.cjs` 生成正式 `PortraitBase` 及各缩放版本。
源图已带菱形透明裁切，完整 100×100 边界映射到头像区域 50×50；不再额外放大 1.24 倍或上移，保留用户提供的帽子、脸和衣领构图。
底座镂空：头像透明区域和外圈留白（经验带外沿到白色外线）只铺 35% 暗纱 `rgba(12,9,20,.35)`，透出游戏画面；经验带底下的深色槽、内线和槽色保持原样。暗角按头像自身 alpha 叠加，只压在人物上，不压暗透出的背景。比较稿见 `hollow-portrait-study.png`（第三行）。

- 头像中心位于生命条原点 (-70, 40)，整组绘制放大 1.4 倍，菱形外线显示半径 44.24（素材坐标 31.6）；由内到外为头像、深色内线、经验带、留白、白色外线与顶点小星。
- 静态层 `PortraitBase` / `PortraitRim`、完整经验带 `PortraitXp`（含 `Glow` / `Capped` / `Mask`）、低血量 `PortraitPulse`、
  前端 `PortraitGlint`、花饰 `LevelFlourish` 及其暗底 `LevelFlourishShadow` 都由 `bake.cjs` 调用 HTML 中同名函数烘焙；局部尺寸记录在 manifest 的 `box`。
- 经验两侧同高上升，运行时按进度水平裁切经验带纹理的源矩形即可得到两条臂，不需要额外 Shader；HTML 采用同一裁切。
- 等级数字使用由 KL Gelasio 母版预制的 `FontRaster/Gelasio` 字形（素材坐标 12px，显示 16.8 UI 像素），居中挂在菱形底尖：数字中心位于底尖下方 2 像素（局部锚点 (0, 23.6)，加 `LevelY` 10），只压住白色外线，经验带起点仍露在数字上方；两侧花饰缩至 65%。位置对比见 `level-position-study.png`（A 方案）。HTML 加载 `.vissandbox/gelasio-source/Gelasio.ttf`，
  其来源与 SHA-256 见 `KL/Fonts/Gelasio.source.json`。数据来自 `ElainaStatePlayer`（等级、经验、升级所需、Boss 进度上限）。
- 等级字色为银白色 `#eee9f5`，满级且暂存经验已满时为灰银 `#cfc8da`。数字使用四向各一个屏幕像素的轻暗边（不透明度 .3，色 `#0e0a1a`），保持跨过白色外线时可读。两侧花饰下另有贴着花纹形状的暗底 `LevelFlourishShadow`（剪影向 8 方向扩 0.9、模糊 1.6，`rgba(18,12,32,.45)`，花纹局部单位），白天浅色背景上托住细线，夜晚几乎不可见；比较稿见 `flourish-shadow-study.png`（中档）。等级花饰使用 0.85 素材像素的卷须和简化四瓣花，按 UI 缩放、花饰自身缩放及升级动画的最终尺寸选择预过滤贴图，减少细线发虚。
- 获得经验时进度缓动，可连续跨级；每次升级经验带闪白、等级放大并散出 22 颗星屑（上限 66）。
  暂存经验还可增长时，经验带保留紫色。生命显示值低于 25% 时经验槽泛蔷薇色脉冲。当前正式实现不提供资源条或头像悬停提示。

## 充能点

在角色拥有端通过 `player.GetModPlayer<ChargePointPlayer>().Points` 访问。`Current` 返回当前点数，`Has(n)` 查询是否充足，`Restore(n)` 返回实际恢复量，`TrySpend(n)` 只在能足额支付时扣除。角色保存当前点数和未攒满一格的进度，新角色从 0 点开始，上限 8 点。在平衡编辑器设置技能的 `energyCost`，普通与快捷施法均由公共入口结算。`OnCast` 返回 `true` 表示成功出手，返回 `false` 表示取消或失败。

`ChargePointPlayer.RecoveryPerSecond` 是伊蕾娜 RPG 属性组件中的每秒充能点恢复量。基础值由 `BaseRecoveryPerSecond` 提供。装备或 Buff 可在效果更新时调用 `player.GetModPlayer<ElainaAttributeModPlayer>().Attributes.AddExtraPercent(ChargePointPlayer.RecoveryPerSecond, .5f)`，使恢复速度增加基础值的 50%。

正式 HUD 复用已打包的 `Star.png`，星与星之间不画连线。`run.ps1` 用正式绘制代码生成 `charge-cycle.png`，四行依次为五点、消耗中、剩余两点、恢复中。

动态辉光（2026-11-25）：已点亮的星上层叠加 `StarGlow.png`（64×64，star4 形状两档高斯模糊加淡圆晕，预乘 `(a,a,a,a)`），由 `Tools/FixedTextureBake/Bake.cs.txt` 的 `StarGlow()` 生成并登记在 `Effects/Tex/Baked/manifest.json`；以顶点 alpha 0 叠加，非后处理 Bloom；所有星画完后统一叠在最上层，垫在星芯下面会被实心星芯盖住，呼吸几乎看不出。辉光按槽位错相缓慢呼吸，满 8 点略亮；不做周期扫光（用户确认只要呼吸感）。获得/消耗时辉光随动画放大后收回。下一颗空星里显示恢复进度的微光（`ChargePoints.RecoveryFraction`），攒满时接上获得动画。时间累计来自 `ConstellationUIClock`，暂停和失焦时随 HUD 重置一起冻结。参数均为 `ChargePointHud` 中的表达式属性。

`run.ps1 -ChargeOnly` 只跑充能点检查与辉光预览：`output/charge-glow/frame-*.png`（30fps 时间线：六点凝聚第七点 → 获得 → 消耗三点，上半夜晚、下半白天，2×），以及 `charge-glow-sheet.png`（1× / 1.5× / 2×、小数原点，满点与五点 + 60% 恢复）。

## 资源与动态绘制

- `bake.cjs` 直接调用 HTML 的 `silverFrame` / `FRAME.rail.under`，生成正式目录
  `ElainaAttribute/ResourceBarAssets` 的银框、花蔓和底槽。4×资源原点为 (32,16)，另有 1×、1.5×、2×
  预过滤版本，避免细线缩小后闪烁。PNG 保存预乘 RGBA，匹配本机 tML rawimg 管线。
- 渐变和粒子笔刷也由 HTML 导出。丝缎褶线、花瓣漂移、星云与星座、星点闪烁仍动态绘制，使用相同种子和公式。
  未烘焙整条动画；运行时不创建纹理、不切换 RenderTarget。
- `Effects/Content/ResourceBarClip.fx/.xnb` 对每层分别做圆角裁剪，并与完整底槽相交。
  这同时处理满值、空值、极短填充和残影尾部；采样包含小粒子的像素覆盖。
- 残影等待 0.4 秒，随后每秒缩短 45%；治疗平滑回升。残影比填充更暗更饱和（生命暗玫红、魔力偏紫），又亮于空槽，实战中能分清实际值；
  回复不再整条闪白，只在填充末端冒星屑。资源上限、角色或魔力类型切换会重置视觉状态。
  受伤花瓣 / 施法星屑是 UI 内的有界粒子（每条至多 96 个），不依赖世界坐标或修改游戏资源值。
- 设置 `BasicConfig.ShowResourceValues` 开启后，`ResourceBarDrawing.DrawValue` 在两条正中绘制 `当前/上限`（四舍五入取整）：字体为 NotoSerifSC（与技能栏冷却数字同款，等高数字），12 UI 像素，按最终屏幕字号选择预制字形，测量与绘制共用该字号，静态位置对齐设备像素；使用四向各一个屏幕像素的暗边。数值回归与截图由本目录 `run.ps1` 生成。
- tML 的 UI layer 已转换 `Main.screenWidth` 与鼠标到 UI 单位；不再次除 `UIScale`。
  绘制使用传入 SpriteBatch，恢复 vanilla UI batch 的矩阵、混合、采样、深度、光栅与裁剪状态。
  填充 Shader 与当前 FNA SpriteEffect 使用相同投影，不额外叠加 D3D9 半像素偏移；预览会在四种 UI 缩放下直接比对两者矩阵。

## 重建

需要 Node.js 的 `playwright`、`sharp`，Chromium（`CHROME_PATH` 可覆盖路径），Python Pillow、.NET 8、实际 tModLoader，
以及 `.vissandbox/gelasio-source/Gelasio.ttf`（缺失时 bake 会报错）。bake 通过本机回环 HTTP 加载页面，避免头像 PNG 污染 file:// 画布。
本机可在当前终端设置 `NODE_PATH=<用户目录>/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules`。

```powershell
node Tools/ResourceBarPreview/bake.cjs
pwsh -File Tools/ResourceBarPreview/compile-shader.ps1
pwsh -File Tools/ResourceBarPreview/run.ps1
python Tools/ResourceBarPreview/verify.py
dotnet msbuild 伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:q
```

`compile-shader.ps1` 使用仓库自带的 XNA 编译器；它偶尔在成功输出后异常退出，脚本检查新产物，
`run.ps1` 再实际加载并运行新 XNB。正式 `.tmod` 构建请使用游戏内“构建并重新加载”或关闭游戏后正常 `dotnet build`。
运行中游戏会锁住已加载的 `.tmod`，不要强行替换或结束玩家进程。

## FNA 离线验证

`run.ps1` 每次链接上述真实 C# 绘制与状态源码，读取正式贴图和 Shader；没有第二套模拟绘制。
覆盖 1× / 1.5× / 3×、暗 / 日间背景、常态 / 受伤 / 空值 / 满值 / 极低值 / 不同时间，
头像另有常态 / 低经验 / 上限 / 升级闪光 / 低血量 / 三位数等级 6 种状态（从 KL/Fonts 加载真实 Gelasio），
以及含头像的小地图布局（1× / 1.5×，另含三位数等级升级放大）、6 个动态受伤帧和升级过程帧条 `portrait-levelup-strip.png`。头像独立画布为 160×140 UI 像素，容纳右移后的等级及升级效果。
状态断言验证延迟、收敛、治疗、上限变化、粒子回收、无效最大值，以及连续跨级、经验回退、上限暂存与星屑上限；已移除调用过期悬停接口的检查。

输出在 `.vissandbox/resource-bars/output`，HTML 同状态参考在 `reference`。
`verify.py` 校验源哈希、贴图尺寸、预乘 Alpha，并对 72 对图（资源条 36、头像 36）做差异检查。
Canvas/FNA 的抗锯齿和纹理采样有细微差异，不将该对照称为逐像素完全相同。
`native-preview.png` 是本次真实 C# 的 FNA 离线图，不是正在运行的游戏截图。

若用独立 `-tmlsavedirectory` 构建了测试包，可执行
`python Tools/ResourceBarPreview/verify.py --package <测试包.tmod>`，核对包内 rawimg 与 PNG 的 RGBA 字节一致、
Shader 一致，且 Tools / .vissandbox 未被打包。

工具源码采用 `.cs.txt`，运行时复制到构建排除目录；`build.txt` 排除 Tools 和 .vissandbox。
离线验证未模拟真实鼠标输入、资源覆盖类模组以及正在运行的世界；重载后需要在游戏中确认这些集成行为。

## 小字号文字（2026-11-10）

资源数字与头像等级复用 `ConstellationFont`，加载共用的 NotoSerifSC 9–48px、Gelasio 8–48px PNG/BIN；静态数字不再缩小 48px XNB。等级升级放大使用密集字形绘制，按原排版字距推进，动画结束后恢复原生字形与设备像素对齐。四向一屏幕像素暗边代替多圈亚像素叠加。正式代码没有创建字体贴图。

`run.ps1` 增加 100%、112.5%、125%、150%、200% 数字和等级检查，包含小数原点、明暗背景及五位数。原生字形在 Point/Linear 采样下 Alpha 差异最多 2；旧母版缩小路径在十组检查中全部失败。`values-*.png` 为局部原生图，`*-legacy-values.png` 保留旧数值绘法作对照（其中头像仍用新绘法）。`screen-*.png` 是 1366×768、1864×977、1920×1080、2560×1440、3840×2160 的完整离线画布，分别使用 100%、125%、112.5%、150%、200% UI 缩放。

Gelasio 新增 27–48px 的 22 对 PNG/BIN，合计 491,996 字节；生成仍使用 `Tools/ConstellationRasterChecks/run.ps1 -Label bake-fonts` 与独立字符清单。`verify.py --package` 同时逐字节检查包内 81 对字体资源、62 个资源条贴图和 Shader，并检查工具目录未打包。

本次 C# Release 编译、FNA 状态与字体回归、72 个 HTML 外观对照均通过。正常打包被运行中游戏以 TML003 阻止；使用 `-tmlsavedirectory .vissandbox/ui-font-package` 的隔离测试包已完成并通过上述资源验证。未替换运行中模组，尚未进行游戏内重新加载与视觉验证。

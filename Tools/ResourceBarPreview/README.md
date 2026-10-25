# 花蔓资源条

`bars.html` 是用户确认的视觉来源。正式入口是 `ElainaAttribute/ElainaPlayerResourceDisplaySet.cs`，
共享绘制与状态分别在 `ResourceBarDrawing.cs`、`ResourceBarState.cs`。
采用 A 款：生命条 262×8、魔力条 262×7，顶部 y=18/46，右侧预留 36 UI 像素。
生命花蔓在右，魔力花蔓在左；无前沿大星光。保留普通魔力 / 独立魔力读取及悬停读数。

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
- 等级数字使用 KL 的 Gelasio XNB（素材坐标 12px，显示 16.8 UI 像素），居中挂在菱形底尖：数字中心位于底尖下方 2 像素（局部锚点 (0, 23.6)，加 `LevelY` 10），只压住白色外线，经验带起点仍露在数字上方；两侧花饰缩至 65%。位置对比见 `level-position-study.png`（A 方案）。HTML 加载 `.vissandbox/gelasio-source/Gelasio.ttf`，
  其来源与 SHA-256 见 `KL/Fonts/Gelasio.source.json`。数据来自 `ElainaStatePlayer`（等级、经验、升级所需、Boss 进度上限）。
- 等级字色为银白色 `#eee9f5`，达到上限时为灰银 `#cfc8da`；数字下叠两圈各 8 个方向的偏移暗影（半径 0.55 / 1.1，不透明度 .2 / .1，色 `#0e0a1a`），只为跨过白色外线时可读，不是实心描边。两侧花饰下另有贴着花纹形状的暗底 `LevelFlourishShadow`（剪影向 8 方向扩 0.9、模糊 1.6，`rgba(18,12,32,.45)`，花纹局部单位），白天浅色背景上托住细线，夜晚几乎不可见；比较稿见 `flourish-shadow-study.png`（中档）。等级花饰使用 0.85 素材像素的卷须和简化四瓣花，按 UI 缩放、花饰自身缩放及升级动画的最终尺寸选择预过滤贴图，减少细线发虚。
- 获得经验时进度缓动，可连续跨级；每次升级经验带闪白、等级放大并散出 22 颗星屑（上限 66）。
  到达上限后经验带转灰银并熄灭光点。生命显示值低于 25% 时经验槽泛蔷薇色脉冲。悬停放大后的菱形或等级区域显示 `Lv 当前/上限  EXP 当前/所需`，提示位于等级下方。

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
状态断言验证延迟、收敛、治疗、上限变化、粒子回收、无效最大值，以及连续跨级、经验回退、上限暂存、星屑上限与菱形悬停判定。

输出在 `.vissandbox/resource-bars/output`，HTML 同状态参考在 `reference`。
`verify.py` 校验源哈希、贴图尺寸、预乘 Alpha，并对 72 对图（资源条 36、头像 36）做差异检查。
Canvas/FNA 的抗锯齿和纹理采样有细微差异，不将该对照称为逐像素完全相同。
`native-preview.png` 是本次真实 C# 的 FNA 离线图，不是正在运行的游戏截图。

若用独立 `-tmlsavedirectory` 构建了测试包，可执行
`python Tools/ResourceBarPreview/verify.py --package <测试包.tmod>`，核对包内 rawimg 与 PNG 的 RGBA 字节一致、
Shader 一致，且 Tools / .vissandbox 未被打包。

工具源码采用 `.cs.txt`，运行时复制到构建排除目录；`build.txt` 排除 Tools 和 .vissandbox。
离线验证未模拟真实鼠标输入、资源覆盖类模组以及正在运行的世界；重载后需要在游戏中确认这些集成行为。

# 花蔓资源条

`bars.html` 是用户确认的视觉来源。正式入口是 `ElainaAttribute/ElainaPlayerResourceDisplaySet.cs`，
共享绘制与状态分别在 `ResourceBarDrawing.cs`、`ResourceBarState.cs`。
采用 A 款：生命条 262×8、魔力条 262×7，顶部 y=18/46，右侧预留 36 UI 像素。
生命花蔓在右，魔力花蔓在左；无头像、无前沿大星光。保留普通魔力 / 独立魔力读取及悬停读数。

## 资源与动态绘制

- `bake.cjs` 直接调用 HTML 的 `silverFrame` / `FRAME.rail.under`，生成正式目录
  `ElainaAttribute/ResourceBarAssets` 的银框、花蔓和底槽。4×资源原点为 (32,16)，另有 1×、1.5×、2×
  预过滤版本，避免细线缩小后闪烁。PNG 保存预乘 RGBA，匹配本机 tML rawimg 管线。
- 渐变和粒子笔刷也由 HTML 导出。丝缎褶线、花瓣漂移、星云与星座、星点闪烁仍动态绘制，使用相同种子和公式。
  未烘焙整条动画；运行时不创建纹理、不切换 RenderTarget。
- `Effects/Content/ResourceBarClip.fx/.xnb` 对每层分别做圆角裁剪，并与完整底槽相交。
  这同时处理满值、空值、极短填充和残影尾部；采样包含小粒子的像素覆盖。
- 残影等待 0.4 秒，随后每秒缩短 45%；治疗平滑回升。资源上限、角色或魔力类型切换会重置视觉状态。
  受伤花瓣 / 施法星屑是 UI 内的有界粒子（每条至多 96 个），不依赖世界坐标或修改游戏资源值。
- tML 的 UI layer 已转换 `Main.screenWidth` 与鼠标到 UI 单位；不再次除 `UIScale`。
  绘制使用传入 SpriteBatch，恢复 vanilla UI batch 的矩阵、混合、采样、深度、光栅与裁剪状态。
  填充 Shader 与当前 FNA SpriteEffect 使用相同投影，不额外叠加 D3D9 半像素偏移；预览会在四种 UI 缩放下直接比对两者矩阵。

## 重建

需要 Node.js 的 `playwright`、`sharp`，Chromium（`CHROME_PATH` 可覆盖路径），Python Pillow、.NET 8 和实际 tModLoader。
本机可在当前终端设置 `NODE_PATH=C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules`。

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
另有小地图布局以及 6 个动态受伤帧。状态断言验证延迟、收敛、治疗、上限变化、粒子回收、无效最大值。

输出在 `.vissandbox/resource-bars/output`，HTML 同状态参考在 `reference`。
`verify.py` 校验源哈希、贴图尺寸、预乘 Alpha，并对 36 对图做差异检查。
Canvas/FNA 的抗锯齿和纹理采样有细微差异，不将该对照称为逐像素完全相同。
`native-preview.png` 是本次真实 C# 的 FNA 离线图，不是正在运行的游戏截图。

若用独立 `-tmlsavedirectory` 构建了测试包，可执行
`python Tools/ResourceBarPreview/verify.py --package <测试包.tmod>`，核对包内 rawimg 与 PNG 的 RGBA 字节一致、
Shader 一致，且 Tools / .vissandbox 未被打包。

工具源码采用 `.cs.txt`，运行时复制到构建排除目录；`build.txt` 排除 Tools 和 .vissandbox。
离线验证未模拟真实鼠标输入、资源覆盖类模组以及正在运行的世界；重载后需要在游戏中确认这些集成行为。

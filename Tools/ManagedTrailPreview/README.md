# 托管拖尾分层回归检查

运行 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ManagedTrailPreview/run.ps1`。
可用 `-TmlPath` 指定 tModLoader 安装目录。输出在 `.vissandbox/managed-trail-preview/out`。
修改 KL 的 ReColor.fx 后先运行同目录的 `compile-recolor.ps1`；它仅在隔离目录编译该 Shader，
检查输出时间戳后更新 KL/Effects/Content/ReColor.xnb。
修改 CrescentFan.fx 或 ArcFlame.fx 后运行 `compile-wind.ps1`，按同样方式重新编译两个风刃 Shader。

每次运行使用 Roslyn 从当前源码提取 KL 的采样逻辑、缓存/绘制钩子，以及
IceConeProj、MagicMissile、MagicMissleSpawner 的 DrawBehind、PreDraw、DrawManagedTrail 和拖尾配置。
直接编译当前 TrailManager、LayerDrawRequestSystem，提取完整 Bloom 绘制、RenderHelper 和效果绑定，
使用真实 FNA、拖尾纹理及已编译的 VertexDraw、Bloom、ReColor Shader。

检查有足够轨迹点时本帧首次显示、当前头部坐标、存活时完整路径/寿命、先拖尾后本体、
PreDraw 不重复采样、暂停/时停不采样、旧弹幕 PreDraw 行为，以及死亡后十次更新内收缩并移除。

另外检查：

- 正式拖尾统一为 `Projectiles.After`，默认 `order = -100`；主体默认 `0`，Bloom 粒子为 `100`。
- 故意打乱提交顺序，验证按 order 排序、同 order 的 ID 稳定顺序、同 ID 不同 order 的独立批次、`IsFirst/IsLast`、极端 order 和清帧；这些请求只触发一次 Bloom 流程。
- 全部九个绘制层的 Before/After 顺序，普通和 Bloom 回调各执行一次；覆盖开关 Bloom 及有无活动 SpriteBatch。
- 两种拖尾图层与 Before/After、开关 Bloom、场景 RT/backbuffer 的 16 种组合。
- 当中间没有其他内容时，BehindProjectiles 的前后阶段与 Projectiles/Before 输出逐像素一致。
- 后续空 Bloom 图层不改变之前场景的颜色。修复前可复现 2,959 个像素变暗、场景最低 Alpha 从 143 降到 80；
  改用预乘合成后，变暗像素为 0，原本不透明的场景 Alpha 保持 255。

昼夜混合回归另外提取实际 WindSlash.DrawWind、几何生成、VertexDrawEffect 和三个风刃 Shader/贴图，
覆盖单条/重叠飞弹拖尾、实际风刃（含 PreDraw 完整路径）、加法与普通透明混合、不透明遮挡共 24 个场景。
以“原回调直接画到 HDR 背景，最终统一截断到显示范围”作为独立颜色参考；UNORM 场景会在混合前截断源颜色，
不适合用作 HDR 混合参考。要求 Bloom 强度为零时恢复该参考 RGB（容差 2/255）、开启光晕只增亮原图、
加法绘制不压暗背景、低于阈值的普通透明图形不产生光晕，并检查风刃每帧只执行一次完整绘制。
修复后 24 个场景的异常像素均为零；同时检查确实存在新增光晕，避免仅关闭 Bloom 而让测试通过。

在 RequestBloom 中，KL 绘制助手自动为标准加法/非预乘模式记录正确的遮挡 Alpha。
自定义绘制直接设置 GraphicsDevice.BlendState 时，应使用 DrawSystem.GetBlendStateForBloom；
直接 SpriteBatch.Begin 也应传入该方法返回的状态。自定义 BlendState 仍须自身提供与 RGB 混合一致的覆盖率。

生命周期图片从上到下为冰锥、魔法飞弹、飞弹生成器。白色矩形是用于观察遮挡的替代本体，
不是实际模型或飞弹外观。这是 FNA 离线预览，绘制真实拖尾回调并运行完整 Bloom 模糊/合成。
wind-*.png 使用实际风刃绘制；trail-/overlap-*.png 使用实际魔法飞弹拖尾，均未使用矩形替代该效果。
固定运动轨迹代替 AI，也不模拟真实光照、碰撞、联机和其他模组。游戏内仍需检查高速转向及其他场景内容的遮挡。

源码模板用 `.cs.txt`，生成文件在 `.vissandbox`；这两个工具目录由现有 buildIgnore 排除。

## 风刃弧度对齐

运行 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ManagedTrailPreview/run.ps1 -WindAlignment`。
单独输出到 `out/wind-alignment`，使用当前 WindSlash 的实际绘制参数和混合模式。
`layer-1/2/3.png` 分别显示月牙、噪声弧面和条纹；合成图覆盖第 5、10、30 帧及左、右、斜向、朝上和昼夜背景。

回放从实际 Shader 参数、纹理尺寸、绘制位置/缩放恢复两个椭圆，比较它们与实际条纹顶点的弧心、半径、弧顶和前缘。
每个合成状态要求弧心、半径及弧顶误差小于 0.001 像素，前缘椭圆方程误差小于 0.0005。
当前三层共用 WindSlash 的 BladeCenterOffset、BladeRadiusX、BladeRadiusY，显现缩放也一起变化。
月牙及噪声层保留用户设定的 NonPremultiplied，条纹仍按调用处指定的模式绘制；这些混合方式不会被预览器替换。

风刃使用的 fadeUP 和 background 是 Alpha 全为 255 的灰度渐变，因此两层开启 useRGBforApha：
Shader 用灰度乘入几何透明度，RGB 只保留绘制颜色；关闭该模式时继续保留材质原色。
这样不会把灰度重复乘入 RGB 和 Alpha，也不会把不透明黑色渐变盖到背景上。
对齐预览会逐张检查分层图和昼夜合成图中是否出现低于背景颜色的像素（容差 2/255）。
当前日间场景的独立月牙层由 3,350 个背景压暗像素降为 0，合成图由 341 降为 0。

## 冰锥碎裂连接段

运行 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ManagedTrailPreview/run.ps1 -IceBreak`。
输出在 `out/ice-break`：原效果/补段后的第 0～4 帧，覆盖左、右及斜向。
使用实际 OnKill 中的连接调用、连接几何、两段绘制回调、TrailManager 和完整 Bloom；
爆炸粒子与模型未模拟，橙色十字表示碎裂中心。

验证最后提交的历史路径和偏移不受死亡帧 rotation 改变影响，连接端点保持在旧拖尾头与实际爆点，
新旧两段在四次更新内同步淡出，连接段结束后不残留亮的断口；另检查零长度切线和未采样便死亡。
宽度取冰锥的 TrailWidth（当前 5），存活时的 TrailOffset（100）保持不变。

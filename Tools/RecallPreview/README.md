# 回城魔法与占位特效

## 多人共用出生点：错位与悬停提示

`RecallPortalLayout` 以 `(owner, PairId, Side)` 排序，重叠或距离过近的法阵优先向左右寻找空位，阵心横向间距至少 344 像素；世界左右边界放不下时向上换行。各端的弹幕槽位和接收顺序不参与排序。成员、配对键或原始阵心发生变化才重新排布，其他帧复用结果。单个法阵保持原位，其他门消失后剩下的门恢复原位。

`Projectile.Center` 保存排布锚点与首次群体传送的施法范围；`DisplayCenter` 是绘制、鼠标命中、右键使用范围和传送目标共用的实际阵心。首次回城与右键往返都统一以另一端的 `DisplayCenter` 为落点，无需判断是否在出生点或是否发生重叠。原有方块/岩浆安全落点处理仍围绕该目标运行。服务端使用同样的布局重新校验，错位前的锚点不再保留隐形交互范围。目的地提示也显示另一端排布后的坐标。原版按弹幕位置的剔除范围已放宽，`PreDraw` 再按实际显示位置和相机变换剔除屏幕外法阵，避免远端门提交 bloom。

场上只有一组回城法阵时，悬停仅显示简短的右键提示或当前不可用状态；有多组时，额外显示施法者、另一端排布后的格子坐标及太空/地表/地下/洞穴/地狱分层。按 `(owner, PairId)` 区分组，同组的入口和出口不重复计数，也不依据在线玩家数量判断。其他组消失后立即恢复简短提示。中英文同步移除“格子坐标 / Tile coordinates”和“免费往返 / No Magic Power cost”后缀，保留坐标数值。坐标以世界左上角为原点，X 向右、Y 向下，单位为格；它不是深度计的英尺读数。环境名称只依赖世界高度元数据，不扫描远端区块、不读取本地玩家群系，也不推测其他模组的自定义群系。多组时，描绘中、距离不足或非队友仍可查看目的地，文字说明当前不可使用的原因。另一端尚未同步或已经消失时显示连接状态，不提供传送入口。

2026-10-26 验证：`pwsh -NoProfile -File Tools/RecallPreview/run.ps1 -Checks` 通过 418 项断言（正式行为代码，引擎和网络适配桩）。新增出生点原地及附近施法后落在可见出口、在两个可见阵心间右键往返、多人共用出生点后的实际落点与提示坐标、拒绝旧锚点交互、双客户端与服务器落点一致的回归；修改前已用出生点场景复现原地传送。还覆盖单组两个端点仅显示简短提示、其他在线玩家无门时不显示详情、多组变单组后收起详情、相同配对 ID 的双客户端显示详情、不同弹幕顺序下的布局一致性、服务端距离/队伍校验、缺失另一端、边界排布和显示位置剔除。旧测试桩已补齐技能基类的 `MaxLevel`，魔力断言改为读取当前技能费用。

`pwsh -NoProfile -File Tools/RecallPreview/run.ps1 -OverlapOnly` 直接链接正式布局、提示生成、实际 HJSON 文本及法阵/KL bloom 绘制，生成 `.vissandbox/recall-preview/overlap-*.png`。已打开检查修改后的中英文单组简短提示和多组详细提示（英文 1.25 倍缩放）；此前也检查过三门常态、45 帧描绘和96 帧爆发。提示文本用项目 FNA 字体在预览中展示，原版鼠标提示位置/边缘避让不在该宿主中模拟；灰色人物和地面为参考背景。这是 FNA 离线预览，不是游戏截图。未新增正式纹理，复用现有预制资源。

中英 HJSON 解析和键/格式参数检查通过，`dotnet build 伊蕾娜.csproj --no-restore -v:q -t:Compile -p:BuildProjectReferences=false` 编译成功。完整打包曾被正在运行的 tModLoader 拦截，尚未将此次改动重载进游戏；真实房间的网络延迟、区块加载、鼠标提示覆盖和输入仍需实测。

## 传送画面转场

成功的首次群体传送和右键往返，在本地玩家实际收到/应用传送的位置更新时启动 `RecallTransition`。20 格内不遮暗，20～120 格间按平滑曲线增强，120 格外完整遮住场景切换；停留 0.045 秒后用 0.38 秒平滑恢复。只画一次全屏黑色遮罩，位于世界画面之后、HUD 之前，使用物理视口坐标，不受 UI 缩放影响。没有新增传送延迟、操作锁定或网络消息；失败落点、其他玩家的位置广播和专用服务器不会触发本地转场。进世界、退出世界或死亡时清除残留。

`pwsh -File Tools/RecallPreview/run.ps1 -Checks` 目前通过 238 项断言，包含 30/60/144 FPS 下转场单调恢复、近中远距离强度、重复传送、失败落点以及两个客户端独立遮暗。项目 C# 编译通过。`pwsh -File Tools/RecallPreview/run.ps1 -TransitionOnly` 链接正式转场状态和绘制，生成近中远三组各 37 帧 FNA 离线预览；`python Tools/RecallPreview/compose-transition.py` 生成 `.vissandbox/recall-preview/transition/contact-sheet.jpg` 和 `near.webp` / `medium.webp` / `far.webp`。已检查传送前、传送瞬间和恢复过程。预览的地形和 HUD 是示意背景，转场来自正式代码；游戏实际镜头、区块加载、HUD 插层和联机延迟仍需游戏内验证。

回城魔法默认属于「待启封」，技能名称和图标显示问号，提示「获得过返回药水」。首次获得原版 `ItemID.PotionOfReturn` 后永久启封，进入「待研习」；研习消耗 50 研习点和 1 瓶返回药水。角色独立保存获得记录，消耗或丢弃药水后不回退；已有药水的旧角色从背包、个人储物或鼠标物品补认，已习得角色保持已习得。旧版本已用掉且未留下任何记录的历史获得无法追溯，需要再次获得。行为及面板验证使用 `pwsh -File Tools/ConstellationSkillPanelPreview/run.ps1 -RecallResearchOnly`。

正式技能为 `ElainaModSkills/Skills/Recall/RecallSkill.cs`，使用已有 `RecallSkill.png`，按现有技能系统解锁、装备。目标选择与治疗一致：全世界没有其他存活同队非 PvP 友军时，左键直接以自己为目标施法；有友军但全部超出范围时，打开只显示自己的轮盘；有范围内友军时，显示自己和这些友军。轮盘中松开选中项确认，中心、右键或 Esc 取消。确认后立即扣魔力和层数并打开法阵。

目前默认消耗 200 魔力、冷却 10 秒；选人范围 3000 像素。施法时同时打开两个法阵：起点固定在所选目标当时的脚底，出口开在所选友军的家（床，出生点无效时为世界出生点），位置在施法时确定，之后不再追踪床的变化。两端一起描绘 1.5 秒，画完的那一帧把起点阵心 160 像素内、存活且同队非 PvP 的友军传送到出口法阵处。双方都未选队伍也按同队处理。此时走出阵法的人不传送，后来进入的人会参与；所选友军在描绘期间死亡或离开不影响传送。

法阵只是两个视觉弹幕，没有确认握手：施法客户端走正常技能流程扣魔力和层数，同时以自己的 `whoAmI` 生成两端弹幕，由原版 SyncProjectile 同步到服务器和其他客户端。服务器上的起点弹幕在年龄到达 `DelayTicks`（90 帧）时结算一次群体传送（`localAI[0]` 标记防止重复），落点在出口附近寻找安全空位，并广播玩家传送。服务器不登记配对状态，右键往返时按 `(owner, 配对 ID, 端)` 直接查找两端弹幕校验。为了让施法者在自己这端知道友军的家，`RecallPortalPlayer` 在换床时把出生点发给服务器并由服务器转发给其他客户端，玩家加入时经 `SyncPlayer` 互相补齐。各端画完与服务器实际传送之间相差一个网络延迟。

画完后两端保留 30 秒。玩家靠近并将鼠标移到魔法阵椭圆上，右键单击即可免费往返，不检查魔力或技能冷却，也不要求拿着魔杖；描绘期间不能往返。每次传送有 45 帧防连触间隔，长按不重复触发。每名施法者最多维持一对门；新施法成功后，施法客户端立即关闭自己的上一对门（包括尚在描绘的法阵），不影响其他玩家。取消选择不会关闭旧门。施法者离线或任一端被销毁时另一端随之关闭。落点被封闭时寻找附近空位，找不到时不传送，不挖掘方块。

## 特效接手位置

- `RecallPortalProjectile.cs`：魔法阵弹幕，`PreDraw` 调用 `RecallPortalVisual.Request` 提交绘制。`Projectile.Center` 是固定阵心。`ai[0]` 为配对 ID（需与 owner 联合使用），`ai[1]` 为年龄帧数，`ai[2]` 为起点 0 / 出口 1（`Side`）。90 帧画完并结算首次传送（`Ready`），1890 帧到期。延迟和残留时长的常量在 `RecallPortalSystem`。
- `RecallPortalVisual.cs`：游戏接入层，把法阵作为 `Elaina:RecallPortal` 提交到 KL `LayerDrawRequestSystem.RequestBloom` 的 `Projectiles.After` 层（与魔力飞弹、冰锥同层共享一次后处理）。回调内按 KL 惯例 `EndBeginDraw()` 后直接绘制顶点，矩阵为 `GameViewMatrix × 屏幕正交投影`，Shader 为 `KL/Effects/Content/VertexDraw`。
- `RecallPortalDrawing.cs`：绘制方法，紫色起点、蓝色出口，最后半秒淡出；纹路与外圈花体字随蓄力在 1.5 秒内逐笔描绘，入口与出口同步。所有笔画在屏幕空间扩成带状顶点（宽度为线宽 + 两侧 0.8 像素柔边，1×32 程序生成的横截面贴图做抗锯齿），拼成一批索引三角形后一次 `DrawUserIndexedPrimitives`，顶点颜色为 Vector4 HDR 预乘值。不依赖 Terraria/KL 类型，传送逻辑无需改动。`Contains` 负责鼠标椭圆命中；更改形状时同步调整。
- 光晕：KL bloom 只提取感知亮度 `dot(rgb, (0.3, 0.59, 0.11)) > 1` 的像素，并按其完整颜色模糊，所以光晕形状就是过曝线条本身。调参在 `RecallPortalDrawing` 顶部：`BaseGlow`（常态 1.5，保持在阈值以下；整体越过阈值会糊成一片雾）、`TipGlow`/`TipLength`（笔尖额外 5 倍、14 像素衰减长度，沿笔画向后从过曝偏白渐变回本色，类似拖尾；笔画到达终点后笔尖余辉按 `TipFade` 18 帧淡出，而不是立刻消失）、`GlintHeat`（花体字流光波峰的轻微过曝，构成常态的微弱光晕）。完成瞬间花体字整圈轻微闪亮。画完（age = delay）后 `BurstTicks` 50 帧内有完成爆发，之后每次有玩家经这对法阵往返成功，服务器广播 `Flash`，两端各重放一次（弹幕 `localAI[1]` 记录起始年龄，作为 `burstStart` 传给绘制）：每条纹路向上拖出高度不一的渐隐加色光幕（`SheetGlow`，像沿竖直方向的径向模糊），少数采样点长出更高的过曝细光束（`RayGlow`、`RayDensity`，最高 `BurstHeight` 1.1 倍半径），光束迅速升高并继续上升、亮度先冲高再衰减；花体字笔迹密集，只长少量光束、不拖光幕。爆发与光幕的 Alpha 为 0，只增亮不遮挡。外环悬停时越过阈值发光。直线笔画在 `RecallPortalPattern` 中细分为等长小段，使笔尖渐变只落在末端。线条贴图在 `RecallPortalSystem.Unload` 中排队到主线程释放。
- `RecallPortalScript.cs`：外圈花体字笔迹。小写字形以控制点描述，Catmull-Rom 平滑后按投影周长均匀排布（直接按角度排会让透视压扁的左右两侧挤成一团），首次使用时生成并缓存，每帧只投影绘制。文字内容只为笔画节奏，不要求辨认；椭圆纵横比需与绘制文件一致。
- `RecallPortalPattern.cs`：内部纹路笔画表，地面平面坐标以半径为单位。每条笔画有描绘时间窗口（以蓄力时长为 1）和旋转层：外环与内框 0–0.3，星圆断弧与刻度 0.2–0.45，六芒星两三角先后 0.15–0.65（每个三角形的各边从顶点同时出发），顶点小圆与日火月水风土符号 0.35–0.75，反转虚线环 0.5–0.8，中心内圆 0.3–0.75、八芒星两方形 0.45–0.8 与 0.53–0.88（方形四边从四角同时画出，阵心不画小圆）。六芒星层正转、虚线环与中心层反转。
- `RecallPortalSystem.cs`：`Cast` 在施法客户端同时生成两端并关闭旧门；`HomeFoot` 由出生点得出出口坐标；`Arrive` 在服务器（单人模式为本地）结算群体传送；`Travel` 校验右键往返；出生点广播与玩家传送同步也在这里。只有四种模组包：往返请求、出生点、传送结果、往返光效。服务器不调用 NewProjectile 代替玩家生成法阵；不同客户端可以使用相同数字 ID，交互和关闭始终同时验证 owner。
- `RecallPortalPlayer.cs`：右键输入、防连触、换床广播与加入时的出生点同步。`ElainaSkillWheel` 在鼠标右键指向可用阵法时让出打开输入。

## 验证与重现

最简单的方式：双击 `Tools/RecallPreview/preview.cmd`，它会绕过 PowerShell 执行策略运行 `run.ps1`，成功后自动用浏览器打开动画播放器（Windows PowerShell 5.1 默认禁止直接运行 `.ps1`，会报“在此系统上禁止运行脚本”）。也可在终端运行 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/RecallPreview/run.ps1`。

在模组根目录执行：

```powershell
& Tools/RecallPreview/run.ps1 -Checks
& Tools/RecallPreview/run.ps1
& Tools/HealingWheelPreview/check-behavior.ps1
python Tools/LocalizationChecks/check.py
dotnet build 伊蕾娜.csproj --no-restore -v quiet -t:Compile -p:BuildProjectReferences=false
```

回城行为检查直接链接正式技能、系统、玩家交互、弹幕和绘制源文件（`RecallPortalVisual` 用空桩替代，渲染由预览验证），101 项断言覆盖选择与取消、确认时失效、一次扣费、两端同时生成、画完才传送、出口坐标在施法时固定、无床回退世界出生点、落点被封或岩浆、左右端往返与往返光效、描绘中不能往返、无二次消耗、精确到期、单端丢失与施法者离线清理、重施替换和右键输入。网络检查使用独立的两个客户端及专用服务器玩家/弹幕数组，服务器 `Main.myPlayer = 255`，严格模拟原版 NewProjectile 的 owner 判断（只有生成端为 owner 时才初始化 AI 并发送生成同步）。实际序列化的模组包交由正式 HandlePacket 处理，覆盖两端同步、服务器在画完时传送、往返光效广播到各客户端、换床广播与加入同步、出口不随换床移动、伪造包头、多人同编号隔离、重施关闭与完整 30 秒寿命。引擎的 SyncProjectile/KillProjectile 转发、碰撞与轮盘宿主仍是适配桩，不等同于真实联机房间复测。

FNA 离线预览（宿主 `Preview.cs.txt`，工程模板与 `BloomBackbufferPreview` 共用，需要 .NET 10 SDK）在运行时用 Roslyn 编译正式的 `RecallPortalDrawing`/`Script`/`Pattern`/`Visual` 与 KL `LayerDrawRequestSystem`，并按冰锥预览的方式从 KL `DrawSystem`、`RenderHelper`、`DrawHelper` 提取完整 RequestBloom 合成流程，加载 KL 已编译的 VertexDraw、Bloom、ReColor Shader；bloom 强度 1、迭代 4 与 KL `BloomConfig` 默认值一致。Terraria 设备、相机与资源请求为最小适配桩。场景先在目标上画背景，再提交请求并在 `Projectiles.After` 合成，与游戏的场景目标路径一致。

检查项：bloom 开启时产生线外光晕且不使任何像素变暗；描绘中（45 帧）至少 50 个像素被光晕提亮 60/255 以上（笔尖强光），起点和出口写完静止（1200 帧）时被提亮 12/255 以上的像素不超过画面 12%（整体只有微弱光晕，当前约 4.7% / 8.8%）；八个时间点开关 bloom 的可见改动都在阵心附近（bloom 允许外扩 48 像素，激活后 50 帧允许向上 1.1 倍半径）；激活后第 96 帧法阵上方有大量像素被明显提亮，第 80 帧和第 150 帧上方保持干净；第 1200 帧有人往返后 6 帧上方再次明显发光；1.2 倍游戏缩放下法阵半宽为 192±6 像素。失败时保存 `recall-bounds-failure.png`；设置环境变量 `RECALL_GAIN_MAP=1` 会额外输出光晕增益灰度图 `gain-*.png`。隐藏窗口执行后退出，正常输出 `.vissandbox/recall-preview/recall-frames.png`。图中为 4×2 格：第一行起点 15 / 45 / 75 帧描绘过程与 96 帧完成爆发，第二行出口 45 帧（与起点同步描绘、悬停）/ 96 帧完成爆发 / 210 帧 / 到期前 1880 帧。画面阵心上方留出了爆发光束的高度。灰色人物是定位参考，预览不模拟真实地图、玩家外观、碰撞和网络。

同一次运行还会逐游戏帧（60 FPS）回放正式绘制与 bloom 合成，生成单文件动画播放器 `.vissandbox/recall-preview/recall-player.html`（帧以 PNG 内嵌，约 5 MB，可直接用浏览器打开）。包含四个连续场景：起点 0–150 帧（蓄力描绘及画完后约 1 秒）、出生地出口 0–150 帧（与起点同步）、往返传送光效 1190–1260 帧（第 1200 帧有人传送）、到期淡出 1830–1895 帧。播放器支持播放/暂停、逐帧、拖动、0.1–1 倍慢放和 1–3 倍像素放大，信息栏显示年龄、描绘进度和阶段；页面模板为 `Tools/RecallPreview/player.html`，C# 将帧数据替换进 `/*SCENES*/[]`。每帧按年龄顺序渲染，以后加入需要逐帧推进的粒子时，在 `Preview.RenderAnimation` 的循环中扩展即可。也可在 `.claude/launch.json` 的“回城法阵动画预览”配置启动本地服务查看。

源码以 `.cs.txt` 保存；生成工程在 `.vissandbox`，工具和预览产物不参与正式编译和模组打包。首次运行需要本机 .NET 8/10 SDK、tModLoader FNA/native 库和相邻的 KL 源码目录；可用 `-Tml` 指定 tModLoader 安装路径。编译命令关闭模组打包，允许游戏运行期间检查源码；应用到游戏需重新构建并加载模组。

仍需实际联机确认：两位玩家设置不同床后的群体回城、新加入玩家能否正确打开友军家的出口、未加载地区的往返，以及不同延迟下的输入和出口寿命表现。

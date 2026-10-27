# 人偶等级成长、协作与共用玩偶 AI

## 家具与桌面摆件挖掘（2026-10-27）

挖掘选区改按物块存在及原版工具类别识别，不再要求 `tileSolid`。床、工作台、桌子、椅子、瓶子、火把和药草等非实心物件可框选或连锁选择；斧类树木与锤类祭坛不进入镐子任务。命中多格家具的任意一格后由原版拆除整件家具，失效标记自动清理；镐力、进度和 `CanKillTile` 限制继续生效。

整件选区：`DollTileSelection` 使用实际 `TileObjectData` 及样式帧推算占用范围，逐格核对避免扩选到相邻同类家具。点击、框选和再次取消均按整件处理，同一笔跨过多个格子不会重复反选；连锁搜索到上限时补齐边界家具。墙继续逐格选择。悬停轮廓、拖拽预览及提交标记都覆盖完整家具；缺少物件元数据或已损坏的家具回退单格。

搬运归属：`DollMiningCargo` 在角色总背包之外记录每只存活玩偶携带的数量，联机请求保存发起玩偶的实例引用，回执把该批掉落绑定给它。其他空闲玩偶不能交还仍由它携带的物品；任务结束后搬运玩偶优先飞回交还，再接药草照料。单机挖掘和敲墙也传入实际工作玩偶。玩偶被销毁、收回或存档重载时，物品仍在角色背包中并可恢复交还；实例身份检查防止弹幕槽位复用后认错搬运者。

本轮验证：`mining.ps1` 通过 476 条单机与 840 条网络/搬运/家具范围断言，覆盖身边空闲玩偶不能抢先交还、同种物品不同搬运数量、原搬运者位置交还、失效搬运者恢复，以及真实原版家具各格与不同样式的范围。`selection.ps1` 372 条，含单格点击整件选中/取消、部分选区补齐；`run.ps1 -Checks` 255 条，新增正式 AI 带货飞回而不接花园任务的检查。主项目编译通过。已打开 `.vissandbox/doll-selection/furniture-selection-2.png` 与 `furniture-selection-3.png` 检查完整轮廓：每次生成时提取正式 `DrawMarks`，用正式范围计算，家具本体是中性格子占位。这是 FNA 离线预览，非游戏截图；未进行双客户端实机验证。

单机收集现在覆盖完整 `WorldGen.KillTile`，包括家具解体和失去支撑的瓶子掉落，避免只捕获单格掉落导致漏收。空格或禁止拆除的目标不会预先生成掉落。

联机带回已补全：`DollMiningNetwork` 在挖掘伤害达到阈值时发送请求，客户端等待服务器结果，不先拆掉家具或预造物品。服务器验证发送者、存活/建造状态、玩偶存在、距离、当前物块与家具帧、背包镐力及 `CanKillTile`，执行完整拆除并捕获解体和连带掉落。捕获物品的 `SyncItem` 广播在生成期间被拦截，清除的格子走原版方块同步，掉落清单只回传给主人，进入原有玩偶背包、角色存档和飞回交还流程。请求 ID 和有序分包避免重复收取，换世界清理待办回执；取消任务或收回期间抵达的有效回执仍会保留。关闭带回继续走原版拆除与原地掉落。敲墙和自动照料药草的既有流程保持原样。

验证：`pwsh -File Tools/DollControlPreview/mining.ps1` 通过 476 条真实引擎检查，逐格覆盖床、工作台、桌子、椅子、瓶子和石块，两种掉落模式、完整解体、重复标记不重复掉落，以及桌上瓶子单独拆除与拆桌后的连带掉落。场景用标准帧搭建，拆除、镐力和掉落调用正式源码及真实 tML；未建立网络连接。`selection.ps1` 通过 364 条选区检查；`run.ps1 -Checks` 通过 253 条 AI/分配检查；主项目 `ResolveReferences;Compile` 通过。未在游戏窗口中实测输入与多人连接。

同一 `mining.ps1` 另通过 723 条联机流程断言：链接正式网络/掉落源码并提取正式 `Hit`、入库、交还和存档方法，使用真实 `ModPacket` 序列化和服务端 `WorldGen`；传输层由记录器替代。覆盖每种家具各格、发送者与目标校验、主人专属回执、重复请求/回执、同床不同格并发请求、桌子连带瓶子、模组 2×2 家具的 140 项掉落分包与禁止拆除钩子、存档后交还、延迟回执和换世界清理、关闭收集及普通物品广播。该夹具不等同于真实套接字连接或两个游戏窗口的端到端验证。

## 等级成长（2026-10-26）

正式技能读取 `SkillBalance.json` 的成长配置，8 级封顶；当前角色节点为 6、10、15、20、25、30、35、40。首次习得要求角色等级与阶段上限都满足 JSON。Lv1–8 的数量为 1、1、2、2、3、3、4、4，累计额外动作速度为 0%、10%、10%、20%、20%、30%、30%、50%。旧有“四只整队”场景现在明确使用 Lv7。

数量与速度共用正式 `DollControlProgression`，技能描述、当前属性、下级预览与实际执行保持一致。挖矿、敲墙、种植盆放置、药草照料和开箱动作按此速度推进，工作动画同步加快；跟随飞行、待机浮动和召唤/收回特效时间不变。+50% 对应原工作时间除以 1.5。每次召唤仍整队收取 20 魔力，实际人数决定所需弹幕空位。

已召出的人偶随技能升级更新速度并免费补齐新增成员；弹幕池不足时稍后重试，收回中的队伍不会被补召。调试降级会收回多出的成员。每只人偶通过 ExtraAI 同步等级，远端和晚加入客户端据此播放动画，实际工作仍只由主人端执行。收回动画按该等级的人数错开。

`run.ps1 -Checks` 目前通过 253 条断言，包含全部八级数量、真实 AI 敲击次数、三种使用动作耗时、显示数值、阶段节点、弹幕池、在场升级及同步等级。解封条件的框架执行、角色存档和真实网络传输不在这个适配宿主范围内。`run.ps1` 新增 `level-progression.png`：每行 Lv1–8，左侧为真实队伍，右侧为实际工作动画在 0/6/12/18/24 帧的采样；使用正式 PreDraw 与 PNG，属于 FNA 离线预览。

正式源码在 `ElainaModSkills/Skills/DollControl/`。`DollControlSkill` 沿用独特魔力支付接口，召唤消耗 20 魔力，基础冷却 1 秒。每名玩家按技能等级一次召出一至四只不同类型的玩偶，整队仅消耗一次 20 魔力，持续存在直到「收回玩偶」、死亡、离线或离开世界。玩偶在场时魔杖不再挥动，左键改由 `DollCommandPlayer` 处理：按住打开 `DollCommandWheel` 指令盘（挖掘物块、敲掉墙壁、取消任务、收回玩偶、放置花盆），选中标记工具后左键在世界中编辑草稿选区（连锁版为点击整组同类），再次框选/点击反选。屏幕下方的「开始挖掘」/「开始敲墙」将草稿追加到对应任务队列并关闭编辑；「退出」仅丢弃本次草稿，不影响已有任务。右键不再退出框选。「取消任务」节点仍统一清空任务。绿色草稿与琥珀色物块任务/蓝色墙任务仅本地可见（`DollMarkSystem`）；切换术式或失去操控条件时丢弃草稿，已提交任务仍保留。玩偶以背包最大镐力的镐子挖实心物块（含平台与种植盆）、最大锤力的锤子敲墙，掉落物由 `DollTileBreaker` 捕获进主人的 `DollCommandPlayer`（随角色存档），任务结束回到主人身边、收回或重新召唤时抛还给主人。无召唤栏消耗或原版宠物互斥。

`DollPetProjectile` 为可复用的抽象弹幕基类。新增玩偶只需继承它并提供 `TexturePrefix`、`MoveFrameCount`（可选 `IdleFrameCount`、`MineFrameCount`），资产约定为 `<prefix>_Idle_N.png`、`<prefix>_Move_N.png`、`<prefix>_Mine_N.png`（N 从 1 开始）的连续独立 PNG，各动作按顺序循环；无需拼纵向帧表。每张以贴图中心为原点绘制，各帧画布尺寸可以不同，只要角色身体在各自画布中心的相对位置一致。有挖掘目标时飞到目标格朝向主人的一侧（`WorkOffset`），到位后面向格子播放挖矿帧，主人端按工具节奏敲击；无目标时移动 > 待机。

四个跟随位置为身后 56/100、上方 36/80 像素的 2×2 编队，四只使用错开的浮动相位；距离玩家超过 1200 像素时传送。速度会追随高速玩家，平滑靠近目标，浮动周期 90 帧，移动浮动目标幅度 4 像素、待机 1.5 像素。待机每张 12 帧、移动每张 8 帧、挖矿每张 6 帧。以上参数、贴图原始朝向、存活条件、跟随目标均可通过受保护属性/方法覆写。`AI` 使用 `ai[0..2]` 保存浮动相位、移动状态和动画计时；子类扩展其他逻辑时应先调用 `base.AI()` 并检查弹幕仍然 active，勿复用这三个槽位。

熊移动帧来自用户编辑的 `wanouyiudong2/wanou2.png`～`wanou5.png`，原字节不变，为 `Kuma/Kuma_Move_1.png`～`Kuma_Move_4.png`（32×32）。站立与挖矿帧的绿幕原图保存在 `sources/`（128×96），由 `python Tools/DollControlPreview/cutout_kuma.py` 按精确背景色扣除并以画布中心对称裁成 48×48 的 `Kuma_Idle_1～3`、`Kuma_Mine_1～4`；`--check` 校验正式 PNG 与原图一致。原图面向左，向右时水平翻转；反重力时偏移和贴图上下翻转。

召唤只在技能所属本地客户端创建；弹幕通过原版同步传给其他端。传送、状态切换、目标格变化（`SendExtraAI`）及每个浮动周期发送更新，`netImportant` 支持晚加入玩家。其他端按同步的目标格推进 AI，只播动画不敲击；物块与墙的拆除由主人端经原版消息同步。死亡/离线清理，不把它设为原版单一宠物。

## 验证与重现

在模组根目录执行：

```powershell
& Tools/DollControlPreview/run.ps1 -Checks
& Tools/DollControlPreview/run.ps1
& Tools/DollControlPreview/selection.ps1
& Tools/DollControlPreview/planters.ps1
dotnet msbuild 伊蕾娜.csproj '-t:ResolveReferences;Compile' -p:BuildProjectReferences=false -v:quiet
```

`-Tml` 可指定 tModLoader 安装路径。检查与预览直接链接正式玩偶类型、`DollPetProjectile`、`DollControlSkill` 和 `DollWorkAssignments`，每次运行还从正式 `DollCommandPlayer` / `DollPlanterTask` 刷新提取两个完整目标选择方法。任务认领与分配使用正式逻辑，世界可挖性、敲击、材料放置和资源宿主仍为替身；真实镐力判定、掉落捕获和网络传输不在此夹具范围内。检查覆盖扣费、资源不足、弹幕池满、玩偶在场时不挥杖不扣费、多人隔离、静止、高速移动、转向、浮动幅度、四帧动画、停下、传送、挖掘目标的站位/朝向/敲击间隔/四帧挖矿、目标同步与其他端只播动画、挖完回到身边交还收集物、收回与重新召唤交还、反重力、服务器生成限制和死亡/离线清理。引擎、技能资源宿主和网络传输为适配桩，不等同于实机联网测试。

FNA 离线预览运行隐藏窗口，直接调用正式 `PreDraw` 并读取正式 PNG，结束后自动退出。输出为 `.vissandbox/kuma-preview/kuma-frames.png`（每行依次为 Idle 1～3 / Move 1～4 / Mine 1～4；三行分别为向左、向右、反重力；每格含 3 倍和原尺寸），以及 `frames/000.png`～`149.png`（10 秒的待机→向右→停止→向左→停止→四只分别飞去挖地面里的四格标记）。灰色人物是位置参考；不模拟地图、真实玩家造型、光照和网络延迟。

工具源码使用 `.cs.txt`，生成工程位于 `.vissandbox`；两者均在模组打包排除目录，工具源码不参与正式模组编译。应用到游戏需要构建并重新加载模组。若命令行完整构建返回 TML003，表示游戏正在使用此模组；可在游戏内执行「构建并重新加载」，或关闭游戏后再完整构建。

## 放置花盆

> 2026-10-27 起该指令、材料菜单与两层框选已移除，由「远程放置物块」取代，见文末同名章节。本节保留历史记录。

轮盘的 `PlacePlanters`（移除攻击占位后为第五项），使用独立的陶土花盆与嫩芽图标 `Commands/DollCommand_PlacePlanters.png`（40×40，预乘 alpha），轮盘与选区操作条共用。框选按左上角对齐，以 1×2 世界格为一组：上方留空、下方种植盆；不足两格的末行向下补齐，世界边界只保留完整组。相同选区再次框选移除，错位的两层不能相互重叠，草稿与已排队任务合计最多 10000 组。两格内遇到现有物块、植物或液体跳过；种植盆下方无需留空或支撑，不检查、不放置也不消耗平台。

图标可用 `python Tools/DollCommandIcons/BuildDollCommandIcons.py --only DollCommand_PlacePlanters --apply` 重建（保留 SVG 源稿与直通 alpha 预览，正式贴图预乘 alpha）。`Tools/SkillWheelPreview/run.ps1 -DollCommands` 链接正式指令路径和轮盘绘制，输出 `.vissandbox/skillwheel-preview/output/doll-place-planters-1.0.png` 与 `doll-place-planters-1.5.png`。这是 FNA 离线图标预览，扇区使用 BasicEffect；未模拟游戏输入，省略指令文本。

点击底部的种植盆行会向上展开材料列表，列出主背包 0–49 槽内的物品图标、名称和合计数量，金色勾选标明当前材料。点击条目直接选择并收起；重复点击材料行或点击外部可收起，外部点击不会穿透为世界框选。长列表支持滚轮和上下翻页，按屏幕空间显示最多六行；背包没有对应材料时显示空列表提示。默认按相同物品类型的合计数量选最多的种植盆；显示持有数量/所需数量，不足时标红。有至少一组的材料即可开始，执行途中不自动改用其他类型，选定材料耗尽后清空剩余放置队列并提示；挖掘任务不受此缺料清空影响。每组只放一个种植盆，成功后才扣一个种植盆；模组钩子拒绝时不扣物品。种植盆遵循原版放置、物品消耗钩子以及 TileManipulation / ObjectPlacement 同步。黏土盆不进入材料列表，也不能通过执行入口放置或登记维护；旧世界中的黏土盆登记保留但停止照料，现有盆和平台不会被拆除。

`selection.ps1` 链接正式 `DollPlanterTask`、`DollPlanterBuilding`、`DollPlanterDrawing`，额外检查三种 UI 缩放下的选择、两层间距、反选、材料聚合、列表选择/分页/滚动/空状态/点击拦截、任务隔离、材料扣除、缺料结束、障碍和取消。`planters.ps1` 独立调用真实 tModLoader 的 Item、Tilemap、WorldGen、TileObject 和钩子，覆盖全部 8 种原版种植盆悬空放置，并验证黏土盆排除、下方已有物块保留、注册的模组种植盆及排除规则，共 58 条断言。没有创建网络连接或读写角色/世界存档。`run.ps1 -Checks` 检查玩偶实际 AI 路由到放置动作、远端只播放动画以及取消后的跟随。

FNA 输出 `.vissandbox/doll-selection/planters.png`、`planters-150.png`、`planters-en-compact.png`、`planters-empty.png`，均使用正式底栏和两层标记绘制代码，覆盖中英文、缩放、窄屏、缺料和空选区；仅预览夹具的物品图标使用指令图标占位，游戏内材料行显示真实物品图标。实机多人消息传输和完整游戏输入树仍需游戏内检查。

## 选区确认操作条

「带回物品」胶囊按钮开关默认开启，挖掘/敲墙及其连锁模式共用，放置花盆界面不显示。关闭后沿用原版拆除与原地掉落，多人客户端使用 TileManipulation 的 0/2 操作让服务器生成掉落。开关立即影响后续敲击；已收集物仍正常交还。选择用 `DollBringBackItems` 随角色保存，包括没有收集物时；旧存档缺少字段默认开启，退出选区、取消任务和换世界不会重置。

开关验证：`selection.ps1` 每次额外提取正式 `SaveData` / `LoadData`，现通过 248 条选区断言与 71 条花盆断言（存档容器和引擎输入为桩），覆盖默认值、持续按住只切换一次、点击不穿透、重新打开/换世界记忆、开关值往返和旧存档兼容。FNA `selection-states.png` 包含开启与关闭、中英文及 150% 缩放。`garden.ps1` 复用真实引擎与已注册掉落钩子，另跑 `LootNativeChecks.cs.txt` 的 26 条断言，验证物块/墙开启→关闭→开启后的捕获数量、原地位置及无重复掉落；未建立实际多人连接。

`selection.ps1` 每次从正式 `DollCommandPlayer.cs` 刷新提取完整输入与标记区域（挖掘区域之前），直接链接正式 `DollCommand.cs` 与 `DollSelectionBarDrawing.cs`。引擎、指令盘回调与背包工具由桩提供，不模拟实际 WorldGen/网络。152 项断言覆盖四种工具在 100%/150%/200% UI 缩放下的草稿隔离、追加/擦除、空选区、开始提交、退出保留已有任务、取消所有任务、右键无退出作用、控件输入拦截、工具移除与选区物块消失，以及平台和种植盆的框选、连锁选择、定期校验与提交（不误选上方植物）。按钮使用原始屏幕坐标除以 UI 缩放命中，不受世界缩放坐标影响。

操作条（`DollSelectionBarDrawing`）沿用炼金手记的视觉语言：Ink / Muted / Lavender / Gold / LineColor 配色，细线框加四角折线，圆圈内放指令图标；数量文案按 `{0}` 拆开，标签用 HarmonyOS Sans SC 小字、数字用 Noto Serif SC；主按钮与「炼制」同款金色胶囊（两端为炼金手记的 `NotebookStars` 四芒星，无选区时整体 40%），次按钮「退出」为淡描边胶囊；虚线下方一行操作说明。工具色只留在世界标记上。设计稿为 `selection-bar.html`（浏览器打开，方案一为定稿）。绘制只依赖传入的像素贴图、星图集和两种字体，不链接 `AlchemyDrawing`。

`selection.ps1` 带 UTF-8 BOM 并以 UTF8 读取正式源码，Windows PowerShell 5 下中文路径与命名空间不会被误读。FNA 隐藏窗口预览直接调用正式操作条绘制，加载 KL 的 `NotoSerifSC.xnb`、`HarmonyOS_Sans_SC.xnb`、预乘 alpha 后的 `NotebookStars.png`、正式指令图标及中英文本地化。本地化共用 `Tools/LocalizationChecks/PreviewLocalization.cs.txt`，通过游戏附带的 Hjson.dll 解析实际文件，不使用逐行拆分代替语法解析。输出 `.vissandbox/doll-selection/selection-states.png`（正常、空选区禁用、150% 敲墙、英文长文案、800×600 窄屏）和各独立画面。背景为泥土色纯色，属于 FNA 离线预览；不代表游戏截图或真实 SUI 输入树测试。游戏内仍需检查世界缩放、其他面板与实际挖掘/联机生命周期。

材料菜单的 FNA 离线预览另输出 `material-menu-planters.png`、`material-menu-planters-150.png`、`material-menu-en-compact.png`、`material-menu-empty.png`；使用正式共享绘制与布局，覆盖三行、长列表、当前选中、悬停、中英文、缩放、窄屏和空列表。`selection.ps1` 共通过 152 条原有选区断言与 71 条花盆/菜单断言。

### 模组种植盆自动识别

`DollPlanterBuilding.IsMaterial` 接受原版种植盆并明确排除黏土盆；额外接受 `Main.tileSolidTop` 为 true、尺寸适合 1×1、无实体或背景墙锚点依赖且被药草底部支撑配置明确允许的物品。这里不要求 `TileID.Sets.Platforms`，也不以名称关键词判断。尺寸按所选物品样式及其替代放置方式检查；有重要帧却没有物件尺寸数据的特殊物块不自动接纳。

识别读取加载后的原版三个药草阶段、七种样式，以及 `Main.tileAlch` 标记的模组药草、内容样本中的种子样式和替代放置数据。`AnchorAlternateTiles` 需要启用底部 `AlternateTile`；`AnchorValidTiles` 需要允许顶部支撑的底部锚点，并排除 `AnchorInvalidTiles`。允许任意平台的泛化规则不作为种植盆证据。索引最多每秒刷新一次，库存菜单和执行时共用判定。

`planters.ps1` 的真实引擎夹具注册独立 ModTile，检查原版药草后续样式的模组支撑、模组药草种子样式、替代锚点、背包列表、真实 TileObject 放置与扣料，并用真实药草放置判定验证支撑。还检查土壤、普通平台、只顶部实心、超尺寸、显式无效锚点、无白名单的泛化规则和缓存刷新。此夹具没有加载第三方模组；完全依赖自定义钩子种植、未公开上述数据的模组仍需单独适配。

## 四玩偶队伍与后续素材替换

四种类型为 `KumaPetProjectile`、`SecondDollProjectile`、`ThirdDollProjectile`、`FourthDollProjectile`，固定编队编号分别为 0–3。后三种位于正式 `AdditionalDollProjectiles.cs`，暂用中性的编号名称；全部复用 Kuma 的站立三帧、移动四帧和挖矿四帧，没有复制或改色原 PNG。新素材加入后，逐个修改该类型的 `TexturePrefix`、三个动作帧数和中英文 `Projectiles.*.DisplayName` 即可，队伍成员不通过动画 ai 槽区分。

任务按物块/墙/花盆分别认领，同一类同一格最多由一只处理。优先沿用有效目标，再选最近的空闲目标；队友已认领的任务会保留，也不会因此清除等待露出边缘的墙。四只可同时挖掘、敲墙或放花盆；单只敲击间隔不变。取消统一清理队列与认领，收回一次移除整队并交还一次共享收集物。玩偶消失会释放认领，意外复用弹幕槽也不会留下锁。弹幕池至少空出四格才允许支付/召唤，避免只召到半队；已有玩偶时左键仍进入指令盘，不重复收费。

`run.ps1 -Checks` 当前通过 90 条断言，覆盖四种类型、四个站位、方向/反重力、各成员周期同步与远端仅动画、三类任务的独占认领与完成、少量任务、边缘墙解锁、混合队列、任务取消/重提、距离暂停、消失接手、死亡/离线、多人隔离、整队收回和不足四个弹幕槽时不扣费。模拟世界的同一批 24 格任务单只用 1219 帧，四只用 356 帧；该数字只用于回归分工效率，不代表实机固定倍率。原有 `selection.ps1` 的 152 条选区断言和 71 条花盆/材料断言也通过。

新增 FNA 离线预览输出 `.vissandbox/kuma-preview/team-formations.png`（向右、向左、反重力）及 `team-working.png`（四只分别处理四格），连续轨迹帧现在绘制整队。画面调用正式 AI / PreDraw，使用真实熊玩偶 PNG；灰色方块是主人位置参考，不模拟实机地图、玩家造型、光照和网络。实际多人联机仍需游戏内验证。


## 自动播种、开花收获与续种

（2026-10-27 起改为：玩偶在场时亲手或由玩偶远程放下的种植盆都会登记，见文末「远程放置物块」。）`DollGardenSystem` 只登记本版本中通过玩偶放置任务成功放下的花盆；已有世界中的手工花盆、更新前未登记的花盆不会被自动接管。花盆坐标、主人 GUID、花盆/种子稳定内容名随世界保存，主人 GUID 随角色保存；支持退出重进、角色换世界和模组物品 ID 重排。拆除花盆通过真实 `WorldGen.KillTile` 钩子解除记录，其他玩家拆除也会通知主人。缺失的模组内容保留名称，等待原模组恢复。每个主人最多登记 10000 个花盆。

原版八种种植盆按名称选对应的七种药草（腐化与猩红盆都种死亡草）；没有对应种子时等待。通用模组种植盆按主背包 0–49 槽顺序选择能合法种下的药草，首次播种后记住品种，不因缺种而换种。主人手工换成可识别的原版/已适配药草时，更新品种后继续照料。只从主背包消耗种子，不从猪猪、保险箱、虚空袋取种。

`DollHerbPlanting` 使用实际放置、物品消耗和物块钩子，确认植物仍存在才扣种。原版药草在原版花盆上走 `WorldGen.PlaceTile` 的 `PlaceAlch` 路径（原版种植盆并未列入药草的 TileObjectData 替代支撑名单）；模组种子/支撑走正常 TileObject 校验。背景查询不改写玩家放置预览。幼苗、非开花成熟药草、液体和占用空间不会被强行覆盖。

登记花盆上出现原版普通装饰草/花（`TileID.Plants`、`Plants2`）时，玩偶先执行一次除草动作，下一次动作再按原品种播种；缺种时也可除草，之后等待补种。只处理自己管理的花盆，保留种植盆和已记住的品种。除草执行时重新检查目标，遵守拆除钩子；药草三个生长阶段、未知模组植物、被适配器声明为作物的植物均不按杂草清除。普通割草掉落沿用原地掉落和原版多人同步，不作为药草收获。

原版开花判定使用 `WorldGen.IsHarvestableHerbWithSeed`，包含昼夜、雨天、月相及闪耀根/寒颤棘等阶段规则。收获调用现有 `DollTileBreaker` 原版/模组掉落路径，只移除植物格，保留种植盆。获得的同种种子优先进主人背包，下一次动作再播种；其余收集物和背包装不下的种子由玩偶携带并交还。照料独立于挖掘界面的「带回物品」选项。

`DollGardenTask` 按主人分批巡视登记花盆，四只共享候选集与独占认领，不扫描世界。显式放盆和挖掘/敲墙任务优先；超过 2400 像素暂停，走近后继续。取消任务暂停自动照料但保留世界归属；收回后重新召唤，或提交新的放盆任务，恢复照料。无种子或等待开花时释放玩偶，让它们跟随并交还掉落。放置钩子拒绝执行后退让 5 秒再试。

四种玩偶都接入 `Kuma_Use_1～4.png`：放盆、播种和收获使用空手动作，每帧 6 tick，一个完整动作 24 tick；挖掘仍用 Mine。新增 `ai[1]=3` 和 ExtraAI 照料标记。任务只由主人端执行，其他端仅动画；花盆登记、种子种类及分块归属快照使用新增 `DollGarden` 消息，实际植物放置/移除沿用原版消息。

### 自定义模组药草

符合 `Main.tileAlch`、单格尺寸与标准物件放置数据的种子可自动识别并种植。模组之间没有统一的开花帧定义，未适配植物不会猜成熟度或自动拔除。适配器为每种种子声明“哪些格子属于该药草”和“何时开花”。在 `PostSetupContent` 调用：

```csharp
if (ModLoader.TryGetMod("伊蕾娜", out Mod elaina)) {
    int tile = ModContent.TileType<MyHerbTile>();
    elaina.Call("RegisterDollHerb", ModContent.ItemType<MyHerbSeeds>(),
        (Func<int, int, bool>)((x, y) => Main.tile[x, y].HasTile && Main.tile[x, y].TileType == tile),
        (Func<int, int, bool>)((x, y) => MyHerbTile.IsBlooming(x, y)));
}
```

`IsBlooming` 是接入方自己的实际生长条件，不是 tModLoader 固定 API。使用多个 TileType 表达生长阶段时，Matches 回调应包含全部阶段。注册也可显式识别未标记 `tileAlch` 的单格草药。自动化遵守 CanPlace、CanKillTile、CanDrop、GetItemDrops 和消耗钩子；需要右键收获、保留作物主体、多格作物或自定义存储状态的植物需另写专用适配。此版本未加载任何指定第三方模组作兼容承诺。

### 验证

```powershell
& Tools/DollControlPreview/garden.ps1
& Tools/DollControlPreview/run.ps1 -Checks
& Tools/DollControlPreview/run.ps1
dotnet msbuild 伊蕾娜.csproj '-t:ResolveReferences;Compile' -p:BuildProjectReferences=false -v:quiet
```

`garden.ps1` 通过 134 条照料断言，链接正式药草规则、世界登记、照料任务、目标认领、玩偶 AI 和掉落捕获；使用真实 tML 的 Tilemap、Item、WorldGen、TileObject、ModPlayer 存档与网络消息处理器。包含七种原版药草的八种盆对应、实际种子扣除、开花收获/回种、手工换种、四只真实 AI 从播种到收获再播种、空手四帧、远端只动画、掉落交还、取消/距离暂停、玩家隔离、消失接手、存档恢复与消息校验。额外注册真实测试 ModTile/ModItem 验证自动播种、未适配不收获、适配后开花、放置/拆除/掉落钩子和原生自定义掉落；不是第三方模组测试。此夹具同时执行原有 158 条花盆检查及 26 条掉落开关检查，共 318 条。没有创建网络连接或写真实角色/世界存档；完整联机、其他模组的特殊钩子仍需实机验证。

`run.ps1` 的 FNA 离线帧表 `.vissandbox/kuma-preview/kuma-frames.png` 更新为 Idle 3 / Move 4 / Mine 4 / Use 4 共 15 列，三行分别为向左、向右、反重力，含三倍和原尺寸，调用正式 PreDraw 并读取正式 PNG。已打开检查；不等同于游戏实机截图。

除草回归新增 20 条真实引擎断言，覆盖自然装饰植物占盆、未登记花盆保护、远端不执行、先除草后耗种、支撑完整、原版各生长阶段及模组植物保护、适配器排除、虚化、缺种等待、取消，以及选中杂草后被换成幼苗的执行时复查；最后使用正式玩偶 AI 验证「杂草→空盆→幼苗」完整流程。


## 四种正式外观与敲墙动作（2026-10-25）

编队 0～3 分别使用熊、白猫、黄狗、兔子。正式资源加载 `ElainaModSkills/Skills/DollControl/NewDoll/Ready/{Bear,WhiteCat,YellowDog,Rabbit}_动作_帧.png`，共 76 张 64×64 RGBA PNG，像素大小不变，透明留边对齐身体并容纳兔耳。原始 NewDoll 文件只读保留。

NewDoll 的每套 1～4 为工具动作，5～7 为空闲姿态；熊新增的工具动作为 Hammer。熊保留旧 Mine、Move、Use 动作，新 Idle 与 Hammer 采用用户新增素材。其他三只的 Mine 与 Idle 使用各自原图，Use 用像素掩码去除镐与残影、按原配色补手、用同姿态空闲头部恢复被工具挡住的耳朵。Hammer 从熊工具层提取，并按每帧握持位置复用；抬起的锤头位于头部前方。其他三只未提供单独 Move，使用 Idle 的 1→2→3→2 作为四帧浮动移动姿态，不借用熊的身体。

生成与校验：

```powershell
python Tools/DollSpriteBake/bake.py
python Tools/DollSpriteBake/bake.py --check
& Tools/DollControlPreview/run.ps1
& Tools/DollControlPreview/garden.ps1
```

生成器位于构建排除的 Tools，清单 `NewDoll/Ready/manifest.json` 保存源 PNG 校验和、正式帧像素校验和和画布尺寸。`--check` 验证全部正式 PNG、透明度及未裁剪；正式运行时只读取打包 PNG，不生成贴图。

正式 AI 新增 `ai[1]=4` 的 Hammer 状态：到达敲墙目标时使用 Hammer 四帧，每帧 6 tick；挖物块仍使用 Mine，放盆、播种、收获、除草使用 Use。既有 ExtraAI 的 workWall 同步控制远端动画，工具敲击间隔与任务逻辑不变。中英文显示名称更新为对应动物名称，保持原弹幕类名和编队槽位。

`run.ps1` 通过 97 条断言，新增四只挥锤四帧、敲墙/挖物块切换与四种远端只动画检查。FNA 从正式 PreDraw 加载全部 76 帧，每只分别输出 `doll-0-frames.png`～`doll-3-frames.png`（每只 19 列，依次 Idle 3 / Move 4 / Mine 4 / Use 4 / Hammer 4；三行左右朝向及反重力）。`kuma-frames.png` 保留为熊帧表别名，`team-formations.png` 显示四种正式外观。已打开检查原尺寸及三倍效果；为离线预览而非游戏截图。种植/除草/掉落原生检查仍通过 318 条，完整实机多人未在本轮重测。

## 挖掘预检与模式按钮（2026-10-25）

指令盘只保留「挖掘物块」「敲掉墙壁」两个拆除入口，打开时默认框选。操作条左侧的「连锁挖掘／连锁敲墙」按钮开启后切换为点击相连同类，关闭后恢复拖动框选；切换保留草稿并吞掉本次按钮点击。右侧为「带回物品」，仍随角色保存。两个整行胶囊均明确显示开启/关闭，开启为金色，关闭为淡紫色，不再使用小方框勾选。指令枚举的原数值保留，轮盘通过 `DollCommands.AtSlot` 显式映射五个可见入口。

`DollMiningRules` 在草稿修改后、编辑期间每 15 tick 和点击开始时检查当前工具，选区含任何工具能力/进度不满足的目标就显示红色原因并禁止整个草稿提交，保留选区供更换工具或取消部分目标。物块调用真实原版/模组镐力计算；缺锤、地牢墙未解锁、神庙墙未解锁分别提示。墙边缘等会随任务推进解除的暂时条件不当成锤力不足。查询隔离 `hitTile` 并恢复 `Main.blockMouse`，避免预检影响玩家输入或已有裂纹。实际目标执行复用同一规则；模组在真正拆除钩子中临时否决的情况仍由执行阶段处理。

`selection.ps1` 通过 313 条选区断言及 71 条花盆断言，新增六入口映射、三种缩放下按钮长按/输入拦截、保留草稿、连锁与框选来回切换、混合选区拒绝、装备/进度变化和再次提交检查。FNA `selection-states.png` 包含中英文、100%/150%、开启/关闭、镐力不足与神庙墙警告，直接链接正式绘制代码，已打开查看。

`garden.ps1` 新增 `MiningNativeChecks.cs.txt` 的 23 条真实 tML 断言，覆盖五类原版门槛、ModTile.MinPick、地牢/神庙进度、普通木锤、墙边缘延迟与玩家鼠标/裂纹状态保护，连同原有检查共 341 条。未连接真实多人会话。六入口轮盘 FNA 预览由 `Tools/SkillWheelPreview/run.ps1 -DollCommands` 重建；工具与输出仍排除正式模组编译/打包，正式 UI 复用已有纹理。

玩偶指令盘已移除中央指令名、取消/未开放提示及底部操作说明，只保留图标、封印和选择动画；挖掘选区面板的条件不足提示仍保留。

## 空闲玩偶的屏幕内宝箱交互（2026-10-25）

宝箱改为被动辅助：有至少一只自己的空闲玩偶时，鼠标右键当前游戏画面内的远处普通宝箱后，空闲玩偶先飞到箱旁，播放完整空手动作，再执行原本的右键行为。四只都忙时恢复原版距离；近距离开箱始终保留。只识别 TileID.Sets.BasicChest，包含按标准注册的模组宝箱；门、床、告示牌、开关、梳妆台和其他储物系统不扩展。世界镜头缩放参与屏幕判断，全屏地图与鼠标被界面拦截时不能远程开箱。

旧的独立开箱指令、选择面板、飞行任务、自建钥匙规则和自定义开箱网络协议已移除；轮盘现保留五个任务入口。此前制作的宝箱 PNG 与 Tools/DollCommandIcons/BuildOpenChestIcon.py 保留，当前被动功能不需要新的贴图。

DollChestReachSystem 仅替换原版 LookForTileInteractions 内的距离调用点，以及 HandleBeingInChestRange 内的界面维持距离调用点。公共 IsInTileInteractionRange / IsInInteractionRangeToMultiTileHitbox 函数与全局工具距离均保持原样，模组自己调用这些函数仍得到原本结果。实际 TileInteractionsUse、ModTile.RightClick、钥匙与进度检查、占用判断、陷阱行为和物品同步全部沿用原有路径。原版上锁箱仍按原版节奏先解锁，再右键打开。模组自定义距离和其他条件不绕过。

DollChestReach 保留一只空闲玩偶维持当前宝箱，它会飞到箱旁并在查看期间停留，关闭界面后返回；等待原版多人开箱响应时最多保留 300 tick。借用期间不接取挖掘、放盆或照料目标。收回/死亡、箱子消失或替换、镜头移出宝箱后释放。维持已有界面时允许屏幕边缘 64 像素缓冲；结束代理时若已在原版近距离内，保留普通开箱状态。没有新增 ModPacket；只观察原版 RequestChestOpen 来识别异步等待。

验证：

```powershell
& Tools/DollControlPreview/garden.ps1
& Tools/DollControlPreview/run.ps1 -Checks
& Tools/DollControlPreview/selection.ps1
& Tools/SkillWheelPreview/run.ps1 -DollCommands
& Tools/LocalizationChecks/parse.ps1
python Tools/LocalizationChecks/check.py
dotnet msbuild 伊蕾娜.csproj '-t:ResolveReferences;Compile' -p:BuildProjectReferences=false -v:quiet
```

ChestNativeChecks.cs.txt 链接正式源码与真实 tML 引擎，运行真实 LookForTileInteractions、TileInteractionsUse、宝箱界面距离检查和实际注册的 ModTile.RightClick。48 条宝箱检查（另有 14 条角标检查）覆盖延迟右键、飞行/空手/等待/返回、重复点击、途中取消/替换/销毁/钥匙移除、输入现场恢复、远端只动画、物品身份保留、空闲分派、全忙、公开距离函数不变、镜头/缩放/地图、其他交互不扩展、消失/死亡、原版钥匙、自定义模组钥匙与自主距离拒绝，以及原版多人请求、异步响应状态和超时。运输端被截获，响应玩家字段由夹具设置，没有建立实际网络连接。与原有检查合计 403 条。四玩偶 AI 97 条、选区 313 条、放盆界面 71 条回归也通过。

FNA 六入口轮盘预览输出 .vissandbox/skillwheel-preview/output/doll-idle-chest-reach-1.0.png 与 -1.5.png，调用正式轮盘绘制、正式图标并已打开检查。它只验证移除独立宝箱入口后的外观，不是远程开箱实机截图。完整游戏输入与真实多人连接仍需实机验证。

### 远程开箱动画

`DollChestReach` 在远程右键时保存本次目标和原始交互委托，点击时不打开界面、不扣钥匙。`DollPetProjectile` 复用正式 Move 与 Use 帧飞到宝箱旁，完成约 24 tick 的空手动作后，仅主人端执行一次原来的 TileInteractionsUse。执行时暂时恢复原点击格和必要的右键状态，finally 恢复玩家此刻的输入；不移动玩家本人、不重写 ModTile.RightClick、不扩大模组自身查询的距离。原版上锁箱仍按原版先解锁、再次右键打开的节奏。重复点击同一目标不重启动作；取消、Esc、死亡、目标被拆除/替换、打开其他交互界面或目标离开屏幕范围会撤销尚未执行的操作。

开箱目标与等待阶段通过 ExtraAI 同步，其他客户端只播放移动/动作，不执行开箱或扣钥匙。等待原版多人响应的 300 tick 从动作完成后开始。近距离自行开箱和关闭已经打开的宝箱仍立即交给原版。

运行 `& Tools/DollControlPreview/run.ps1` 后执行 `python Tools/DollControlPreview/chest-gif.py`，生成 `.vissandbox/kuma-preview/chest-approach/chest-approach.gif` 和 `chest-approach-sheet.png`。预览直接使用正式玩偶 AI、PreDraw 和已打包的动作 PNG；玩家/宝箱为中性场景占位，开箱回调由夹具模拟，真实交互在原生检查中另行验证。已查看飞行、伸手、箱旁等待与返回阶段；这是 FNA 离线预览，不是游戏录像。

## 召唤/收回特效（2026-10-25）

收回不再立即移除弹幕：`Dismiss` 交还收集物、释放任务后进入收回状态（`ai[1] = 5`），播放约 26 tick 的收缩星屑动画（后排先走，每只间隔 6 tick）后由主人端移除；`FindOwned` 与远程开箱选玩偶会跳过收回中的玩偶。召唤时按编队依次从星芒中弹出。外观与预览见 `Tools/DollSummonPreview/README.md`。桩环境新增 `Main.dedServ`、`Projectile.localAI` 和空的 `DollSummonDust.Burst`；`Preview.cs.txt` 的帧表用 `Shown()` 跳过出现动画。

## 种植盆独立放置（2026-10-25）

移除底部平台、平台选择行及平台缺料提示；框选与正式标记改为两格一组。任务队列只记录种植盆物品类型。黏土盆及需要支撑的模组盆器不参与自动放置和维护，旧黏土盆归属即使经过存档恢复也不再触发播种或收获。旧建筑本身保持原样。

本次验证：`planters.ps1` 58 条真实引擎断言；`garden.ps1` 合计 308 条（含上述放置检查、139 条照料检查和已有掉落/挖掘/宝箱检查）；`selection.ps1` 313 条选区检查与 64 条种植盆检查；`run.ps1 -Checks` 100 条 AI 检查。主项目 ResolveReferences/Compile 通过。花园夹具补链正式召唤时序类，粒子渲染在无窗口宿主中使用空实现。

FNA 离线预览已检查 `planters-150.png`、`material-menu-planters-150.png` 和 `planters-en-compact.png`：只有一行种植盆选择，框选只含生长空间与种植盆，覆盖中文、英文和 150% 缩放。预览物品图标为占位；真实联网与游戏内输入仍需实机验证。上文历史检查数字对应各阶段，本段为本次结果。

2026-10-27：按用户要求移除人偶攻击占位，删除枚举、执行分支、封印标记和中英文条目。现有 Mine/BreakWall/Cancel/Dismiss/PlacePlanters 编号保持不变，轮盘按五等分绘制。`pwsh -File Tools/SkillWheelPreview/run.ps1 -DollCommands` 复用正式绘制和图标生成 100%／150% FNA 离线预览；完整游戏输入仍需实机验证。

## 远程放置物块（2026-10-27）

不是指令，与远程开箱同属被动辅助。手持可放置物块（`createTile >= 0`，含魔杖类）且至少有一只自己的玩偶在场时，点击原版放置距离外、但仍在当前画面内的位置，由玩偶飞过去放下；距离内完全走原版。光标显示原版的物块图标并叠加熊头像角标（`DollChestCursorSystem`）。

`DollPlacementSystem` 只在这一次调用里放宽距离：给 `PlaceThing_Tiles` 临时加大 `blockRange`、关闭物块替换（替换会在远处直接挖掉旧块），让原版完成全部判定（贴邻规则、TileObject 锚点、绳索/火把/种子等特殊规则、模组 `TileLoader.CanPlace`）。走到 `PlaceThing_Tiles_PlaceIt` 时改为登记待放置：不设置使用间隔、不触发 `UseItem`，用 `ItemCheckContext.SkipItemConsumption` 跳过这次物品消耗，魔杖材料同理不扣。智能光标打开时，`SmartCursorLookup` 的可达范围同样临时放宽到恰好覆盖屏幕，沿线铺块、自动寻找落点等原版行为在远处照常可用，结束后恢复原值。

待放置（`DollPlacementTask`）记录光标格、实际占用区域（物件按 TileObjectData 尺寸）、物品与登记时的朝向，只在主人端存在、不存档。占用区域互不重叠；同时排队数量受背包库存限制（消耗品按同种物品合计，魔杖按材料合计，`ForceConsumption = false` 等不消耗的物品不限），上限 1000。每半秒放弃超出 2400 像素的远处目标，库存减少时从最晚登记的开始取消多出的部分。世界中以淡紫色底框和半透明物品图标显示（`DollMarkSystem`）。「取消任务」一并清空。

玩偶按最近距离认领待放置（与挖掘/敲墙独立认领），优先级在开箱、交还箱子货物之后，挖掘之前；到位后播放 Use 动作，主人端以主人身份在该格回放一次原版 `PlaceThing`：手持临时换成背包里同种物品、朝向取登记时的朝向，结束后选中格、光标格、使用计时、距离、物块替换开关和放置预览全部还原。确实放下后才扣一个物品（魔杖扣一份材料），均经过 `ItemLoader.ConsumeItem`；被占用或不再合法的目标直接放弃、不扣物品。物块变化走原版网络消息。

原「放置种植盆」指令移除（轮盘恢复四项，枚举编号保留，`Commands/DollCommand_PlacePlanters.png` 与图标生成入口删除）。照料改为：玩偶在场时放下的种植盆（亲手或由玩偶远程放置，`DollPlanterBuilding.IsMaterial` 判定）都会登记给自己的玩偶照料；不在场时放下的、或其他玩家的种植盆不会被接管。照料不会占用即将放置物块的格子。

验证：

```powershell
& Tools/DollControlPreview/planters.ps1
& Tools/DollControlPreview/run.ps1 -Checks
& Tools/DollControlPreview/selection.ps1
& Tools/SkillWheelPreview/run.ps1 -DollCommands
```

`planters.ps1` 新增 `PlacementNativeChecks.cs.txt`（宿主 `PlacementNativeHost.cs.txt` 只替换玩偶弹幕、箱子辅助与花园登记为记录器），链接正式 `DollPlacementSystem`、`DollPlacementTask`、`DollWorkAssignments`，在真实 tML 中加载 On_ 钩子并调用原版 `Player.PlaceThing` 与 `SmartCursorHelper.SmartCursorLookup`。22 条断言覆盖：越界排队不放置且跳过消耗、熊头像悬停、重复格拒绝、库存上限、原版贴邻规则、屏幕外、无玩偶保持原版、回放放置与单次扣除、回放后玩家状态还原、被占用目标放弃、工作台 2×1 占位与重叠拒绝、TileObject 回放、远程与近距离种植盆登记、库存减少取消、智能光标远处落点并排队、无玩偶时智能光标保持原版距离。连同原有 58 条共 80 条。种植盆旧检查改用夹具专用 `PlanterFixture.cs.txt` 摆盆，实际放置仍调用正式 `PlaceOne` / `IsMaterial`。

`run.ps1 -Checks` 253 条（提取正式 `TrySelectPlacementTarget`，放置动作由替身计数）、`selection.ps1` 313 条选区断言均通过；原 `PlanterSelectionChecks.cs.txt` 与种植盆/材料菜单 FNA 预览删除。四项轮盘 FNA 预览 `.vissandbox/skillwheel-preview/output/doll-idle-chest-reach-1.0.png` 已查看。

游戏内输入、连续拖动手感与真实多人连接未在本轮实机验证。（当时 `garden.ps1` 无法编译的问题已在下文「远程箱子放入」一节修复。）

### 拖动连续放置与悬空（2026-10-27 补充）

- 远程登记不再受使用间隔节流。按住拖动时，`DollPlacementSystem` 记下上一帧的光标格，把两帧之间经过的格子按四连通逐格补齐（斜向拖动得到阶梯，单帧最多 256 格），每格都完整走一遍原版判定后再登记；够得着的格子仍交给原版，屏幕外的不登记。新的一次按下不会和上一笔连起来；智能光标每帧自行选落点，此时不补格。
- `PlaceThing_Tiles_BlockPlacementForAssortedThings` 只在远程登记与回放时、原版判定失败且目标格为空时补充放行：平台（`TileID.Sets.Platforms`）、种植盆（`TileID.PlanterBox` 与 `DollPlanterBuilding.IsMaterial` 识别的模组盆）可以悬空；普通实心块（实心、非顶部实心、无特殊帧，排除草种、苔藓和再生工具）贴着其他待放置格也算有支撑，所以从地面拖出的整条线、往空中拉的柱子不会因前一格尚未放下而断开。回放时按同一规则，先放到的格子可以暂时悬空。距离内的亲手放置仍完全按原版贴邻规则。
- `planters.ps1` 的远程放置检查增至 32 条，新增：悬空平台与种植盆的登记、回放、扣料与登记照料；悬空泥土块仍被拒绝；距离内悬空平台保持原版；待放置支撑的竖列与提前回放；一帧跳 6 格的地面补齐、向上拖入空中、斜向阶梯、新一笔不相连、平台悬空拖成一行。连同原有检查共 90 条。`run.ps1 -Checks` 253 条、主项目编译通过。游戏内拖动手感未实机验证。

### 待放置虚影（2026-10-27 补充）

待放置不再显示物品图标，而是即将放下的物块的半透明虚影（`DollPlacementGhost`）。登记时记下物块类型、样式、物件替代样式与随机样式。家具等按物件数据放置的物块直接调用原版放置预览 `TileObject.DrawPreview`（与亲手放置时光标处的虚影相同，白色半透明、不受光照）；平台、原版种植盆和普通实心块按原版 `WorldGen.TileFrame` 的接缝规则，以相邻的真实物块与其他待放置格选帧：平台看左右的平台/实心块，种植盆看左右种植盆，普通方块看上下左右同类及四面相邻时的斜角内角，三种随机变体按坐标固定避免拖动时闪烁。不含与其他种类物块的融合边和斜坡。其余物块（火把、绳索等）仍退回淡紫色占位框加物品图标。

`planters.ps1` 的远程放置检查增至 36 条：登记记录的物块类型（泥土、工作台物件、平台、种植盆），以及在真实引擎中摆出 44 格泥土（覆盖全部 16 种相邻组合及四种内角）、平台（两端、单块、贴实心块）和种植盆，调用原版 `WorldGen.TileFrame` 后逐格比对虚影帧，全部一致。连同原有检查共 94 条。游戏内实际观感未截图验证。

### 登记即带走物品（2026-10-27 修正）

修复：鼠标抓着物块远程放置时物品不减少。抓在鼠标上的物品位于第 58 格，原版每帧用 `Main.mouseItem` 的副本覆盖该格，旧实现等到玩偶放下时才在这一格扣数量，扣掉的只是副本。

现在登记待放置的瞬间，玩偶就从背包带走一份（`DollCommandPlayer.TryCarry`，经过 `ItemLoader.ConsumeItem`）：消耗品取手上的这一个，抓在鼠标上时同步写回 `Main.mouseItem`；魔杖取一份材料，魔杖本身不动；钩子不允许消耗或物品本不消耗时什么也不带。待放置记下所放物品的单个副本与带走的那一份。玩偶到位后，回放时把手上临时换成这件物品，放置本身不再扣背包；魔杖回放时不再要求背包里仍有材料（`PlaceThing_Tiles_CheckWandUsability` 在回放时放行）。取消任务、收回玩偶（两者都走 `ClearTasks`）、目标放不下、主人走出 2400 像素时，带走的物品放回背包，装不下的掉在主人脚下。存档时尚未放下的物品并入玩偶携带物（`DollChestCargo`），下次召唤后由玩偶交还；不再按库存计算排队上限，手上没有物品后自然无法继续登记。

`planters.ps1` 的远程放置检查增至 45 条，新增登记即扣、放下不再扣、放不下归还、取消归还、鼠标抓取（含最后一个清空鼠标与取消后归还）、魔杖带走材料并在材料已空时完成回放。连同原有检查共 103 条；`run.ps1 -Checks` 253 条、`selection.ps1` 选区断言、主项目编译均通过。

## 远程箱子放入（送货玩偶）

玩偶远程打开的箱子（主人本人够不着时）现在也能放入，需要至少两只玩偶：开箱玩偶守着箱子，另一只空闲或只在照料种植盆的玩偶当送货员（`DollChestDeposit.cs`）。鼠标物品点箱子格、Shift 点击背包物品、存入全部、快速堆叠都不直接写箱子：放得下的部分（按原版 `TryPlacingInChest` 的顺序对“箱子现状 + 在途放入”预先计算）立刻从背包/鼠标交到送货玩偶手上，它飞到箱子另一侧、播完 Use 动作后才真正放入并发原版 `SyncChestItem`。途中再放入的物品由同一只玩偶一起送；箱子已关闭、被替换或放不下的部分转为它的携带物，回到主人身边抛还；送货玩偶失效、收回或主人死亡时同样转为携带物，存档时并入 `DollChestCargo`。没有能送货的玩偶时拦下并提示。快速补货属于取出，由开箱玩偶按原版补货结果（补满背包已有的可堆叠种类、弹药在有空格时整组带走，扣掉玩偶已带着的同类）带回。存入全部、快速堆叠、补货三个按钮在远程状态下恢复显示。

`garden.ps1` 恢复可编译：宿主 `GardenNativeHost.cs.txt` 只保留挖掘目标与登记的桩，箱子货物、送货、挖掘搬运和本地化改为链接正式 `DollChestCargo` / `DollChestDeposit` / `DollChestTransferSystem` / `DollMiningCargo` / `UIText`，收集物交还与存档从 `DollCommandPlayer.cs` 的「收集物」region 每次提取生成，不再手写替身。离线宿主补注册 tML 内置的 bool / Point / Vector2 存档序列化器（正式加载时自动完成）。照料夹具按现行语义改写：挂起的任务（`HoldGardenForTask` 登记一个占位标记）暂停自主照料，任务清空后恢复；跟随交还改为检查收集物已清空。宝箱夹具按现行 `FindChestHelper` 改写：四只都在工作时仍借一只开箱，有空闲时优先空闲。

`ChestDepositNativeChecks.cs.txt` 在同一真实引擎夹具中调用原版 `ItemSlot.LeftClick`、`ChestUI.TryPlacingInChest` / `DepositAll` / `QuickStack` / `Restock`，经正式钩子与玩偶 AI 运行。37 条断言覆盖：只有开箱玩偶时拦下、物品留在鼠标且提示不刷屏；挖掘中的玩偶不当送货员；点击格后物品立即交给另一只玩偶、箱子不变；飞行途中不放入；远端副本只播动画；Use 动作后在箱子另一侧放进点击的格子；Shift 悬停与放入、后续放入并入同一只、前缀保持、同类先堆叠；存入全部跳过快捷栏/收藏/弹药栏；快速堆叠只放已有种类、不放钱币；补货由开箱玩偶带回且只补到满；满箱只带走放得下的部分且计入在途放入；多人发 `SyncChestItem`；途中关箱、送货玩偶失效、收回、存档往返后物品各交还一次且不进箱。变异验证：把放入改成交接时立即完成，检查即失败。

本次验证：`garden.ps1` 全部通过（掉落 26、照料 139、挖掘 23、角标 14、送货 37、宝箱 48；宝箱一行输出的 99 条已含角标与送货，连同花盆放置检查合计 345 条）；`planters.ps1` 45 + 103 条；`run.ps1 -Checks` 255 条；Mod 编译 0 错误。存档往返用无前缀物品，因为离线宿主未初始化前缀集合，`ItemIO.Load` 无法还原前缀；前缀保持由放入流程本身的断言覆盖。未建立实际多人连接，也未实机验证。

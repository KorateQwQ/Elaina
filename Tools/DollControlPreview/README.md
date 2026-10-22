# 熊玩偶与共用玩偶 AI

正式源码在 `ElainaModSkills/Skills/DollControl/`。`DollControlSkill` 沿用独特魔力支付接口，召唤消耗 20 魔力，基础冷却 1 秒。每名玩家保留一只熊玩偶，持续存在直到「收回玩偶」、死亡、离线或离开世界。玩偶在场时魔杖不再挥动，左键改由 `DollCommandPlayer` 处理：按住打开 `DollCommandWheel` 指令盘（挖掘物块、连锁挖掘、敲掉墙壁、连锁敲墙、攻击〔未实装〕、取消任务、收回玩偶），选中标记工具后左键在世界中框选（连锁版为点击整组同类），再次框选/点击反选，右键退出。标记仅本地可见、切换术式后仍显示（`DollMarkSystem`）。玩偶以背包最大镐力的镐子挖实心物块、最大锤力的锤子敲墙，掉落物由 `DollTileBreaker` 捕获进主人的 `DollCommandPlayer`（随角色存档），任务结束回到主人身边、收回或重新召唤时抛还给主人。无召唤栏消耗或原版宠物互斥。

`DollPetProjectile` 为可复用的抽象弹幕基类。新增玩偶只需继承它并提供 `TexturePrefix`、`MoveFrameCount`（可选 `IdleFrameCount`、`MineFrameCount`），资产约定为 `<prefix>_Idle_N.png`、`<prefix>_Move_N.png`、`<prefix>_Mine_N.png`（N 从 1 开始）的连续独立 PNG，各动作按顺序循环；无需拼纵向帧表。每张以贴图中心为原点绘制，各帧画布尺寸可以不同，只要角色身体在各自画布中心的相对位置一致。有挖掘目标时飞到目标格朝向主人的一侧（`WorkOffset`），到位后面向格子播放挖矿帧，主人端按工具节奏敲击；无目标时移动 > 待机。

默认跟随位置为身后 56、上方 36 像素；距离玩家超过 1200 像素时传送。速度会追随高速玩家，平滑靠近目标，浮动周期 90 帧，移动浮动目标幅度 4 像素、待机 1.5 像素。待机每张 12 帧、移动每张 8 帧、挖矿每张 6 帧。以上参数、贴图原始朝向、存活条件、跟随目标均可通过受保护属性/方法覆写。`AI` 使用 `ai[0..2]` 保存浮动相位、移动状态和动画计时；子类扩展其他逻辑时应先调用 `base.AI()` 并检查弹幕仍然 active，勿复用这三个槽位。

熊移动帧来自用户编辑的 `wanouyiudong2/wanou2.png`～`wanou5.png`，原字节不变，为 `Kuma/Kuma_Move_1.png`～`Kuma_Move_4.png`（32×32）。站立与挖矿帧的绿幕原图保存在 `sources/`（128×96），由 `python Tools/DollControlPreview/cutout_kuma.py` 按精确背景色扣除并以画布中心对称裁成 48×48 的 `Kuma_Idle_1～3`、`Kuma_Mine_1～4`；`--check` 校验正式 PNG 与原图一致。原图面向左，向右时水平翻转；反重力时偏移和贴图上下翻转。

召唤只在技能所属本地客户端创建；弹幕通过原版同步传给其他端。传送、状态切换、目标格变化（`SendExtraAI`）及每个浮动周期发送更新，`netImportant` 支持晚加入玩家。其他端按同步的目标格推进 AI，只播动画不敲击；物块与墙的拆除由主人端经原版消息同步。死亡/离线清理，不把它设为原版单一宠物。

## 验证与重现

在模组根目录执行：

```powershell
& Tools/DollControlPreview/run.ps1 -Checks
& Tools/DollControlPreview/run.ps1
dotnet build 伊蕾娜.csproj --no-restore -v quiet -t:Compile -p:BuildProjectReferences=false
```

`-Tml` 可指定 tModLoader 安装路径。检查与预览直接链接 `DollPetProjectile`、`KumaPetProjectile`、`DollControlSkill` 三个正式 C# 文件；`DollCommandPlayer` 依赖 WorldGen 与网络，用按队列给目标格的替身代替，因此标记、框选、镐力判定、掉落捕获和指令盘不在离线检查范围内。检查覆盖扣费、资源不足、弹幕池满、玩偶在场时不挥杖不扣费、多人隔离、静止、高速移动、转向、浮动幅度、四帧动画、停下、传送、挖掘目标的站位/朝向/敲击间隔/四帧挖矿、目标同步与其他端只播动画、挖完回到身边交还收集物、收回与重新召唤交还、反重力、服务器生成限制和死亡/离线清理。引擎、技能资源宿主和网络传输为适配桩，不等同于实机联网测试。

FNA 离线预览运行隐藏窗口，直接调用正式 `PreDraw` 并读取正式 PNG，结束后自动退出。输出为 `.vissandbox/kuma-preview/kuma-frames.png`（每行依次为 Idle 1～3 / Move 1～4 / Mine 1～4；三行分别为向左、向右、反重力；每格含 3 倍和原尺寸），以及 `frames/000.png`～`149.png`（10 秒的待机→向右→停止→向左→停止→飞去挖地面里的一格标记）。灰色人物是位置参考；不模拟地图、真实玩家造型、光照和网络延迟。

工具源码使用 `.cs.txt`，生成工程位于 `.vissandbox`；两者均在模组打包排除目录，工具源码不参与正式模组编译。应用到游戏需要构建并重新加载模组。若命令行完整构建返回 TML003，表示游戏正在使用此模组；可在游戏内执行「构建并重新加载」，或关闭游戏后再完整构建。

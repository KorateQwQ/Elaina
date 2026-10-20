# 熊玩偶与共用玩偶 AI

正式源码在 `ElainaModSkills/Skills/DollControl/`。`DollControlSkill` 沿用独特魔力支付接口，每次施法消耗 20 魔力，基础冷却 1 秒。每名玩家保留一只熊玩偶，重复施法支付同样费用并召回已有玩偶；死亡、离线、离开世界后不保留，重新施法召唤。当前无攻击、采集、召唤栏消耗或原版宠物互斥。

`DollPetProjectile` 为可复用的抽象弹幕基类。新增玩偶只需继承它并提供 `TexturePrefix`、`MoveFrameCount`，资产约定为 `<prefix>_Idle.png` 与 `<prefix>_Move_1.png` 开始的连续独立 PNG。待机一帧，移动帧按顺序循环；无需拼纵向帧表。

默认跟随位置为身后 56、上方 36 像素；距离玩家超过 1200 像素时传送。速度会追随高速玩家，平滑靠近目标，浮动周期 90 帧，移动浮动目标幅度 4 像素、待机 1.5 像素。移动动画每张 8 帧（约 0.133 秒）。以上参数、贴图原始朝向、存活条件、跟随目标均可通过受保护属性/方法覆写。`AI` 使用 `ai[0..2]` 保存浮动相位、移动状态和动画计时；子类扩展其他逻辑时应先调用 `base.AI()` 并检查弹幕仍然 active，勿复用这三个槽位。

熊贴图来自用户当前编辑后的 `wanouyiudong2/wanou1.png`～`wanou5.png`，原字节不变，依次移动为 `Kuma/Kuma_Idle.png`、`Kuma/Kuma_Move_1.png`～`Kuma/Kuma_Move_4.png`。原图面向左，向右时水平翻转；反重力时偏移和贴图上下翻转。

召唤只在技能所属本地客户端创建；弹幕通过原版同步传给其他端。传送、状态切换及每个浮动周期发送更新，`netImportant` 支持晚加入玩家。其他端自行推进确定性 AI；死亡/离线清理，不把它设为原版单一宠物。

## 验证与重现

在模组根目录执行：

```powershell
& Tools/DollControlPreview/run.ps1 -Checks
& Tools/DollControlPreview/run.ps1
dotnet build 伊蕾娜.csproj --no-restore -v quiet -t:Compile -p:BuildProjectReferences=false
```

`-Tml` 可指定 tModLoader 安装路径。检查与预览直接链接三个正式 C# 文件。检查覆盖扣费、资源不足、弹幕池满、重复召回、多人隔离、静止、高速移动、转向、浮动幅度、四帧动画、停下、传送、反重力、服务器生成限制和死亡/离线清理。引擎、技能资源宿主和网络传输为适配桩，不等同于实机联网测试。

FNA 离线预览运行隐藏窗口，直接调用正式 `PreDraw` 并读取正式 PNG，结束后自动退出。输出为 `.vissandbox/kuma-preview/kuma-frames.png`（每行依次为 Idle / Move 1～4；三行分别为向左、向右、反重力；每格含 3 倍和原尺寸），以及 `frames/000.png`～`119.png`（8 秒的待机→向右→停止→向左→停止）。灰色人物是位置参考；不模拟地图、真实玩家造型、光照和网络延迟。

工具源码使用 `.cs.txt`，生成工程位于 `.vissandbox`；两者均在模组打包排除目录，工具源码不参与正式模组编译。应用到游戏需要构建并重新加载模组。若命令行完整构建返回 TML003，表示游戏正在使用此模组；可在游戏内执行「构建并重新加载」，或关闭游戏后再完整构建。

# 普通角色脚底对齐预览

运行：`pwsh -File Tools/PlayerPosePreview/run.ps1`。输出位于 `.vissandbox/player-pose-preview`，隐藏 FNA 窗口运行后自动退出，不启动或修改真实游戏与存档。

`standing-comparison-4.00.png` 与 `standing-comparison-1.25.png` 从左到右为：向右修正前、向右修正后、向左修正前、向左修正后、翻书参考。为凸显鞋底被埋的问题，场景地面在人物之后覆盖绘制；这是一条平面参考线，不是完整的 Terraria 瓦片绘制顺序。

直接链接正式 `System/ElainaDrawAlignment.cs`、`ElainaNewBodyLayer` 与 `ElainaHatLayer`；腿部使用安装版本真正的 `PlayerDrawLayers.DrawPlayer_13_Leggings`，素材使用正式身体、腿部、头部、帽子和翻书 PNG。角色身份与炼金状态是轻量隔离夹具，头部使用原版装备帧基准合成，光照、完整角色渲染器和输入未模拟。白像素加载原版 MagicPixel.xnb，无正式贴图生成或修改。

2026-10-26：真实腿部 DrawData 和贴图 alpha 测量确认普通鞋底低于角色碰撞底面 4 像素，来自原版 56 像素装备帧保留的底部留白。新 ModPlayer 对普通正重力立绘的绘制位置与手持物位置统一上移 4 像素，保持真实人物位置不变；头像、死亡、坐姿、睡眠、坐骑、变身、炼金及倒重力不应用这项修正。左右方向在两种缩放下均测得修正后鞋底与地面齐平，已打开检查生成图片；特殊姿势偏移保护断言通过，完整模组编译通过。实际游戏中的走动、跳跃、持物与其他模组绘制叠加仍需重新加载后验证。

## 暂停与主界面外观缓存检查

同一脚本还运行 `AppearanceChecks.cs.txt`，直接链接正式 `ElainaAppearanceSystem`，调用安装版本真正的 `PlayerDrawSet.BoringSetup`、`HeadOnlySetup` 及头部/腿部绘制。光照固定为白色，宿主窗口与角色身份使用夹具，装备 ID 来自实际 `Item`；正式身体、帽子与原版星云 GlowMask 均加载真实资源。

`appearance-paused-4.00.png` / `appearance-paused-1.25.png` 的三列依次为巫师帽、仅星云头盔、星云整套；上排复现先生成原版状态再替换装备的旧流程，下排是提前设置伊蕾娜外观的新流程。预览真实复现了脸部/帽子上的紫色尖角及星云腿部发光残留，已打开两种缩放确认修正后的外观。

2026-10-26：642 项断言通过，包括正常游戏/暂停/主菜单、原版头像准备、全部 282 个头部装备槽、两个正式渲染入口的调用先后、非伊蕾娜直通、原装备与角色皮肤/袍装标记恢复、嵌套绘制与异常恢复，以及重复暂停/菜单绘制不推进帽子掉落计时。正式装备隐藏皮肤/手臂标记在 PostSetupContent 初始化，绘制阶段不再修改全局装备表；材质、着色、头发、发光和复合手臂状态由原版按当前临时外观一次性计算。装备物品槽、属性、染色和饰品不改动。暂停期间也禁止旧 DrawEffects 分支推进动作计数、触发物品/变身生成或播放睡眠声。

此预览没有运行真实游戏 UI、其他模组渲染器或多人客户端；更新需重新加载模组后验证实际暂停、角色选择界面、坐骑与整合环境。所有正式贴图仍为已有打包资源。

## 睡眠小地图头像回归

2026-10-27：同一脚本新增 `SleepPortraitChecks.cs.txt`，链接正式 `SleepLayer`，通过实际头部父子图层递归和 `ElainaAppearanceSystem.DrawHead` 验证头像。`sleep-portrait-4.00.png` 与 `sleep-portrait-1.25.png` 的两列分别为向右、向左；三行分别为旧头像问题复现、修正后的头像、保留的世界睡姿图层。第三行只检查睡姿图层，不模拟床或完整世界变换。

`AfterParent(Head)` 的子图层不会再经过头像入口的 `IsHeadLayer` 筛选，因此睡姿图层必须明确排除 `headOnlyRender`。睡眠还会把装备帧移到空白区域；头像钩子临时选用正常头部帧并在 finally 中恢复，避免头像消失或污染世界姿态。检查覆盖左右朝向、两种缩放、睡姿图层保留、非伊蕾娜直通、醒来隐藏及异常后的帧恢复。当前共 670 项断言通过，已查看两张本次 FNA 离线截图；纯编译通过，游戏内效果需构建并重新加载模组后确认。

## 更好的体验绘制兼容回归

2026-10-27：正式外观钩子迁移到 `LegacyPlayerRenderer.DrawPlayerInternal`，同时覆盖世界人物、选人预览与头像；睡眠帧修正只在 `headOnly` 时应用。`ImproveGame` 提前拦截 `DrawPlayerFull` 后，其编译后的原方法可能内联很短的 `DrawPlayer` 转发方法，绕过后来注册在转发方法上的外观钩子。原有 `sortAfter = I,ImproveGame` 保持不变，也无需更改对方模组或隐身配置。

`run.ps1` 默认从相邻的 `../ImproveGame/Content/Functions/MinorPatches.cs` 每次重新提取 `TranslucentInvisPotion` 类，保留其真实 `Load`、绘制钩子与 `DrawEffects`；只适配命名空间、类可见性及配置单例。可通过 `-ImproveGame <源码目录>` 指定另一份源码。生成的夹具只保存在不打包的 `.vissandbox/player-pose-preview`。

`ImproveGameChecks.cs.txt` 在安装版本的真实 `LegacyPlayerRenderer` 上注册实际 MonoMod 钩子。旧实现对照在本机输出 `world appearance BYPASSED (reported failure reproduced)`；迁移后共 919 项断言通过，包含两种相对加载顺序、无 ImproveGame、本地/其他玩家、普通/伊蕾娜角色、隐身开关与 0/0.5/1 透明度、菜单/头像/直接内部调用、状态恢复及卸载重载。此前的暂停发光、睡眠头像与脚底对齐检查仍运行。

已打开本次 `improvegame-compat-4.00.png`（左：正常；右：50% 隐身）、暂停装备与睡眠头像图片检查。结果日志为 `checks.log`。兼容图的入口经过真实世界绘制方法及对方钩子，最终绘制由终端钩子调用原版装备状态准备、头腿图层与正式伊蕾娜身体/帽子图层；没有启动完整游戏或真实多人客户端，其他玩家以 `whoAmI` 夹具覆盖。此离线检查不能代替游戏内最终复测。

源码编译：`dotnet msbuild 伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false -v:quiet -clp:ErrorsOnly`。本次未更改预制贴图，未打包或重新加载正在运行的游戏。

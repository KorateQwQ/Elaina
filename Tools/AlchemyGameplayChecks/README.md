# 炼金实际制作检查

2026-10-26：新增炼金1～8级240→60帧计时、逐批额外产出概率边界、额外产物零经验零额外耗材、同轮升级不改变概率、动态时长联机同步及显迹尘闪耀根＋晶状体配方检查。独立计时器与真实大锅启动路径均覆盖。

同日确认首版经验规则100/150/200/250/300/350/400，沿用每批10/15/20经验。`StageProgressionChecks.cs.txt` 逐档验证封顶不预存、开放上限不自动升级、最后一批才升级、批次溢出丢弃、满级继续制作不发经验及半级进度存档往返，并检查关键配方的等级先后关系。技能上限仍由隔离夹具提供，配方研究检查不替代真实卷轴来源与新角色流程；本次未改变配方、材料或贴图。

本次验证：`pwsh -NoProfile -File Tools/AlchemyGameplayChecks/run.ps1` 的 Release 1434 项、Debug 1460 项通过；`dotnet msbuild 伊蕾娜.csproj /t:Compile /p:Configuration=Debug /p:BuildProjectReferences=false` 通过，存在项目原有编译警告。未打包或重载运行中的游戏，未新增 FNA 预览或完成新角色游玩验证。

运行 `& Tools/AlchemyGameplayChecks/run.ps1`。工具直接加载本机 tModLoader，并链接实际制作、研究、存档、UI 快照与灰赝尘代码；没有模拟 Terraria/Recipe/ModContent 的桩。测试使用隔离玩家、原版物品和注册的实际 Recipe，关闭文字弹出，不创建世界或修改玩家存档。

Debug、Release 各验证：原版背包/箱子/个人容器/虚空袋/模组材料提供者的统计与扣除，替代配方组、原版回调、鼠标物品保留、背包满后的落地、研究门槛、未来配方物品、重复研究、经验与上限、角色 TagCompound 往返、Debug 十倍补料/重置及 Release 拒绝、灰赝尘默认隔离与可选启用。

测试源码为 `VanillaChecks.cs.txt`，工程输出放 `.vissandbox/alchemy-gameplay-checks`，不会打包进模组。实际游戏内内容加载、SUI 输入和共享箱子场景仍需重载后验证。

删除温香树脂后，旧存档检查覆盖“已删除条目位于完整研究列表首位”，保证过滤旧 ID 后仍保留所有有效研究。此制作宿主不加载料理与玩家 Buff，因此排除 `ImproveGameFoodCompatibility.cs`；不验证其外部模组钩子。

2026-10-25：增加 `BrewingChecks.cs.txt`，链接正式大锅交互、玩家状态机、原版世界瓦片和合成路径；仅 SUI 窗口、聊天显示与角色身份使用隔离桩。验证普通锅/距离/身份拒绝、四格定位、关书继续、重开保留进度、重复请求拒绝、结束前零扣料、末帧受伤/死亡/拆锅/传送/切换角色/退出取消、材料变化及只结算一次。原有原版合成接口检查直接调用完成阶段，保留箱子、虚空袋和其他模组材料回调覆盖。运行 `pwsh -File Tools/AlchemyGameplayChecks/run.ps1`；不启动游戏，不操作真实存档。

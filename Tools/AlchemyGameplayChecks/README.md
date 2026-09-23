# 炼金实际制作检查

运行 `& Tools/AlchemyGameplayChecks/run.ps1`。工具直接加载本机 tModLoader，并链接实际制作、研究、存档、UI 快照与灰赝尘代码；没有模拟 Terraria/Recipe/ModContent 的桩。测试使用隔离玩家、原版物品和注册的实际 Recipe，关闭文字弹出，不创建世界或修改玩家存档。

Debug、Release 各验证：原版背包/箱子/个人容器/虚空袋/模组材料提供者的统计与扣除，替代配方组、原版回调、鼠标物品保留、背包满后的落地、研究门槛、未来配方物品、重复研究、经验与上限、角色 TagCompound 往返、Debug 十倍补料/重置及 Release 拒绝、灰赝尘默认隔离与可选启用。

测试源码为 `VanillaChecks.cs.txt`，工程输出放 `.vissandbox/alchemy-gameplay-checks`，不会打包进模组。实际游戏内内容加载、SUI 输入和共享箱子场景仍需重载后验证。

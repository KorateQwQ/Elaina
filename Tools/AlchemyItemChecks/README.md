# 炼金物品与魔药效果离线检查

运行 `& Tools/AlchemyItemChecks/run.ps1 -PotionsOnly` 验证本轮魔药；可用 `-Tml` 指定 tModLoader 安装目录。省略 `-PotionsOnly` 会额外检查所有非药剂的图像资产。

链接真实 `AlchemyItem`、`AlchemyPotion`、21 个具体物品、6 个 Buff 及效果类型，直接使用本机 tModLoader 程序集，没有 Terraria 或 ModContent 桩。通过真实 `ILoadable.Load` / `ItemLoader` / `ContentInstance` 路径注册，再调用生产 `SetDefaults`，检查：

- 未配置 Buff 的测试物品仍不可饮用；净土露滴保持普通物品且不可消耗。
- 六种增益药剂沿用原版饮用、消耗与持续时间字段；止痛药 60 秒，其余沿用基类 5 分钟。
- 物品继承关系、目录尺寸/稀有度和像素图资产。
- 中英两种语言的 23 个物品均使用 `Alchemy.Items` 且名称/描述非空，没有旧 `NotebookItems` 键。
- 原版 AddBuff 续时/移除、百分比生命恢复、星力伤害上限、嗜血与原版吸血额度/月噬/目标限制。
- 集中夹角边界、发射方向保留、子弹丸继承、ExtraAI 往返和实际伤害钩子。
- 共鸣归属/续时/到期/NPC 槽位重用，鞭击与召唤物/哨兵伤害分类。
- 轻羽跑鞋限速、加速度、翅膀参数、喷气背包上下重力分支及飞行时长。

运行 `& Tools/AlchemyItemChecks/run-integration.ps1` 编译模组 DLL，并直接验证完整程序集里的扫帚和月露合剂：双玩家速度隔离、普通/冲刺速度、Buff 失效、脱战/角色/死亡/满槽限制、储备存档与 50% 独特魔力恢复。仅执行 `Compile;CopyFilesToOutputDirectory`，不会打包、退出游戏或重载当前模组。灰赝尘仍由独立检查覆盖。

2026-10-15：药剂范围检查通过 399 项注册/资源/本地化检查、61 项行为检查和 30 项完整程序集检查。全量资产检查在既有 `RevealingDust pixel asset has catalog resolution` 处失败；`-PotionsOnly` 只缩小图像资产检查范围，保留全部物品注册和本地化检查。

此工具不启动游戏、不加载贴图、不创建世界或写入角色存档，不替代游戏内使用、多人端到端和完整内容加载验证。输出放在忽略目录 `.vissandbox/alchemy-item-checks` 与 `.vissandbox/alchemy-potion-integration`。

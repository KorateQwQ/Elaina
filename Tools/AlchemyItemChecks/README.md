# 炼金物品基类离线检查

运行 `& Tools/AlchemyItemChecks/run.ps1`；可用 `-Tml` 指定 tModLoader 安装目录。

链接真实 `AlchemyItem`、`AlchemyPotion`、21 个具体物品、3 个既有 Buff 及其玩家类型，直接使用本机 tModLoader 程序集，没有 Terraria 或 ModContent 桩。通过真实 `ILoadable.Load` / `ItemLoader` / `ContentInstance` 路径注册，再调用生产 `SetDefaults`，检查：

- 未配置 Buff 的物品可注册，3 个未来药剂不能饮用或消耗，不生成占位 Buff。
- 配置 Buff 后自动启用饮用、消耗和默认/自定义持续时间；既有药剂的饮用属性保持原值。
- 未来药剂与非药剂继承关系、目录尺寸/稀有度、普通物品的像素图资产。
- 中英两种语言的 23 个物品均使用 `Alchemy.Items` 且名称/描述非空，没有旧 `NotebookItems` 键。

月露合剂及灰赝尘依赖既有玩家/常规合成系统，本工具对它们仅检查源码中的基类、像素图和本地化约定；既有效果由对应系统检查覆盖。此工具不启动游戏、不加载贴图、不创建世界，不替代游戏内使用和完整内容加载验证。项目与输出在忽略目录 `.vissandbox/alchemy-item-checks`。

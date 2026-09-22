# 新炼金系统

此目录是独立重做的炼金系统，不依赖旧 `炼金` 目录中的配方、等级或玩家状态。

- `item/`：所有新炼金产物及其共同基类 `AlchemyItem`，可继续按类别建立子目录。
- `AlchemyRecipe`：材料列表与单次产出数量；所属物品就是产物。
- `AlchemyIngredient`：材料的物品类型 ID 与数量。
- `Buffs/`：新炼金产物的增益及对应的玩家效果。
- `Crafting/`：灰赝尘对原版正常合成栏的补料支持，与炼金配方系统隔离。

当前已添加止痛药（30 秒）、嗜血药剂（5 分钟）、星力药水（5 分钟）。三者继承 `AlchemyPotion`，再继承 `AlchemyItem`；物品图标均从 `item/PotionIcon.png` 复制，buff 暂用对应原版图标。药剂可以饮用，但配方为 null、尚未解锁，因此 `CanCraft` 返回 false。后续配置配方时同时实现解锁条件。

灰赝尘（`item/AshenFacsimileDust.cs` / Ashen Facsimile Dust）是直接继承 `AlchemyItem` 的炼金造物，不是可饮用药剂；获取配方为 null、解锁状态为 false、暂不添加掉落或正常工作台配方。图标独立复制自 `item/Painkiller.png`，便于以后直接替换。

灰赝尘携带于原版可用于合成的材料来源中即可生效：每种直接材料（配方组按任意有效替代物计）至少有 1 件，且 `平均(min(可用数量 / 需求数量, 1)) >= 0.5`。默认模式优先消耗原材料，补足每个缺失单位消耗 1 份灰赝尘；右键点击灰赝尘可切换为优先灰赝尘模式，此时每条合并后的逻辑材料需求保留 1 件实际材料，其余需求由灰赝尘支付。优先灰赝尘模式仍使用同一材料满足率门槛；灰赝尘不足时，完整的原材料配方会回退到原版制作。单次配方产出多件时不重复乘以产出数量。原料齐全时默认走原版路径且不消耗灰赝尘。工作台、环境、模组配方条件仍须满足。支持背包分堆、当前打开的箱子/个人容器、启用的虚空袋，以及 `AddMaterialsForCrafting` 提供的正常合成材料；箱子槽位变动发送原版同步消息，额外来源保留消耗通知。每次合成都重新检查实际槽位，炼药桌和配方省料回调只执行一次，并按当前模式和实际需求扣料。当前选中配方的物品提示显示省料前的粉尘需求；灰赝尘物品提示显示当前模式。模式按玩家本地数据保存，不参与联机同步。

**作用边界：**只接入正常合成栏，适用于原版及其他模组注册到该合成栏的 `Recipe`；不改动 `AlchemyRecipe.HasIngredients`、`AlchemyItem.CanCraft` 或旧炼金系统。Magic Storage 等独立合成界面没有专门适配，不承诺自动支持。相同材料或等价配方组的重复条目合并计算；相交但不等价的配方组暂时回退到原版合成，不提供粉尘补料，避免把同一份库存计入两种需求。涉及灰赝尘本身的配方不再享受补料。未禁用微光：粉尘补料制成的产物仍可按原版规则分解，因此可能把粉尘转成原材料；不额外记录产物来源。

实现不复制配方，也不临时修改全局配方材料。配方类型匹配关系在 `PostSetupRecipes` 缓存，合成列表刷新复用原版物品数量表，点击制作时才扫描真实槽位并生成扣料计划。原料不足的这条路径自行执行扣料，但仍由原版 `Main.CraftItem` 处理产出、前缀、`OnCraft`/`OnCreate` 和堆叠；回调收到实际消耗的原料与粉尘。直接替换 `Recipe.Create` 内部实现的其他模组仍有潜在冲突，不能把这里的支持理解为任意合成改造模组的兼容保证。

验证脚本：`Tools/AshenFacsimileChecks/run.ps1`。它使用本机 tModLoader 程序集测试规则、真实 Recipe/RecipeGroup、扣料与省料行为、炼金隔离和安装版本的接入位置，并单独编译全模组；输出保存在忽略目录 `.vissandbox/ashen-checks`，不会启动游戏或覆盖已安装模组。发布前还需游戏内验证：合成栏显示与长按制作、打开箱子/虚空袋、两名玩家共享箱子以及目标整合包的其他合成钩子。

止痛药每秒额外恢复最终最大生命的 2%，采用原版生命再生机制；嗜血使最终受击伤害乘以 1.2、近战伤害增加 10%，近战武器与近战弹幕对有效敌人的命中恢复实际伤害的 5%，保留不足 1 点的小数，遵循月噬禁止吸血的规则；魔能加成按原版最终最大魔力 `statManaMax2` 计算，每点增加 0.05% 魔法伤害。

新增物品继承 `AlchemyItem`，实现 `AlchemyRecipe`、`IsRecipeUnlocked(Player)`，并按普通 `ModItem` 设置物品属性。配方属性可采用以下写法：

```csharp
private AlchemyRecipe recipe;

public override AlchemyRecipe AlchemyRecipe => recipe ??= new AlchemyRecipe(
    resultStack: 1,
    new AlchemyIngredient(ItemID.BottledWater),
    new AlchemyIngredient(ItemID.Daybloom, 2));

// 示例：击败克苏鲁之眼后可见、可制作。始终可见的配方直接返回 true。
public override bool IsRecipeUnlocked(Player player) => NPC.downedBoss1;

// 可选；不重写则无提示。在该物品的本地化条目中添加 RecipeUnlockHint。
public override LocalizedText RecipeUnlockHint => this.GetLocalization("RecipeUnlockHint");
```

上述代码使用 `Terraria`、`Terraria.ID`、`Terraria.Localization`、`Terraria.ModLoader` 和 `伊蕾娜.ElainaModAlchemy` 命名空间。

未来 UI 通过 `IsRecipeUnlocked(player)` 决定显示真实物品还是问号；未解锁时不展示真实配方，可展示 `RecipeUnlockHint`。此阶段没有自定义绘制代码。

`CanCraft(player, materials)` 同时检查解锁状态和材料数量，即使材料齐全，未解锁时也返回 false。材料槽由调用方提供（例如未来炼金容器中的槽位），每个槽位只传一次。此方法只做判断；制作入口、材料消耗、产物生成、解锁存档和联机同步留待后续实现。不会自动添加原版工作台配方。

# 新炼金系统

此目录是独立重做的炼金系统，不依赖旧 `炼金` 目录中的配方、等级或玩家状态。

- `item/`：所有新炼金产物及其共同基类 `AlchemyItem`，可继续按类别建立子目录。
- `AlchemyRecipe`：材料列表与单次产出数量；所属物品就是产物。
- `AlchemyIngredient`：材料的物品类型 ID 与数量。
- `Buffs/`：新炼金产物的增益及对应的玩家效果。

当前已添加止痛药（30 秒）、嗜血药剂（5 分钟）、星力药水（5 分钟）。三者继承 `AlchemyPotion`，再继承 `AlchemyItem`；物品图标均从 `item/PotionIcon.png` 复制，buff 暂用对应原版图标。药剂可以饮用，但配方为 null、尚未解锁，因此 `CanCraft` 返回 false。后续配置配方时同时实现解锁条件。

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

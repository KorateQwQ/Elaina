# 炼金手记

默认 **P** 开关，可在控制设置中改键；没有常驻按钮。Esc 先关闭帮助，再合上手记。开合继续使用技能表同款动画，关闭重开保留选中条目和滚动位置。

界面只按魔药、奇物、料理、素材四类排列。顶部原来的状态筛选已移除，其菱形与辉光选中效果用于分类导航。右上角显示角色的炼金等级、经验及升级进度。

## 真实制作与材料

已移除生产代码中的演示库存。`AlchemyNotebookState` 只管理导航，数量与资格由 `AlchemyNotebookSource` 读取角色和原版合成材料统计。

`Gameplay/AlchemyVanillaRecipes.cs` 在正常配方加载阶段注册 17 条实际 `Recipe`，配方带“仅在炼金手记中制作”条件，不会绕过研究出现在普通合成栏。材料收集直接调用安装版本的 `Recipe.CollectItemsToCraftWithFrom`；是否足够调用 `Recipe.CollectedEnoughItemsToCraftRecipeNew`；点击制作调用原版 `Main.CraftItem`，包含 `Recipe.Create`、材料消耗钩子、物品创建/制作回调、前缀与箱子同步。

因此材料来源、消耗顺序及替代材料组遵循原版：背包、打开的箱子/个人容器、启用的虚空袋，以及 `AddMaterialsForCrafting` 提供的材料。铁/铅、椎骨/腐肉以正式 RecipeGroup 注册。批量制作逐批重新检查，材料被其他逻辑改变时只报告实际完成的批数。

成品通过原版 `Player.GetItem` 放入背包，放不下的部分在玩家位置掉落；手上已有的鼠标物品会保留。没有新增炼金专用联机协议，材料变化沿用游戏已有同步。

灰赝尘默认不参与炼金。以后将 `AlchemyVanillaRecipes.AllowFacsimileDust` 设为 true，即可同时启用现有灰赝尘系统的材料判定、最大批数和实际补料消耗；无需重写扣料逻辑，已有正常合成功能不变。

## 研究与成长

`Gameplay/AlchemyProgressPlayer.cs` 用角色存档保存已研究条目、等级与经验，不与旧 `炼金/` 系统混用。新角色等级为 1、经验为 0，所有条目尚未研究。未研究条目隐藏图标、名称、效果和制作材料，只显示问号与研究条件。

暂定规则集中在 `Gameplay/AlchemyProgressionRules.cs`：上限 **20 级**；1→2 级需要 **100 XP**，之后每级增加 **50 XP**；每批制作获得 **10 + 5 × 稀有度**经验，按批数而非产物件数计算。只有成功制作才给经验，到达上限后停止累积。`RequiredLevel(id)` 集中配置各条目门槛；当前基础药液和蜂蜜面包为 1 级，后续造物分布在 2～12 级。

所有现有条目暂按等级研究，不新建实体配方。每个 `AlchemyItem` 可以覆盖 `ResearchRequirement`，以后配置配方物品：

```csharp
// 仅等级：达到 5 级后，点击研究。
public override AlchemyResearchRequirement ResearchRequirement => new(RequiredLevel: 5);

// 配方物品：未来创建配方类后填入其 ModContent.ItemType。
public override AlchemyResearchRequirement ResearchRequirement => new(
    RequiredLevel: 1,
    RequiresRecipe: true,
    RecipeItemType: ModContent.ItemType<YourRecipeScroll>(),
    RecipeStack: 1,
    ConsumeRecipe: true);

// 每批经验也可以逐物品覆盖。
public override int CraftExperience => 20;
```

配方物品按背包检查；`ConsumeRecipe` 控制研究后是否消耗。设置 `RequiresRecipe=true` 而配方类型仍为 0 时，研究保持禁用，不会误解锁。已研究条目不能重复消耗配方。

## Debug

与技能表一致，补充素材、重置手记按钮及相应操作使用 `#if DEBUG`。

- 补充素材：给予当前配方单批需求的 **10 倍**，与数量选择器无关；替代材料组给予其中一种，优先放背包，满包落地。
- 重置手记：清空全部已研究条目，恢复等级 1、经验 0，保留真实物品。
- Release 构建不创建这两个按钮，也拒绝相应操作。

## 绘制与资产

外框与纸页复用技能表的紫色主题，局部使用同一张 Glow.png；界面固定彩铅。原始图片来自 `item/ExampleAssets`，UI 使用 `Assets/Pencil` 内的预乘透明副本；正常背包和世界物品使用像素版本。

选中羽毛取自 HTML 注脚的 `notebook-quill`，注脚自身不再绘制羽毛。封面、选中标记、字体、滚动裁剪与开合动画继续沿用已实现版本。

## 验证与工具

- `Tools/AlchemyGameplayChecks/run.ps1`：真实 tML 原版材料收集、制作、容器、替代材料、满包掉落、回调、研究、存档、经验及 Debug/Release 检查。
- `Tools/AlchemyNotebookPreview/run.ps1`：无游戏依赖的 UI 快照与导航检查。
- `Tools/AlchemyItemChecks/run.ps1`：物品注册、Buff 与贴图/本地化检查。
- `Tools/AshenFacsimileChecks/run.ps1`：已有灰赝尘功能回归及全模组源码编译。
- `Tools/AlchemyPreview/run.ps1 -Configuration Release`：FNA 离线预览，包含问号、等级不足、可研究、配方不足、配方齐全、满级和制作状态。预览数据仅存在于 Tools/AlchemyPreview/NotebookFixture.cs.txt，生产界面不会使用。

材料收集入口为当前 tML 的私有方法，只在加载时解析为委托，安装版本检查已覆盖；升级 tML 后应重新运行原版流程测试。离线检查不替代游戏内内容重载、实际点击/滚动及两人共享箱子的验收。
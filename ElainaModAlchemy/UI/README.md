# 炼金手记

默认 **P** 开关，可在控制设置中改键；没有常驻按钮。Esc 先关闭帮助，再合上手记。开合继续使用技能表同款动画，关闭重开保留选中条目和滚动位置。

默认「全部」显示完整物品目录，按魔药、奇物、料理、素材顺序排列。点击分类后只显示对应分类，点击「全部」恢复完整目录。分类切换会回到该分类首行；卡片点击、素材链接和方向键在当前分类内导航。研究数量统计当前列表，右上角保留角色炼金等级、经验及升级进度。

主标题仅保留「炼金手记」。物品列表不再显示分类篇章标题、说明与「伊蕾娜的手记」引文，也不保留其滚动占位。底部图例与卡片共用状态标记：实心菱形「可炼制」、空心菱形「素材不足」、虚线圆环「未研究」；达到研究条件的圆环仍以金色提示。购买或采集素材不显示炼制标记，获取方式保留在详情中。

## 真实制作与材料

已移除生产代码中的演示库存。`AlchemyNotebookState` 只管理导航，数量与资格由 `AlchemyNotebookSource` 读取角色和原版合成材料统计。

`Gameplay/AlchemyVanillaRecipes.cs` 在正常配方加载阶段注册 17 条实际 `Recipe`，配方带“仅在炼金手记中制作”条件，不会绕过研究出现在普通合成栏。材料收集直接调用安装版本的 `Recipe.CollectItemsToCraftWithFrom`；是否足够调用 `Recipe.CollectedEnoughItemsToCraftRecipeNew`；点击制作调用原版 `Main.CraftItem`，包含 `Recipe.Create`、材料消耗钩子、物品创建/制作回调、前缀与箱子同步。

因此材料来源、消耗顺序及替代材料组遵循原版：背包、打开的箱子/个人容器、启用的虚空袋，以及 `AddMaterialsForCrafting` 提供的材料。铁/铅、椎骨/腐肉以正式 RecipeGroup 注册。批量制作逐批重新检查，材料被其他逻辑改变时只报告实际完成的批数。

成品通过原版 `Player.GetItem` 放入背包，放不下的部分在玩家位置掉落；手上已有的鼠标物品会保留。没有新增炼金专用联机协议，材料变化沿用游戏已有同步。

灰赝尘默认不参与炼金。以后将 `AlchemyVanillaRecipes.AllowFacsimileDust` 设为 true，即可同时启用现有灰赝尘系统的材料判定、最大批数和实际补料消耗；无需重写扣料逻辑，已有正常合成功能不变。

## 研究与成长

`Gameplay/AlchemyProgressPlayer.cs` 用角色存档保存已研究条目、等级与经验，不与旧 `炼金/` 系统混用。新角色等级为 1、经验为 0，目录中标记为初始解锁的条目直接可用；其余未研究条目隐藏图标、名称、效果和制作材料，只显示问号与研究条件。

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

与技能表一致，等级 +1、补充素材、重置手记按钮及相应操作使用 `#if DEBUG`。

- 补充素材：给予当前配方单批需求的 **10 倍**，与数量选择器无关；替代材料组给予其中一种，优先放背包，满包落地。
- 重置手记：清空全部已研究条目，恢复等级 1、经验 0，保留真实物品。
- 等级 +1：每次提升一个炼金等级，保留当前经验和研究记录；达到 20 级时经验归零，按钮显示「已满级」并禁用。制作中不能升级。
- Release 构建不创建这三个按钮，也拒绝相应操作。

## 绘制与资产

外框沿用技能表的紫色书页主题，纸页使用炼金专属 `Assets/NotebookSurface.png` 连续渐变底图，避免技能星图裁剪区域带来的横向底色断层。用 `python Tools/AlchemyPreview/bake_surface.py` 重建，图片为 1100×800 不透明 RGB，按 RGBA8 加载约占 3.36 MiB，运行时缓存并单次绘制。技能表底图不受影响。局部辉光仍使用同一张 Glow.png。23 个炼金物品使用 `item/ExampleAssets/*_Pixel`，目录与详情按原图 1 倍居中绘制，随整体 UI 缩放。配方与底部素材栏中的 28 种原版材料，通过现有物品 ID 和 `Main.GetItemDrawFrame` 读取游戏贴图及当前动画帧，在槽位中居中绘制为原先适配大小的 0.75 倍。右侧详情内容宽度比滚动区域窄 24 设计像素，为滚动条留出间隔。所有像素图标使用 `PointClamp`，绘制后恢复原批次状态；未研究剪影、字体和装饰保持现有绘制。

未研究物品使用 `Assets/MysteryPixel/*_Mystery_Pixel.png`：沿用对应物品像素图的画布和轮廓，问号直接绘进透明贴图，在手记中与已研究图标同样按原图 1 倍、Point 采样显示。修改物品像素图或新增目录条目后，运行 `python Tools/AlchemyPreview/prepare_mystery_pixels.py` 更新对应问号图。

`AlchemyCatalogLayout` 统一正式 SUI 与预览的卡片尺寸、位置、分类过滤、键盘移动及目标可见性计算。卡片按四列排布；不添加篇章标题或装饰旁注。目录和详情分别滚动，刷新材料与制作数量不改变目录位置。

选中羽毛取自 HTML 注脚的 `notebook-quill`，注脚自身不再绘制羽毛。封面、选中标记、字体、滚动裁剪与开合动画继续沿用已实现版本。

`AlchemySelectionQuillMotion` 只对更换选中条目进行路径缓动。目录滚动与面板移动通过当前绘制帧的卡片坐标同步应用；落定后相对卡片的偏移归零。羽毛笔放在目录原生遮罩内的独立叠加层，不参与卡片排版、不拦截输入；快速重新选择从当前动画位置继续。

## 验证与工具

- `Tools/AlchemyGameplayChecks/run.ps1`：真实 tML 原版材料收集、制作、容器、替代材料、满包掉落、回调、研究、存档、经验及 Debug/Release 检查。
- `Tools/AlchemyNotebookPreview/run.ps1`：无游戏依赖的 UI 快照与导航检查。
- `Tools/AlchemyItemChecks/run.ps1`：物品注册、Buff 与贴图/本地化检查。
- `Tools/AshenFacsimileChecks/run.ps1`：已有灰赝尘功能回归及全模组源码编译。
- `Tools/AlchemyPreview/run.ps1 -Configuration Release`：FNA 离线预览，包含问号、等级不足、可研究、配方不足、配方齐全、满级和制作状态。预览数据仅存在于 Tools/AlchemyPreview/NotebookFixture.cs.txt，生产界面不会使用。

材料收集入口为当前 tML 的私有方法，只在加载时解析为委托，安装版本检查已覆盖；升级 tML 后应重新运行原版流程测试。离线检查不替代游戏内内容重载、实际点击/滚动及两人共享箱子的验收。

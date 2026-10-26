# 新炼金系统

此目录是独立重做的炼金系统，不依赖旧 `炼金` 目录中的配方、等级或玩家状态。

- `item/`：共同基类 `AlchemyItem`、`AlchemyPotion` 与物品 ID 映射。具体物品按用途放入 `Potions/`（8）、`Curios/`（3）、`Foods/`（3）、`Materials/`（7）、`Tools/`（1）。
- `AlchemyRecipe`：材料列表与单次产出数量；所属物品就是产物。
- `AlchemyIngredient`：材料的物品类型 ID 与数量。
- `Buffs/`：新炼金产物的增益及对应的玩家效果。
- `Crafting/`：灰赝尘对原版正常合成栏的补料支持，与炼金配方系统隔离。
- `UI/`：P 打开的新版彩铅炼金手记，接入真实合成材料、配方研究与角色炼金等级；详见 `UI/README.md`。

所有炼金造物都是普通的正式 ModItem，按用途继承 `AlchemyItem` 或 `AlchemyPotion`，不区分“展示物品”与“正式物品”。`AlchemyItem.EntryId` 可关联炼金目录，统一取得像素贴图、基础尺寸与稀有度；本地化统一使用 `Alchemy.Items`。目录中有材料的物品通过 `AlchemyRecipe` 提供配方定义，`IsRecipeUnlocked` 读取角色研究记录。

`AlchemyPotion` 用于饮用后获得 Buff 的药剂。`PotionBuffType` 是可选配置，默认 0：物品可正常存在于背包和炼金手记中，但暂不可饮用或消耗。实现效果时覆盖 `PotionBuffType`，按需覆盖 `DurationTicks`，无需更换父类。集中、共鸣、轻羽药水目前使用这一方式。月露合剂也继承 `AlchemyPotion`：仅供伊蕾娜使用，持续 4 分钟，每秒恢复最大独特魔力值的 1%，不恢复原版魔力；重复饮用刷新持续时间。已移除原有五格药水槽、收藏自动补充与缺魔时自动消耗充能。净土露滴是滴落使用的药液，直接继承 `AlchemyItem`，同样归入 `Potions/`。

止痛药（60 秒）、嗜血药水（1% 最大生命/秒）、星力药水已具有实际效果，均位于 `item/Potions/`。其饮用、Buff、堆叠与原有属性保持不变；制作配方已接入炼金手记，完成研究且材料足够后可以制作。

灰赝尘（`item/Curios/AshenFacsimileDust.cs` / Ashen Facsimile Dust）直接继承 `AlchemyItem`，为常规合成栏提供右键补料制作。灰赝尘自身的获取配方在炼金手记中研究后制作；不添加掉落或普通工作台制作入口。

瓶中雨（`item/Curios/BottledRain.cs`）已实现投掷消耗与 60 秒局部雨云：约 20 格宽的细雨加速现有植物生长，使成熟水叶草真正开花并可收获种子；实心屋顶/地面阻挡雨水。云、雨丝、水花、开瓶碎片和淡出共用 `Rain/RainCloudDrawing.cs`，使用原版云图集，不新增材料或图标。服务器执行浇灌并同步图块，世界天气与液体保持原状。实现边界、复现命令和验证记录见 `Tools/BottledRainPreview/README.md`。

瓶中雨当前默认启用 `Effects/Content/BottledRainCloud` 体积云 Shader，正式绘制为 `Rain/RainCloudShaderRenderer.cs`。客户端共享一份固定种子噪声和克隆材质，沿用相机/缩放矩阵并恢复原 SpriteBatch 状态；屏幕外云跳过体积绘制，专用服务器不创建图形资源。`RainCloudDrawing` 保留原贴图云分支，在材质初始化失败时回退；雨丝、水花与玩法继续共用原逻辑。正式 Shader 的编译、FNA 对照、状态检查和包验证入口见 `Tools/BottledRainShaderPreview/README.md`。

瓶中雨实际覆盖的植物上方显示浅青色向上箭头（`RainPlantIndicatorSystem` / `RainPlantIndicatorDrawing`），轻微上浮与呼吸，重叠雨云及多格植物合并标记。图标和浇灌共用范围/遮挡判定，不给屋顶下、范围外或虚化植物误标；采集、雨云过期和世界切换会清理。客户端只重建显示状态，不修改植物、不发送额外网络包。

正常物品绘制统一使用 `item/ExampleAssets/*_Pixel.png`；炼金手记独立使用彩铅贴图。旧液体绘制测试的 `PotionLiquidRenderer`、Shader 源码/编译资源与月露合剂的无效绘制钩子已移除，旧图稿保留原路径。移动源码不改变物品类名和 tML 的 `模组名/物品名` 存档标识。

物品基类与分类检查：`& Tools/AlchemyItemChecks/run.ps1`，使用真实 tML 注册路径验证无 Buff/已有 Buff 的药剂行为、分类、贴图与中英本地化；不启动游戏或打包模组。

灰赝尘携带于原版可用于合成的材料来源中即可生效：每种直接材料（配方组按任意有效替代物计）至少有 1 件即可，不再要求总体满足率（旧版的 50% 门槛已取消）。选中配方后：**左键**只按原版消耗原料，原料不足时不制作、也不消耗灰赝尘；**右键**使用灰赝尘——无论原料是否齐全，每条合并后的逻辑材料需求都只用 1 件实际材料，其余全部由灰赝尘支付（如火山需 20 狱石锭，右键固定为 1 锭 + 19 份灰赝尘），灰赝尘不足时不能右键制作；门槛仍是“每种至少 1 件”。没有可用灰赝尘方案的配方左右键均保持原版行为。原版合成按钮本身左右键都会调用 `Main.CraftItem`，这里在钩子里按“右键单独按下”区分；炼金手记（可选启用时）不区分按键，仍只在缺料时补料。单次配方产出多件时不重复乘以产出数量。工作台、环境、模组配方条件仍须满足。支持背包分堆、当前打开的箱子/个人容器、启用的虚空袋，以及 `AddMaterialsForCrafting` 提供的正常合成材料；箱子槽位变动发送原版同步消息，额外来源保留消耗通知。每次合成都重新检查实际槽位，炼药桌和配方省料回调只执行一次，并按左右键对应的方式和实际需求扣料。实际悬停的正常合成配方（含展开列表、未选中项及同产物的不同配方）的物品提示在物品名右侧追加淡紫色“可通过灰赝尘制作”（物品名本身保留稀有度颜色），并以一行淡紫色文字说明右键用法和单次消耗的灰赝尘份数：“右键制作：每种材料只用 1 个，其余用 N 份灰赝尘代替”。这两段文字由 `PreDrawTooltipLine` 逐字绘制，带每 2 秒一次的扫光和 3 颗轮换位置的闪烁星点（`Crafting/FacsimileTooltipEffect.cs`；星点复用技能星图同款四芒星 `UI/Assets/NotebookStars.png` 第一行与星图 `Glow.png` 柔光，不新增贴图、无 shader，提示框宽度仍由原版按文本计算）；若其他模组用 `PreDrawTooltip` 整体接管提示绘制，这两行仍会按本模组方式绘制。份数按省料前计算，炼药桌等省料效果只会让实际消耗更少，提示不再单独说明。展开列表中需先选中配方再右键制作。普通背包物品、向导查询与无可用灰赝尘方案的配方不显示该提示。旧版本保存的“优先灰赝尘”角色偏好已废弃，不再读取。

**作用边界：**只接入正常合成栏，适用于原版及其他模组注册到该合成栏的 `Recipe`；不改动 `AlchemyRecipe.HasIngredients`、`AlchemyItem.CanCraft` 。Magic Storage 等独立合成界面没有专门适配，不承诺自动支持。相同材料或等价配方组的重复条目合并计算；相交但不等价的配方组暂时回退到原版合成，不提供粉尘补料，避免把同一份库存计入两种需求。涉及灰赝尘本身的配方不再享受补料。未禁用微光：粉尘补料制成的产物仍可按原版规则分解，因此可能把粉尘转成原材料；不额外记录产物来源。

实现不复制配方，也不临时修改全局配方材料。配方类型匹配关系在 `PostSetupRecipes` 缓存，合成列表刷新复用原版物品数量表，点击制作时才扫描真实槽位并生成扣料计划。原料不足的这条路径自行执行扣料，但仍由原版 `Main.CraftItem` 处理产出、前缀、`OnCraft`/`OnCreate` 和堆叠；回调收到实际消耗的原料与粉尘。直接替换 `Recipe.Create` 内部实现的其他模组仍有潜在冲突，不能把这里的支持理解为任意合成改造模组的兼容保证。

验证脚本：`Tools/AshenFacsimileChecks/run.ps1`。它使用本机 tModLoader 程序集测试规则、真实 Recipe/RecipeGroup、扣料与省料行为、炼金隔离和安装版本的接入位置，并单独编译全模组；输出保存在忽略目录 `.vissandbox/ashen-checks`，不会启动游戏或覆盖已安装模组。悬停提示离线检查使用 `pwsh -File Tools/AshenFacsimileChecks/preview-tooltip.ps1`，链接正式 `FacsimileCraftingTooltip.ModifyTooltips` 与补料规则，使用原版 `Mouse_Text` 字体，输出中英文、100%/125% 缩放及缺料/齐料各有、无灰赝尘四种场景，以及 1.5 倍缩放 4 秒循环的特效动图（`tooltip-<语言>.gif`，需 Pillow）到 `.vissandbox/ashen-tooltip-preview`。此 FNA 宿主只模拟悬停、库存和工具提示背景，不替代完整游戏 UI 与其他模组合成栏的兼容验证。2026-10-24：改为左键原料、右键灰赝尘后，66 项回归、炼金玩法检查（Release 74 / Debug 100 项）与全模组编译通过，已打开检查中英文 100%/125% 提示预览；PowerShell 5 需以带 BOM 的副本运行这些脚本。2026-10-21：61 项回归与全模组编译通过，已打开检查中英文提示预览，并通过正式资源检查与灰赝尘手记场景预览；本次没有新增材料或图标。发布前还需游戏内验证：合成栏左右键与长按制作、打开箱子/虚空袋、两名玩家共享箱子以及目标整合包的其他合成钩子。

止痛药每秒额外恢复最终最大生命的 2%，采用原版生命再生机制；嗜血使最终受击伤害乘以 1.2、近战伤害增加 10%，近战武器与近战弹幕对有效敌人的命中恢复实际伤害的 5%，保留不足 1 点的小数，遵循月噬禁止吸血的规则；魔能加成按原版最终最大魔力 `statManaMax2` 计算，每点增加 0.05% 魔法伤害。

新增物品按用途继承 `AlchemyItem` 或 `AlchemyPotion`，设置 `EntryId` 后即可使用统一图标与基础属性；有具体功能时再覆盖普通 `ModItem` 方法。需要真实炼金配方时覆盖 `AlchemyRecipe`、`IsRecipeUnlocked(Player)`。配方属性可采用以下写法：

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

当前手记使用真实材料，研究条件由 `ResearchRequirement` 配置；等级、经验和研究存档位于 `Gameplay/`，具体接口与首版经验方案见 `UI/README.md`。

`CanCraft(player, materials)` 同时检查解锁状态和材料数量，即使材料齐全，未解锁时也返回 false。材料槽由调用方提供（例如未来炼金容器中的槽位），每个槽位只传一次。此方法只做判断；手记的正式入口通过 `AlchemyVanillaRecipes` 复用原版材料收集与 `Main.CraftItem`，产物放背包，满包落地。右键大锅进入手记，搅拌结束时才检查、扣料并产出；关闭手记继续搅拌，受伤或死亡取消且不扣料。联机仅额外同步姿势与计时，材料与成品沿用原版同步。

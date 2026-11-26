# 本地化规范

## 专有名词

部分文本需要使用统一的专有名词直接指代，不应改写为一般性描述语言。

目前约定的专有名词如下：

- 技能消耗的可回复星形资源称为“魔力结晶”（Magic Crystals），名称共用 `Terms.MagicCrystal.Name`。技能栏在消耗行使用 `SkillInfo.Snippets.MagicCrystalCost`，星图在魔力旁列出所需颗数；消耗为零时省略。
- “伊蕾娜的独特魔力”或“伊蕾娜的专属魔力”统一使用图标与颜色标签指代：中文为 `[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FF:魔力]`，英文为 `[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FF:Magic Power]`。资源路径不翻译。
- 效果判定根据角色使用原版魔力或独特魔力时，写成“最大魔力或[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FF:魔力]”，共用“最大”而不重复。单纯增加原版魔力上限时只写“最大魔力增加40”等表述，灰之魔女的被动转换无需在此再次说明。
- KL 的冻结状态（冻结值、冻结冷却等）统一写成：中文 `[klicon/16,0:KL/Drawing/Snippets/Icons/IceIcon][c/8FE3FF:冻结]`，英文 `[klicon/16,0:KL/Drawing/Snippets/Icons/IceIcon][c/8FE3FF:Freeze]`。冻结时长、霜裂和无法行动由名词说明给出，技能描述只写自身的积累量与联动。
- KL 的潮湿状态统一写成：中文 `[klicon/16,0:KL/Drawing/Snippets/Icons/WaterIcon][c/66C3E8:潮湿]`，英文 `[klicon/16,0:KL/Drawing/Snippets/Icons/WaterIcon][c/66C3E8:Wet]`。积累阈值、续时上限及状态转换由 `Mods.KL.Terms.Wet` 说明，技能正文只写自身每次积累的潮湿值与增伤效果。
- KL 的燃烧状态统一写成：中文 `[klicon/16,0:KL/Drawing/Snippets/Icons/FireIcon][c/FF8F5A:燃烧]`，英文 `[klicon/16,0:KL/Drawing/Snippets/Icons/FireIcon][c/FF8F5A:Burning]`。积累阈值、持续上限、续燃及“生命上限较低更易点燃”由 `Mods.KL.Terms.Burning` 说明，技能正文不重复。“点燃”作动词时保留普通文字。
- 炼金大锅统一写成：中文 `[klicon/16,0:Item:Cauldron][c/E8C27A:炼金大锅]`，英文 `[klicon/16,0:Item:Cauldron][c/E8C27A:Alchemy Cauldron]`。klicon 路径写成 `Item:原版物品名` 时直接使用原版物品贴图（按 `ItemID.Search` 解析，不硬写物品 ID），无需另画图标。
- 水魔法书作为研习材料使用专有名词说明：图标 `伊蕾娜/Snippets/WaterSpellbookIcon`（物品贴图为多帧动画，图标取单帧），名称取物品本地化名，说明 `Mods.伊蕾娜.Terms.WaterSpellbook.Description` 写用途和制作方法，不写“每个角色只能制作一本”等判定。通过 `KLTerms.RegisterItem` 与物品关联后，星图研习/进修需求中的该物品行显示图标、名词颜色与下划线，悬停显示说明卡；需要富文本引用时写 `[klicon/16,0:伊蕾娜/Snippets/WaterSpellbookIcon][c/66C3C5:水魔法书]`。
- 专有名词在 `KL.Drawing.Snippets.KLTerms` 按图标路径注册名称与说明：冻结、潮湿、燃烧在 KL `ElementBuildUpSystem` 注册（文本 `Mods.KL.Terms.Freeze` / `Wet` / `Burning`，数值取自对应常量），魔力、炼金大锅与水魔法书在 `Localization/ElainaTerms.cs` 注册（文本 `Mods.伊蕾娜.Terms.MagicPower` / `AlchemyCauldron` / `WaterSpellbook`）。注册时用 `Category` 指定说明卡右上角的类型，常用类型取 KL 的 `Mods.KL.Terms.Categories`（魔力、冻结为“机制”，炼金大锅为“制作站”）；伊蕾娜独有的类型放在本模组 `Mods.伊蕾娜.Terms.Categories`，如水魔法书等魔法书使用 `Spellbook`（“魔法书”）。星图详情中的已注册名词带淡色下划线，悬停显示说明卡；固定规则数值（如冻结时长、冻结冷却）通过 `KLTerm.Stats` 显示为大号数值栏，说明正文不再重复。新增名词时同步注册并补充中英文说明；文本中的颜色需与注册颜色一致。`[c/…]` 只按 6 位 RGB 解析，8 位写法会丢掉前两位而偏色（如 `4C91FFFF` 实际显示为 `91FFFF`），一律用 6 位。
- 所有界面都应绘制实际魔力图标及彩色名称，不允许用“★”等普通字符替代。纯文本绘制界面需要适配图标的测量、绘制和换行；炼金手册由 `AlchemyDrawing` 处理图标与名称组合，不将标签直接交给普通 DrawString。

## 玩家界面

玩家界面文本放在 `zh-Hans_Mods.伊蕾娜.UI.hjson` / `en-US_Mods.伊蕾娜.UI.hjson`，通过 `UIText.LocalizeUI` 按当前语言读取。插值使用完整句子的格式参数；不要用翻译后的文字判断业务状态。炼金目录保存 `Catalog.*` 键，显示属性按需读取，避免静态初始化固定语言。Debug 编辑、加点和预览演示文本可保留中文。

`Tools/ConstellationSkillPanelPreview/run.ps1 -Culture en-US -LocalizationOnly` 与 `Tools/AlchemyPreview/run.ps1 -Culture en-US` 使用实际 HJSON 和正式绘制代码生成英文 FNA 离线预览；中文使用 `-Culture zh-Hans`。`LocalizationOnly` 跳过原有玩法/图标检查，仅用于文本布局检查。

## 原版物品名称引用

玩家可见文本中出现原版物品名称时，必须引用游戏本地化，不手写中文译名或英文名称。把名称写进模组自己的 HJSON 仍然属于硬编码；中英文文件都必须使用原版引用。此约定适用于炼金材料列表、物品 Tooltip、配方说明、材料来源、技能启封条件和材料不足提示等。

- HJSON 使用 `{$ItemName.<原版键>}`，例如 `获得过{$ItemName.PotionOfReturn}`；英文使用 `Obtain a {$ItemName.PotionOfReturn} at least once`。纯名称或以引用开头的单行值加双引号，例如 `Waterleaf: "{$ItemName.Waterleaf}"`。
- C# 已有物品 ID 时，用 `Lang.GetItemNameValue(itemType)` 按当前语言取得名称，再传入完整句子的格式参数。不要在静态初始化时缓存显示名称。
- 根据真实 ItemID 和本机游戏资源确认引用键，不从模组目录键或手写译名猜测。例如炼金 `Catalog.AetherBlock` 实际映射 `ItemID.ShimmerBlock`，应引用 `{$ItemName.ShimmerBlock}`。键保持稳定，显示名称由当前游戏版本和语言决定。
- 同一说明用于手记和物品 Tooltip 时，复用同一模组本地化键；例如月露 Tooltip 引用 `{$Mods.伊蕾娜.UI.Catalog.ABaseMixtureMadeFromBottledWaterAndMoonglowReadyForAlchemy}`，该说明内部再引用原版材料名。普通叙述中的水、蜂蜜等环境或液体概念不强行替换为物品名称。

修改后运行 `pwsh -File Tools/LocalizationChecks/parse.ps1` 和 `python Tools/LocalizationChecks/check.py`；新增或变更的原版引用还应核对本机游戏中对应键存在。涉及界面显示时，使用加载本机原版本地化资源的 FNA 离线预览检查中英文及缩放，确认没有显示未解析的 `{$...}`。新增炼金材料仍须遵守 `ElainaModAlchemy/AGENTS.md` 的资源与预览检查要求。

## 效果描述

本节的简洁原则同样适用于设置名称、设置提示、界面提示和操作反馈，不只适用于技能或物品说明。

游戏内的技能、物品、Buff 和其他效果描述，应使用玩家能够轻易理解的游戏逻辑，简洁说明使用条件、操作方式、实际效果和必要消耗。保留影响玩家选择的关键数值，不追溯实现原理，也不逐一解释细枝末节或边界情况。

不要展开伤害取整、小数累计补偿、逐帧或逐跳结算、内部计算顺序、同步方式等技术细节，也不要为了说明周全而追加玩家通常不需要的例外条款。实现原理和测试边界写在代码注释或开发文档中。中英文描述遵循相同尺度。

用户提出的实现要求不等于需要逐项写进玩家文案。例如显迹尘的“仅自己可见”用于约束宝箱金光的实现，物品说明和手记只写“能开出炼金配方的宝箱会闪烁金光”，不追加可见对象或联机同步说明；英文同样处理。

不写玩家自然能摸索出的施放操作（如何选目标、右键交互等）和与玩家无直观关联的内部数值（如像素距离，改写为“附近”“站在魔法阵上”等）。

按钮图标与已有标识已清楚表达用途时，不再添加复述用途的悬浮提示。例如技能星图右上角已有关闭叉号与 ESC 标识，无需再提示“关闭星图”。

魔力不足时不能通过灰之魔女转换生命施放的技能，不在正文中说明，而是在技能类中覆写 `AllowsLifeConversion => false`；描述末尾会自动追加强调色的通用警告 `SkillInfo.Snippets.NoLifeConversion`。

技能描述中造成的伤害和治疗量整段着色：中英文 HJSON 只写占位符（“造成{0}”“恢复{0}”），C# 用 `Localization/SkillValueText.cs` 的 `Damage(值, 类型)` / `Heal(值)` 生成“96水元素伤害”“100生命”这类着色短语传入；句中已说明伤害类型的数值（如“伤害改为X”）用 `Number`。颜色只在该类维护：火 `FFB45C`、冰 `B5F1FF`、水 `5FA2FF`、魔法 `D9B8FF`、治疗 `8CEB6E`，与名词标签色错开且不加下划线。秒数、百分比、层数、护盾抵消量等次要数值保持默认颜色。

积累冻结、潮湿、燃烧等异常值的技能，统一按冰锥术的写法给出每次命中的具体积累量：“每枚造成X冰元素伤害，并积累N点[冻结]值”“积累N点[燃烧]值”。不写“最多两次即可点燃”“持续命中约3秒”等换算后的次数或时间，也不在技能正文重复名词卡已说明的阈值、持续时间和高伤害补偿。积累量从实际施加积累的常量或配置计算后传入格式参数，不在文案中写死。

固定积蓄表示玩家可预期的基础积累速度，配置值与显示值必须一致，不额外减半。伤害补偿与固定积蓄取较大值，状态倍率再作用于有效积蓄；描述列出固定值与必要的状态联动。只有终击施加积蓄的技能应明确写“最终冰爆”等触发阶段，避免让玩家误以为每段都积累。

对异常目标的伤害加成和减免由具体技能决定，数值在 `SkillHitBalance` 配置，并写入对应技能说明。异常名词卡只描述状态效果与积蓄规则。

例如，止痛药只需写“每秒恢复最大生命值的1%”，无需解释直接治疗或小数如何累计。水流术只需说明潮湿积累与对燃烧目标的增伤，不追加同一射线脉冲的体节去重或两帧附着限制等实现细节。

设置提示只补充名称未说清、且有助于玩家使用的信息，不复述滑块已经呈现的上下限、默认值或显而易见的约束。例如“技能栏大小倍率”的提示写“调整技能栏的显示大小。”即可，不追加“只能缩小”“不能比原来大”“1.0 为原始大小”等说明。实现上的限制由控件落实，确实影响玩家决策且无法从界面得知的条件才需要说明。

交付前单独审阅本次新增或修改的中英文文案，逐句判断：玩家是否需要这句话才能正确理解或作出选择？删除它是否会造成实际误解？无助于理解的解释应删除，不能因为信息属实就加入。优先沿用用户已修订的同类表述，不自行补回用户删去的内容。解析、键名、占位符和编译检查不代替这项文案审阅。

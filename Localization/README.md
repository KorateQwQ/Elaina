# 本地化规范

## 专有名词

部分文本需要使用统一的专有名词直接指代，不应改写为一般性描述语言。

目前约定的专有名词如下：

- “伊蕾娜的独特魔力”或“伊蕾娜的专属魔力”统一使用图标与颜色标签指代：中文为 `[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:魔力]`，英文为 `[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:Magic Power]`。资源路径不翻译。
- 效果判定根据角色使用原版魔力或独特魔力时，写成“最大魔力或[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:魔力]”，共用“最大”而不重复。单纯增加原版魔力上限时只写“最大魔力增加40”等表述，灰之魔女的被动转换无需在此再次说明。
- KL 的冻结状态（冻结值、冻结冷却等）统一写成：中文 `[klicon/16,0:KL/Drawing/Snippets/Icons/IceIcon][c/8FE3FFFF:冻结]`，英文 `[klicon/16,0:KL/Drawing/Snippets/Icons/IceIcon][c/8FE3FFFF:Freeze]`。冻结时长、冷却和无法行动由名词说明给出，技能描述只写自身的积累量与联动。
- 炼金大锅统一写成：中文 `[klicon/16,0:Item:Cauldron][c/E8C27A:炼金大锅]`，英文 `[klicon/16,0:Item:Cauldron][c/E8C27A:Alchemy Cauldron]`。klicon 路径写成 `Item:原版物品名` 时直接使用原版物品贴图（按 `ItemID.Search` 解析，不硬写物品 ID），无需另画图标。
- 专有名词在 `KL.Drawing.Snippets.KLTerms` 按图标路径注册名称与说明：冻结在 KL `ElementBuildUpSystem` 注册（文本 `Mods.KL.Terms.Freeze`，数值取自冻结常量），魔力与炼金大锅在 `Localization/ElainaTerms.cs` 注册（文本 `Mods.伊蕾娜.Terms.MagicPower` / `AlchemyCauldron`）。注册时用 `Category` 指定说明卡右上角的类型，常用类型取 KL 的 `Mods.KL.Terms.Categories`（魔力、冻结为“机制”，炼金大锅为“制作站”）。星图详情中的已注册名词带淡色下划线，悬停显示说明卡；固定规则数值（如冻结时长、冻结冷却）通过 `KLTerm.Stats` 显示为大号数值栏，说明正文不再重复。新增名词时同步注册并补充中英文说明；文本中的颜色需与注册颜色一致。`[c/…]` 只按 6 位 RGB 解析，8 位写法会丢掉前两位而偏色（如 `4C91FFFF` 实际显示为 `91FFFF`），新名词用 6 位。
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

魔力不足时不能通过灰之魔女转换生命施放的技能，不在正文中说明，而是在技能类中覆写 `AllowsLifeConversion => false`；描述末尾会自动追加强调色的通用警告 `SkillInfo.Snippets.NoLifeConversion`。

例如，止痛药只需写“每秒恢复最大生命值的1%”，无需解释直接治疗或小数如何累计。水流术现有的命中减冷却描述已足够，无需追加“同时命中多个敌人不会额外减少冰凌锁冷却”等说明。

设置提示只补充名称未说清、且有助于玩家使用的信息，不复述滑块已经呈现的上下限、默认值或显而易见的约束。例如“技能栏大小倍率”的提示写“调整技能栏的显示大小。”即可，不追加“只能缩小”“不能比原来大”“1.0 为原始大小”等说明。实现上的限制由控件落实，确实影响玩家决策且无法从界面得知的条件才需要说明。

交付前单独审阅本次新增或修改的中英文文案，逐句判断：玩家是否需要这句话才能正确理解或作出选择？删除它是否会造成实际误解？无助于理解的解释应删除，不能因为信息属实就加入。优先沿用用户已修订的同类表述，不自行补回用户删去的内容。解析、键名、占位符和编译检查不代替这项文案审阅。

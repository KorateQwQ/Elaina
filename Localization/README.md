# 本地化规范

## 专有名词

部分文本需要使用统一的专有名词直接指代，不应改写为一般性描述语言。

目前约定的专有名词如下：

- “伊蕾娜的独特魔力”或“伊蕾娜的专属魔力”统一使用图标与颜色标签指代：中文为 `[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:魔力]`，英文为 `[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:Magic Power]`。资源路径不翻译。
- 效果判定根据角色使用原版魔力或独特魔力时，写成“最大魔力或[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:魔力]”，共用“最大”而不重复。单纯增加原版魔力上限时只写“最大魔力增加40”等表述，灰之魔女的被动转换无需在此再次说明。
- KL 的冻结状态（冻结值、冻结冷却等）统一写成：中文 `[klicon/16,0:KL/Drawing/Snippets/Icons/IceIcon][c/8FE3FFFF:冻结]`，英文 `[klicon/16,0:KL/Drawing/Snippets/Icons/IceIcon][c/8FE3FFFF:Freeze]`。冻结时长、冷却和无法行动由名词说明给出，技能描述只写自身的积累量与联动。
- 专有名词在 `KL.Drawing.Snippets.KLTerms` 按图标路径注册名称与说明：冻结在 KL `ElementBuildUpSystem` 注册（文本 `Mods.KL.Terms.Freeze`，数值取自冻结常量），魔力在 `Localization/ElainaTerms.cs` 注册（文本 `Mods.伊蕾娜.Terms.MagicPower`）。星图详情中的已注册名词带淡色下划线，悬停显示说明卡；固定规则数值（如冻结时长、冻结冷却）通过 `KLTerm.Stats` 显示为大号数值栏，说明正文不再重复。新增名词时同步注册并补充中英文说明；文本中的颜色需与注册颜色一致。
- 所有界面都应绘制实际魔力图标及彩色名称，不允许用“★”等普通字符替代。纯文本绘制界面需要适配图标的测量、绘制和换行；炼金手册由 `AlchemyDrawing` 处理图标与名称组合，不将标签直接交给普通 DrawString。

## 玩家界面

玩家界面文本放在 `zh-Hans_Mods.伊蕾娜.UI.hjson` / `en-US_Mods.伊蕾娜.UI.hjson`，通过 `UIText.LocalizeUI` 按当前语言读取。插值使用完整句子的格式参数；不要用翻译后的文字判断业务状态。炼金目录保存 `Catalog.*` 键，显示属性按需读取，避免静态初始化固定语言。Debug 编辑、加点和预览演示文本可保留中文。

`Tools/ConstellationSkillPanelPreview/run.ps1 -Culture en-US -LocalizationOnly` 与 `Tools/AlchemyPreview/run.ps1 -Culture en-US` 使用实际 HJSON 和正式绘制代码生成英文 FNA 离线预览；中文使用 `-Culture zh-Hans`。`LocalizationOnly` 跳过原有玩法/图标检查，仅用于文本布局检查。

## 效果描述

游戏内的技能、物品、Buff 和其他效果描述，应使用玩家能够轻易理解的游戏逻辑，简洁说明使用条件、操作方式、实际效果和必要消耗。保留影响玩家选择的关键数值，不追溯实现原理，也不逐一解释细枝末节或边界情况。

不要展开伤害取整、小数累计补偿、逐帧或逐跳结算、内部计算顺序、同步方式等技术细节，也不要为了说明周全而追加玩家通常不需要的例外条款。实现原理和测试边界写在代码注释或开发文档中。中英文描述遵循相同尺度。

不写玩家自然能摸索出的施放操作（如何选目标、右键交互等）和与玩家无直观关联的内部数值（如像素距离，改写为“附近”“站在魔法阵上”等）。

魔力不足时不能通过灰之魔女转换生命施放的技能，不在正文中说明，而是在技能类中覆写 `AllowsLifeConversion => false`；描述末尾会自动追加强调色的通用警告 `SkillInfo.Snippets.NoLifeConversion`。

例如，止痛药只需写“每秒恢复最大生命值的1%”，无需解释直接治疗或小数如何累计。水流术现有的命中减冷却描述已足够，无需追加“同时命中多个敌人不会额外减少冰凌锁冷却”等说明。

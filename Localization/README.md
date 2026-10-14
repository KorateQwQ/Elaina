# 本地化规范

## 专有名词

部分文本需要使用统一的专有名词直接指代，不应改写为一般性描述语言。

目前约定的专有名词如下：

- “伊蕾娜的独特魔力”或“伊蕾娜的专属魔力”统一使用图标与颜色标签指代：中文为 `[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:魔力]`，英文为 `[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:Magic Power]`。资源路径不翻译。

## 玩家界面

玩家界面文本放在 `zh-Hans_Mods.伊蕾娜.UI.hjson` / `en-US_Mods.伊蕾娜.UI.hjson`，通过 `UIText.LocalizeUI` 按当前语言读取。插值使用完整句子的格式参数；不要用翻译后的文字判断业务状态。炼金目录保存 `Catalog.*` 键，显示属性按需读取，避免静态初始化固定语言。Debug 编辑、加点和预览演示文本可保留中文。

`Tools/ConstellationSkillPanelPreview/run.ps1 -Culture en-US -LocalizationOnly` 与 `Tools/AlchemyPreview/run.ps1 -Culture en-US` 使用实际 HJSON 和正式绘制代码生成英文 FNA 离线预览；中文使用 `-Culture zh-Hans`。`LocalizationOnly` 跳过原有玩法/图标检查，仅用于文本布局检查。

## 技能简要描述

技能描述应使用玩家能够轻易理解的游戏逻辑，简洁说明使用条件、操作方式、主要效果和必要消耗。不要深入描述伤害取整、逐帧或逐跳结算、内部计算顺序等底层机制。

using KL.Drawing.Snippets;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.Localization;

/// <summary>注册伊蕾娜的专有名词，供星图详情等可悬停界面显示说明；富文本写法见 Localization/README.md。</summary>
public sealed class ElainaTerms : ModSystem
{
    public const string MagicPowerIcon = "伊蕾娜/Snippets/MagicStarIcon";
    public const string AlchemyCauldronIcon = "Item:Cauldron";

    public override void Load()
    {
        KLTerms.Register(new KLTerm(MagicPowerIcon, new Color(76, 145, 255),
            () => Language.GetTextValue("Mods.伊蕾娜.Terms.MagicPower.Name"),
            () => Language.GetTextValue("Mods.伊蕾娜.Terms.MagicPower.Description",
                ElainaMagicAttributes.CombatRecoveryPerSecond,
                ElainaMagicAttributes.CombatMaxMagicPointRecoveryPercentPerSecond * 100f,
                ElainaMagicAttributes.CombatStateDurationSeconds,
                ElainaMagicAttributes.OutOfCombatRefillSeconds),
            Category: () => Language.GetTextValue("Mods.KL.Terms.Categories.Mechanic")));
        KLTerms.Register(new KLTerm(AlchemyCauldronIcon, new Color(232, 194, 122),
            () => Language.GetTextValue("Mods.伊蕾娜.Terms.AlchemyCauldron.Name"),
            () => Language.GetTextValue("Mods.伊蕾娜.Terms.AlchemyCauldron.Description"),
            Category: () => Language.GetTextValue("Mods.KL.Terms.Categories.CraftingStation")));
    }
}

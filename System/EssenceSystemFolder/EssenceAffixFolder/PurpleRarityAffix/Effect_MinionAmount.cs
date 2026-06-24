using System.Text;

namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.PurpleRarityAffix;

public class Effect_MinionAmount : IEssenceAffixEffect
{
    public static readonly Effect_MinionAmount Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        //GetLinearFactor()的整数部分作为minions数量，小数部分作为召唤伤害加成
        int minions = (int)(ctx.GetLinearFactor()*1.5f);
        float damageAddition = (ctx.GetLinearFactor()*1.5f - minions)*0.15f;
        
        player.GetDamage(DamageClass.Summon) += damageAddition;
        player.maxMinions += minions;
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        int minions = (int)(ctx.GetLinearFactor()*1.5f);
        float damageAddition = (ctx.GetLinearFactor()*1.5f - minions)*15;

        string valueText1 = minions.ToString("0.0");
        string valueText2 = damageAddition.ToString("0.0");

        string minionsText = Language.GetText("CommonItemTooltip.IncreasesMaxMinionsBy").WithFormatArgs(valueText1)
            .Value;
        string damageText = Language.GetText("CommonItemTooltip.PercentIncreasedSummonDamage")
            .WithFormatArgs(valueText2).Value;
        
        var sb = new StringBuilder(128);
        return sb.Append(minionsText).Append(", ").Append(damageText).ToString();
    }
}
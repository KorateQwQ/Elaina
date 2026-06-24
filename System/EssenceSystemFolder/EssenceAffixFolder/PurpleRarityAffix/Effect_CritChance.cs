namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.PurpleRarityAffix;

public sealed class Effect_CritChance : IEssenceAffixEffect
{
    public static readonly Effect_CritChance Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        player.GetCritChance(DamageClass.Generic) += ctx.GetLinearFactor()*12f;
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = ctx.GetLinearFactor()*12f;
        string valueText = value.ToString("0.0");

        return Language.GetText("CommonItemTooltip.PercentIncreasedCritChance")
                .WithFormatArgs(valueText).Value;
    }
    
}
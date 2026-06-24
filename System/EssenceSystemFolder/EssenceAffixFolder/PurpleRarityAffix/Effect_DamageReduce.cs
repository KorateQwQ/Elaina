
namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.PurpleRarityAffix;

public class Effect_DamageReduce : IEssenceAffixEffect
{
    public static readonly Effect_DamageReduce Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        float value = ctx.GetLinearFactor()*0.1f;
        player.endurance += value;
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = ctx.GetLinearFactor()*10;
        string valueText = value.ToString("0.0");
        
        return Language.GetText("CommonItemTooltip.ReducesDamageTakenByPercent").WithFormatArgs(valueText).Value;
    }
}
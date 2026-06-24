using static 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.IEssenceAffixEffect;

namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder;

//伤害加成effect
public sealed class Effect_DamageAddition : IEssenceAffixEffect
{
    public static readonly Effect_DamageAddition MeleeInstance = new(DamageClass.Melee);
    public static readonly Effect_DamageAddition MagicInstance = new(DamageClass.Magic);
    public static readonly Effect_DamageAddition RangedInstance = new(DamageClass.Ranged);
    public static readonly Effect_DamageAddition SummonInstance = new(DamageClass.Summon);
    public static readonly Effect_DamageAddition ThrowingInstance = new(DamageClass.Throwing);
    
    public static readonly Effect_DamageAddition RandomInstance = new(Main.rand.NextFromList(
            DamageClass.Melee, DamageClass.Magic, DamageClass.Ranged, DamageClass.Summon, DamageClass.Throwing
        ));
    
    public DamageClass DamageClass;
    

    private Effect_DamageAddition(DamageClass damageClass)
    {
        DamageClass = damageClass;
    }

    //根据基质的roll和boss进度决定增伤强度
    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        // 2% ~ 10% * BossProgress01（你后续可以按稀有度/词条组进一步改）
        float add = GetAddition(ctx);
        player.GetDamage(DamageClass) += add;
    }

    float GetAddition(in EssenceAffixEffectContext ctx)
    {
        return ctx.GetLinearFactor()*0.15f;
    }
    
    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = GetAddition(ctx)*100;
        string valueText = value.ToString("0.0");
        
        //return "[c/bd6ff:Test Description]";
        if (DamageClass == DamageClass.Melee)
        {
            return Language.GetText("CommonItemTooltip.PercentIncreasedMeleeDamage").WithFormatArgs(valueText).Value;
        }
        if (DamageClass == DamageClass.Magic)
        {
            return Language.GetText("CommonItemTooltip.PercentIncreasedMagicDamage").WithFormatArgs(valueText).Value;
        }

        if (DamageClass == DamageClass.Ranged)
        {
            return Language.GetText("CommonItemTooltip.PercentIncreasedRangedDamage").WithFormatArgs(valueText).Value;
        }

        if (DamageClass == DamageClass.Summon)
        {
            return Language.GetText("CommonItemTooltip.PercentIncreasedSummonDamage").WithFormatArgs(valueText).Value;
        }

        if (DamageClass == DamageClass.Throwing)
        {
            return Language.GetText("Mods.伊蕾娜.EssenceSystem.CommonItemTooltip.PercentIncreasedThrowingDamage").WithFormatArgs(valueText).Value;
        }
        
        return Language.GetText("CommonItemTooltip.PercentIncreasedDamage")
            .WithFormatArgs(valueText).Value;
    }
}
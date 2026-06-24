using System;
using KL.Extensions;
using KL.Utils;
using static 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.IEssenceAffixEffect;

namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder;

public readonly struct EssenceAffixEffectContext
{
    public readonly EssenceAffixDef Def;
    public readonly float Roll01;
    public readonly string BossName;
    public readonly float BossState;
    public readonly float BossProgress01;

    ///获得以肉山为基准点的线性系数，当boss阶段为肉山（7）时，词条的效果将为100%。当boss阶段为月总（18）时，词条的效果将为200%,当boss阶段为月总大于18时，词条的效果为每多1的进度，词条的效果增加10%
    /// 当state值为23时，效果为320%
    public float GetLinearFactor()
    {
        //如果boss阶段为肉山时，词条的效果将为100%
        if (BossState <= 7)
        {
            return KLMathF.ClampLerp(0.3f, 1f, BossState / 7f)* Roll01;
        }
        
        //如果boss阶段为月总时，词条的效果将为200%
        if (BossState <= 18)
        {
            //return Utils.GetLerpValue(1f, 2.5f, (BossState-7)/11f,true)* Roll01;
            return KLMathF.ClampLerp(1f, 2f, (BossState-7)/11f)* Roll01;
        }
        
        //如果boss阶段为月总大于18时，词条的效果为每多1的进度，词条的效果增加10%
        // 以月总(18)的 2.0f 为基准，之后每 +1 进度按 10% 递增（乘算），用于兼容更多后续Boss/模组进度。
        return 2f * MathF.Pow(1.1f, BossState - 18f)* Roll01;
    } 
    public EssenceAffixEffectContext(EssenceAffixDef def, float roll01, string bossName, float bossState, float bossProgress01)
    {
        Def = def;
        Roll01 = roll01;
        BossName = bossName;
        BossState = bossState;
        BossProgress01 = bossProgress01;
    }
}

/// <summary>
/// 词条效果（逻辑层）的最小接口：定义层只“挂一个效果对象”，不靠继承 EssenceAffixDef 来承载逻辑。
/// 这样 EssenceAffixDef 仍然可以保持为稳定的数据结构（也能继续 sealed）。
/// </summary>
public interface IEssenceAffixEffect
{
    /// <summary>
    /// 更新词条效果，所有端都会调用。
    /// </summary>
    /// <param name="player"></param>
    /// <param name="ctx"></param>
    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        UpdateAccessory(player);
    }
    public void UpdateAccessory(Player player)
    {
        // do nothing
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        return "";
    }
    
    public bool SameAs(IEssenceAffixEffect other)
    {
        return this == other;
    }
}


/// <summary>
/// 无效果,用来做什么呢，我也不知道，我是自由的。
/// </summary>
public sealed class NoEssenceAffixEffect : IEssenceAffixEffect
{
    public static readonly NoEssenceAffixEffect Instance = new();

    private NoEssenceAffixEffect()
    {
    }

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        return Language.GetText("CommonItemTooltip.PercentIncreasedDamage")
            .WithFormatArgs("").Value;
    }
}


//近战速度词条
public sealed class Effect_MeleeSpeed : IEssenceAffixEffect
{
    public static readonly Effect_MeleeSpeed Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        player.GetAttackSpeed(DamageClass.Melee) += ctx.GetLinearFactor()*0.12f;
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = ctx.GetLinearFactor()*12f;
        string valueText = value.ToString("0.0");
        
        return Language.GetText("CommonItemTooltip.PercentIncreasedMeleeSpeed")
            .WithFormatArgs(valueText).Value;
    }
}

//魔力消耗减少词条
public sealed class Effect_ManaCostReduce : IEssenceAffixEffect
{
    public static readonly Effect_ManaCostReduce Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        player.manaCost -= ctx.GetLinearFactor() * 0.1f;
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = ctx.GetLinearFactor() * 10f;
        string valueText = value.ToString("0.0");
        
        return Language.GetText("CommonItemTooltip.PercentReducedManaCost")
            .WithFormatArgs(valueText).Value;
    }
}

//生命回复增加词条
public sealed class Effect_LifeRegen : IEssenceAffixEffect
{
    public static readonly Effect_LifeRegen Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        player.lifeRegen += (int)(ctx.BossState);
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = ((int)ctx.BossState /2f);
        string valueText = value.ToString("0.0");
        return Language.GetText("Mods.伊蕾娜.EssenceSystem.CommonItemTooltip.IncreasedLifeRegen")
            .WithFormatArgs(valueText).Value;
    }
}

//护甲提高词条
public sealed class Effect_Armor : IEssenceAffixEffect
{
    public static readonly Effect_Armor Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        player.statDefense += (int)(ctx.GetLinearFactor() * 5);
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = (int)(ctx.GetLinearFactor() * 5);
        string valueText = value.ToString("0.0");
        return Language.GetText("CommonItemTooltip.IncreasesDefenseBy")
            .WithFormatArgs(valueText).Value;
    }
}

//穿甲提升词条
public sealed class Effect_ArmorPenetration : IEssenceAffixEffect
{
    public static readonly Effect_ArmorPenetration Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        player.GetArmorPenetration(DamageClass.Generic) += ctx.GetLinearFactor() * 10;
    }

    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = ctx.GetLinearFactor() * 10;
        string valueText = value.ToString("0.0");
        return Language.GetText("CommonItemTooltip.IncreasesArmorPenBy")
            .WithFormatArgs(valueText).Value;
    }
}
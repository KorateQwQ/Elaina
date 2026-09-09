using System;
using KL.AttributeSystem;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 伊蕾娜魔力系统的代码侧数值和唯一 KL 属性入口。
/// 这些 C# 属性直接返回代码中的数值，不依赖 ModConfig。
/// </summary>
public static class ElainaMagicAttributes
{
    /// <summary>KL 属性系统中的“伊蕾娜独特魔力”属性类型。</summary>
    public static readonly AttributeDefinition UniqueMagic =
        new("伊蕾娜.UniqueMagic", 0f, 0f);

    /// <summary>1 点原版最大 mana 对应的独特魔力上限。</summary>
    public static float VanillaManaToMagicPointRatio => 2f;

    /// <summary>灰之魔女开启时，额外生命收益保留比例。</summary>
    public static float ExtraLifeRetentionRatio => 0.5f;

    /// <summary>战斗中的固定每秒恢复量。</summary>
    public static float CombatRecoveryPerSecond => 10f;

    /// <summary>战斗中的每秒最大魔力百分比恢复量。</summary>
    public static float CombatMaxMagicPointRecoveryPercentPerSecond => 0.01f;

    /// <summary>脱战后回满独特魔力所需的秒数。</summary>
    public static float OutOfCombatRefillSeconds => 2f;

    /// <summary>受伤后暂停独特魔力恢复的秒数。</summary>
    public static float HurtRecoveryPauseSeconds => 3f;

    /// <summary>生命转魔力时每一步消耗的最大生命百分比。</summary>
    public static float LifePercentPerConversionStep => 1f;

    /// <summary>生命转魔力时每一步恢复的最大魔力百分比。</summary>
    public static float MagicPointPercentPerConversionStep => 1f;

    /// <summary>消耗魔力、命中或受伤后保持战斗状态的时间。</summary>
    public static float CombatStateDurationSeconds => 10f;

    public static int ToTicks(float seconds)
    {
        return Math.Max(0, (int)MathF.Round(Math.Max(0f, seconds) * 60f));
    }
}

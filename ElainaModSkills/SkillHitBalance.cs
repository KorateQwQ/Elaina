namespace 伊蕾娜.ElainaModSkills;

/// <summary>技能命中的基础积蓄（点）与额外伤害。Multiplier 表示总伤害倍率，BonusRatio 表示加到基础 DPS 系数上的数值。</summary>
public static class SkillHitBalance
{
    // 冰锥术
    public static float IceConeBuildup => 1.6f;
    public static float IceConeWetDamageMultiplier => 1.1f;

    // 冰凌锁（仅最终冰爆积累冻结）
    public static float ShardbindFinalBuildup => 50f;
    public static float ShardbindWetDamageMultiplier => 1.5f;

    // 水流术
    public static float WaterStreamBuildup => 1.5555556f;
    public static float WaterStreamBurningDamageMultiplier => 1.5f;
    public static float WaterStreamSplashDamageMultiplier => .5f;

    // 水球魔法
    public static float WaterBallBuildup => 30f;
    public static float WaterBallBurningDamageMultiplier => 1.5f;

    // 火焰射线
    public static float FireLaserBuildup => 5.5555556f;
    public static float FireLaserFrostDamageMultiplier => 1.1f;
    public static double FireLaserDomainBonusRatio => .2;

    // 炎爆。单目标加成是相对基础伤害的比例，.43 表示额外 43%。
    public static float FireBurstBuildup => 50f;
    public static float FireBurstFrostDamageMultiplier => 1.5f;
    public static double FireBurstSingleTargetBonus => .43;

    // 风刃、风龙卷
    public static float WindSlashBuildup => 0f;
    public static float WindTornadoBuildup => 0f;

    // 魔法飞弹被动溅射
    public static float MagicMissileSplashDamageMultiplier => 2f;
}

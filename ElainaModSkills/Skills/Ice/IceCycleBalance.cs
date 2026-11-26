namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>冰水循环的机制参数；伤害、耗蓝与基础冷却仍读取 SkillBalance.json。</summary>
public static class IceCycleBalance
{
    // 相对确认：75 帧开始齐射，78–106 帧命中，92 帧蓄光，164 帧终爆。
    public const int LockChargeStartFrame = ShardbindSequence.ChargeStartTick;
    public const int LockBurstFrame = ShardbindSequence.BurstTick;
    public static int LockSpawnDelay(int frame) => 1 + (int)global::System.MathF.Ceiling((frame - 1) * 1.5f);
    public const float LockRadius = 200f;
    public const float LockImpactRadius = 80f;

    public static int LockFinalDamage(int totalDamage) => (totalDamage + 1) / 2;
    public static int LockShardDamage(int totalDamage, int shardIndex, int shardCount)
    {
        int budget = totalDamage - LockFinalDamage(totalDamage);
        return budget / shardCount + (shardIndex < budget % shardCount ? 1 : 0);
    }
}

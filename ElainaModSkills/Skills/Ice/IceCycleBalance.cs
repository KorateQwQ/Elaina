namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>冰水循环的机制参数；伤害、耗蓝与基础冷却仍读取 SkillBalance.json。</summary>
public static class IceCycleBalance
{
    public const float BeamFreezeRefund = 0.5f;
    public const float BeamLockRefund = 0.30f;
    public const float BallFreezeRefund = 4f;
    public const float BallLockRefund = 3f;
    public const int LockBurstFrame = 110;
    public const float LockRadius = 200f;
    public const float LockImpactRadius = 80f;

    public static int LockFinalDamage(int totalDamage) => (totalDamage + 1) / 2;
    public static int LockShardDamage(int totalDamage, int shardIndex, int shardCount)
    {
        int budget = totalDamage - LockFinalDamage(totalDamage);
        return budget / shardCount + (shardIndex < budget % shardCount ? 1 : 0);
    }
}

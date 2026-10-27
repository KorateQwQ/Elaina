using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.System.Experience;

/// <summary>角色经验与升级研习点规则；光球数量不影响奖励。</summary>
public static class ExperienceRules
{
    public const float DistributionRadius = 1200f;
    public const int EnemyLifePerExperience = 10;
    public const int FirstBossRewardDivisor = 4;
    public const int RepeatBossLifePerExperience = 200;
    public const int RepeatBossBarDivisor = 20;
    public const int StudyPointsPerLevel = 20;
    public const int StudyPointBonusInterval = 5;
    public const int StudyPointBonus = 20;
    public const int FloatTicks = 30;
    public const int Lifetime = 900;
    public const float PickupRadius = 18f;

    // 每级增加 10：肉前常见混合遭遇每五级约 100–250 只；肉后暂沿用，另行校准。
    public static int RequiredForLevel(int level)
    {
        return (int)Math.Min(int.MaxValue, 100L + 10L * (Math.Max(1, level) - 1L));
    }

    public static int ForEnemyLife(int baseLife) => baseLife <= 0 ? 0 : Math.Max(1, baseLife / EnemyLifePerExperience);

    public static int ForFirstBoss(int previousLevelCap) => RequiredForLevel(previousLevelCap) / FirstBossRewardDivisor;

    /// <summary>从 previousLevel 升到 reachedLevel 的研习点总额；Lv1 无初始奖励。</summary>
    public static long StudyPointsForLevels(int previousLevel, int reachedLevel)
    {
        int from = Math.Max(1, previousLevel), to = Math.Max(from, reachedLevel);
        return (long)(to - from) * StudyPointsPerLevel
            + (long)(to / StudyPointBonusInterval - from / StudyPointBonusInterval) * StudyPointBonus;
    }

    /// <summary>旧版每级 10、每五级额外 10；仅补发新旧差额，包含调试提前结算的等级。</summary>
    public static long LegacyStudyPointIncrease(int rewardedLevel)
    {
        int level = Math.Max(1, rewardedLevel);
        long previousReward = (level - 1L) * 10 + (long)(level / 5) * 10;
        return StudyPointsForLevels(1, level) - previousReward;
    }

    // 补偿而非主要升级来源：KL 血条总生命的 0.5%，至多当前等级一条经验的 5%。
    public static int ForRepeatBoss(int realMaxHp, int level) => realMaxHp <= 0 ? 0
        : Math.Min(Math.Max(1, realMaxHp / RepeatBossLifePerExperience),
            Math.Max(1, RequiredForLevel(level) / RepeatBossBarDivisor));

    // 到达 Boss 上限时最多暂存该等级的一条经验；提高上限后按真实等级逐级消化。
    public static int Add(ref int level, ref int experience, int maximumLevel, int amount)
    {
        maximumLevel = Math.Max(1, maximumLevel);
        level = Math.Clamp(level, 1, maximumLevel);
        // 降低曲线后，旧存档的余经验可能超过新的一条；先用于升级，再按当前上限封顶。
        experience = Math.Max(0, experience);
        amount = Math.Max(0, amount);
        long remaining = (long)experience + amount;
        while (level < maximumLevel && remaining >= RequiredForLevel(level))
        {
            remaining -= RequiredForLevel(level);
            level++;
        }
        experience = (int)Math.Min(remaining, RequiredForLevel(level));
        return (int)Math.Max(0L, (long)amount - (remaining - experience));
    }

    public static Vector2 NextVelocity(Vector2 position, Vector2 velocity, Vector2 target, float age)
    {
        if (age < FloatTicks)
            return new Vector2(velocity.X * .94f, MathHelper.Lerp(velocity.Y, -.45f, .08f));

        Vector2 delta = target - position;
        float distance = delta.Length();
        if (distance < .001f) return Vector2.Zero;
        float speed = Math.Min(24f, 3f + (age - FloatTicks) * .45f);
        Vector2 desired = delta / distance * Math.Min(speed, distance);
        Vector2 result = Vector2.Lerp(velocity, desired, .18f);
        // 接近玩家时及时收拢，避免绕身打转。
        return distance < speed * 2 ? desired : result;
    }
}

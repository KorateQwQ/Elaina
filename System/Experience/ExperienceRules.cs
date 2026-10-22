using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.System.Experience;

/// <summary>第一版占位数值集中在这里，经验数量与光球数量无关。</summary>
public static class ExperienceRules
{
    public const float DistributionRadius = 1200f;
    public const int ExperiencePerEnemy = 10;
    public const int ExperiencePerLevel = 100;
    public const int FloatTicks = 30;
    public const int Lifetime = 900;
    public const float PickupRadius = 18f;

    // 等级上限仍由 Boss 进度决定；到达上限时最多暂存一条经验，解锁后可继续使用。
    public static int Add(ref int level, ref int experience, int maximumLevel, int amount)
    {
        maximumLevel = Math.Max(1, maximumLevel);
        level = Math.Clamp(level, 1, maximumLevel);
        experience = Math.Clamp(experience, 0, ExperiencePerLevel);
        long previous = (long)(level - 1) * ExperiencePerLevel + experience;
        long total = Math.Min((long)maximumLevel * ExperiencePerLevel, previous + Math.Max(0, amount));
        level = (int)Math.Min(maximumLevel, total / ExperiencePerLevel + 1);
        experience = (int)(total - (long)(level - 1) * ExperiencePerLevel);
        return (int)(total - previous);
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

using KL.Buffs.ElementalDebuff;
using KL.DamageSystem;
using KL.Extensions;
using KL.Utils;

namespace 伊蕾娜.ElainaModSkills;

internal static class PredictiveAim
{
    // 按飞行时间估算提前量；限制为 15 帧，避免远距离或高速目标导致过度偏移。
    internal static Vector2 GetAimPoint(Vector2 origin, NPC target, float flightSpeed)
    {
        if (flightSpeed <= 0f || IsTargetMovementPaused(target))
            return target.Center;

        float leadTime = MathHelper.Clamp(Vector2.Distance(origin, target.Center) / flightSpeed, 0f, 15f);
        return target.Center + target.velocity * leadTime;
    }

    private static bool IsTargetMovementPaused(NPC target)
    {
        // KL 冻结和时停会保留 velocity，暂停期间不能把它当作实际移动速度。
        if (TimeStopManager.timeStop || IsFrozen(target))
            return true;

        // 多部位 Boss 共用元素锚点；不能只依赖 PostAI 中会被清除的 IsLogicFrozen。
        NPC anchor = target.GetElementAnchorNPC();
        return anchor != target && anchor is { active: true } && IsFrozen(anchor);
    }

    private static bool IsFrozen(NPC target)
    {
        ElementalGlobalNPC state = target.GetGlobalNPC<ElementalGlobalNPC>();
        return target.HasBuff<ElementalAffliction_Freeze>()
            || state.GetAffliction(ElementType.Ice)
            || state.IsLogicFrozen;
    }
}

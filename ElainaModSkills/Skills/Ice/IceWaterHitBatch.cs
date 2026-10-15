using System.Collections.Generic;
using KL.Extensions;
using KL.Utils;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>一次施法/付费脉冲：玩家返还一次，每个元素锚点的冻结冷却只扣一次。</summary>
public sealed class IceWaterHitBatch
{
    private readonly HashSet<int> anchors = new();
    private bool refunded;

    public void Reset()
    {
        anchors.Clear();
        refunded = false;
    }

    public void OnHit(Player owner, NPC target, int damageDone, bool waterBall)
    {
        if (owner.whoAmI != Main.myPlayer || damageDone <= 0 || target.friendly || target.immortal) return;
        var cycle = owner.GetModPlayer<IceCyclePlayer>();
        if (!refunded)
        {
            refunded = true;
            cycle.ReduceLockCooldown(waterBall ? IceCycleBalance.BallLockRefund : IceCycleBalance.BeamLockRefund);
        }

        NPC anchor = target.GetElementAnchorNPC();
        if (anchor is { active: true } && anchors.Add(anchor.whoAmI))
            cycle.RequestWaterHit(anchor, waterBall);
    }
}

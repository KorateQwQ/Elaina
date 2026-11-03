using System.Collections.Generic;
using KL.DamageSystem;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

/// <summary>一发水球或一个付费脉冲共享命中前状态；体节伤害独立，附着按主体去重。</summary>
public sealed class WaterExposureBatch
{
    private readonly Dictionary<(int Index, int Generation), bool> burning = new();
    private readonly Dictionary<int, FireBurnHit> hits = new();
    private readonly HashSet<(int Index, int Generation)> applied = new();

    public void Reset() { burning.Clear(); hits.Clear(); applied.Clear(); }

    public bool Capture(NPC target)
    {
        if (FireBurnNPC.CaptureHit(target) is not { } hit) return false;
        var key = (hit.AnchorIndex, hit.AnchorGeneration);
        if (!burning.TryGetValue(key, out bool wasBurning)) burning[key] = wasBurning = hit.WasBurning;
        hits[target.whoAmI] = hit with { WasBurning = wasBurning };
        return wasBurning;
    }

    public void OnHit(Player owner, NPC target, int damageDone, Projectile projectile, int attachmentTicks, int sequence)
    {
        if (owner.whoAmI != Main.myPlayer || damageDone <= 0 || !hits.TryGetValue(target.whoAmI, out var hit)
            || !applied.Add((hit.AnchorIndex, hit.AnchorGeneration))) return;
        owner.GetModPlayer<WaterExposurePlayer>().Request(hit, projectile, attachmentTicks, sequence);
    }
}

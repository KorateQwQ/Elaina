using System.Collections.Generic;
using KL.DamageSystem;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

/// <summary>一发水球或一个付费脉冲共享命中前状态；体节伤害独立，附着按主体去重。
/// 去重只在发起端本地完成：每个主体只提交一次请求，服务器不再按弹幕标识或脉冲序号判断重复。</summary>
public sealed class WaterExposureBatch
{
    private readonly Dictionary<(int Index, int Generation), bool> burning = new();

    public void Reset() { burning.Clear(); }

    public bool Capture(NPC target)
    {
        if (FireBurnNPC.CaptureHit(target) is not { } hit) return false;
        var key = (hit.AnchorIndex, hit.AnchorGeneration);
        if (!burning.TryGetValue(key, out bool wasBurning)) burning[key] = wasBurning = hit.WasBurning;
        return wasBurning;
    }

}

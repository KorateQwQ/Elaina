using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModSkills;

/// <summary>一次性战斗解封，与技能层数/冷却独立；不写入角色存档。</summary>
public abstract class ElainaSealedSkill : ElainaSkill
{
    private bool unsealed;
    private ulong? releaseTick, deniedTick;
    public bool IsSealed => !unsealed;
    public float SealReleaseAge => releaseTick.HasValue ? (Main.GameUpdateCount - releaseTick.Value) / 60f : -1;
    public float SealDeniedAge => deniedTick.HasValue ? (Main.GameUpdateCount - deniedTick.Value) / 60f : 9;

    public bool Unseal()
    {
        if (unsealed) return false;
        unsealed = true;
        releaseTick = Main.GameUpdateCount;
        deniedTick = null;
        return true;
    }

    public void Seal()
    {
        unsealed = false;
        releaseTick = deniedTick = null;
    }

    public void NotifySealedAttempt()
    {
        if (IsSealed && SealDeniedAge >= .38f) deniedTick = Main.GameUpdateCount;
    }

    public override void Initialize() { Seal(); base.Initialize(); }
    public override void DeserializeData(TagCompound tag) { base.DeserializeData(tag); Seal(); }
    public override void OnLockSkill() { Seal(); base.OnLockSkill(); }
    public override bool CanUseSkill() => !IsSealed && Stack > 0 && base.CanUseSkill();
}

using Terraria.ID;
using BarrierPlayer = 伊蕾娜.ElainaModSkills.Skills.MagicBarrier.MagicBarrierSkill.MagicBarrierModPlayer;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public sealed class FrostVeilPlayer : ModPlayer
{
    public FrostVeilState Veil { get; } = new();
    internal uint Generation;
    private bool pendingHit;
    private bool IsOwner => Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer;

    public override void Initialize() { Veil.Clear(); Generation = 0; pendingHit = false; }
    public override void OnEnterWorld() => Initialize();

    public override void PostUpdate()
    {
        pendingHit = false;
        if (Veil.Tick(IsOwner)) FrostVeilSystem.SendOwnerState(this);
        if (IsOwner && !Player.dead && Veil.Shields > 0) EnsureVisual();
    }

    public override void UpdateDead()
    {
        pendingHit = false;
        if (Veil.Shields == 0) return;
        Veil.Clear();
        if (IsOwner) FrostVeilSystem.SendOwnerState(this);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (Main.netMode == NetmodeID.Server && Player.active)
            FrostVeilSystem.SendState(this, toWho);
    }

    /// <summary>Called explicitly after MagicBarrier's percentage reduction, independent of ModPlayer load order.</summary>
    internal void ResolveDirectHit(ref Player.HurtInfo info)
    {
        pendingHit = false;
        if (!IsOwner || Player.dead || info.Cancelled || info.Damage <= 0 || Veil.Shields == 0) return;
        int remaining = Veil.ReduceDamage(info.Damage);
        if (remaining == info.Damage) return;
        if (remaining == 0)
        {
            ConsumeShield();
            info.Cancelled = true;
            Player.GetModPlayer<BarrierPlayer>().ApplyMinimumHitImmunity(info);
            Player.lifeRegenTime = 0f;
        }
        else
        {
            info.Damage = remaining;
            // HurtInfo is evaluated before vanilla dodges. A dodged partial hit must not spend a layer.
            pendingHit = true;
        }
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (pendingHit && IsOwner && !info.Cancelled) ConsumeShield();
        pendingHit = false;
    }

    private void ConsumeShield()
    {
        if (Veil.Consume()) FrostVeilSystem.SendOwnerState(this);
    }

    private void EnsureVisual()
    {
        int type = ModContent.ProjectileType<FrostVeilProjectile>();
        if (Player.ownedProjectileCounts[type] > 0) return;
        Projectile.NewProjectile(Player.GetSource_Misc("FrostVeil"), Player.MountedCenter, Vector2.Zero,
            type, 0, 0f, Player.whoAmI);
    }
}

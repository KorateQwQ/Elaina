using System;
using KL.DamageSystem;
using KL.Utils;
using KL.Utils.Net;
using Terraria.ID;
using 伊蕾娜.ElainaModSkills.Skills.Wind;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>拥有端提交命中，由服务器确认燃烧及风刃传播。</summary>
public sealed class FireCyclePlayer : KLModPlayer
{
    public void RequestBeamHit(FireBurnHit hit, Projectile projectile, int exposureTicks, int pulseSequence, int damageDone)
    {
        if (Player.whoAmI != Main.myPlayer || damageDone <= 0) return;
        int dotDamage = Math.Max(1, Player.GetModPlayer<ElainaStatePlayer>().GetLevel() / 10);
        if (Main.netMode == NetmodeID.MultiplayerClient)
            RPC(nameof(ApplyBeamHit), [hit.TargetIndex, hit.TargetGeneration, hit.AnchorIndex, hit.AnchorGeneration,
                hit.WasBurning, projectile.identity, projectile.type, exposureTicks, dotDamage, damageDone, pulseSequence],
                KLNetModule.NetSendType.ClientToServer);
        else ApplyBeamHit(hit.TargetIndex, hit.TargetGeneration, hit.AnchorIndex, hit.AnchorGeneration, hit.WasBurning,
            projectile.identity, projectile.type, exposureTicks, dotDamage, damageDone, pulseSequence);
    }

    public void ApplyBeamHit(int targetIndex, int targetGeneration, int anchorIndex, int anchorGeneration, bool wasBurning,
        int identity, int projectileType, int exposureTicks, int dotDamage, int damageDone, int pulseSequence)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead) return;
        Projectile source = FindOwnedProjectile(identity, projectileType);
        if (source?.ModProjectile is not FireLaser) return;
        FireBurnNPC.ApplyBeamHit(new(targetIndex, targetGeneration, anchorIndex, anchorGeneration, wasBurning),
            Player.whoAmI, Math.Clamp(exposureTicks, 1, 60), dotDamage, damageDone, identity, pulseSequence);
    }

    private Projectile FindOwnedProjectile(int identity, int type)
    {
        foreach (Projectile candidate in Main.ActiveProjectiles)
            if (candidate.owner == Player.whoAmI && candidate.identity == identity && candidate.type == type)
                return candidate;
        return null;
    }

    public void RequestHit(NPC target, Projectile projectile, int exposureTicks = 0, int pulseSequence = 0)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        int damage = Math.Max(1, Player.GetModPlayer<ElainaStatePlayer>().GetLevel() / 10);
        int generation = target.GetGlobalNPC<FireBurnNPC>().InstanceId;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            RPC(nameof(ApplyHit), [target.whoAmI, generation, projectile.identity, projectile.type, exposureTicks, damage, pulseSequence], KLNetModule.NetSendType.ClientToServer);
        else ApplyHit(target.whoAmI, generation, projectile.identity, projectile.type, exposureTicks, damage, pulseSequence);
    }

    public void ApplyHit(int targetIndex, int generation, int identity, int projectileType, int exposureTicks, int damage, int pulseSequence = 0)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead
            || targetIndex < 0 || targetIndex >= Main.maxNPCs) return;
        NPC target = Main.npc[targetIndex];
        if (!target.active || target.friendly || target.immortal || target.dontTakeDamage) return;
        var burn = target.GetGlobalNPC<FireBurnNPC>();
        if (generation != burn.InstanceId) return;
        Projectile source = FindOwnedProjectile(identity, projectileType);
        if (source == null) return;
        if (source.ModProjectile is not (WindSlash or WindBlade or WindTornado)) return;
        var wind = source.GetGlobalProjectile<FireWindState>();
        if (FireBurnNPC.IsBurning(target)) wind.CarriesFire = true;
        if (wind.CarriesFire) burn.ApplyExposure(target, Player.whoAmI, 1, damage, ignite: true);
    }

}

public sealed class FireWindState : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public bool CarriesFire;

    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (damageDone > 0 && projectile.owner == Main.myPlayer
            && projectile.ModProjectile is WindSlash or WindBlade or WindTornado)
            Main.player[projectile.owner].GetModPlayer<FireCyclePlayer>().RequestHit(target, projectile);
    }
}


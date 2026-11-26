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
    public override void PostUpdateEquips() => Player.GetModPlayer<ElementalInfusionPlayer>().BurnDamage =
        Math.Max(1, Player.GetModPlayer<ElainaStatePlayer>().GetLevel() / 10);

    /// <summary>服务器（单人为本地）调用：把领域延长广播给所有端，各端同时累加。</summary>
    public void BroadcastDomainExtension(int identity, int ticks)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        RPC(nameof(ApplyDomainExtension), [identity, ticks], KLNetModule.NetSendType.ServerToAll);
    }

    public void ApplyDomainExtension(int identity, int ticks)
    {
        if (FindOwnedProjectile(identity, ModContent.ProjectileType<FireBurstDomain>())?.ModProjectile is FireBurstDomain domain)
            domain.Extend(ticks);
    }

    private Projectile FindOwnedProjectile(int identity, int type)
    {
        foreach (Projectile candidate in Main.ActiveProjectiles)
            if (candidate.owner == Player.whoAmI && candidate.identity == identity && candidate.type == type)
                return candidate;
        return null;
    }


    public override void Load()
    {
        base.Load();
        ElementStrikeSystem.WindProjectileHit += SpreadFire;
    }
    
    public override void Unload() => ElementStrikeSystem.WindProjectileHit -= SpreadFire;

    private static void SpreadFire(FireBurnHit hit, int owner, int type, int identity)
    {
        var player = Main.player[owner];
        var source = player.GetModPlayer<FireCyclePlayer>().FindOwnedProjectile(identity, type);
        if (source?.ModProjectile is not (WindSlash or WindBlade or WindTornado)) return;
        NPC target = Main.npc[hit.TargetIndex];
        if (!target.active || target.life <= 0 || target.GetGlobalNPC<FireBurnNPC>().InstanceId != hit.TargetGeneration) return;
        var wind = source.GetGlobalProjectile<FireWindState>();
        wind.CarriesFire |= hit.WasBurning;
        if (wind.CarriesFire) target.GetGlobalNPC<FireBurnNPC>().ApplyExposure(target, owner, 1,
            player.GetModPlayer<ElementalInfusionPlayer>().BurnDamage, ignite: true,
            attack: source.GetGlobalProjectile<ElementalGlobalProjectile>().BuildupBatch);
    }
}

public sealed class FireWindState : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public bool CarriesFire;
}

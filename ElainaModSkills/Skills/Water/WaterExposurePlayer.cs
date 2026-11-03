using System;
using KL.DamageSystem;
using KL.Utils;
using KL.Utils.Net;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

public sealed class WaterExposurePlayer : KLModPlayer
{
    public void Request(FireBurnHit hit, Projectile projectile, int ticks, int sequence)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            RPC(nameof(Apply), [hit.TargetIndex, hit.TargetGeneration, hit.AnchorIndex, hit.AnchorGeneration,
                hit.WasBurning, projectile.type, projectile.identity, ticks, sequence], KLNetModule.NetSendType.ClientToServer);
        else Apply(hit.TargetIndex, hit.TargetGeneration, hit.AnchorIndex, hit.AnchorGeneration,
            hit.WasBurning, projectile.type, projectile.identity, ticks, sequence);
    }

    public void Apply(int target, int generation, int anchor, int anchorGeneration, bool wasBurning,
        int projectileType, int identity, int ticks, int sequence)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead) return;
        if (projectileType == ModContent.ProjectileType<WaterBall>()) ticks = WaterExposureNPC.ExposureTicks;
        else if (projectileType == ModContent.ProjectileType<WaterLaser>()) ticks = Math.Clamp(ticks, 1, 60);
        else return;
        // 水球在 OnKill 的爆炸中仍可提交；不依赖已被销毁的弹幕继续 active。
        WaterExposureNPC.ApplyHit(new(target, generation, anchor, anchorGeneration, wasBurning),
            Player.whoAmI, ticks, projectileType, identity, sequence);
    }
}

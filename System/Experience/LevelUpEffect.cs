using System;
using System.Collections.Generic;
using KL.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System.Experience;

/// <summary>
/// 角色升级特效：由升级者本端生成并随弹幕同步，各端跟随主人脚底绘制星环与星芒，按时间轴放出 <see cref="LevelUpDust"/>。
/// localAI[0] 为本端已播放的 tick；晚收到同步的客户端从收到时开始播放。
/// </summary>
public sealed class LevelUpEffect : ModProjectile
{
    private static readonly Vector3 CircleLight = new(.55f, .45f, .62f), FlareLight = new(1f, .82f, .5f);
    private readonly List<LevelUpParticle> emitted = new();
    private Random random;
    private Action<LayerDrawRequestSystem.DrawRequestContext> bloomDraw;

    public override string Texture => LevelUpDust.UiAssets + "GlowDot";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 160;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.aiStyle = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = LevelUpVisuals.BodyTicks;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;

    /// <summary>升级者本端调用；弹幕随原版同步让其他玩家也看到。</summary>
    public static void Spawn(Player player)
    {
        if (Main.netMode == NetmodeID.Server || player.whoAmI != Main.myPlayer) return;
        Projectile.NewProjectile(player.GetSource_Misc("ElainaLevelUp"), player.Bottom, Vector2.Zero,
            ModContent.ProjectileType<LevelUpEffect>(), 0, 0, player.whoAmI);
    }

    private Vector2 Feet(Player owner) => owner.Bottom + new Vector2(0, owner.gfxOffY);

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        Projectile.Center = Feet(owner);
        int age = (int)Projectile.localAI[0]++;
        if (age >= LevelUpVisuals.BodyTicks) { Projectile.Kill(); return; }
        if (Main.dedServ) return;

        random ??= new Random(Projectile.identity * 7919 + Projectile.owner * 131);
        emitted.Clear();
        LevelUpVisuals.Emit(random, age, emitted);
        foreach (LevelUpParticle particle in emitted) LevelUpDust.Spawn(Projectile.Center, particle);

        Lighting.AddLight(Projectile.Center + new Vector2(0, -8), CircleLight * LevelUpVisuals.CircleStrength(age));
        float flare = LevelUpVisuals.FlareStrength(age);
        if (flare > 0) Lighting.AddLight(Projectile.Center - new Vector2(0, LevelUpVisuals.HeadHeight), FlareLight * flare);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (Main.dedServ) return;
        // 与玩偶星屑同法：预乘 AlphaBlend + ReColor HDR，排在粒子之前。
        bloomDraw ??= context =>
        {
            if (!Projectile.active || Projectile.type != Type) return;
            DrawHelper.EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
            DrawHelper.ReColorEffect(new Vector4(LevelUpDust.Hdr, LevelUpDust.Hdr, LevelUpDust.Hdr, 1));
            LevelUpVisuals.DrawBody(Main.spriteBatch, LevelUpDust.Textures(), Projectile.Center - Main.screenPosition,
                Projectile.localAI[0] - 1);
            DrawHelper.EndBeginDraw();
        };
        LayerDrawRequestSystem.RequestBloom("Elaina:LevelUp:Body", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, bloomDraw, order: LayerDrawRequestSystem.ProjectileOrder);
    }

    public override bool PreDraw(ref Color lightColor) => false;
}

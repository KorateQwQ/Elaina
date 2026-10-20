using Terraria.GameContent;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>Visual carrier only. Layers and damage reduction live on the protected player.</summary>
public sealed class FrostVeilProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 100;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        Player target = Main.player[Projectile.owner];
        if (!target.active || target.dead || Projectile.owner == Main.myPlayer
            && target.GetModPlayer<FrostVeilPlayer>().Veil.Shields == 0)
        {
            Projectile.Kill();
            return;
        }
        Projectile.Center = target.MountedCenter;
        Projectile.timeLeft = 2;
        Projectile.localAI[0]++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Main.netMode == NetmodeID.Server) return false;
        Player target = Main.player[Projectile.owner];
        FrostVeilDrawing.Draw(Main.spriteBatch, TextureAssets.MagicPixel.Value,
            Projectile.Center - Main.screenPosition, target.GetModPlayer<FrostVeilPlayer>().Veil.Shields,
            Projectile.localAI[0] / 60f, target.gravDir);
        return false;
    }
}

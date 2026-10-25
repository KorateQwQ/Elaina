using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System.Experience;

public sealed class ExperienceOrb : ModProjectile
{
    private Player recipient;
    private bool settled;
    private ExperienceOrbTrail trail;
    public override string Texture => "Terraria/Images/Projectile_0";

    internal void BindRecipient(Player player) => recipient = player;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.aiStyle = -1;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = ExperienceRules.Lifetime;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;

    public override void AI()
    {
        int targetIndex = (int)Projectile.ai[0];
        bool authority = Main.netMode != NetmodeID.MultiplayerClient;
        if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
        {
            if (authority) Projectile.Kill();
            return;
        }

        Player target = Main.player[targetIndex];
        if (!target.active || target.dead || target.ghost || !target.GetModPlayer<ElainaModplayer>().Elaina
            || (authority && !ReferenceEquals(recipient, target)))
        {
            // 死亡、离线或槽位换人只清理；不能在 OnKill 中统一发放经验。
            if (authority) Projectile.Kill();
            return;
        }

        float age = ++Projectile.ai[2];
        Projectile.velocity = ExperienceRules.NextVelocity(Projectile.Center, Projectile.velocity, target.Center, age);
        if (!Main.dedServ)
        {
            (trail ??= new ExperienceOrbTrail()).Record(Projectile.Center);
            Lighting.AddLight(Projectile.Center, .18f, .10f, .28f);
        }

        if (authority && !settled && age >= ExperienceRules.FloatTicks
            && Vector2.DistanceSquared(Projectile.Center, target.Center) <= ExperienceRules.PickupRadius * ExperienceRules.PickupRadius)
        {
            settled = true;
            ExperienceNetwork.Award(target, (int)Projectile.ai[1]);
            Projectile.Kill();
            return;
        }

        if (authority && (int)age % 30 == 0)
            Projectile.netUpdate = true;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        ExperienceOrbDrawing.Draw(Main.spriteBatch, TextureAssets.MagicPixel.Value,
            Projectile.Center, Projectile.ai[2], trail, Main.screenPosition);
        return false;
    }
}

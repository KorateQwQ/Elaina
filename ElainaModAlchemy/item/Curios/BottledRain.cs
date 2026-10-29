using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Rain;

namespace 伊蕾娜.ElainaModAlchemy.item.Curios;

/// <summary>投出后展开一分钟的局部雨云，浇灌植物。</summary>
public sealed class BottledRain : AlchemyItem
{
    public override string EntryId => "rain";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 24;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<BottledRainCloud>();
        Item.shootSpeed = 9f;
        Item.UseSound = SoundID.Item1;
    }

    public override void HoldItem(Player player) => RainAimPreviewSystem.TrackAim(player);

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Vector2 origin = RainThrowTrajectory.Origin(player);
        velocity = RainThrowTrajectory.Aim(origin, Main.MouseWorld, player.gravDir, out int flight);
        Projectile.NewProjectile(source, origin, velocity, type, 0, 0, player.whoAmI, 0, 0, flight);
        return false;
    }
}

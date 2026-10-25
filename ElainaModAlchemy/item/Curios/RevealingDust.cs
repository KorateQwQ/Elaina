using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Revealing;

namespace 伊蕾娜.ElainaModAlchemy.item.Curios;

/// <summary>出手时立即散开，在出手位置留下固定的显迹区域。</summary>
public sealed class RevealingDust : AlchemyItem
{
    public override string EntryId => "trace";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 24;
        Item.noUseGraphic = Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<RevealingDustArea>();
        Item.shootSpeed = 1f;
        Item.UseSound = SoundID.Item1;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        Vector2 direction = velocity.SafeNormalize(new Vector2(player.direction, 0));
        Projectile.NewProjectile(source, player.MountedCenter + direction * 24f, Vector2.Zero, type, 0, 0, player.whoAmI);
        return false;
    }
}

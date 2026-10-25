using Terraria;
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
}

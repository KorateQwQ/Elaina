using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Gameplay;

namespace 伊蕾娜.ElainaModAlchemy.item.Materials;

/// <summary>One bottle releases one full tile of Shimmer, or is consumed as an alchemy ingredient.</summary>
[LegacyName("ShimmerDroplet")]
public sealed class BottledShimmer : AlchemyItem
{
    private bool _consumeOnSuccess;
    public override string EntryId => "shimmer";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 20;
        Item.UseSound = SoundID.Item19;
        _consumeOnSuccess = false;
    }

    public override bool CanUseItem(Player player) =>
        AlchemyShimmerTransfer.CanRelease(player, Player.tileTargetX, Player.tileTargetY);

    public override void UseAnimation(Player player) => _consumeOnSuccess = false;

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer || !player.ItemTimeIsZero || _consumeOnSuccess || Item.stack <= 0)
            return false;
        if (!CanUseItem(player) || !WorldGen.PlaceLiquid(Player.tileTargetX, Player.tileTargetY,
                (byte)LiquidID.Shimmer, byte.MaxValue))
            return false;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            Liquid.AddWater(Player.tileTargetX, Player.tileTargetY);
        _consumeOnSuccess = true;
        return true;
    }

    public override bool ConsumeItem(Player player)
    {
        bool consume = player.whoAmI == Main.myPlayer && _consumeOnSuccess;
        if (consume) _consumeOnSuccess = false;
        return consume;
    }
}


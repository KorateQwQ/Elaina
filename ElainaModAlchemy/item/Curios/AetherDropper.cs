using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.item.Materials;

namespace 伊蕾娜.ElainaModAlchemy.item.Curios;

/// <summary>Single-use extractor: collects Shimmer like a bucket and yields one Bottled Shimmer.</summary>
public sealed class AetherDropper : AlchemyItem
{
    private bool _consumeOnSuccess;
    public override string EntryId => "dropper";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 20;
        Item.UseSound = SoundID.Item19;
        _consumeOnSuccess = false;
    }

    public override bool CanUseItem(Player player) =>
        AlchemyShimmerTransfer.CanCollect(player, Player.tileTargetX, Player.tileTargetY);

    public override void UseAnimation(Player player) => _consumeOnSuccess = false;

    public override bool? UseItem(Player player)
    {
        // Vanilla liquid updates are sent by the owning client, just like buckets.
        if (player.whoAmI != Main.myPlayer || !player.ItemTimeIsZero || _consumeOnSuccess || Item.stack <= 0)
            return false;
        if (!AlchemyShimmerTransfer.TryCollect(player, Player.tileTargetX, Player.tileTargetY))
            return false;

        player.QuickSpawnItem(player.GetSource_ItemUse(Item), ModContent.ItemType<BottledShimmer>());
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

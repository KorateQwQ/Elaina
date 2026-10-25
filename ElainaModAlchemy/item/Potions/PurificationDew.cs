using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Purification;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

/// <summary>在目标格释放三格宽的净化露滴。</summary>
public sealed class PurificationDew : AlchemyItem
{
    private bool consume;
    public override string EntryId => "isolation";
    public override AlchemyResearchRequirement ResearchRequirement => new(
        AlchemyProgressionRules.RequiredLevel(EntryId), true, ModContent.ItemType<PurificationDewRecipe>());

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 30;
        Item.noMelee = true;
        Item.UseSound = SoundID.Item4;
    }

    public override bool CanUseItem(Player player) => PurificationDewSystem.CanRequest(player);
    public override void UseAnimation(Player player) => consume = false;
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer || !player.ItemTimeIsZero || consume) return false;
        consume = PurificationDewSystem.Request(player, Main.MouseWorld.ToTileCoordinates());
        return consume;
    }
    public override bool ConsumeItem(Player player)
    {
        bool result = consume && player.whoAmI == Main.myPlayer;
        consume = false;
        return result;
    }
}

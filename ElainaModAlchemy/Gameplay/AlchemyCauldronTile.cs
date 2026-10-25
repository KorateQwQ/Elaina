using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

public sealed class AlchemyCauldronTile : GlobalTile
{
    public override void MouseOver(int i, int j, int type)
    {
        if (Main.dedServ || type != TileID.CookingPots ||
            !Main.LocalPlayer.GetModPlayer<ElainaModplayer>().Elaina ||
            !AlchemyBrewingPlayer.TryGetCauldron(i, j, out _)) return;
        Main.LocalPlayer.noThrow = 2;
        Main.LocalPlayer.cursorItemIconEnabled = true;
        Main.LocalPlayer.cursorItemIconID = ItemID.Cauldron;
    }

    public override void RightClick(int i, int j, int type)
    {
        if (!Main.dedServ && type == TileID.CookingPots)
            Main.LocalPlayer.GetModPlayer<AlchemyBrewingPlayer>().Enter(i, j);
    }
}

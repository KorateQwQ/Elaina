using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills.Skills.Alchemy;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

public sealed class AlchemyCauldronTile : GlobalTile
{
    public override void MouseOver(int i, int j, int type)
    {
        if (Main.dedServ || type != TileID.CookingPots ||
            !AlchemySkill.IsLearned(Main.LocalPlayer) ||
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

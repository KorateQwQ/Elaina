using Terraria.DataStructures;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Gameplay;

namespace 伊蕾娜.System;

public sealed class ElainaDrawAlignment : ModPlayer
{
    // Vanilla's 56px equipment frames reserve four pixels below the feet.
    // Elaina2legs uses the last row for the soles, so remove that padding
    // from the entire ordinary pose, keeping head, hat and held items aligned.
    internal const int EquipmentBottomPadding = 4;

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        var player = drawInfo.drawPlayer;
        if (drawInfo.headOnlyRender || player.dead || player.gravDir != 1 ||
            drawInfo.isSitting || player.sleeping.isSleeping || player.mount.Active ||
            !player.GetModPlayer<ElainaModplayer>().Elaina ||
            player.GetModPlayer<ElainaModplayer>().BecomeAnimal ||
            player.GetModPlayer<ElainaModplayer>().isACritter ||
            player.GetModPlayer<AlchemyBrewingPlayer>().Active) return;

        drawInfo.Position.Y -= EquipmentBottomPadding;
        drawInfo.ItemLocation.Y -= EquipmentBottomPadding;
    }
}

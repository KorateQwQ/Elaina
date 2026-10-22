using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Bucket-style Shimmer collection and safe bottle placement.</summary>
internal static class AlchemyShimmerTransfer
{
    private static bool CanTarget(Player player, int x, int y)
    {
        if (player == null || !player.active || player.dead || player.noBuilding ||
            !WorldGen.InWorld(x, y, 1) ||
            !player.IsInTileInteractionRange(x, y, TileReachCheckSettings.Simple))
            return false;

        Tile tile = Main.tile[x, y];
        return !tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType];
    }

    internal static bool CanCollect(Player player, int x, int y)
    {
        if (!CanTarget(player, x, y) || Main.tile[x, y].LiquidType != LiquidID.Shimmer ||
            Main.tile[x, y].LiquidAmount == 0)
            return false;

        // Match Player.ItemCheck_UseBuckets: the target need not be full. Count matching
        // liquid in its 3x3 neighbourhood and require more than 100 liquid units.
        int total = 0;
        for (int i = x - 1; i <= x + 1; i++)
            for (int j = y - 1; j <= y + 1; j++)
                if (Main.tile[i, j].LiquidType == LiquidID.Shimmer)
                    total += Main.tile[i, j].LiquidAmount;
        return total > 100;
    }

    internal static bool TryCollect(Player player, int x, int y)
    {
        // Recheck at use time; no liquid or item is spent if the pool has changed.
        if (!CanCollect(player, x, y)) return false;

        int remaining = byte.MaxValue;
        Drain(x, y, ref remaining);
        for (int i = x - 1; i <= x + 1 && remaining > 0; i++)
            for (int j = y - 1; j <= y + 1 && remaining > 0; j++)
                if (i != x || j != y)
                    Drain(i, j, ref remaining);
        return true;
    }

    private static void Drain(int x, int y, ref int remaining)
    {
        Tile tile = Main.tile[x, y];
        if (tile.LiquidType != LiquidID.Shimmer || tile.LiquidAmount == 0) return;

        int amount = global::System.Math.Min(remaining, tile.LiquidAmount);
        tile.LiquidAmount -= (byte)amount;
        remaining -= amount;
        WorldGen.SquareTileFrame(x, y, resetFrame: false);
        // Each changed cell follows the vanilla bucket's liquid update path.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.sendWater(x, y);
        else
            Liquid.AddWater(x, y);
    }

    // Do not overwrite another liquid or top up a partial tile: that would destroy some of the bottle's contents.
    internal static bool CanRelease(Player player, int x, int y) => CanTarget(player, x, y) &&
        Main.tile[x, y].LiquidAmount == 0;
}

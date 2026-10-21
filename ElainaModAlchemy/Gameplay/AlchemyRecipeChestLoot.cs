using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Adds exploration scrolls once, when a new world's treasure chests are generated.</summary>
public sealed class AlchemyRecipeChestLoot : ModSystem
{
    public const int FeatherlightChanceDenominator = 10;

    public override void PostWorldGen()
    {
        int scrollType = ModContent.ItemType<FeatherlightPotionRecipe>();
        foreach (Chest chest in Main.chest)
        {
            if (!IsUndergroundGoldChest(chest)) continue;
            int slot = Array.FindIndex(chest.item, item => item.IsAir);
            if (slot < 0 || !WorldGen.genRand.NextBool(FeatherlightChanceDenominator)) continue;

            // This is extra treasure; never replace the primary reward or other supplies.
            chest.item[slot].SetDefaults(scrollType);
            chest.item[slot].stack = 1;
        }
    }

    internal static bool IsUndergroundGoldChest(Chest chest)
    {
        if (chest == null || chest.x < 0 || chest.x >= Main.maxTilesX ||
            chest.y < Main.worldSurface || chest.y >= Main.UnderworldLayer ||
            chest.item[0].IsAir)
            return false;

        Tile tile = Main.tile[chest.x, chest.y];
        // Style 1 is the ordinary unlocked Gold Chest. Dungeon, biome and trap
        // variants have separate styles and are deliberately outside this pool.
        return tile.HasTile && tile.TileType == TileID.Containers && tile.TileFrameX == 36;
    }
}

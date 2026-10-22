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
    public const int PainkillerChanceDenominator = 10;

    public override void PostWorldGen()
    {
        foreach (Chest chest in Main.chest)
        {
            int scrollType;
            int chanceDenominator;
            if (IsUndergroundGoldChest(chest))
            {
                scrollType = ModContent.ItemType<FeatherlightPotionRecipe>();
                chanceDenominator = FeatherlightChanceDenominator;
            }
            else if (IsJungleChest(chest))
            {
                scrollType = ModContent.ItemType<PainkillerRecipe>();
                chanceDenominator = PainkillerChanceDenominator;
            }
            else continue;

            int slot = Array.FindIndex(chest.item, item => item.IsAir);
            if (slot < 0 || !WorldGen.genRand.NextBool(chanceDenominator)) continue;

            // This is extra treasure; never replace the primary reward or other supplies.
            chest.item[slot].SetDefaults(scrollType);
            chest.item[slot].stack = 1;
        }
    }

    internal static bool IsJungleChest(Chest chest)
    {
        if (chest == null || chest.x < 0 || chest.x >= Main.maxTilesX ||
            chest.y < 0 || chest.y >= Main.maxTilesY || chest.item[0].IsAir)
            return false;

        Tile tile = Main.tile[chest.x, chest.y];
        // WorldGen's jungle shrines and living mahogany trees use Ivy Chest
        // style 10. Style 24 is the locked Dungeon biome chest, not this pool.
        return tile.HasTile && tile.TileType == TileID.Containers && tile.TileFrameX == 10 * 36;
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

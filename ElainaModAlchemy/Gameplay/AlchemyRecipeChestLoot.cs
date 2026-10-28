using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

[Flags]
internal enum ExplorationRecipe : byte { None = 0, Featherlight = 1, Painkiller = 2 }

/// <summary>World generation records natural treasure; the first eligible opening rolls its contents.</summary>
public sealed partial class AlchemyRecipeChestLoot : ModSystem
{
    // Coordinates survive save/load; live identity also rejects replacement/reused chest slots.
    private sealed record Candidate(Chest Chest, ExplorationRecipe Remaining);
    private static readonly Dictionary<Point, Candidate> candidates = new();

    public override void ClearWorld() => candidates.Clear();

    public override void PostWorldGen()
    {
        candidates.Clear();
        foreach (Chest chest in Main.chest)
        {
            if (!IsTreasureChest(chest)) continue;
            ExplorationRecipe recipes = ExplorationRecipe.None;
            if (IsUndergroundGoldChest(chest)) recipes |= ExplorationRecipe.Featherlight;
            if (IsIvyChest(chest)) recipes |= ExplorationRecipe.Painkiller;
            if (recipes != ExplorationRecipe.None) candidates[new(chest.x, chest.y)] = new(chest, recipes);
        }
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag["NaturalRecipeChestsV1"] = candidates.Where(p => IsSameChest(p.Key, p.Value.Chest))
            .Select(p => new TagCompound { ["X"] = p.Key.X, ["Y"] = p.Key.Y,
                ["Recipes"] = (byte)p.Value.Remaining }).ToList();
    }

    public override void LoadWorldData(TagCompound tag)
    {
        candidates.Clear();
        // No migration scan: only worlds generated with this registry have eligible chests.
        foreach (TagCompound entry in tag.GetList<TagCompound>("NaturalRecipeChestsV1"))
        {
            int x = entry.GetInt("X"), y = entry.GetInt("Y");
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) continue;
            int index = Chest.FindChest(x, y);
            var recipes = (ExplorationRecipe)(entry.GetByte("Recipes") & 3);
            if (index >= 0 && recipes != ExplorationRecipe.None)
                candidates[new(x, y)] = new(Main.chest[index], recipes);
        }
    }

    private static bool IsSameChest(Point point, Chest expected)
    {
        int index = Chest.FindChest(point.X, point.Y);
        return index >= 0 && ReferenceEquals(Main.chest[index], expected);
    }

    internal static int ScrollType(ExplorationRecipe recipe) => recipe switch
    {
        ExplorationRecipe.Featherlight => ModContent.ItemType<FeatherlightPotionRecipe>(),
        ExplorationRecipe.Painkiller => ModContent.ItemType<PainkillerRecipe>(),
        _ => 0
    };

    // Only before a successful opening publishes contents. Never poll an already-open chest.
    internal static void PrepareOpening(Player player, int index)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || player is not { active: true, dead: false }
            || !player.GetModPlayer<ElainaModplayer>().Elaina || index < 0 || index >= Main.chest.Length
            || Main.chest[index] is not { } chest) return;
        Point point = new(chest.x, chest.y);
        if (!candidates.TryGetValue(point, out var candidate)) return;
        if (!ReferenceEquals(chest, candidate.Chest)) { candidates.Remove(point); return; }
        if (!WorldGen.InWorld(chest.x, chest.y, 0) || !Main.tile[chest.x, chest.y].HasTile
            || Chest.IsLocked(chest.x, chest.y)) return;
        var discovery = player.GetModPlayer<AlchemyRecipeDiscoveryPlayer>();
        if (Main.netMode == NetmodeID.Server && !discovery.ServerReady) return;
        discovery.ObservePossessions();
        ExplorationRecipe remaining = candidate.Remaining;
        foreach (var recipe in new[] { ExplorationRecipe.Featherlight, ExplorationRecipe.Painkiller })
        {
            if ((remaining & recipe) == 0) continue;
            int type = ScrollType(recipe);
            if (chest.item.Any(item => item is { IsAir: false } && item.type == type))
            {
                discovery.RecordRoll(recipe, true);
                remaining &= ~recipe;
                continue;
            }
            int slot = Array.FindIndex(chest.item, item => item == null || item.IsAir);
            // Full chests keep their opportunity until a later opening with space.
            if (slot < 0) continue;
            bool success = Main.rand.Next(100) < discovery.ChancePercent(recipe);
            if (success) chest.item[slot] = new Item(type);
            discovery.RecordRoll(recipe, success);
            remaining &= ~recipe;
        }
        if (remaining == ExplorationRecipe.None) candidates.Remove(point);
        else candidates[point] = candidate with { Remaining = remaining };
        discovery.SendServerState();
    }

    private static bool IsTreasureChest(Chest chest)
    {
        if (chest == null || chest.x < 0 || chest.y < 0 || chest.x >= Main.maxTilesX || chest.y >= Main.maxTilesY
            || !chest.item.Any(item => item is { IsAir: false })) return false;
        Tile tile = Main.tile[chest.x, chest.y];
        return tile.HasTile && TileID.Sets.BasicChest[tile.TileType];
    }

    internal static bool IsUndergroundGoldChest(Chest chest) => IsTreasureChest(chest)
        && chest.y >= Main.worldSurface && chest.y < Main.UnderworldLayer
        && Main.tile[chest.x, chest.y].TileType == TileID.Containers
        && Main.tile[chest.x, chest.y].TileFrameX == 36;

    // Only naturally generated Ivy Chests are registered for painkiller discovery.
    internal static bool IsIvyChest(Chest chest) => IsTreasureChest(chest)
        && Main.tile[chest.x, chest.y].TileType == TileID.Containers
        && Main.tile[chest.x, chest.y].TileFrameX == 10 * 36;
}

using System;
using Terraria.ID;
using Terraria.ObjectData;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>药草放置走真实物件/消耗钩子；未声明的模组生长帧不作为成熟证据。</summary>
public static class DollHerbPlanting
{
    private sealed record Rule(Func<int, int, bool> Matches, Func<int, int, bool> Blooming);
    private static readonly Dictionary<int, Rule> rules = [];
    private static readonly int[] vanillaSeeds = [ItemID.DaybloomSeeds, ItemID.MoonglowSeeds, ItemID.BlinkrootSeeds,
        ItemID.DeathweedSeeds, ItemID.WaterleafSeeds, ItemID.FireblossomSeeds, ItemID.ShiverthornSeeds];

    /// <summary>在 PostSetupContent 注册。回调坐标是植物格；Matches 应包含全部生长阶段。
    /// 适用于单格、通过正常 KillTile 掉落并移除的药草。特殊右键收获或多格作物需要专用适配。</summary>
    public static void Register(int seedItemType, Func<int, int, bool> matches, Func<int, int, bool> blooming)
    {
        if (seedItemType <= 0 || seedItemType >= ItemLoader.ItemCount) throw new ArgumentOutOfRangeException(nameof(seedItemType));
        rules[seedItemType] = new(matches ?? throw new ArgumentNullException(nameof(matches)),
            blooming ?? throw new ArgumentNullException(nameof(blooming)));
    }

    internal static void Clear() => rules.Clear();

    internal static int PreferredSeed(int planter) => planter switch
    {
        ItemID.DayBloomPlanterBox => ItemID.DaybloomSeeds,
        ItemID.MoonglowPlanterBox => ItemID.MoonglowSeeds,
        ItemID.BlinkrootPlanterBox => ItemID.BlinkrootSeeds,
        ItemID.CorruptPlanterBox or ItemID.CrimsonPlanterBox => ItemID.DeathweedSeeds,
        ItemID.WaterleafPlanterBox => ItemID.WaterleafSeeds,
        ItemID.FireBlossomPlanterBox => ItemID.FireblossomSeeds,
        ItemID.ShiverthornPlanterBox => ItemID.ShiverthornSeeds,
        _ => 0,
    };

    internal static bool IsSeed(Item item)
    {
        if (item == null || item.IsAir || item.createTile < 0 || item.createTile >= TileLoader.TileCount) return false;
        if (!Main.tileAlch[item.createTile] && !rules.ContainsKey(item.type)) return false;
        TileObjectData data = TileObjectData.GetTileData(item.createTile, Math.Max(0, item.placeStyle));
        if (!SingleCell(data)) return false;
        for (int alternate = 1; ; alternate++)
        {
            var next = TileObjectData.GetTileData(item.createTile, Math.Max(0, item.placeStyle), alternate);
            if (next == null || ReferenceEquals(next, data)) return true;
            if (!SingleCell(next)) return false;
        }
    }

    private static bool SingleCell(TileObjectData data) => data != null && data.Width == 1 && data.Height == 1
        && data.Origin.X == 0 && data.Origin.Y == 0;

    internal static bool Matches(int seedType, int x, int y)
    {
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile || !ContentSamples.ItemsByType.TryGetValue(seedType, out Item seed)) return false;
        if (seed.createTile == TileID.ImmatureHerbs && seed.placeStyle is >= 0 and < 7)
            return tile.TileType is TileID.ImmatureHerbs or TileID.MatureHerbs or TileID.BloomingHerbs
                && tile.TileFrameX / 18 == seed.placeStyle;
        if (rules.TryGetValue(seedType, out Rule rule)) return rule.Matches(x, y);
        return tile.TileType == seed.createTile && TileObjectData.GetTileStyle(tile) == seed.placeStyle;
    }

    internal static int IdentifySeed(int x, int y)
    {
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile) return 0;
        if (tile.TileType is TileID.ImmatureHerbs or TileID.MatureHerbs or TileID.BloomingHerbs)
        {
            int style = tile.TileFrameX / 18;
            return style >= 0 && style < vanillaSeeds.Length ? vanillaSeeds[style] : 0;
        }
        foreach (var (type, rule) in rules)
            if (rule.Matches(x, y)) return type;
        return 0;
    }

    internal static bool CanHarvest(int seedType, int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 1) || !Matches(seedType, x, y)) return false;
        Tile tile = Main.tile[x, y];
        if (tile.IsActuated || !WorldGen.CanKillTile(x, y)) return false;
        if (!SingleCell(TileObjectData.GetTileData(tile))) return false;
        if (tile.TileType is TileID.MatureHerbs or TileID.BloomingHerbs)
            return WorldGen.IsHarvestableHerbWithSeed(tile.TileType, tile.TileFrameX / 18);
        return rules.TryGetValue(seedType, out Rule rule) && rule.Blooming(x, y);
    }

    internal static bool CanClearWeed(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 1)) return false;
        Tile tile = Main.tile[x, y];
        // 花盆自然长出的装饰草/花。不能用 tileCut 泛判：药草幼苗和模组作物也可能可割。
        return tile.HasTile && !tile.IsActuated && tile.TileType is TileID.Plants or TileID.Plants2
            && !Main.tileAlch[tile.TileType] && IdentifySeed(x, y) == 0 && WorldGen.CanKillTile(x, y);
    }

    internal static bool CanPlant(Player player, Point pot, Item seed)
    {
        if (!WorldGen.InWorld(pot.X, pot.Y - 1, 1) || !IsSeed(seed)) return false;
        Tile support = Main.tile[pot.X, pot.Y], plant = Main.tile[pot.X, pot.Y - 1];
        if (!support.HasTile || support.TileType == TileID.ClayPot || support.IsActuated || support.IsHalfBlock || support.Slope != SlopeType.Solid
            || plant.HasTile || plant.LiquidAmount > 0 || !TileLoader.CanPlace(pot.X, pot.Y - 1, seed.createTile)) return false;
        // 原版种植盆走 PlaceAlch 的专用规则，未列入药草的 TileObjectData.AnchorAlternateTiles。
        if (seed.createTile == TileID.ImmatureHerbs && seed.placeStyle is >= 0 and < 7
            && support.TileType == TileID.PlanterBox) return true;
        // onlyCheck=true 会改写玩家的放置预览缓存；后台查询不触碰该预览。
        return TileObject.CanPlace(pot.X, pot.Y - 1, seed.createTile, seed.placeStyle, player.direction, out _);
    }

    internal static Item FindSeed(Player player, Point pot, int seedType)
    {
        for (int i = 0; i < 50; i++)
        {
            Item seed = player.inventory[i];
            if ((seedType == 0 || seed.type == seedType) && CanPlant(player, pot, seed)) return seed;
        }
        return null;
    }

    internal static bool Plant(Player player, Point pot, Item seed)
    {
        if (player.whoAmI != Main.myPlayer || !CanPlant(player, pot, seed)) return false;
        bool vanilla = seed.createTile == TileID.ImmatureHerbs
            && Main.tile[pot.X, pot.Y].TileType == TileID.PlanterBox;
        return DollPlanterBuilding.PlaceOne(player, new(pot.X, pot.Y - 1), seed, useLegacyPlacement: vanilla);
    }
}

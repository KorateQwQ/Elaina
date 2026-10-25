using System;
using Terraria.ID;
using Terraria.ObjectData;
using Terraria.Enums;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>一组为上方生长空格与下方种植盆；只使用主背包中的种植盆。</summary>
internal static class DollPlanterBuilding
{
    internal const int Limit = 10000;

    private static readonly HashSet<int> herbSupports = [];
    private static bool[] indexedHerbs;
    private static uint supportIndexTick;

    internal static bool IsMaterial(Item item)
    {
        if (item == null || item.IsAir || item.createTile < 0 || item.createTile >= TileLoader.TileCount) return false;
        if (item.createTile == TileID.ClayPot) return false;
        if (item.createTile == TileID.PlanterBox) return true;
        // 判定顶部实心，不使用 Platforms 标签：原版种植盆也没有那个标签。
        if (!Main.tileSolidTop[item.createTile] || !FitsPlanterCell(item)) return false;
        RefreshHerbSupports();
        return herbSupports.Contains(item.createTile);
    }

    private static bool FitsPlanterCell(Item item)
    {
        TileObjectData data = TileObjectData.GetTileData(item.createTile, Math.Max(0, item.placeStyle));
        // 无物件数据、非重要帧的物块按单格绘制；特殊帧而无尺寸声明的不猜测尺寸。
        if (data == null) return !Main.tileFrameImportant[item.createTile];
        if (!SingleCell(data)) return false;
        for (int alternate = 1; ; alternate++)
        {
            var next = TileObjectData.GetTileData(item.createTile, Math.Max(0, item.placeStyle), alternate);
            if (next == null || ReferenceEquals(next, data)) return true;
            if (!SingleCell(next)) return false;
        }

        // 只接受可独立放置的单格种植盆，不接管需要实体支撑或背景墙的盆器。
        static bool SingleCell(TileObjectData data) => data.Width == 1 && data.Height == 1
            && data.Origin.X == 0 && data.Origin.Y == 0
            && data.AnchorBottom.tileCount == 0 && data.AnchorTop.tileCount == 0
            && data.AnchorLeft.tileCount == 0 && data.AnchorRight.tileCount == 0 && !data.AnchorWall;
    }

    private static void RefreshHerbSupports()
    {
        // 列表每帧查询库存，最多每秒重建一次；重新加载的 tileAlch 数组会立即使缓存失效。
        if (ReferenceEquals(indexedHerbs, Main.tileAlch) && Main.GameUpdateCount - supportIndexTick < 60) return;
        indexedHerbs = Main.tileAlch;
        supportIndexTick = Main.GameUpdateCount;
        herbSupports.Clear();
        HashSet<(int Tile, int Style)> styles = [];
        for (int tile = TileID.ImmatureHerbs; tile <= TileID.BloomingHerbs; tile++)
            for (int style = 0; style < 7; style++) styles.Add((tile, style));
        for (int tile = 0; tile < TileLoader.TileCount; tile++)
            if (Main.tileAlch[tile]) styles.Add((tile, 0));
        // 模组药草的种子可以声明额外样式；不依赖物品名字或模组名。
        foreach (Item sample in ContentSamples.ItemsByType.Values)
            if (sample.createTile >= 0 && sample.createTile < TileLoader.TileCount && Main.tileAlch[sample.createTile])
                styles.Add((sample.createTile, Math.Max(0, sample.placeStyle)));
        foreach (var (tile, style) in styles)
        {
            TileObjectData data = TileObjectData.GetTileData(tile, style);
            if (data == null) continue;
            AddSupports(data);
            for (int alternate = 1; ; alternate++)
            {
                var next = TileObjectData.GetTileData(tile, style, alternate);
                if (next == null || ReferenceEquals(next, data)) break;
                AddSupports(next);
            }
        }

        void AddSupports(TileObjectData data)
        {
            if (data.AnchorBottom.tileCount == 0) return;
            AnchorType anchor = data.AnchorBottom.type;
            if ((anchor & AnchorType.AlternateTile) != 0 && data.AnchorAlternateTiles is { } alternates)
                foreach (int type in alternates) herbSupports.Add(type);
            // 泛化的“任意平台都能支撑”不作为花盆证据，必须有明确的合法类型名单。
            if ((anchor & (AnchorType.SolidWithTop | AnchorType.Table)) != 0 && data.AnchorValidTiles is { } valid)
                foreach (int type in valid)
                    if (data.isValidTileAnchor(type)) herbSupports.Add(type);
        }
    }

    internal static List<(int Type, int Count)> Options(Player player)
    {
        Dictionary<int, int> counts = [];
        for (int i = 0; i < 50; i++)
        {
            Item item = player.inventory[i];
            if (IsMaterial(item)) counts[item.type] = counts.GetValueOrDefault(item.type) + item.stack;
        }
        List<(int Type, int Count)> result = [];
        foreach (var pair in counts) result.Add((pair.Key, pair.Value));
        result.Sort((a, b) => a.Count != b.Count ? b.Count.CompareTo(a.Count) : a.Type.CompareTo(b.Type));
        return result;
    }

    internal static int Count(Player player, int type)
    {
        if (type <= 0) return 0;
        int count = 0;
        for (int i = 0; i < 50; i++)
            if (player.inventory[i].type == type) count += player.inventory[i].stack;
        return count;
    }

    private static Item Find(Player player, int type)
    {
        for (int i = 0; i < 50; i++)
            if (!player.inventory[i].IsAir && player.inventory[i].type == type) return player.inventory[i];
        return null;
    }

    internal static Rectangle Area(Point a, Point b)
    {
        int left = Math.Clamp(Math.Min(a.X, b.X), 1, Main.maxTilesX - 2);
        int right = Math.Clamp(Math.Max(a.X, b.X), 1, Main.maxTilesX - 2);
        int top = Math.Clamp(Math.Min(a.Y, b.Y), 1, Main.maxTilesY - 3);
        int bottom = Math.Clamp(Math.Max(a.Y, b.Y), top, Main.maxTilesY - 2);
        // 不完整的一行向下补齐；世界边界只保留完整的两层。
        int rows = Math.Min((bottom - top + 2) / 2, (Main.maxTilesY - 1 - top) / 2);
        return new(left, top, right - left + 1, rows * 2);
    }

    internal static bool CanBuild(Point p)
    {
        if (!WorldGen.InWorld(p.X, p.Y - 1, 1) || !WorldGen.InWorld(p.X, p.Y, 1)) return false;
        for (int y = p.Y - 1; y <= p.Y; y++)
        {
            Tile tile = Main.tile[p.X, y];
            // 不覆盖任何物块或植物，也不在液体里预留生长空间。
            if (tile.LiquidAmount > 0 || tile.HasTile) return false;
        }
        return true;
    }

    internal enum Result { Placed, Blocked, MissingPlanter }

    internal static Result Place(Player player, Point p, int planter)
    {
        if (player.whoAmI != Main.myPlayer || !CanBuild(p)) return Result.Blocked;
        Item pot = Find(player, planter);
        if (!IsMaterial(pot)) return Result.MissingPlanter;
        return PlaceOne(player, p, pot) ? Result.Placed : Result.Blocked;
    }

    internal static bool PlaceOne(Player player, Point p, Item item, bool useLegacyPlacement = false)
    {
        if (Main.tile[p.X, p.Y].HasTile || !TileLoader.CanPlace(p.X, p.Y, item.createTile)) return false;
        int tileType = item.createTile, style = item.placeStyle;
        bool custom = !useLegacyPlacement && TileObjectData.CustomPlace(tileType, style);
        TileObject data = default;
        if (custom && !TileObject.CanPlace(p.X, p.Y, tileType, style, player.direction, out data)) return false;
        bool placed = custom ? TileObject.Place(data)
            : WorldGen.PlaceTile(p.X, p.Y, tileType, mute: true, forced: false, plr: player.whoAmI, style: style);
        // PlaceTile 的 bool 对某些旧式物块仅表示尝试成功，还需检查实际格子。
        if (!placed || !Main.tile[p.X, p.Y].HasTile || Main.tile[p.X, p.Y].TileType != tileType) return false;
        WorldGen.SquareTileFrame(p.X, p.Y);
        if (custom) TileObjectData.CallPostPlacementPlayerHook(p.X, p.Y, tileType, style, player.direction, data.alternate, data);
        TileLoader.PlaceInWorld(p.X, p.Y, item);
        if (!Main.tile[p.X, p.Y].HasTile || Main.tile[p.X, p.Y].TileType != tileType) return false;
        if (item.consumable && ItemLoader.ConsumeItem(item, player))
        {
            item.stack--;
            if (item.stack <= 0) item.TurnToAir();
        }
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (custom) NetMessage.SendObjectPlacement(-1, p.X, p.Y, tileType, style, data.alternate, data.random, player.direction);
            else NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, p.X, p.Y, tileType, style);
        }
        return true;
    }
}

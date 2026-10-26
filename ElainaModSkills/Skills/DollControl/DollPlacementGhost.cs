using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ObjectData;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 待放置位置的半透明虚影，外观与即将放下的物块一致：
/// 物件（家具等）直接用原版放置预览 <see cref="TileObject.DrawPreview"/>；平台、种植盆和普通实心块按原版
/// <c>WorldGen.TileFrame</c> 的接缝规则，以相邻的真实物块和待放置格挑选帧。其余物块返回 false，由调用方画物品图标。
/// </summary>
internal static class DollPlacementGhost
{
    /// <summary>相邻格在平台接缝规则中的分类。</summary>
    internal enum Side { Empty, Platform, Solid }

    private static readonly TileObjectPreviewData preview = new();

    /// <param name="pending">查询某格待放置的物块类型；没有待放置时返回 -1。</param>
    internal static bool Draw(SpriteBatch batch, DollCommandPlayer.PendingPlacement placement, Func<Point, int> pending)
    {
        int type = placement.TileType;
        if (type < 0 || type >= TextureAssets.Tile.Length) return false;
        if (placement.IsObject)
        {
            var data = TileObjectData.GetTileData(type, placement.Style, placement.Alternate);
            if (data == null || data.Width != placement.Area.Width || data.Height != placement.Area.Height) return false;
            Main.instance.LoadTiles(type);
            preview.Reset();
            preview.Active = true;
            preview.Type = (ushort)type;
            preview.Style = (short)placement.Style;
            preview.Alternate = placement.Alternate;
            preview.Random = placement.Random;
            preview.Size = new Point16(data.Width, data.Height);
            preview.ObjectStart = Point16.Zero;
            preview.Coordinates = new Point16(placement.Area.X, placement.Area.Y);
            for (int i = 0; i < data.Width; i++)
                for (int j = 0; j < data.Height; j++)
                    preview[i, j] = TileObjectPreviewData.ValidSpot;
            TileObject.DrawPreview(batch, preview, Main.screenPosition);
            return true;
        }
        Point cell = placement.Target;
        if (LegacyFrame(type, placement.Style, cell, p => Neighbor(p, pending)) is not { } frame) return false;
        Main.instance.LoadTiles(type);
        // 与原版放置预览相同：白色半透明，不受光照影响。
        batch.Draw(TextureAssets.Tile[type].Value, new Vector2(cell.X * 16, cell.Y * 16) - Main.screenPosition, frame,
            Color.White * .5f);
        return true;
    }

    /// <summary>相邻格的物块类型：待放置优先，其次是世界中激活的物块；空格为 -1。</summary>
    private static int Neighbor(Point p, Func<Point, int> pending)
    {
        int type = pending(p);
        if (type >= 0) return type;
        if (!WorldGen.InWorld(p.X, p.Y)) return -1;
        Tile tile = Main.tile[p.X, p.Y];
        return tile.HasTile && !tile.IsActuated ? tile.TileType : -1;
    }

    /// <summary>不依赖物件数据的单格物块帧；无法按原版规则确定时返回 null。</summary>
    internal static Rectangle? LegacyFrame(int type, int style, Point cell, Func<Point, int> neighbor)
    {
        if (type < 0 || type >= TileLoader.TileCount) return null;
        int left = neighbor(new(cell.X - 1, cell.Y)), right = neighbor(new(cell.X + 1, cell.Y));
        if (TileID.Sets.Platforms[type])
            return new(PlatformFrame(Classify(left), Classify(right)), style * 18, 16, 16);
        if (type == TileID.PlanterBox)
            return new(PlanterFrame(left == type, right == type), style * 18, 16, 16);
        if (!Main.tileSolid[type] || Main.tileSolidTop[type] || Main.tileFrameImportant[type]) return null;
        bool Same(int dx, int dy) => neighbor(new(cell.X + dx, cell.Y + dy)) == type;
        // 原版每格随机取三种变体之一；这里按坐标固定，拖动时不会闪烁。
        int variant = (cell.X * 7 + cell.Y * 13 & int.MaxValue) % 3;
        var (x, y) = BlockFrame(Same(0, -1), Same(0, 1), left == type, right == type,
            Same(-1, -1), Same(1, -1), Same(-1, 1), Same(1, 1), variant);
        return new(x, y, 16, 16);
    }

    private static Side Classify(int type) => type < 0 ? Side.Empty
        : TileID.Sets.Platforms[type] ? Side.Platform : Main.tileSolid[type] ? Side.Solid : Side.Empty;

    /// <summary>原版平台（未斜坡、非半砖）的横向接缝帧。</summary>
    internal static int PlatformFrame(Side left, Side right) => (left, right) switch
    {
        (Side.Platform, Side.Platform) => 0,
        (Side.Platform, Side.Empty) => 18,
        (Side.Empty, Side.Platform) => 36,
        (Side.Solid, Side.Platform) => 54,
        (Side.Platform, Side.Solid) => 72,
        (Side.Solid, Side.Empty) => 108,
        (Side.Empty, Side.Solid) => 126,
        _ => 90,
    };

    /// <summary>原版种植盆：两侧同为种植盆时连成一排。</summary>
    internal static int PlanterFrame(bool left, bool right) => (left, right) switch
    {
        (true, true) => 18,
        (true, false) => 36,
        (false, true) => 0,
        _ => 54,
    };

    /// <summary>原版普通方块按上下左右同类相邻选帧，四面相邻时再看斜角选内角帧（不含与其他物块的融合边与斜坡）。</summary>
    internal static (int X, int Y) BlockFrame(bool up, bool down, bool left, bool right,
        bool upLeft, bool upRight, bool downLeft, bool downRight, int variant)
    {
        int v = variant * 18;
        return (up, down, left, right) switch
        {
            (true, true, true, true) when !upLeft && !upRight => (108 + v, 18),
            (true, true, true, true) when !downLeft && !downRight => (108 + v, 36),
            (true, true, true, true) when !upLeft && !downLeft => (180, v),
            (true, true, true, true) when !upRight && !downRight => (198, v),
            (true, true, true, true) => (18 + v, 18),
            (false, true, true, true) => (18 + v, 0),
            (true, false, true, true) => (18 + v, 36),
            (true, true, false, true) => (0, v),
            (true, true, true, false) => (72, v),
            (false, true, false, true) => (variant * 36, 54),
            (false, true, true, false) => (18 + variant * 36, 54),
            (true, false, false, true) => (variant * 36, 72),
            (true, false, true, false) => (18 + variant * 36, 72),
            (true, true, false, false) => (90, v),
            (false, false, true, true) => (108 + v, 72),
            (false, true, false, false) => (108 + v, 0),
            (true, false, false, false) => (108 + v, 54),
            (false, false, false, true) => (162, v),
            (false, false, true, false) => (216, v),
            _ => (162 + v, 54),
        };
    }
}

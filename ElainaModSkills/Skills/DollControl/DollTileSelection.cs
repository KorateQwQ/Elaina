using System;
using Terraria.ObjectData;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>选区、预览与家具任务共用的实际占用格；按帧定位，避免把紧邻的同类家具合成一件。</summary>
internal static class DollTileSelection
{
    internal static Rectangle Bounds(Point point, bool walls = false)
    {
        Rectangle single = new(point.X, point.Y, 1, 1);
        if (walls || !WorldGen.InWorld(point.X, point.Y, 1)) return single;
        Tile tile = Main.tile[point.X, point.Y];
        if (!tile.HasTile || !Main.tileFrameImportant[tile.TileType]) return single;
        TileObjectData data = TileObjectData.GetTileData(tile);
        if (data == null || data.Width <= 0 || data.Height <= 0 || data.CoordinateFullWidth <= 0
            || data.CoordinateFullHeight <= 0 || tile.TileFrameX < 0 || tile.TileFrameY < 0) return single;
        int partX = tile.TileFrameX % data.CoordinateFullWidth;
        int partY = tile.TileFrameY % data.CoordinateFullHeight;
        int column = partX / (data.CoordinateWidth + data.CoordinatePadding), row = 0, rowOffset = 0;
        while (row + 1 < data.Height && partY >= rowOffset + data.CoordinateHeights[row] + data.CoordinatePadding)
        { rowOffset += data.CoordinateHeights[row] + data.CoordinatePadding; row++; }
        Rectangle bounds = new(point.X - column, point.Y - row, data.Width, data.Height);
        if (!WorldGen.InWorld(bounds.Left, bounds.Top, 1) || !WorldGen.InWorld(bounds.Right - 1, bounds.Bottom - 1, 1)) return single;
        // 损坏/被替换的物件不向邻居扩选；不同样式、朝向和高度用实际帧核对。
        int baseX = tile.TileFrameX - partX, baseY = tile.TileFrameY - partY;
        int frameY = baseY;
        for (int y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (int x = bounds.Left; x < bounds.Right; x++)
            {
                Tile part = Main.tile[x, y];
                if (!part.HasTile || part.TileType != tile.TileType
                    || part.TileFrameX != baseX + (x - bounds.Left) * (data.CoordinateWidth + data.CoordinatePadding)
                    || part.TileFrameY != frameY) return single;
            }
            frameY += data.CoordinateHeights[y - bounds.Top] + data.CoordinatePadding;
        }
        return bounds;
    }

    internal static HashSet<Point> RectangleCells(Point a, Point b, bool walls)
    {
        HashSet<Point> cells = [];
        int left = Math.Clamp(Math.Min(a.X, b.X), 1, Main.maxTilesX - 2);
        int right = Math.Clamp(Math.Max(a.X, b.X), 1, Main.maxTilesX - 2);
        int top = Math.Clamp(Math.Min(a.Y, b.Y), 1, Main.maxTilesY - 2);
        int bottom = Math.Clamp(Math.Max(a.Y, b.Y), 1, Main.maxTilesY - 2);
        for (int x = left; x <= right; x++) for (int y = top; y <= bottom; y++)
        {
            Point p = new(x, y);
            if (cells.Contains(p) || !DollCommandPlayer.IsMarkable(x, y, walls)) continue;
            AddObject(cells, p, walls);
        }
        return cells;
    }

    internal static void AddObject(HashSet<Point> cells, Point point, bool walls)
    {
        Rectangle bounds = Bounds(point, walls);
        for (int x = bounds.Left; x < bounds.Right; x++) for (int y = bounds.Top; y < bounds.Bottom; y++)
            cells.Add(new(x, y));
    }
}

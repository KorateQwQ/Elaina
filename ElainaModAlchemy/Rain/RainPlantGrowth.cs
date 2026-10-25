using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

internal static class RainPlantGrowth
{
    // 缓存安装版本的原版单格生长入口。只调度已存在的植物，不更新土壤、矿石或空格。
    // 复用原版空间/深度/液体约束与 TileLoader.RandomUpdate，避免复制整套植物生长规则。
    private static readonly Action<int, int, bool, int> SurfaceUpdate = Bind("UpdateWorld_OvergroundTile");
    private static readonly Action<int, int, bool, int> UndergroundUpdate = Bind("UpdateWorld_UndergroundTile");

    private static Action<int, int, bool, int> Bind(string name)
    {
        MethodInfo method = typeof(WorldGen).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(WorldGen).FullName, name);
        return (Action<int, int, bool, int>)method.CreateDelegate(typeof(Action<int, int, bool, int>));
    }

    internal static bool IsPlant(ushort type) => Main.tileAlch[type] || TileID.Sets.CommonSapling[type]
        || TileID.Sets.IsVine[type] || TileID.Sets.SwaysInWindBasic[type]
        || type is TileID.Pumpkins or TileID.Cactus or TileID.Bamboo or TileID.Cattail or TileID.Seaweed;

    internal static bool BlocksRain(Tile tile) => tile.HasTile && !tile.IsActuated
        && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType] && !IsPlant(tile.TileType);

    public static void MeasureRain(Vector2 center, float[] lengths)
    {
        for (int column = 0; column < lengths.Length; column++)
        {
            float x = center.X - RainCloudDrawing.Width * .5f + (column + .5f) * 16;
            int tx = (int)MathF.Floor(x / 16);
            int startY = (int)MathF.Floor((center.Y + 16) / 16);
            lengths[column] = RainCloudDrawing.MaxFall;
            for (int y = startY; y <= startY + RainCloudDrawing.MaxFall / 16; y++)
            {
                if (!WorldGen.InWorld(tx, y, 10)) { lengths[column] = Math.Max(0, y * 16 - center.Y - 16); break; }
                Tile tile = Main.tile[tx, y];
                if (BlocksRain(tile) || tile.LiquidAmount > 128)
                { lengths[column] = Math.Clamp(y * 16 - center.Y - 16, 0, RainCloudDrawing.MaxFall); break; }
            }
        }
    }

    public static void Water(Vector2 center)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        VisitCoveredPlants(center, null);
    }

    // 仅收集，不调用生长或写图块。客户端图标和服务器浇灌共用同一套覆盖/遮挡判定。
    internal static void CollectCoveredPlants(Vector2 center, List<Point> plants)
    {
        plants.Clear();
        VisitCoveredPlants(center, plants);
    }

    private static void VisitCoveredPlants(Vector2 center, List<Point> plants)
    {
        int top = (int)MathF.Floor((center.Y + 16) / 16);
        for (int column = 0; column < RainCloudDrawing.Columns; column++)
        {
            int x = (int)MathF.Floor((center.X - RainCloudDrawing.Width * .5f + (column + .5f) * 16) / 16);
            for (int y = top; y <= top + RainCloudDrawing.MaxFall / 16; y++)
            {
                if (!WorldGen.InWorld(x, y, 10)) break;
                Tile tile = Main.tile[x, y];
                if (BlocksRain(tile)) break;
                if (CanReceiveGrowth(x, y))
                {
                    if (plants == null) GrowPlant(x, y);
                    else plants.Add(new Point(x, y));
                }
                if (tile.LiquidAmount > 128) break;
            }
        }
    }

    internal static bool CanReceiveGrowth(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 10)) return false;
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile || tile.IsActuated || !IsPlant(tile.TileType)) return false;
        // 植物获得额外随机生长机会。树苗与南瓜按对象根部采样，避免多格重复加速。
        if (TileID.Sets.CommonSapling[tile.TileType] && tile.TileFrameY != 18) return false;
        if (tile.TileType == TileID.Pumpkins && (tile.TileFrameX % 36 != 0 || tile.TileFrameY != 0)) return false;
        return true;
    }

    internal static void GrowPlant(int x, int y)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !CanReceiveGrowth(x, y)) return;
        Tile tile = Main.tile[x, y];
        if (y > Main.worldSurface) UndergroundUpdate(x, y, false, 3);
        else SurfaceUpdate(x, y, false, 3);

        // ItemID.Waterleaf 对应 style 4；使用真实开花物块使收获也给种子，不只替换画面。
        if (tile.HasTile && tile.TileType == TileID.MatureHerbs && tile.TileFrameX == 4 * 18)
        {
            tile.TileType = TileID.BloomingHerbs;
            WorldGen.SquareTileFrame(x, y);
            if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, x, y);
        }
    }

    internal static Point IndicatorRoot(Point plant)
    {
        ushort type = Main.tile[plant.X, plant.Y].TileType;
        if (!TileID.Sets.IsVine[type] && type is not (TileID.Cactus or TileID.Bamboo or TileID.Cattail or TileID.Seaweed))
            return plant;
        int y = plant.Y;
        // 连续茎段只显示一个标记，避免竹子、藤蔓或仙人掌每格各出现一枚箭头。
        while (y > 10)
        {
            Tile above = Main.tile[plant.X, y - 1];
            if (!above.HasTile || above.IsActuated || above.TileType != type) break;
            y--;
        }
        return new Point(plant.X, y);
    }

    internal static Vector2 IndicatorAnchor(Point root)
    {
        ushort type = Main.tile[root.X, root.Y].TileType;
        if (TileID.Sets.CommonSapling[type]) return new Vector2(root.X * 16 + 8, (root.Y - 1) * 16);
        if (type == TileID.Pumpkins) return new Vector2(root.X * 16 + 16, root.Y * 16);
        return new Vector2(root.X * 16 + 8, root.Y * 16);
    }
}

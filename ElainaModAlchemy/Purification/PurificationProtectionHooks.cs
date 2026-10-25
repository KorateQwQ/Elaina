using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

public sealed partial class PurificationProtectionSystem
{
    private delegate bool ConvertOriginal(int x, int y, int conversion);
    [ThreadStatic] private static bool repairing;

    public override void Load()
    {
        // 常规感染、溶液/粉末以及困难模式 V 形地形都经过这个按目标格调用的入口。
        MonoModHooks.Add(typeof(TileLoader).GetMethod(nameof(TileLoader.Convert)),
            (Func<ConvertOriginal, int, int, int, bool>)AllowConversion);
        On_WorldGen.SpreadGrass += SpreadGrass;
        On_WorldGen.GrowMoreVines += GrowMoreVines;
        On_WorldGen.TileFrame += TileFrame;
    }

    public override void Unload()
    {
        On_WorldGen.SpreadGrass -= SpreadGrass;
        On_WorldGen.GrowMoreVines -= GrowMoreVines;
        On_WorldGen.TileFrame -= TileFrame;
        ClearWorld();
    }

    private static bool AllowConversion(ConvertOriginal orig, int x, int y, int conversion) =>
        !((conversion is BiomeConversionID.Corruption or BiomeConversionID.Crimson or BiomeConversionID.Hallow)
            && IsProtected(x, y)) && orig(x, y, conversion);

    private static bool Infected(int type) => type < TileID.Count &&
        (TileID.Sets.Corrupt[type] || TileID.Sets.Crimson[type] || TileID.Sets.Hallow[type]
            || type is TileID.CorruptThorns or TileID.CrimsonThorns or TileID.CorruptPlants
                or TileID.CrimsonPlants or TileID.HallowedPlants or TileID.HallowedPlants2);

    private static void SpreadGrass(On_WorldGen.orig_SpreadGrass orig, int i, int j, int dirt, int grass,
        bool repeat, TileColorCache color)
    {
        if (Infected(grass) && IsProtected(i, j)) return;
        orig(i, j, dirt, grass, repeat, color);
    }

    private static bool GrowMoreVines(On_WorldGen.orig_GrowMoreVines orig, int x, int y)
    {
        if (WorldGen.InWorld(x, y, 1) && Infected(Main.tile[x, y].TileType) && IsProtected(x, y + 1)) return false;
        return orig(x, y);
    }

    private static void TileFrame(On_WorldGen.orig_TileFrame orig, int i, int j, bool resetFrame, bool noBreak)
    {
        // 原版 GrowSpike 在新荆棘激活后、发送地形包前调用 TileFrame；在此同步清除，
        // 同样覆盖玩家新放下的感染块、落沙及联机 TileSquare 的 RangeFrame。
        // 不扫描周边；正常物块先按类型快速返回，只有感染块才查询区间。
        CleanTile(i, j);
        orig(i, j, resetFrame, noBreak);
    }

    internal static void CleanTile(int x, int y)
    {
        if (repairing || Main.netMode == NetmodeID.MultiplayerClient || !WorldGen.InWorld(x, y, 1)) return;
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile || !Infected(tile.TileType) || !IsProtected(x, y)) return;
        repairing = true;
        try
        {
            if (tile.TileType is TileID.CorruptThorns or TileID.CrimsonThorns or TileID.CorruptVines
                or TileID.CrimsonVines or TileID.HallowedVines or TileID.CorruptPlants
                or TileID.CrimsonPlants or TileID.HallowedPlants or TileID.HallowedPlants2)
            {
                // 只清掉本格，避免 KillTile 顺带清除区域外整条藤蔓；保留墙、液体、电线。
                tile.ClearTile();
                if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, x, y);
            }
            else WorldGen.Convert(x, y, BiomeConversionID.Purity, 0, tiles: true, walls: false);
        }
        finally { repairing = false; }
    }
}

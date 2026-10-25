using System;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 玩偶在主人客户端拆物块与墙；按开关选择收集掉落或沿用原版原地掉落。
/// 收集模式下，客户端的 KillTile 本不掉落，因此先在捕获期间单独执行原版掉落计算，再无掉落地拆除并同步；
/// 墙没有“无掉落”的网络操作，收集时改为本地拆除后发送物块方块同步。关闭收集时使用原版拆除消息。
/// 判定镐力、草地转换与掉落沿用原版私有实现，避免各模组物块的规则走样。
/// </summary>
internal static class DollTileBreaker
{
    private static Func<Player, int, int, int, int, Tile, int> _pickaxeDamage;
    private static Func<Player, HitTile, int, int, int, int, int, Tile, bool> _transformsOnKill;
    private static Action<int, int, Tile, bool, bool> _dropTileItems;
    private static readonly HitTile PreviewHits = new();

    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>原版镐力判定可能移动裂纹记录、阻止鼠标输入；预检使用独立记录并恢复输入状态。</summary>
    internal static bool CanDamageTile(Player player, int x, int y, int pickPower)
    {
        HitTile originalHits = player.hitTile;
        bool originalBlockMouse = Main.blockMouse;
        int index = PreviewHits.HitObject(x, y, 1);
        try
        {
            player.hitTile = PreviewHits;
            return PickaxeDamage(player, x, y, pickPower, index, Main.tile[x, y]) > 0;
        }
        finally
        {
            player.hitTile = originalHits;
            Main.blockMouse = originalBlockMouse;
            PreviewHits.Clear(index);
        }
    }

    public static int PickaxeDamage(Player player, int x, int y, int pickPower, int hitIndex, Tile tile)
    {
        _pickaxeDamage ??= typeof(Player).GetMethod("GetPickaxeDamage", Private)!
            .CreateDelegate<Func<Player, int, int, int, int, Tile, int>>();
        return _pickaxeDamage(player, x, y, pickPower, hitIndex, tile);
    }

    public static bool TransformsOnKill(Player player, HitTile hitTile, int damage, int x, int y, int pickPower,
        int hitIndex, Tile tile)
    {
        _transformsOnKill ??= typeof(Player).GetMethod("DoesPickTargetTransformOnKill", Private)!
            .CreateDelegate<Func<Player, HitTile, int, int, int, int, int, Tile, bool>>();
        return _transformsOnKill(player, hitTile, damage, x, y, pickPower, hitIndex, tile);
    }

    /// <summary>拆掉实心物块；收集模式下只有物块确实消失才返回掉落，否则让原版把掉落留在世界中。</summary>
    public static bool BreakTile(int x, int y, List<(int Type, int Stack)> drops, bool bringBackItems = true)
    {
        drops.Clear();
        if (!bringBackItems)
        {
            // 客户端沿用原版挖掘消息，让服务器计算掉落，不能再发送 noItem 操作。
            WorldGen.KillTile(x, y);
            if (Main.tile[x, y].HasTile) return false;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);
            return true;
        }
        _dropTileItems ??= typeof(WorldGen).GetMethod("KillTile_DropItems", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Action<int, int, Tile, bool, bool>>();
        DollLootCapture.Begin(drops);
        try { _dropTileItems(x, y, Main.tile[x, y], false, false); }
        finally { DollLootCapture.End(); }

        WorldGen.KillTile(x, y, noItem: true);
        if (Main.tile[x, y].HasTile)
        {
            drops.Clear();
            return false;
        }
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 4, x, y);
        return true;
    }

    /// <summary>拆掉墙；收集时捕获本地掉落并同步方块，关闭收集则使用原版拆墙与掉落同步。</summary>
    public static bool BreakWall(int x, int y, List<(int Type, int Stack)> drops, bool bringBackItems = true)
    {
        drops.Clear();
        if (!bringBackItems)
        {
            WorldGen.KillWall(x, y);
            if (Main.tile[x, y].WallType > 0) return false;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 2, x, y);
            return true;
        }
        DollLootCapture.Begin(drops);
        try { WorldGen.KillWall(x, y); }
        finally { DollLootCapture.End(); }

        if (Main.tile[x, y].WallType > 0)
        {
            drops.Clear();
            return false;
        }
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendTileSquare(-1, x, y);
        return true;
    }
}

/// <summary>
/// 捕获期间生成的物品只记下种类与数量。生成过程中物品保持原样（原版与其他钩子在 NewItem 之后还会读它，
/// 例如合并附近物品、物块复原的 GlobalItem），只移到世界外避免与附近物品合并；
/// 原版掉落流程返回、<see cref="End"/> 时再统一移除。
/// </summary>
public sealed class DollLootCapture : GlobalItem
{
    private static List<(int Type, int Stack)> _sink;
    private static readonly List<Item> _spawned = [];
    private static readonly Vector2 Away = new(-100000f);

    internal static void Begin(List<(int Type, int Stack)> sink) => _sink = sink;

    internal static void End()
    {
        _sink = null;
        foreach (Item item in _spawned)
        {
            item.TurnToAir();
            item.active = false;
        }
        _spawned.Clear();
    }

    /// <summary>玩偶正在拆物块并收走掉落物；此期间生成的物品会被收走，其他逻辑不应再处理它。</summary>
    public static bool Capturing => _sink != null;

    public override void OnSpawn(Item item, IEntitySource source)
    {
        if (_sink == null || item.IsAir) return;
        _sink.Add((item.type, item.stack));
        _spawned.Add(item);
        item.position = Away;
        item.velocity = Vector2.Zero;
    }
}

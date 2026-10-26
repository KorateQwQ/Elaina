using System;
using System.IO;
using Terraria.ID;
using Terraria.Localization;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>联机收集：主人申请完成的一次挖掘，服务器拆除并捕获掉落，只向主人回传物品。</summary>
public sealed class DollMiningNetwork : ModSystem
{
    private enum Operation : byte { Break, Loot }
    private sealed class Pending(Point point, DollPetProjectile worker)
    {
        internal readonly Point Point = point;
        internal readonly DollPetProjectile Worker = worker;
        internal int NextChunk;
    }
    private static readonly Dictionary<Guid, Pending> pending = [];
    private static readonly HashSet<(int Player, Guid Request)> handled = [];
    private static readonly Queue<(int Player, Guid Request)> history = [];
    private static HashSet<Point> changedTiles;
    private const int ChunkSize = 128;

    public override void Load()
    {
        On_WorldGen.KillTile += TrackBrokenTiles;
        On_NetMessage.SendData += SuppressCapturedItemSync;
    }

    public override void Unload()
    {
        On_WorldGen.KillTile -= TrackBrokenTiles;
        On_NetMessage.SendData -= SuppressCapturedItemSync;
        ClearWorld();
    }

    public override void ClearWorld()
    {
        pending.Clear(); handled.Clear(); history.Clear(); changedTiles = null;
    }

    internal static bool Request(Player player, Point point, DollPetProjectile worker = null)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || player.whoAmI != Main.myPlayer
            || !WorldGen.InWorld(point.X, point.Y, 1)) return false;
        foreach (Pending request in pending.Values)
            if (request.Point == point) return true;
        if (pending.Count >= 512) return false;
        Tile tile = Main.tile[point.X, point.Y];
        if (!tile.HasTile) return false;
        Guid id = Guid.NewGuid();
        pending.Add(id, new(point, worker));
        var packet = Packet(Operation.Break, player.whoAmI, id);
        packet.Write((short)point.X); packet.Write((short)point.Y);
        packet.Write(tile.TileType); packet.Write(tile.TileFrameX); packet.Write(tile.TileFrameY);
        packet.Send();
        return true;
    }

    private static ModPacket Packet(Operation operation, int player, Guid id)
    {
        var packet = ModContent.GetInstance<DollMiningNetwork>().Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.DollMining);
        packet.Write(player); packet.Write((byte)operation); packet.Write(id.ToByteArray());
        return packet;
    }

    internal static void HandlePacket(BinaryReader reader, int sender, int playerIndex)
    {
        Operation operation = (Operation)reader.ReadByte();
        byte[] bytes = reader.ReadBytes(16);
        if (bytes.Length != 16) return;
        Guid id = new(bytes);
        if (Main.netMode == NetmodeID.Server)
        {
            if (operation != Operation.Break || sender != playerIndex || sender < 0 || sender >= Main.maxPlayers
                || !Main.player[sender].active || !handled.Add((sender, id))) return;
            history.Enqueue((sender, id));
            if (history.Count > 4096) handled.Remove(history.Dequeue());
            Point point = new(reader.ReadInt16(), reader.ReadInt16());
            ushort type = reader.ReadUInt16();
            short frameX = reader.ReadInt16(), frameY = reader.ReadInt16();
            var drops = BreakOnServer(Main.player[sender], point, type, frameX, frameY);
            // 分包避免模组家具的多种掉落超出 ModPacket 长度。TCP 顺序回执，每批只接收一次。
            for (int offset = 0, chunk = 0; ; offset += ChunkSize, chunk++)
            {
                int count = Math.Min(ChunkSize, drops.Count - offset);
                bool last = offset + count == drops.Count;
                var packet = Packet(Operation.Loot, sender, id);
                packet.Write(chunk); packet.Write(last); packet.Write((ushort)count);
                for (int i = offset; i < offset + count; i++)
                { packet.Write(drops[i].Type); packet.Write(drops[i].Stack); }
                packet.Send(sender);
                if (last) break;
            }
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && operation == Operation.Loot
            && playerIndex == Main.myPlayer && pending.TryGetValue(id, out Pending request))
        {
            int chunk = reader.ReadInt32(); bool last = reader.ReadBoolean(); int count = reader.ReadUInt16();
            if (chunk != request.NextChunk || count > ChunkSize) return;
            List<(int Type, int Stack)> drops = [];
            for (int i = 0; i < count; i++) drops.Add((reader.ReadInt32(), reader.ReadInt32()));
            Main.LocalPlayer.GetModPlayer<DollCommandPlayer>().ReceiveMiningLoot(drops, request.Worker);
            request.NextChunk++;
            if (last) pending.Remove(id);
            // 取消任务/收回期间抵达的有效回执也要保留，随原有玩偶背包存档及交还流程处理。
        }
    }

    private static List<(int Type, int Stack)> BreakOnServer(Player player, Point point, ushort type, short frameX, short frameY)
    {
        List<(int Type, int Stack)> drops = [];
        int x = point.X, y = point.Y;
        if (player.dead || player.noBuilding || !WorldGen.InWorld(x, y, 1)
            || Vector2.DistanceSquared(player.Center, point.ToWorldCoordinates()) > DollCommandPlayer.WorkRange * DollCommandPlayer.WorkRange
            || DollPetProjectile.FindOwned(player) == null) return drops;
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile || tile.TileType != type || Main.tileAxe[type] || Main.tileHammer[type]
            || Main.tileFrameImportant[type] && (tile.TileFrameX != frameX || tile.TileFrameY != frameY)) return drops;
        Item pickaxe = player.GetBestPickaxe();
        if (pickaxe == null || !DollTileBreaker.CanDamageTile(player, x, y, pickaxe.pick) || !WorldGen.CanKillTile(x, y)) return drops;
        HashSet<Point> changed = [];
        changedTiles = changed;
        try { DollTileBreaker.BreakTile(x, y, drops); }
        finally { changedTiles = null; }
        // 同步真实清除的格子及相邻帧。客户端只应用结果，不再执行一次 KillTile 产生家具副本。
        foreach (Point cell in changed) NetMessage.SendTileSquare(-1, cell.X, cell.Y, 3);
        return drops;
    }

    private static void TrackBrokenTiles(On_WorldGen.orig_KillTile orig, int x, int y, bool fail, bool effectOnly, bool noItem)
    {
        bool track = changedTiles != null && WorldGen.InWorld(x, y, 1) && Main.tile[x, y].HasTile;
        orig(x, y, fail, effectOnly, noItem);
        if (track && !Main.tile[x, y].HasTile) changedTiles.Add(new(x, y));
    }

    private static void SuppressCapturedItemSync(On_NetMessage.orig_SendData orig, int message, int remote, int ignore,
        NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        // NewItem 在 OnSpawn 后立即广播；被收入玩偶背包的物品必须在此阻止广播，不能先同步再删除。
        if (Main.netMode == NetmodeID.Server && message == MessageID.SyncItem
            && number >= 0 && number <= Main.maxItems && DollLootCapture.IsCaptured(Main.item[number])) return;
        orig(message, remote, ignore, text, number, number2, number3, number4, number5, number6, number7);
    }
}

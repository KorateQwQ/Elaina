using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

public sealed partial class PurificationProtectionSystem
{
    private const int RangesPerPacket = 256;
    private const byte Request = 0, SnapshotPart = 1, Delta = 2;

    private static ModPacket Packet(int player, byte kind)
    {
        var packet = ModContent.GetInstance<PurificationProtectionSystem>().Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.PurificationDewProtection);
        packet.Write(player); packet.Write(kind);
        return packet;
    }

    internal static void RequestSnapshot()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) Packet(Main.myPlayer, Request).Send();
    }

    internal static void SyncStrip(int x, int firstRow, int rows)
    {
        if (Main.netMode != NetmodeID.Server || rows <= 0) return;
        var packet = Packet(-1, Delta);
        packet.Write((short)x); packet.Write((short)firstRow); packet.Write((byte)rows);
        packet.Send(); // 所有客户端需要保护坐标，防止本地溶液预测把它重新染色。
    }

    internal static void HandlePacket(BinaryReader reader, int sender, int player)
    {
        byte kind = reader.ReadByte();
        if (Main.netMode == NetmodeID.Server)
        {
            if (kind != Request || sender != player || player < 0 || player >= Main.maxPlayers
                || !Main.player[player].active) return;
            int[] ranges = Snapshot();
            // 加入时分包同步完整区间，不塞进每次 WorldData，更不依赖露滴还活着。
            for (int offset = 0; offset < Math.Max(1, ranges.Length); offset += RangesPerPacket * 3)
            {
                int count = Math.Min(RangesPerPacket, (ranges.Length - offset) / 3);
                var packet = Packet(player, SnapshotPart);
                packet.Write(offset == 0); packet.Write((ushort)count);
                for (int i = offset; i < offset + count * 3; i++) packet.Write((short)ranges[i]);
                packet.Send(player);
            }
            return;
        }
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        if (kind == SnapshotPart && player == Main.myPlayer)
        {
            bool reset = reader.ReadBoolean(); int count = reader.ReadUInt16();
            if (count > RangesPerPacket) return;
            if (reset) { lock (gate) columns.Clear(); }
            for (int i = 0; i < count; i++) AddSpan(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());
        }
        else if (kind == Delta && player == -1)
        {
            int x = reader.ReadInt16(), first = reader.ReadInt16(), rows = reader.ReadByte();
            if (rows < 1 || rows > PurificationDewMapSystem.RowsPerPacket) return;
            for (int dx = -1; dx <= 1; dx++) AddSpan(x + dx, first, first + rows - 1);
        }
    }
}

public sealed class PurificationProtectionPlayer : ModPlayer
{
    public override void OnEnterWorld() => PurificationProtectionSystem.RequestSnapshot();
}

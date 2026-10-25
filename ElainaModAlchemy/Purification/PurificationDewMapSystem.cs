using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

/// <summary>沿净化路径逐行开图；远处地图使用原版逐格刷新队列，不重绘整图。</summary>
public sealed class PurificationDewMapSystem : ModSystem
{
    internal const int Radius = 4;
    internal const int RowsPerPacket = 8;
    private const int RowsPerTick = 32;
    private static readonly Queue<Point> pendingRows = new();

    internal static void RevealStrip(int player, int centerX, int firstRow, int rows)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || player < 0 || player >= Main.maxPlayers
            || !Main.player[player].active) return;
        if (!Bounds(centerX, firstRow, rows, out Rectangle area)) return;
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (player == Main.myPlayer && Main.mapEnabled) Enqueue(centerX, area);
            return;
        }

        // 远处区块可能未下载。只向使用者同步刚经过的窄条，再发开图通知；
        // 同一连接按序接收，客户端无需请求整块地形，也不把未下载的格子当成空地。
        // 直接发送 TileSquare：地形已由净化流程处理过，无需再次 RangeFrame。
        NetMessage.SendData(MessageID.TileSquare, player, -1, null, area.X, area.Y, area.Width, area.Height);
        var packet = ModContent.GetInstance<PurificationDewMapSystem>().Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.PurificationDewMap);
        packet.Write(player);
        packet.Write((short)centerX);
        packet.Write((short)area.Y);
        packet.Write((byte)area.Height);
        packet.Send(player);
    }

    internal static void HandlePacket(BinaryReader reader, int player)
    {
        int centerX = reader.ReadInt16(), firstRow = reader.ReadInt16(), rows = reader.ReadByte();
        if (Main.netMode != NetmodeID.MultiplayerClient || player != Main.myPlayer || !Main.mapEnabled) return;
        if (Bounds(centerX, firstRow, rows, out Rectangle area)) Enqueue(centerX, area);
    }

    private static bool Bounds(int centerX, int firstRow, int rows, out Rectangle area)
    {
        area = Rectangle.Empty;
        if (centerX < 2 || centerX >= Main.maxTilesX - 2 || rows < 1 || rows > RowsPerPacket) return false;
        // 与原版地图一致，保留世界边缘 40 格黑边。
        int left = Math.Max(40, centerX - Radius), right = Math.Min(Main.maxTilesX - 40, centerX + Radius + 1);
        int top = Math.Max(40, firstRow), bottom = Math.Min(Main.maxTilesY - 40, firstRow + rows);
        if (left >= right || top >= bottom) return false;
        area = new Rectangle(left, top, right - left, bottom - top);
        return true;
    }

    private static void Enqueue(int centerX, Rectangle area)
    {
        for (int y = area.Top; y < area.Bottom; y++) pendingRows.Enqueue(new Point(centerX, y));
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.netMode == NetmodeID.Server || Main.gameMenu || !Main.mapEnabled
            || Main.Map == null || Main.loadMap) return;
        int budget = RowsPerTick;
        while (budget-- > 0 && pendingRows.Count > 0)
        {
            // 队列满时留到下一帧，禁止用 refreshMap 退化为全图重绘。
            if (MapHelper.numUpdateTile + Radius * 2 + 1 >= MapHelper.maxUpdateTile) break;
            Point row = pendingRows.Dequeue();
            int left = Math.Max(40, row.X - Radius), right = Math.Min(Main.maxTilesX - 40, row.X + Radius + 1);
            for (int x = left; x < right; x++)
            {
                // 中央三格最亮，外侧逐渐变暗；已有更高亮度由 UpdateLighting 保留。
                byte light = (byte)(255 - Math.Max(0, Math.Abs(x - row.X) - 1) * 45);
                if (!Main.Map.UpdateLighting(x, row.Y, light)) continue;
                int index = MapHelper.numUpdateTile++;
                MapHelper.updateTileX[index] = (short)x;
                MapHelper.updateTileY[index] = (short)row.Y;
                Main.updateMap = true;
            }
        }
    }

    public override void ClearWorld() => pendingRows.Clear();
    public override void Unload() => pendingRows.Clear();
}

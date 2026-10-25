using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModAlchemy.Revealing;

/// <summary>独立于 Buff 槽位和免疫的显迹状态；服务器施加，离区后仍保留五分钟。</summary>
public sealed class RevealingDustNPC : GlobalNPC
{
    public const int MarkDuration = 5 * 60 * 60;
    private ulong expiresAt, lastRefresh, lastSync;
    private bool departureSyncPending;
    public override bool InstancePerEntity => true;
    public int Remaining => expiresAt > Main.GameUpdateCount ? (int)Math.Min((ulong)MarkDuration, expiresAt - Main.GameUpdateCount) : 0;
    public bool Marked => Remaining > 0;

    public override void SetDefaults(NPC npc)
    {
        expiresAt = lastRefresh = lastSync = 0;
        departureSyncPending = false;
    }

    internal static bool IsHostile(NPC npc) => npc.active && !npc.friendly && !npc.townNPC && npc.lifeMax > 1
        && npc.catchItem <= 0 && npc.type != NPCID.TargetDummy && !NPCID.Sets.CountsAsCritter[npc.type];

    internal void Refresh(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        ulong now = Main.GameUpdateCount;
        // 首次沾染立即同步；停留时每 15 秒校准一次，避免每秒为每只怪发送完整 NPC 包。
        if (!Marked || now - lastSync >= 15 * 60)
        {
            npc.netUpdate = true;
            lastSync = now;
        }
        expiresAt = now + MarkDuration;
        lastRefresh = now;
        departureSyncPending = true;
    }

    public override void PostAI(NPC npc)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient && departureSyncPending
            && Main.GameUpdateCount > lastRefresh + RevealingDustArea.ScanInterval)
        {
            // 离区/区域到期后校准最后一次沾染的剩余时长；不要求客户端继续看到来源区域。
            npc.netUpdate = true;
            departureSyncPending = false;
            lastSync = Main.GameUpdateCount;
        }
    }

    public override void DrawEffects(NPC npc, ref Color drawColor)
    {
        if (Marked) drawColor = RevealingDustDrawing.DangerColor(drawColor);
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bool marked = Marked;
        bitWriter.WriteBit(marked);
        if (marked) binaryWriter.Write((ushort)Remaining);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        => expiresAt = bitReader.ReadBit() ? Main.GameUpdateCount + Math.Min(binaryReader.ReadUInt16(), (ushort)MarkDuration) : 0;
}

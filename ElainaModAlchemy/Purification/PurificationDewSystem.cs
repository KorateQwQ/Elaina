using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.item.Potions;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

/// <summary>仅服务器创建露滴；库存沿用原版拥有者消费，拒绝时按请求退还一次。</summary>
public sealed class PurificationDewSystem : ModSystem
{
    internal const float UseRange = 960;
    private static readonly int[] cooldown = new int[256];
    private static readonly int[] lastRequest = new int[256];
    private static int sequence, pending;
    private static Item refund;

    internal static bool CanRequest(Player player) => player != null && player.active && !player.dead
        && !player.noItems && !player.CCed && player.selectedItem is >= 0 and < 58
        && pending == 0 && cooldown[player.whoAmI] == 0;

    internal static bool ValidTarget(Player player, Point target) =>
        target.X >= 2 && target.X < Main.maxTilesX - 2 && target.Y >= 1 && target.Y < Main.maxTilesY - 1
        && Vector2.DistanceSquared(player.Center, target.ToWorldCoordinates()) <= UseRange * UseRange;

    internal static bool Request(Player player, Point target)
    {
        if (!CanRequest(player) || !ValidTarget(player, target)) return false;
        if (Main.netMode == NetmodeID.SinglePlayer) return TryRelease(player, player.selectedItem, target);
        if (Main.netMode != NetmodeID.MultiplayerClient) return false;
        pending = ++sequence;
        refund = player.HeldItem.Clone(); refund.stack = 1;
        // 请求在原版扣除前发送。先同步手持槽，最后一件物品也能由服务器校验。
        NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: player.selectedItem);
        var packet = Packet(player.whoAmI);
        packet.Write(pending); packet.Write((byte)player.selectedItem);
        packet.Write((short)target.X); packet.Write((short)target.Y); packet.Send();
        return true;
    }

    internal static bool TryRelease(Player player, int slot, Point target)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || player == null || !player.active || player.dead
            || player.noItems || player.CCed || slot is < 0 or >= 58
            || cooldown[player.whoAmI] > 0 || !ValidTarget(player, target)) return false;
        Item item = player.inventory[slot];
        if (item.type != ModContent.ItemType<PurificationDew>() || item.stack <= 0) return false;
        bool free = false;
        for (int i = 0; i < Main.maxProjectiles; i++)
            if (!Main.projectile[i].active) { free = true; break; }
        if (!free) return false; // 原版 NewProjectile 满槽时可能替换旧弹幕；拒绝并退还更合适。
        int index = Projectile.NewProjectile(player.GetSource_ItemUse(item), target.ToWorldCoordinates(),
            Vector2.Zero, ModContent.ProjectileType<PurificationDewDrop>(), 0, 0,
            Main.myPlayer, target.Y, 0, player.whoAmI);
        if (index < 0 || index >= Main.maxProjectiles) return false;
        cooldown[player.whoAmI] = 30;
        return true;
    }

    private static ModPacket Packet(int player)
    {
        var packet = ModContent.GetInstance<PurificationDewSystem>().Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.PurificationDew);
        packet.Write(player);
        return packet;
    }

    internal static void HandlePacket(BinaryReader reader, int sender, int playerIndex)
    {
        int request = reader.ReadInt32();
        if (Main.netMode == NetmodeID.Server)
        {
            int slot = reader.ReadByte();
            Point target = new(reader.ReadInt16(), reader.ReadInt16());
            if (sender != playerIndex || sender < 0 || sender >= Main.maxPlayers || request <= 0) return;
            if (request <= lastRequest[sender]) return;
            lastRequest[sender] = request;
            bool accepted = TryRelease(Main.player[sender], slot, target);
            var packet = Packet(sender); packet.Write(request); packet.Write(accepted); packet.Send(sender);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            bool accepted = reader.ReadBoolean();
            if (playerIndex != Main.myPlayer || request != pending || refund == null) return;
            Item returned = refund;
            pending = 0; refund = null;
            if (!accepted) Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PurificationDewRejected"), returned, 1);
        }
    }

    public override void PostUpdateWorld()
    {
        for (int i = 0; i < cooldown.Length; i++)
        {
            if (cooldown[i] > 0) cooldown[i]--;
            if (Main.netMode == NetmodeID.Server && !Main.player[i].active) lastRequest[i] = 0;
        }
    }

    public override void ClearWorld()
    {
        Array.Clear(cooldown); Array.Clear(lastRequest);
        pending = sequence = 0; refund = null;
    }
}

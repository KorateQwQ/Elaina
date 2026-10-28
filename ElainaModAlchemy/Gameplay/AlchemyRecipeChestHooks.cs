using System;
using System.Buffers.Binary;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

public sealed partial class AlchemyRecipeChestLoot
{
    private static int openingPlayer = -1;
    private static Point openingPoint;

    public override void Load()
    {
        On_Player.OpenChest += OpenLocalChest;
        On_MessageBuffer.GetData += ReceiveChestRequest;
        On_NetMessage.SendData += BeforeChestContents;
        On_Chest.DestroyChest += DestroyChest;
        On_Chest.DestroyChestDirect += DestroyChestDirect;
        On_Chest.CreateChest += CreateChest;
    }

    public override void Unload()
    {
        On_Player.OpenChest -= OpenLocalChest;
        On_MessageBuffer.GetData -= ReceiveChestRequest;
        On_NetMessage.SendData -= BeforeChestContents;
        On_Chest.DestroyChest -= DestroyChest;
        On_Chest.DestroyChestDirect -= DestroyChestDirect;
        On_Chest.CreateChest -= CreateChest;
        candidates.Clear();
        openingPlayer = -1;
    }

    private static void OpenLocalChest(On_Player.orig_OpenChest orig, Player player, int x, int y, int newChest)
    {
        if (Main.netMode == NetmodeID.SinglePlayer && newChest >= 0 && player.chest != newChest)
            PrepareOpening(player, newChest);
        orig(player, x, y, newChest);
    }

    private static void ReceiveChestRequest(On_MessageBuffer.orig_GetData orig, MessageBuffer buffer,
        int start, int length, out int messageType)
    {
        int previousPlayer = openingPlayer;
        Point previousPoint = openingPoint;
        openingPlayer = -1;
        // Do not consume the reader or grant an opening here. Vanilla/mod hooks still validate the request.
        if (Main.netMode == NetmodeID.Server && length >= 5 && start >= 0 && start + 5 <= buffer.readBuffer.Length
            && buffer.readBuffer[start] == MessageID.RequestChestOpen && buffer.whoAmI >= 0 && buffer.whoAmI < Main.maxPlayers)
        {
            openingPlayer = buffer.whoAmI;
            openingPoint = new(BinaryPrimitives.ReadInt16LittleEndian(buffer.readBuffer.AsSpan(start + 1, 2)),
                BinaryPrimitives.ReadInt16LittleEndian(buffer.readBuffer.AsSpan(start + 3, 2)));
        }
        try { orig(buffer, start, length, out messageType); }
        finally { openingPlayer = previousPlayer; openingPoint = previousPoint; }
    }

    private static void BeforeChestContents(On_NetMessage.orig_SendData orig, int msg, int remote, int ignore, NetworkText text,
        int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient && msg == MessageID.RequestChestOpen)
            Main.LocalPlayer.GetModPlayer<AlchemyRecipeDiscoveryPlayer>().SyncBeforeOpening();
        if (Main.netMode == NetmodeID.Server && msg == MessageID.SyncChestItem && remote == openingPlayer
            && openingPlayer >= 0 && number >= 0 && number < Main.chest.Length
            && Main.chest[number] is { } chest && chest.x == openingPoint.X && chest.y == openingPoint.Y)
        {
            int owner = openingPlayer;
            openingPlayer = -1; // Once, BEFORE the first slot is serialized; no subsequent item pop-in.
            PrepareOpening(Main.player[owner], number);
        }
        orig(msg, remote, ignore, text, number, number2, number3, number4, number5, number6, number7);
    }

    private static bool DestroyChest(On_Chest.orig_DestroyChest orig, int x, int y)
    {
        bool destroyed = orig(x, y);
        if (destroyed) candidates.Remove(new(x, y));
        return destroyed;
    }

    private static void DestroyChestDirect(On_Chest.orig_DestroyChestDirect orig, int x, int y, int id)
    {
        orig(x, y, id);
        if (Chest.FindChest(x, y) < 0) candidates.Remove(new(x, y));
    }

    private static int CreateChest(On_Chest.orig_CreateChest orig, int x, int y, int id)
    {
        int index = orig(x, y, id);
        if (index >= 0 && Main.netMode != NetmodeID.MultiplayerClient) candidates.Remove(new(x, y));
        return index;
    }
}

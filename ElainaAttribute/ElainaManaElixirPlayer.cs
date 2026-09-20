using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.System;

namespace 伊蕾娜.ElainaAttribute;

public sealed class ElainaManaElixirPlayer : ModPlayer
{
    public const int MaxCharges = 5;

    private List<int> restoreAmounts = new(MaxCharges);

    public int ChargeCount => restoreAmounts.Count;

    public long TotalRestoreAmount
    {
        get
        {
            long total = 0;
            foreach (int amount in restoreAmounts)
            {
                total += amount;
            }

            return total;
        }
    }

    public bool CanStoreCharge(int restoreAmount)
    {
        return restoreAmount > 0 && !Player.dead
            && Player.GetModPlayer<ElainaModplayer>().Elaina
            && ChargeCount < MaxCharges;
    }

    public bool TryStoreCharge(int restoreAmount)
    {
        if (!CanStoreCharge(restoreAmount))
        {
            return false;
        }

        restoreAmounts.Add(restoreAmount);
        return true;
    }

    internal float PreviewRecovery(float currentMagic, float targetMagic, out int chargesNeeded)
    {
        chargesNeeded = 0;
        foreach (int amount in restoreAmounts)
        {
            if (currentMagic >= targetMagic)
            {
                break;
            }

            // 施法费用尚未结算，此处不按魔力上限裁剪。
            currentMagic += amount;
            chargesNeeded++;
        }

        return currentMagic;
    }

    public bool TryRestoreMagic()
    {
        var attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
        if (Player.dead || !attributePlayer.UniqueMagicEnabled || ChargeCount == 0
            || attributePlayer.MagicPoint >= attributePlayer.MaxMagicPoint)
        {
            return false;
        }

        float magicBefore = attributePlayer.MagicPoint;
        int restoreAmount = restoreAmounts[0];
        attributePlayer.RegenMagicPoint(restoreAmount);
        attributePlayer.InBattleState();
        ConsumeCharges(1, attributePlayer.MagicPoint - magicBefore);

        return true;
    }

    internal void ConsumeCharges(int count, float restoredMagic)
    {
        if (count == 0)
        {
            return;
        }

        restoreAmounts.RemoveRange(0, count);

        if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
        {
            Player.ManaEffect((int)MathF.Ceiling(restoredMagic));
            SoundEngine.PlaySound(SoundID.Item3, Player.Center);
        }
    }

    public override void Initialize()
    {
        restoreAmounts = new List<int>(MaxCharges);
    }

    public override void SaveData(TagCompound tag)
    {
        tag["restoreAmounts"] = new List<int>(restoreAmounts);
    }

    public override void LoadData(TagCompound tag)
    {
        restoreAmounts.Clear();
        foreach (int amount in tag.GetList<int>("restoreAmounts"))
        {
            if (amount > 0 && ChargeCount < MaxCharges)
            {
                restoreAmounts.Add(amount);
            }
        }
    }

    public override void CopyClientState(ModPlayer targetCopy)
    {
        ((ElainaManaElixirPlayer)targetCopy).restoreAmounts = new List<int>(restoreAmounts);
    }

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        var previous = (ElainaManaElixirPlayer)clientPlayer;
        if (ChargeCount != previous.ChargeCount)
        {
            SyncPlayer(-1, -1, false);
            return;
        }

        for (int i = 0; i < ChargeCount; i++)
        {
            if (restoreAmounts[i] != previous.restoreAmounts[i])
            {
                SyncPlayer(-1, -1, false);
                return;
            }
        }
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        ModPacket packet = Mod.GetPacket();
        packet.Write((byte)伊蕾娜.MessageType.ManaElixirCharges);
        packet.Write(Player.whoAmI);
        packet.Write((byte)ChargeCount);
        foreach (int amount in restoreAmounts)
        {
            packet.Write(amount);
        }

        packet.Send(toWho, fromWho);
    }

    internal static void ReceiveCharges(BinaryReader reader, int playerIndex, int sender)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers
            || (Main.netMode == NetmodeID.Server && playerIndex != sender))
        {
            return;
        }

        int count = reader.ReadByte();
        if (count > MaxCharges)
        {
            return;
        }

        var amounts = new List<int>(MaxCharges);
        for (int i = 0; i < count; i++)
        {
            int amount = reader.ReadInt32();
            if (amount <= 0)
            {
                return;
            }

            amounts.Add(amount);
        }

        var elixirPlayer = Main.player[playerIndex].GetModPlayer<ElainaManaElixirPlayer>();
        elixirPlayer.restoreAmounts = amounts;
        if (Main.netMode == NetmodeID.Server)
        {
            elixirPlayer.SyncPlayer(-1, sender, false);
        }
    }
}

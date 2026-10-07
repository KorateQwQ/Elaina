using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModAlchemy.item.Potions;
using 伊蕾娜.System;

namespace 伊蕾娜.ElainaAttribute;

public sealed class ElainaManaElixirPlayer : ModPlayer
{
    public const int MaxCharges = 5;

    private int chargeCount;

    public int ChargeCount => chargeCount;
    private float ChargeRestoreAmount => Player.GetModPlayer<ElainaAttributeModPlayer>().MaxMagicPoint * 0.5f;
    public long TotalRestoreAmount => (long)MathF.Ceiling(chargeCount * ChargeRestoreAmount);

    public bool CanStoreCharge()
    {
        return !Player.dead && !Player.GetModPlayer<ElainaAttributeModPlayer>().InBattle
            && Player.GetModPlayer<ElainaModplayer>().Elaina
            && ChargeCount < MaxCharges;
    }

    public bool TryStoreCharge()
    {
        if (!CanStoreCharge())
        {
            return false;
        }

        chargeCount++;
        return true;
    }

    public override void PostUpdate()
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer || !CanStoreCharge()) return;

        int elixirType = ModContent.ItemType<MoonDewElixir>();
        bool consumed = false;
        for (int slot = 0; slot < Math.Min(50, Player.inventory.Length) && CanStoreCharge(); slot++)
        {
            Item item = Player.inventory[slot];
            if (item.type != elixirType || !item.favorited) continue;

            bool consumedSlot = false;
            while (item.stack > 0 && CanStoreCharge() && ItemLoader.ConsumeItem(item, Player))
            {
                consumed = consumedSlot = true;
                if (--item.stack == 0) item.TurnToAir();
            }

            if (consumedSlot && Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncEquipment, number: Player.whoAmI,
                    number2: PlayerItemSlotID.Inventory0 + slot, number3: item.prefix);
        }

        if (consumed) Recipe.FindRecipes();
    }

    internal float PreviewRecovery(float currentMagic, float targetMagic, out int chargesNeeded)
    {
        chargesNeeded = 0;
        for (int i = 0; i < chargeCount; i++)
        {
            if (currentMagic >= targetMagic)
            {
                break;
            }

            // 施法费用尚未结算，此处不按魔力上限裁剪。
            currentMagic += ChargeRestoreAmount;
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
        attributePlayer.RegenMagicPoint(ChargeRestoreAmount);
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

        chargeCount -= count;

        if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
        {
            Player.ManaEffect((int)MathF.Ceiling(restoredMagic));
            SoundEngine.PlaySound(SoundID.Item3, Player.Center);
        }
    }

    public override void Initialize()
    {
        chargeCount = 0;
    }

    public override void SaveData(TagCompound tag)
    {
        tag["chargeCount"] = chargeCount;
    }

    public override void LoadData(TagCompound tag)
    {
        if (tag.ContainsKey("chargeCount"))
        {
            chargeCount = Math.Clamp(tag.GetInt("chargeCount"), 0, MaxCharges);
            return;
        }

        chargeCount = 0;
        foreach (int amount in tag.GetList<int>("restoreAmounts"))
            if (amount > 0 && chargeCount < MaxCharges) chargeCount++;
    }

    public override void CopyClientState(ModPlayer targetCopy)
    {
        ((ElainaManaElixirPlayer)targetCopy).chargeCount = chargeCount;
    }

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        if (ChargeCount != ((ElainaManaElixirPlayer)clientPlayer).ChargeCount)
            SyncPlayer(-1, -1, false);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        ModPacket packet = Mod.GetPacket();
        packet.Write((byte)伊蕾娜.MessageType.ManaElixirCharges);
        packet.Write(Player.whoAmI);
        packet.Write((byte)ChargeCount);

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

        var elixirPlayer = Main.player[playerIndex].GetModPlayer<ElainaManaElixirPlayer>();
        elixirPlayer.chargeCount = count;
        if (Main.netMode == NetmodeID.Server)
        {
            elixirPlayer.SyncPlayer(-1, sender, false);
        }
    }
}

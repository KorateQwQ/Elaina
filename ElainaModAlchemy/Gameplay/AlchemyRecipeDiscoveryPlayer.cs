using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Permanent discovery (successful chest roll or acquisition) and per-formula pity belong to the character.</summary>
public sealed class AlchemyRecipeDiscoveryPlayer : ModPlayer
{
    private ExplorationRecipe obtained;
    private int featherlightMisses, painkillerMisses;
    private bool sentInitial;
    private ExplorationRecipe sentObtained;
    internal bool ServerReady { get; private set; }
    private enum Operation : byte { Initialize, Obtained, State }

    public override void Initialize()
    {
        obtained = sentObtained = ExplorationRecipe.None;
        featherlightMisses = painkillerMisses = 0;
        sentInitial = ServerReady = false;
    }

    internal bool HasObtained(ExplorationRecipe recipe) => (obtained & recipe) != 0;
    internal int Misses(ExplorationRecipe recipe) => recipe == ExplorationRecipe.Painkiller ? painkillerMisses : featherlightMisses;
    internal int ChancePercent(ExplorationRecipe recipe) => HasObtained(recipe) ? 5
        : recipe == ExplorationRecipe.Painkiller ? painkillerMisses >= 2 ? 100 : 30 + painkillerMisses * 30
        : Math.Min(100, 20 + featherlightMisses * 10);

    internal void RecordRoll(ExplorationRecipe recipe, bool success)
    {
        if (success)
        {
            // Opening counts even when the scroll is left in the chest; reveal forecasts never call this.
            Obtain(recipe);
            return;
        }
        int next = HasObtained(recipe) ? 0 : Misses(recipe) + 1;
        if (recipe == ExplorationRecipe.Painkiller) painkillerMisses = Math.Clamp(next, 0, 2);
        else featherlightMisses = Math.Clamp(next, 0, 8);
    }

    private void Obtain(ExplorationRecipe recipe)
    {
        obtained |= recipe;
        if ((recipe & ExplorationRecipe.Featherlight) != 0) featherlightMisses = 0;
        if ((recipe & ExplorationRecipe.Painkiller) != 0) painkillerMisses = 0;
    }

    // Called when a doll takes cargo, before delivery: it already belongs to the owner.
    public void Observe(Item item)
    {
        if (item is not { IsAir: false, ModItem: AlchemyRecipeScroll scroll }) return;
        if (scroll.EntryId == "featherlight") Obtain(ExplorationRecipe.Featherlight);
        if (scroll.EntryId == "painkiller") Obtain(ExplorationRecipe.Painkiller);
    }

    internal void ObservePossessions()
    {
        var research = Player.GetModPlayer<AlchemyProgressPlayer>();
        if (research.IsUnlocked("featherlight")) Obtain(ExplorationRecipe.Featherlight);
        if (research.IsUnlocked("painkiller")) Obtain(ExplorationRecipe.Painkiller);
        if ((byte)obtained == 3) return;
        foreach (Item item in Player.inventory) Observe(item);
        // Personal storage counts as ownership; viewing a shared chest does not.
        foreach (var bank in new[] { Player.bank, Player.bank2, Player.bank3, Player.bank4 })
            foreach (Item item in bank.item) Observe(item);
        if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer) Observe(Main.mouseItem);
    }

    public override void SaveData(TagCompound tag)
    {
        tag["Obtained"] = (byte)obtained;
        tag["FeatherlightMisses"] = featherlightMisses;
        tag["PainkillerMisses"] = painkillerMisses;
    }

    public override void LoadData(TagCompound tag)
    {
        obtained = (ExplorationRecipe)(tag.GetByte("Obtained") & 3);
        featherlightMisses = Math.Clamp(tag.GetInt("FeatherlightMisses"), 0, 8);
        painkillerMisses = Math.Clamp(tag.GetInt("PainkillerMisses"), 0, 2);
        Obtain(obtained);
    }

    public override void OnEnterWorld()
    {
        sentInitial = false;
        SyncBeforeOpening();
    }

    public override void PostUpdate()
    {
        if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer) SyncBeforeOpening();
    }

    internal void SyncBeforeOpening()
    {
        ObservePossessions();
        if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI != Main.myPlayer) return;
        if (!sentInitial)
        {
            var packet = Packet(Operation.Initialize);
            packet.Write(Player.GetModPlayer<ElainaModplayer>().Elaina);
            WriteState(packet);
            packet.Send();
            sentInitial = true;
            sentObtained = obtained;
        }
        else if (sentObtained != obtained)
        {
            var packet = Packet(Operation.Obtained);
            packet.Write((byte)obtained);
            packet.Send();
            sentObtained = obtained;
        }
    }

    private ModPacket Packet(Operation operation)
    {
        var packet = Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.AlchemyRecipeDiscovery);
        packet.Write(Player.whoAmI);
        packet.Write((byte)operation);
        return packet;
    }

    private void WriteState(BinaryWriter writer)
    {
        writer.Write((byte)obtained);
        writer.Write((byte)featherlightMisses);
        writer.Write((byte)painkillerMisses);
    }

    internal void SendServerState()
    {
        if (Main.netMode != NetmodeID.Server) return;
        var packet = Packet(Operation.State);
        WriteState(packet);
        packet.Send(Player.whoAmI);
    }

    internal static void HandlePacket(BinaryReader reader, int sender, int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return;
        var state = Main.player[playerIndex].GetModPlayer<AlchemyRecipeDiscoveryPlayer>();
        var operation = (Operation)reader.ReadByte();
        if (Main.netMode == NetmodeID.Server)
        {
            if (sender != playerIndex || !state.Player.active) return;
            if (operation == Operation.Initialize)
            {
                bool elaina = reader.ReadBoolean();
                var history = (ExplorationRecipe)(reader.ReadByte() & 3);
                int goldMisses = reader.ReadByte(), jungleMisses = reader.ReadByte();
                if (state.ServerReady) { state.Obtain(history); return; }
                state.Player.GetModPlayer<ElainaModplayer>().Elaina = elaina;
                state.featherlightMisses = Math.Clamp(goldMisses, 0, 8);
                state.painkillerMisses = Math.Clamp(jungleMisses, 0, 2);
                state.Obtain(history);
                state.ServerReady = true;
            }
            else if (operation == Operation.Obtained && state.ServerReady)
                state.Obtain((ExplorationRecipe)(reader.ReadByte() & 3));
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && playerIndex == Main.myPlayer && operation == Operation.State)
        {
            var history = (ExplorationRecipe)(reader.ReadByte() & 3);
            state.featherlightMisses = Math.Clamp((int)reader.ReadByte(), 0, 8);
            state.painkillerMisses = Math.Clamp((int)reader.ReadByte(), 0, 2);
            // A snapshot sent before a local pickup cannot erase the newer acquisition.
            state.Obtain(history | state.obtained);
        }
    }
}

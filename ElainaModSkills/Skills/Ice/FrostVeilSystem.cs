using System;
using System.IO;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>The caster pays once; the server selects recipients; each recipient resolves its own hurt and refreshes.</summary>
public sealed class FrostVeilSystem : ModSystem
{
    public const float CastRadius = 800f;
    private enum Operation : byte { Cast, OwnerState, State }

    public static bool IsEligible(Player caster, Player target) => caster.active && !caster.dead
        && target.active && !target.dead
        && (target.whoAmI == caster.whoAmI || !caster.hostile && !target.hostile && caster.team == target.team)
        && Vector2.DistanceSquared(caster.Center, target.Center) <= CastRadius * CastRadius;

    public static void Cast(Player caster, float absorption)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) { Grant(caster, absorption); return; }
        if (Main.netMode != NetmodeID.MultiplayerClient || caster.whoAmI != Main.myPlayer) return;
        ModPacket packet = Packet(Operation.Cast, caster.whoAmI);
        // Maximum magic includes local equipment/passive bonuses. Preserve the actual paid cast's snapshot.
        packet.Write(absorption);
        packet.Send();
    }

    private static void Grant(Player caster, float absorption)
    {
        if (!float.IsFinite(absorption) || absorption <= 0f) return;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (!IsEligible(caster, Main.player[i])) continue;
            var recipient = Main.player[i].GetModPlayer<FrostVeilPlayer>();
            recipient.Generation++;
            recipient.Veil.Apply(absorption);
            if (Main.netMode == NetmodeID.Server) SendState(recipient);
        }
    }

    internal static void SendOwnerState(FrostVeilPlayer recipient)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || recipient.Player.whoAmI != Main.myPlayer) return;
        ModPacket packet = Packet(Operation.OwnerState, recipient.Player.whoAmI);
        packet.Write(recipient.Generation);
        packet.Write((byte)recipient.Veil.Shields);
        packet.Write(recipient.Veil.RemainingTicks);
        packet.Send();
    }

    internal static void SendState(FrostVeilPlayer recipient, int toWho = -1, int ignore = -1)
    {
        ModPacket packet = Packet(Operation.State, recipient.Player.whoAmI);
        packet.Write(recipient.Generation);
        packet.Write((byte)recipient.Veil.Shields);
        packet.Write(recipient.Veil.RemainingTicks);
        packet.Write(recipient.Veil.Absorption);
        packet.Send(toWho, ignore);
    }

    private static ModPacket Packet(Operation operation, int player)
    {
        ModPacket packet = ModContent.GetInstance<FrostVeilSystem>().Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.FrostVeil);
        packet.Write(player);
        packet.Write((byte)operation);
        return packet;
    }

    internal static void HandlePacket(BinaryReader reader, int sender, int playerIndex)
    {
        Operation operation = (Operation)reader.ReadByte();
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return;
        Player player = Main.player[playerIndex];
        var recipient = player.GetModPlayer<FrostVeilPlayer>();
        if (Main.netMode == NetmodeID.Server)
        {
            if (sender != playerIndex || !player.active) return;
            if (operation == Operation.Cast) Grant(player, reader.ReadSingle());
            else if (operation == Operation.OwnerState)
            {
                uint generation = reader.ReadUInt32();
                int count = reader.ReadByte(), ticks = reader.ReadInt32();
                // An in-flight hit/refresh from an older veil cannot overwrite a newly cast veil.
                if (generation != recipient.Generation || count > FrostVeilState.MaximumShields
                    || ticks < 0 || ticks > FrostVeilState.DurationTicks || recipient.Veil.Shields == 0) return;
                recipient.Veil.Restore(count, ticks, recipient.Veil.Absorption);
                SendState(recipient, ignore: sender);
            }
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && operation == Operation.State)
        {
            recipient.Generation = reader.ReadUInt32();
            int count = reader.ReadByte(), ticks = reader.ReadInt32();
            recipient.Veil.Restore(count, ticks, reader.ReadSingle());
        }
    }
}

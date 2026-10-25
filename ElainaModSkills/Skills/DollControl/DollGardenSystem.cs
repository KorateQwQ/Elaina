using System;
using System.IO;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>花盆归属随世界保存；主人标识随角色保存，退出、换世界、其他玩家拆除都不会误接管花盆。</summary>
public sealed class DollGardenSystem : ModSystem
{
    internal sealed record Bed(Guid Owner, ItemDefinition Planter, ItemDefinition Seed);
    internal static readonly Dictionary<Point, Bed> Beds = [];
    internal static int Revision { get; private set; }
    private enum Operation : byte { Join, Register, Seed, Snapshot, Remove }

    public override void Load() => On_WorldGen.KillTile += AfterKillTile;
    public override void Unload()
    {
        On_WorldGen.KillTile -= AfterKillTile;
        ClearWorld();
        DollHerbPlanting.Clear();
    }
    public override void ClearWorld() { Beds.Clear(); Revision++; }

    private static void AfterKillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
    {
        orig(i, j, fail, effectOnly, noItem);
        if (WorldGen.InWorld(i, j, 1) && !Main.tile[i, j].HasTile) Remove(new(i, j));
    }

    internal static bool Exists(Point point, Bed bed)
    {
        if (!WorldGen.InWorld(point.X, point.Y - 1, 1) || !ContentSamples.ItemsByType.TryGetValue(bed.Planter.Type, out Item pot)
            || !DollPlanterBuilding.IsMaterial(pot)) return false;
        Tile tile = Main.tile[point.X, point.Y];
        return tile.HasTile && tile.TileType == pot.createTile;
    }

    internal static void Register(Player player, Point point, int planter)
    {
        if (player.whoAmI != Main.myPlayer) return;
        Guid owner = player.GetModPlayer<DollCommandPlayer>().GardenOwner;
        var bed = new Bed(owner, new ItemDefinition(planter), new ItemDefinition(DollHerbPlanting.PreferredSeed(planter)));
        if (!Exists(point, bed) || Beds.ContainsKey(point)
            || Beds.Values.Count(b => b.Owner == owner) >= DollPlanterBuilding.Limit) return;
        Beds[point] = bed; Revision++;
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = Packet(Operation.Register, player.whoAmI);
            WritePoint(packet, point); packet.Write(planter); packet.Send();
        }
    }

    internal static void SetSeed(Player player, Point point, int seed)
    {
        if (!Beds.TryGetValue(point, out var bed) || bed.Owner != player.GetModPlayer<DollCommandPlayer>().GardenOwner
            || bed.Seed.Type == seed) return;
        Beds[point] = bed with { Seed = new(seed) }; Revision++;
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = Packet(Operation.Seed, player.whoAmI);
            WritePoint(packet, point); packet.Write(seed); packet.Send();
        }
    }

    internal static void Remove(Point point)
    {
        if (!Beds.Remove(point, out var bed)) return;
        Revision++;
        if (Main.netMode != NetmodeID.Server) return;
        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i].active && Main.player[i].GetModPlayer<DollCommandPlayer>().GardenOwner == bed.Owner)
            {
                var packet = Packet(Operation.Remove, i); WritePoint(packet, point); packet.Send(i);
            }
    }

    internal static void Join(Player player)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || player.whoAmI != Main.myPlayer) return;
        var packet = Packet(Operation.Join, player.whoAmI);
        packet.Write(player.GetModPlayer<DollCommandPlayer>().GardenOwner.ToByteArray()); packet.Send();
    }

    private static ModPacket Packet(Operation operation, int player)
    {
        var packet = ModContent.GetInstance<DollGardenSystem>().Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.DollGarden);
        packet.Write(player); packet.Write((byte)operation);
        return packet;
    }
    private static void WritePoint(BinaryWriter writer, Point p) { writer.Write((short)p.X); writer.Write((short)p.Y); }
    private static Point ReadPoint(BinaryReader reader) => new(reader.ReadInt16(), reader.ReadInt16());

    internal static void HandlePacket(BinaryReader reader, int sender, int playerIndex)
    {
        Operation operation = (Operation)reader.ReadByte();
        if (Main.netMode == NetmodeID.Server)
        {
            if (sender != playerIndex || sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active) return;
            Player player = Main.player[sender];
            var commands = player.GetModPlayer<DollCommandPlayer>();
            if (operation == Operation.Join)
            {
                byte[] bytes = reader.ReadBytes(16);
                if (bytes.Length != 16) return;
                Guid owner = new(bytes);
                if (owner == Guid.Empty) return;
                // 同一个角色副本不能在同一服务器上同时驱动两队照料相同花盆。
                if (Main.player.Any(p => p.active && p.whoAmI != sender && p.GetModPlayer<DollCommandPlayer>().GardenOwner == owner)) return;
                commands.GardenOwner = owner;
                SendSnapshot(player);
                return;
            }
            if (operation is not (Operation.Register or Operation.Seed)) return;
            Point point = ReadPoint(reader);
            int type = reader.ReadInt32();
            if (type <= 0 || type >= ItemLoader.ItemCount || !WorldGen.InWorld(point.X, point.Y - 1, 1)
                || Vector2.DistanceSquared(player.Center, point.ToWorldCoordinates()) > DollCommandPlayer.WorkRange * DollCommandPlayer.WorkRange
                || commands.GardenOwner == Guid.Empty) return;
            if (operation == Operation.Register)
            {
                if (Beds.ContainsKey(point) || !ContentSamples.ItemsByType.TryGetValue(type, out Item pot)
                    || !DollPlanterBuilding.IsMaterial(pot)
                    || Beds.Values.Count(b => b.Owner == commands.GardenOwner) >= DollPlanterBuilding.Limit) return;
                var bed = new Bed(commands.GardenOwner, new(type), new(DollHerbPlanting.PreferredSeed(type)));
                if (Exists(point, bed)) { Beds[point] = bed; Revision++; }
            }
            else if (Beds.TryGetValue(point, out var bed) && bed.Owner == commands.GardenOwner && Exists(point, bed)
                && ContentSamples.ItemsByType.TryGetValue(type, out var seed) && DollHerbPlanting.IsSeed(seed)
                && DollHerbPlanting.Matches(type, point.X, point.Y - 1))
            {
                Beds[point] = bed with { Seed = new(type) }; Revision++;
            }
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && playerIndex == Main.myPlayer)
        {
            if (operation == Operation.Remove) { Remove(ReadPoint(reader)); return; }
            if (operation != Operation.Snapshot) return;
            bool first = reader.ReadBoolean();
            int count = reader.ReadByte();
            if (count > 64) return;
            if (first) Beds.Clear();
            Guid owner = Main.LocalPlayer.GetModPlayer<DollCommandPlayer>().GardenOwner;
            for (int i = 0; i < count; i++)
            {
                Point point = ReadPoint(reader);
                var pot = new ItemDefinition(reader.ReadString());
                var seed = new ItemDefinition(reader.ReadString());
                if (WorldGen.InWorld(point.X, point.Y - 1, 1)) Beds[point] = new(owner, pot, seed);
            }
            Revision++;
        }
    }

    private static void SendSnapshot(Player player)
    {
        var owned = Beds.Where(p => p.Value.Owner == player.GetModPlayer<DollCommandPlayer>().GardenOwner).ToArray();
        // 分包，避免上万花盆超过单条 ModPacket 的大小限制。
        for (int start = 0; start < Math.Max(1, owned.Length); start += 64)
        {
            var packet = Packet(Operation.Snapshot, player.whoAmI);
            int count = Math.Min(64, owned.Length - start);
            packet.Write(start == 0); packet.Write((byte)count);
            for (int i = start; i < start + count; i++)
            {
                WritePoint(packet, owned[i].Key);
                packet.Write(owned[i].Value.Planter.ToString()); packet.Write(owned[i].Value.Seed.ToString());
            }
            packet.Send(player.whoAmI);
        }
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag["DollGarden"] = Beds.Select(pair => new TagCompound
        {
            ["x"] = pair.Key.X, ["y"] = pair.Key.Y, ["owner"] = pair.Value.Owner.ToString(),
            ["pot"] = pair.Value.Planter.ToString(), ["seed"] = pair.Value.Seed.ToString(),
        }).ToList();
    }

    public override void LoadWorldData(TagCompound tag)
    {
        Beds.Clear();
        foreach (var entry in tag.GetList<TagCompound>("DollGarden"))
        {
            var point = new Point(entry.GetInt("x"), entry.GetInt("y"));
            if (!WorldGen.InWorld(point.X, point.Y - 1, 1) || !Guid.TryParse(entry.GetString("owner"), out var owner)) continue;
            Beds[point] = new(owner, new(entry.GetString("pot")), new(entry.GetString("seed")));
        }
        Revision++;
    }
}

using System;
using System.IO;
using Terraria.ID;
using 伊蕾娜.Items;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>施法客户端以自己的 whoAmI 生成和管理两端弹幕；服务器只登记配对、校验并结算玩家传送。</summary>
public sealed class RecallPortalSystem : ModSystem
{
    public const float SelectionRadius = 3000f;
    public const float AreaRadius = 160f;
    public const int DelayTicks = 90;
    public const int ResidueTicks = 30 * 60;
    public const int TravelCooldownTicks = 45;

    internal enum Operation : byte { Cast, Travel, SpawnPoint, Teleport, CastResult, Commit, OpenDestination, DestinationReady, Close }
    private const int RequestTimeout = 300;
    private sealed class Pair
    {
        public int Id, Caster, Target, Age, OriginIndex, DestinationIndex = -1;
        public Vector2 Origin, Destination;
        public bool Committed, DestinationRequested, Activated;
        public int WaitingTicks;
    }
    private sealed class ClientRequest
    {
        public int Caster, Id, Age;
        public Func<bool> Pay;
    }
    private static readonly List<Pair> pairs = [];
    private static readonly List<ClientRequest> requests = [];
    private static int nextId;

    public static bool IsAlly(Player caster, Player candidate) => caster.active &&
        candidate.active && !candidate.dead && (caster.whoAmI == candidate.whoAmI ||
        (!caster.hostile && !candidate.hostile && caster.team == candidate.team));

    public static bool IsSelectable(Player caster, int target) => !caster.dead &&
        target >= 0 && target < Main.maxPlayers && IsAlly(caster, Main.player[target]) &&
        Vector2.DistanceSquared(caster.Center, Main.player[target].Center) <= SelectionRadius * SelectionRadius;

    public static bool InArea(Player player, Vector2 foot) =>
        Vector2.DistanceSquared(player.Bottom, foot) <= AreaRadius * AreaRadius;

    internal static ModPacket Packet(Operation operation, int player)
    {
        ModPacket packet = ModContent.GetInstance<RecallPortalSystem>().Mod.GetPacket();
        packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.RecallPortal);
        packet.Write(player);
        packet.Write((byte)operation);
        return packet;
    }

    internal static bool HasPendingCast(Player caster) => requests.Exists(r => r.Caster == caster.whoAmI);

    internal static void RequestCast(Player caster, int target, Func<bool> pay = null)
    {
        if (Main.netMode == NetmodeID.Server || caster.whoAmI != Main.myPlayer || HasPendingCast(caster) ||
            !IsSelectable(caster, target)) return;
        if (++nextId >= 16_000_000) nextId = 1;
        int id = nextId;
        // NewProjectile 必须由 owner 所在客户端调用，原版才会初始化 ai 并发送 SyncProjectile。
        // -1 表示等待服务器确认，尚不播放法阵，也不开始蓄力或扣费。
        int index = SpawnOwnedPortal(caster.whoAmI, id, Main.player[target].Bottom, 0, -1);
        if (index >= Main.maxProjectiles) { Main.NewText(LocalizeUI("RecallCastFailed")); return; }
        requests.Add(new ClientRequest { Caster = caster.whoAmI, Id = id, Pay = pay });
        if (Main.netMode == NetmodeID.SinglePlayer) { RegisterCast(caster.whoAmI, target, id); return; }
        ModPacket packet = Packet(Operation.Cast, caster.whoAmI);
        packet.Write(target);
        packet.Write(id);
        packet.Send();
    }

    internal static void RequestTravel(Player player, RecallPortalProjectile portal)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) { Travel(player.whoAmI, portal.Projectile.owner, portal.PairId, portal.Side); return; }
        ModPacket packet = Packet(Operation.Travel, player.whoAmI);
        packet.Write(portal.Projectile.owner);
        packet.Write(portal.PairId);
        packet.Write(portal.Side);
        packet.Send();
    }

    internal static void HandlePacket(BinaryReader reader, int sender, int playerIndex)
    {
        Operation operation = (Operation)reader.ReadByte();
        if (Main.netMode == NetmodeID.Server)
        {
            // 从连接确定操作者；不接受客户端指定其他玩家或目的坐标。
            if (sender < 0 || sender >= Main.maxPlayers || playerIndex != sender || !Main.player[sender].active) return;
            switch (operation)
            {
                case Operation.Cast: RegisterCast(sender, reader.ReadInt32(), reader.ReadInt32()); break;
                case Operation.Commit: CommitCast(sender, reader.ReadInt32(), reader.ReadBoolean()); break;
                case Operation.DestinationReady: ActivatePair(sender, reader.ReadInt32()); break;
                case Operation.Travel: Travel(sender, reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()); break;
                case Operation.SpawnPoint:
                    int x = reader.ReadInt32(), y = reader.ReadInt32();
                    bool valid = x >= 10 && x < Main.maxTilesX - 10 && y >= 10 && y < Main.maxTilesY - 10;
                    Main.player[sender].SpawnX = valid ? x : -1;
                    Main.player[sender].SpawnY = valid ? y : -1;
                    break;
            }
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && playerIndex >= 0 && playerIndex < Main.maxPlayers)
        {
            if (operation == Operation.Teleport)
            {
                Vector2 position = new(reader.ReadSingle(), reader.ReadSingle());
                if (float.IsFinite(position.X) && float.IsFinite(position.Y)) ApplyTeleport(Main.player[playerIndex], position);
            }
            else if (playerIndex == Main.myPlayer)
            {
                switch (operation)
                {
                    case Operation.CastResult: CompleteRequest(reader.ReadInt32(), reader.ReadBoolean()); break;
                    case Operation.OpenDestination:
                        OpenOwnedDestination(reader.ReadInt32(), new Vector2(reader.ReadSingle(), reader.ReadSingle())); break;
                    case Operation.Close: CloseOwnedPair(playerIndex, reader.ReadInt32()); break;
                }
            }
        }
    }

    private static int FindPortal(int caster, int id, int side)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
            if (Main.projectile[i] is { active: true, ModProjectile: RecallPortalProjectile portal } p &&
                p.owner == caster && portal.PairId == id && portal.Side == side) return i;
        return -1;
    }

    private static int SpawnOwnedPortal(int caster, int id, Vector2 foot, int side, int age)
    {
        if (Main.netMode == NetmodeID.Server || caster != Main.myPlayer) return Main.maxProjectiles;
        return Projectile.NewProjectile(Main.player[caster].GetSource_Misc("RecallPortal"), foot, Vector2.Zero,
            ModContent.ProjectileType<RecallPortalProjectile>(), 0, 0, caster, id, age, side);
    }

    private static void RegisterCast(int caster, int target, int id)
    {
        int index = FindPortal(caster, id, 0);
        bool accepted = id > 0 && IsSelectable(Main.player[caster], target) &&
            Main.player[caster].HeldItem.ModItem is ElainaWand && index >= 0 &&
            Vector2.DistanceSquared(Main.projectile[index].Center, Main.player[target].Bottom) <= AreaRadius * AreaRadius &&
            Main.player[caster].GetModPlayer<RecallPortalPlayer>().CastRequestCooldown == 0 &&
            !pairs.Exists(p => p.Caster == caster && p.Id == id);
        if (accepted)
            pairs.Add(new Pair { Id = id, Caster = caster, Target = target, OriginIndex = index, Origin = Main.projectile[index].Center });
        if (Main.netMode == NetmodeID.SinglePlayer) CompleteRequest(id, accepted);
        else
        {
            ModPacket result = Packet(Operation.CastResult, caster);
            result.Write(id); result.Write(accepted); result.Send(caster);
        }
    }

    private static void CompleteRequest(int id, bool accepted)
    {
        ClientRequest request = requests.Find(r => r.Caster == Main.myPlayer && r.Id == id);
        if (request == null) return; // 超时或重复回包不再扣费。
        requests.Remove(request);
        int source = FindPortal(request.Caster, id, 0);
        bool paid = accepted && source >= 0 && (request.Pay?.Invoke() ?? true);
        if (paid)
        {
            // 新施法获准后，只有生成端关闭自己的旧门并广播 KillProjectile。
            foreach (Projectile projectile in Main.ActiveProjectiles)
                if (projectile.owner == request.Caster && projectile.ModProjectile is RecallPortalProjectile portal && portal.PairId != id)
                    projectile.Kill();
            Main.projectile[source].ai[1] = 0;
            SyncOwnedPortal(source);
        }
        else
        {
            CloseOwnedPair(request.Caster, id);
            Main.NewText(LocalizeUI("RecallCastFailed"));
        }
        if (Main.netMode == NetmodeID.SinglePlayer) CommitCast(request.Caster, id, paid);
        else
        {
            ModPacket packet = Packet(Operation.Commit, request.Caster);
            packet.Write(id); packet.Write(paid); packet.Send();
        }
    }

    private static void CommitCast(int caster, int id, bool paid)
    {
        Pair pair = pairs.Find(p => p.Caster == caster && p.Id == id);
        if (pair == null || pair.Committed) return;
        if (!paid || !HasPortal(pair, pair.OriginIndex)) { RemovePair(pairs.IndexOf(pair)); return; }
        for (int i = pairs.Count - 1; i >= 0; i--)
            if (pairs[i].Caster == caster && pairs[i] != pair) RemovePair(i);
        pair.Committed = true;
        pair.WaitingTicks = 0;
        Main.player[caster].GetModPlayer<RecallPortalPlayer>().CastRequestCooldown = 60;
    }

    private static void SyncOwnedPortal(int index)
    {
        // 回包处理不在 AI 内，不能只设 netUpdate（原版更新开头会清零它）。
        if (Main.netMode == NetmodeID.MultiplayerClient && Main.projectile[index].owner == Main.myPlayer)
            NetMessage.SendData(MessageID.SyncProjectile, number: index);
    }

    private static void OpenOwnedDestination(int id, Vector2 foot)
    {
        int caster = Main.myPlayer, source = FindPortal(caster, id, 0);
        if (source < 0 || FindPortal(caster, id, 1) >= 0 || !float.IsFinite(foot.X) || !float.IsFinite(foot.Y)) return;
        int destination = SpawnOwnedPortal(caster, id, foot, 1, DelayTicks - 1);
        if (destination >= Main.maxProjectiles) { CloseOwnedPair(caster, id); return; }
        Main.projectile[source].ai[1] = DelayTicks - 1;
        SyncOwnedPortal(source);
        if (Main.netMode == NetmodeID.SinglePlayer) ActivatePair(caster, id);
        else
        {
            ModPacket packet = Packet(Operation.DestinationReady, caster);
            packet.Write(id); packet.Send();
        }
    }

    private static void ActivatePair(int caster, int id)
    {
        Pair pair = pairs.Find(p => p.Caster == caster && p.Id == id);
        if (pair == null || !pair.Committed || !pair.DestinationRequested || pair.Activated) return;
        if (!HasPortal(pair, pair.OriginIndex) || !IsAlly(Main.player[caster], Main.player[pair.Target]))
        { RemovePair(pairs.IndexOf(pair)); return; }
        int index = FindPortal(caster, id, 1);
        if (index < 0 || Vector2.DistanceSquared(Main.projectile[index].Center, pair.Destination) > 4) return;
        pair.DestinationIndex = index;
        pair.Activated = true;
        pair.Age = DelayTicks;
        for (int p = 0; p < Main.maxPlayers; p++)
            if (IsAlly(Main.player[caster], Main.player[p]) && InArea(Main.player[p], pair.Origin))
                TeleportPlayer(Main.player[p], pair.Destination);
    }

    public override void PostUpdatePlayers()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            for (int i = requests.Count - 1; i >= 0; i--)
                if (requests[i].Caster == Main.myPlayer && ++requests[i].Age >= RequestTimeout)
                    CompleteRequest(requests[i].Id, false);
            return;
        }
        for (int i = pairs.Count - 1; i >= 0; i--)
        {
            Pair pair = pairs[i];
            if (!Main.player[pair.Caster].active || !HasPortal(pair, pair.OriginIndex) ||
                pair.Activated && !HasPortal(pair, pair.DestinationIndex))
            { RemovePair(i); continue; }
            if (!pair.Committed || pair.DestinationRequested && !pair.Activated)
            {
                if (++pair.WaitingTicks >= RequestTimeout) RemovePair(i);
                continue;
            }
            if (++pair.Age >= DelayTicks + ResidueTicks) { RemovePair(i); continue; }
            if (pair.DestinationRequested || pair.Age < DelayTicks) continue;
            Player target = Main.player[pair.Target];
            // 锚定的友军死亡、离线或换队时取消，不能误用同一玩家槽位的新状态。
            if (!IsAlly(Main.player[pair.Caster], target) || !TryGetSpawnFoot(target, out pair.Destination))
            { RemovePair(i); continue; }
            pair.DestinationRequested = true;
            pair.WaitingTicks = 0;
            if (Main.netMode == NetmodeID.SinglePlayer) OpenOwnedDestination(pair.Id, pair.Destination);
            else
            {
                ModPacket packet = Packet(Operation.OpenDestination, pair.Caster);
                packet.Write(pair.Id); packet.Write(pair.Destination.X); packet.Write(pair.Destination.Y);
                packet.Send(pair.Caster);
            }
        }
    }

    private static bool HasPortal(Pair pair, int index) => index >= 0 && index < Main.maxProjectiles &&
        Main.projectile[index] is { active: true, ModProjectile: RecallPortalProjectile portal } p &&
        p.owner == pair.Caster && portal.PairId == pair.Id;

    private static void RemovePair(int index)
    {
        Pair pair = pairs[index];
        pairs.RemoveAt(index);
        if (Main.netMode == NetmodeID.SinglePlayer) CloseOwnedPair(pair.Caster, pair.Id);
        else
        {
            ModPacket packet = Packet(Operation.Close, pair.Caster);
            packet.Write(pair.Id); packet.Send(pair.Caster);
        }
    }

    private static void CloseOwnedPair(int caster, int id)
    {
        if (Main.netMode == NetmodeID.Server || caster != Main.myPlayer) return;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == caster && projectile.ModProjectile is RecallPortalProjectile portal && portal.PairId == id)
                projectile.Kill();
    }

    private static void Travel(int playerIndex, int caster, int pairId, int side)
    {
        if (side != 0 && side != 1) return;
        Pair pair = pairs.Find(p => p.Caster == caster && p.Id == pairId);
        Player player = Main.player[playerIndex];
        if (pair == null || !pair.Activated || pair.Age >= DelayTicks + ResidueTicks ||
            !HasPortal(pair, pair.OriginIndex) || !HasPortal(pair, pair.DestinationIndex) ||
            !IsAlly(Main.player[pair.Caster], player) || player.GetModPlayer<RecallPortalPlayer>().TravelCooldown > 0 ||
            !InArea(player, side == 0 ? pair.Origin : pair.Destination)) return;
        TeleportPlayer(player, side == 0 ? pair.Destination : pair.Origin);
    }

    private static bool TryGetSpawnFoot(Player target, out Vector2 foot)
    {
        int x = target.SpawnX, y = target.SpawnY;
        bool bed = x >= 10 && x < Main.maxTilesX - 10 && y >= 10 && y < Main.maxTilesY - 10 && Player.CheckSpawn(x, y);
        foot = new Vector2((bed ? x : Main.spawnTileX) * 16 + 8, (bed ? y : Main.spawnTileY) * 16);
        if (TryFindLanding(target, foot, out Vector2 landing))
        { foot = landing + new Vector2(target.width / 2f, target.height); return true; }
        if (!bed) return false;
        foot = new Vector2(Main.spawnTileX * 16 + 8, Main.spawnTileY * 16);
        if (!TryFindLanding(target, foot, out landing)) return false;
        foot = landing + new Vector2(target.width / 2f, target.height);
        return true;
    }

    private static bool TryFindLanding(Player player, Vector2 foot, out Vector2 position)
    {
        // 端点被方块封住时只寻找附近安全空位，不破坏方块，也不把玩家塞进墙或岩浆。
        for (int up = 0; up <= 12; up++)
        for (int offset = 0; offset <= 12; offset++)
        {
            int dx = offset == 0 ? 0 : (offset + 1) / 2 * (offset % 2 == 0 ? -1 : 1);
            position = foot + new Vector2(dx * 16 - player.width / 2f, -up * 16 - player.height);
            if (position.X < 16 || position.Y < 16 || position.X + player.width >= (Main.maxTilesX - 1) * 16 ||
                position.Y + player.height >= (Main.maxTilesY - 1) * 16) continue;
            if (!Collision.SolidCollision(position, player.width, player.height) &&
                !Collision.LavaCollision(position, player.width, player.height)) return true;
        }
        position = default;
        return false;
    }

    private static void TeleportPlayer(Player player, Vector2 foot)
    {
        if (!TryFindLanding(player, foot, out Vector2 position)) return;
        if (Main.netMode == NetmodeID.Server) RemoteClient.CheckSection(player.whoAmI, position);
        ApplyTeleport(player, position);
        if (Main.netMode != NetmodeID.Server) return;
        ModPacket packet = Packet(Operation.Teleport, player.whoAmI);
        packet.Write(position.X); packet.Write(position.Y); packet.Send();
    }

    private static void ApplyTeleport(Player player, Vector2 position)
    {
        player.Teleport(position, TeleportationStyleID.RecallPotion);
        player.velocity = Vector2.Zero;
        player.fallStart = player.fallStart2 = (int)(position.Y / 16);
        player.GetModPlayer<RecallPortalPlayer>().TravelCooldown = TravelCooldownTicks;
    }

    public override void OnWorldUnload() { pairs.Clear(); requests.Clear(); nextId = 0; }
    public override void Unload() { pairs.Clear(); requests.Clear(); nextId = 0; }
}

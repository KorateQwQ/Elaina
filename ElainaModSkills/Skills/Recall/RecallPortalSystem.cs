using System.IO;
using Terraria.ID;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>
/// 施法客户端以自己的 whoAmI 同时生成两端法阵，原版 SyncProjectile 负责同步；
/// 法阵画完（年龄到 DelayTicks）时由服务器结算群体传送，右键往返也由服务器校验。服务器不登记任何配对状态。
/// </summary>
public sealed class RecallPortalSystem : ModSystem
{
    public const float SelectionRadius = 3000f;
    public const float AreaRadius = RecallPortalLayout.Radius;
    public const int DelayTicks = 90;
    public const int ResidueTicks = 30 * 60;
    public const int TravelCooldownTicks = 45;

    internal enum Operation : byte { Travel, SpawnPoint, Teleport, Flash }
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

    /// <summary>
    /// 在目标脚下和目标的“家”（床，无床时为世界出生点）同时打开一对法阵；只在施法客户端或单人模式调用。
    /// 出口锚点在施法时确定，不再追踪床的变化；传送使用排布后实际显示的阵心。
    /// </summary>
    internal static bool Cast(Player caster, int target)
    {
        if (Main.netMode == NetmodeID.Server || caster.whoAmI != Main.myPlayer || !IsSelectable(caster, target)) return false;
        if (++nextId >= 16_000_000) nextId = 1;
        int id = nextId;
        Player anchor = Main.player[target];
        int origin = Spawn(caster.whoAmI, id, anchor.Bottom, 0);
        int exit = origin < Main.maxProjectiles ? Spawn(caster.whoAmI, id, HomeFoot(anchor), 1) : Main.maxProjectiles;
        if (exit >= Main.maxProjectiles)
        {
            if (origin < Main.maxProjectiles) Main.projectile[origin].Kill();
            Main.NewText(LocalizeUI("RecallCastFailed"));
            return false;
        }
        // 每名施法者只保留一对门：新法阵建立后关闭自己的旧门（包括尚在描绘的），KillProjectile 由原版同步。
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == caster.whoAmI && projectile.ModProjectile is RecallPortalProjectile portal && portal.PairId != id)
                projectile.Kill();
        return true;
    }

    /// <summary>床脚下的坐标；出生点无效时为世界出生点。客户端可能没有加载床所在区域，因此不检查床是否还在。</summary>
    internal static Vector2 HomeFoot(Player player)
    {
        int x = player.SpawnX, y = player.SpawnY;
        bool bed = ValidSpawn(x, y);
        return new Vector2((bed ? x : Main.spawnTileX) * 16 + 8, (bed ? y : Main.spawnTileY) * 16);
    }

    private static bool ValidSpawn(int x, int y) => x >= 10 && x < Main.maxTilesX - 10 && y >= 10 && y < Main.maxTilesY - 10;

    private static int Spawn(int caster, int id, Vector2 foot, int side) =>
        // NewProjectile 必须由 owner 所在客户端调用，原版才会初始化 ai 并发送 SyncProjectile。
        Projectile.NewProjectile(Main.player[caster].GetSource_Misc("RecallPortal"), foot, Vector2.Zero,
            ModContent.ProjectileType<RecallPortalProjectile>(), 0, 0, caster, id, 0, side);

    internal static void RequestTravel(Player player, RecallPortalProjectile portal)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) { Travel(player.whoAmI, portal.Projectile.owner, portal.PairId, portal.Side); return; }
        ModPacket packet = Packet(Operation.Travel, player.whoAmI);
        packet.Write(portal.Projectile.owner);
        packet.Write(portal.PairId);
        packet.Write(portal.Side);
        packet.Send();
    }

    internal static void SendSpawnPoint(Player player, int toClient = -1, int ignoreClient = -1)
    {
        ModPacket packet = Packet(Operation.SpawnPoint, player.whoAmI);
        packet.Write(player.SpawnX);
        packet.Write(player.SpawnY);
        packet.Send(toClient, ignoreClient);
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
                case Operation.Travel: Travel(sender, reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()); break;
                case Operation.SpawnPoint:
                    ApplySpawnPoint(Main.player[sender], reader.ReadInt32(), reader.ReadInt32());
                    // 转发给其他客户端：施法者需要知道所选友军的家在哪里才能打开出口。
                    SendSpawnPoint(Main.player[sender], ignoreClient: sender);
                    break;
            }
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && playerIndex >= 0 && playerIndex < Main.maxPlayers)
        {
            switch (operation)
            {
                case Operation.Teleport:
                    Vector2 position = new(reader.ReadSingle(), reader.ReadSingle());
                    if (float.IsFinite(position.X) && float.IsFinite(position.Y)) ApplyTeleport(Main.player[playerIndex], position);
                    break;
                case Operation.SpawnPoint:
                    if (playerIndex != Main.myPlayer) ApplySpawnPoint(Main.player[playerIndex], reader.ReadInt32(), reader.ReadInt32());
                    break;
                case Operation.Flash: FlashPair(playerIndex, reader.ReadInt32()); break;
            }
        }
    }

    private static void ApplySpawnPoint(Player player, int x, int y)
    {
        bool valid = ValidSpawn(x, y);
        player.SpawnX = valid ? x : -1;
        player.SpawnY = valid ? y : -1;
    }

    internal static int FindPortal(int caster, int id, int side)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
            if (Main.projectile[i] is { active: true, ModProjectile: RecallPortalProjectile portal } p &&
                p.owner == caster && portal.PairId == id && portal.Side == side) return i;
        return -1;
    }

    /// <summary>起点法阵画完的那一帧：把阵内存活的同队友军一起传送到出口法阵。只在服务器或单人模式结算。</summary>
    internal static void Arrive(RecallPortalProjectile origin)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        Projectile projectile = origin.Projectile;
        int exit = FindPortal(projectile.owner, origin.PairId, 1);
        if (exit < 0) return;
        RecallPortalPresentation.Refresh();
        Vector2 destination = ((RecallPortalProjectile)Main.projectile[exit].ModProjectile).DisplayCenter;
        Player caster = Main.player[projectile.owner];
        for (int p = 0; p < Main.maxPlayers; p++)
            if (IsAlly(caster, Main.player[p]) && InArea(Main.player[p], projectile.Center))
                TeleportPlayer(Main.player[p], destination);
    }

    private static void Travel(int playerIndex, int caster, int pairId, int side)
    {
        if (side != 0 && side != 1 || caster < 0 || caster >= Main.maxPlayers) return;
        int from = FindPortal(caster, pairId, side), to = FindPortal(caster, pairId, 1 - side);
        RecallPortalPresentation.Refresh();
        Player player = Main.player[playerIndex];
        if (from < 0 || to < 0 || !((RecallPortalProjectile)Main.projectile[from].ModProjectile).Ready ||
            !((RecallPortalProjectile)Main.projectile[to].ModProjectile).Ready ||
            !IsAlly(Main.player[caster], player) || player.GetModPlayer<RecallPortalPlayer>().TravelCooldown > 0 ||
            !RecallPortalPresentation.InReach(player, (RecallPortalProjectile)Main.projectile[from].ModProjectile)) return;
        if (!TeleportPlayer(player, ((RecallPortalProjectile)Main.projectile[to].ModProjectile).DisplayCenter)) return;
        // 传送特效：两端各重放一次画完时的向上爆发。
        FlashPair(caster, pairId);
        if (Main.netMode != NetmodeID.Server) return;
        ModPacket packet = Packet(Operation.Flash, caster);
        packet.Write(pairId); packet.Send();
    }

    private static void FlashPair(int caster, int pairId)
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == caster && projectile.ModProjectile is RecallPortalProjectile portal && portal.PairId == pairId)
                portal.Flash();
    }

    private static bool TryFindLanding(Player player, Vector2 foot, out Vector2 position)
    {
        // 落点被方块封住时只寻找附近安全空位，不破坏方块，也不把玩家塞进墙或岩浆；找不到就不传送。
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

    private static bool TeleportPlayer(Player player, Vector2 foot)
    {
        if (!TryFindLanding(player, foot, out Vector2 position)) return false;
        if (Main.netMode == NetmodeID.Server) RemoteClient.CheckSection(player.whoAmI, position);
        ApplyTeleport(player, position);
        if (Main.netMode != NetmodeID.Server) return true;
        ModPacket packet = Packet(Operation.Teleport, player.whoAmI);
        packet.Write(position.X); packet.Write(position.Y); packet.Send();
        return true;
    }

    private static void ApplyTeleport(Player player, Vector2 position)
    {
        if (Main.netMode != NetmodeID.Server && player.whoAmI == Main.myPlayer)
            player.GetModPlayer<RecallPortalPlayer>().Transition.Begin(Vector2.Distance(player.position, position));
        player.Teleport(position, TeleportationStyleID.RecallPotion);
        player.velocity = Vector2.Zero;
        player.fallStart = player.fallStart2 = (int)(position.Y / 16);
        player.GetModPlayer<RecallPortalPlayer>().TravelCooldown = TravelCooldownTicks;
    }

    public override void OnWorldUnload() { nextId = 0; RecallPortalPresentation.Clear(); }
    public override void Unload()
    {
        nextId = 0;
        RecallPortalPresentation.Clear();
        Main.QueueMainThreadAction(RecallPortalDrawing.Unload);
    }
}

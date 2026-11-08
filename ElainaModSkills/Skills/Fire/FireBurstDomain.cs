using System;
using System.Collections.Generic;
using KL.DamageSystem;
using KL.Drawing;
using KL.ActionsSystem;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>
/// 炎爆领域：固定在施放位置，持续期间由拥有端以略带随机的间隔持续降下炎爆：
/// 圈内有敌人时轮流锁定其脚下，敌人越少落下越慢（单体按目标 DPS 换算频率）；没有敌人时随机轰击圈内地面。
/// 圈内敌人首次被点燃时（含点燃时被击杀）由服务器延长持续时间，不论点燃者与领域归属；同一玩家再次施放会替换旧领域。
/// ai[0]：累计延长的帧数；ai[1]：已存在帧数；ai[2]：拥有端确认的射线维持状态。
/// </summary>
public sealed class FireBurstDomain : ModProjectile
{
    // 可调参数写成表达式属性，便于热重载。
    public static float Radius => 1500f;    // 领域与火环半径（像素），锁定、点燃延长与绘制都实时读取
    public static int BaseTicks => 600;
    public static int ExtendTicks => 60;
    public static int MaxRemainingTicks => 900;

    public override string Texture => "伊蕾娜/ElainaModSkills/Skills/Fire/FireBurstProj";

    private float ExtendedTicks => Projectile.ai[0];
    private int Age => (int)Projectile.ai[1];
    private int TotalTicks => BaseTicks + (int)ExtendedTicks;
    internal int RemainingTicks => Math.Clamp(TotalTicks - Age, 0, MaxRemainingTicks);

    internal static FireBurstDomain FindContainingOwner(Player player)
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == player.whoAmI && projectile.ModProjectile is FireBurstDomain domain
                && domain.RemainingTicks > 0 && domain.Contains(player.Center))
                return domain;
        return null;
    }

    // 仅拥有端使用：每个 NPC 槽位上次被锁定的帧，以及距下一次落下的剩余帧数。
    private readonly Dictionary<int, uint> lastLocked = new();
    // 距下一次落下的剩余量：每帧按当前频率倍率扣减，圈内敌人数变化时立即生效。
    private float strikeCountdown;
    // 仅拥有端使用（伤害只在拥有端结算）：施放时换算好的单体倍率与冻结加成，传给每道炎爆。
    private float singleMultiplier = 1f, frozenBonus;
    // 仅拥有端使用：圈内只有一名敌人时每秒落下的炎爆数，由施放时按目标 DPS 换算；未设置时按满频率。
    private float singleTargetStrikesPerSecond = float.MaxValue;

    internal void SetDamageMultipliers(float single, float frozen)
    {
        singleMultiplier = single;
        frozenBonus = frozen;
    }

    internal void SetSingleTargetStrikeRate(float strikesPerSecond) => singleTargetStrikesPerSecond = strikesPerSecond;
    private Action drawRing;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.aiStyle = -1;
        Projectile.penetrate = -1;
        Projectile.timeLeft = BaseTicks;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.netImportant = true;
        Projectile.hide = true;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public bool Contains(Vector2 point) => Vector2.DistanceSquared(Projectile.Center, point) <= Radius * Radius;

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles,
        List<int> overPlayers, List<int> overWiresUI)
    {
        if (Main.dedServ) return;
        // 范围圈与炎爆同在 Projectiles.After 的 Bloom 组内，排在炎爆法阵与火柱之前。
        drawRing ??= DrawRing;
        LayerDrawRequestSystem.RequestBloom("Elaina:FireBurstDomain:Ring", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, drawRing, order: LayerDrawRequestSystem.TrailOrder - 1);
    }

    private void DrawRing()
    {
        if (!Projectile.active) return;
        Vector2 screenCenter = Projectile.Center - Main.screenPosition;
        // 整圈离屏时跳过（火舌最长约 120 像素）
        Vector2 nearest = Vector2.Clamp(screenCenter, Vector2.Zero, new Vector2(Main.screenWidth, Main.screenHeight));
        float reach = Radius + 160f;
        if (Vector2.DistanceSquared(nearest, screenCenter) > reach * reach) return;
        Matrix transform = Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
        DrawHelper.EndBeginDraw();
        FireBurstDomainRingDrawing.Draw(Main.graphics.GraphicsDevice, transform, screenCenter, Radius, Age, RemainingTicks);
        DrawHelper.EndBeginDraw();
    }

    public override void OnSpawn(IEntitySource source)
    {
        // 类似传送法阵：同一玩家只保留最新的领域，其他玩家的领域不受影响。
        if (Projectile.owner != Main.myPlayer) return;
        foreach (Projectile other in Main.ActiveProjectiles)
            if (other.whoAmI != Projectile.whoAmI && other.owner == Projectile.owner && other.ModProjectile is FireBurstDomain)
                other.Kill();
    }

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        if (!Main.player[Projectile.owner].active)
        {
            Projectile.Kill();
            return;
        }
        UpdateLifetime();
        int remaining = RemainingTicks;
        if (remaining <= 0)
        {
            Projectile.Kill();
            return;
        }
        // 原版会在本次 AI 后扣减 timeLeft；保留最后一帧，让维持状态有机会续上。
        Projectile.timeLeft = Math.Max(2, remaining);
        if (Projectile.owner != Main.myPlayer) return;

        // 展开时在中心下方的地面先降下一次炎爆，标示领域位置；下方没有地面时落在中心。
        if (Age == 1)
        {
            Vector2 center = Projectile.Center;
            if (!FireBurstSkillHelper.GetValidPositionForFireBurst(ref center, Radius) || !Contains(center))
                center = Projectile.Center;
            SpawnStrike(center);
            strikeCountdown = Main.rand.Next(12, 25);
        }
        UpdateStrikes();
    }

    internal void UpdateLifetime()
    {
        if (Projectile.owner == Main.myPlayer)
        {
            Player owner = Main.player[Projectile.owner];
            bool sustaining = Contains(owner.Center)
                && owner.GetModPlayer<ActionModPlayer>().CurrentAnimAction is Action_FireLaser { SustainsDomain: true };
            float state = sustaining ? 1f : 0f;
            if (Projectile.ai[2] != state)
            {
                Projectile.ai[2] = state;
                Projectile.netUpdate = true;
            }
            // 同步剩余时间与维持状态；远端在同步之间按游戏 tick 推进，无需每帧发包。
            if (sustaining && Age % 60 == 0) Projectile.netUpdate = true;
        }
        Projectile.ai[1]++;
        if (Projectile.ai[2] == 1f) Extend(1);
        else Extend(0);
    }

    private void UpdateStrikes()
    {
        // 间隔在两者之间随机取值（帧），每次落下后重新抽取
        int minStrikeInterval = 30;
        int maxStrikeInterval = 45;
        int maxActiveStrikes = 12;  // 同时存在的炎爆上限（性能）
        int fullRateEnemies = 5;    // 圈内敌人达到该数量时按上面的间隔落下；更少时降低频率
        float leadTicks = 10f;      // 预判帧数：炎爆出现后 30 帧爆发且不追踪，只预判一部分，高速移动的敌人仍能躲开

        strikeCountdown -= GetStrikeRate(CountTargets(), minStrikeInterval, maxStrikeInterval, fullRateEnemies);
        if (strikeCountdown > 0) return;
        strikeCountdown += Main.rand.Next(minStrikeInterval, maxStrikeInterval + 1);

        int strikeType = ModContent.ProjectileType<FireBurstProj>();
        int active = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.type == strikeType) active++;
        if (active >= maxActiveStrikes) return;

        NPC target = PickTarget();
        if (target != null)
        {
            lastLocked[target.whoAmI] = Main.GameUpdateCount;
            // 落点在敌人脚下，按其当前速度少量预判；冻结、时停等定身状态不预判。
            SpawnStrike(PredictiveAim.GetLeadPoint(target, target.Bottom, leadTicks));
            return;
        }
        // 圈内没有敌人时随机轰击圈内地面；找不到地面（如悬空、整圈埋在实心物块里）则本次不落。
        if (TryFindGroundPoint(out Vector2 ground))
            SpawnStrike(ground);
    }

    /// <summary>
    /// 相对满频率的落下速度倍率：没有或只有一名敌人时为单体目标频率，敌人数达到 fullRateEnemies 时为 1，其间线性过渡。
    /// </summary>
    private float GetStrikeRate(int targets, int minInterval, int maxInterval, int fullRateEnemies)
    {
        float fullStrikesPerSecond = 60f * 2f / (minInterval + maxInterval);
        float singleRate = Math.Min(1f, singleTargetStrikesPerSecond / fullStrikesPerSecond);
        float progress = Math.Clamp((targets - 1f) / Math.Max(1, fullRateEnemies - 1), 0f, 1f); // 0 名敌人截为 0，同单体
        return MathHelper.Lerp(singleRate, 1f, progress);
    }

    /// <summary>圈内可锁定的敌人；蠕虫等共享血量的体节只算主体。</summary>
    private bool IsTarget(NPC npc) => npc.CanBeChasedBy(Projectile)
        && (npc.realLife < 0 || npc.realLife == npc.whoAmI) && Contains(npc.Center);

    private int CountTargets()
    {
        int count = 0;
        foreach (NPC npc in Main.ActiveNPCs)
            if (IsTarget(npc)) count++;
        return count;
    }

    /// <summary>圈内可攻击的敌人中优先最久未被锁定的，同等情况下随机挑选。</summary>
    private NPC PickTarget()
    {
        NPC best = null;
        uint bestLocked = uint.MaxValue;
        int ties = 0;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsTarget(npc)) continue;
            uint locked = lastLocked.TryGetValue(npc.whoAmI, out uint tick) ? tick : 0;
            if (locked > bestLocked) continue;
            if (locked < bestLocked)
            {
                best = npc;
                bestLocked = locked;
                ties = 1;
            }
            else if (Main.rand.Next(++ties) == 0)
                best = npc;
        }
        return best;
    }

    /// <summary>在圈内随机取点，向下找到第一块实心物块的表面作为落点。</summary>
    private bool TryFindGroundPoint(out Vector2 point)
    {
        int attempts = 8;       // 每次落下最多尝试的随机点数
        int maxClimbTiles = 30; // 起点埋在物块里时最多向上找多少格
        for (int i = 0; i < attempts; i++)
        {
            Vector2 sample = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius);
            // 起点在实心物块内时先上移出物块，避免落点埋在地下。
            int climb = 0;
            while (FireBurstSkillHelper.IsSolidBrick(Framing.GetTileSafely(sample)) && climb++ < maxClimbTiles)
                sample.Y -= 16f;
            if (climb > maxClimbTiles) continue;
            if (!FireBurstSkillHelper.GetValidPositionForFireBurst(ref sample, Radius * 2f)) continue;
            if (!Contains(sample)) continue;
            point = sample;
            return true;
        }
        point = default;
        return false;
    }

    private void SpawnStrike(Vector2 position)
    {
        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, Vector2.Zero,
            ModContent.ProjectileType<FireBurstProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        ((FireBurstProj)Main.projectile[index].ModProjectile).SetDamageMultipliers(singleMultiplier, frozenBonus);
    }

    /// <summary>服务器（单人为本地）确认点燃后，由拥有者的 <see cref="FireCyclePlayer"/> 广播到各端调用。</summary>
    internal void Extend(int ticks)
    {
        // 上限约束剩余时间，连续维持可以超过 15 秒的总存活时间。
        int remaining = (int)Math.Clamp((long)TotalTicks - Age + Math.Max(0, ticks), 0, MaxRemainingTicks);
        Projectile.ai[0] = Age + remaining - BaseTicks;
    }
}

/// <summary>
/// 圈内敌人首次被点燃时延长所有包含它的领域；点燃只在服务器或单人端结算，点燃时被击杀也会触发。
/// 小动物与弹幕类敌人（同原版，生命上限不超过 5）不计。
/// </summary>
public sealed class FireBurstDomainSystem : ModSystem
{
    public override void Load() => FireBurnSystem.Ignited += OnIgnited;
    public override void Unload() => FireBurnSystem.Ignited -= OnIgnited;

    private static void OnIgnited(NPC npc, int player)
    {
        if (npc.lifeMax <= 5) return;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.ModProjectile is FireBurstDomain domain && domain.Contains(npc.Center))
                Main.player[projectile.owner].GetModPlayer<FireCyclePlayer>()
                    .BroadcastDomainExtension(projectile.identity, FireBurstDomain.ExtendTicks);
    }
}

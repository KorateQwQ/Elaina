using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 浮游玩偶共用的跟随、传送、挖掘、远程放置物块和药草照料。
/// 子类提供 TexturePrefix 和各动作帧数，可覆写移动参数或在 base.AI() 后添加功能。
/// 资源为 &lt;prefix&gt;_Idle_N、_Move_N、_Mine_N、_Use_N、_Hammer_N（N 从 1 开始），各帧以贴图中心对齐，尺寸可以不同。
/// ai[0]：浮动周期；ai[1]：待机 0 / 移动 1 / 挖矿 2 / 通用动作 3 / 敲墙 4 / 收回中 5；ai[2]：动画（收回时为收回）计时。三项随弹幕同步。
/// localAI[0]：本端出现计时，召唤、晚加入与瞬移后按编队依次从星芒中弹出（<see cref="DollSummonVisuals"/>）。
/// 有挖掘标记时，主人端从 <see cref="DollCommandPlayer"/> 选出目标格并经 ExtraAI 同步，各端据此飞到格子旁播放挖矿动画；
/// 敲击只由主人端执行。任务结束后回到主人身边，再把收集物抛还给主人。
/// </summary>
public abstract class DollPetProjectile : ModProjectile
{
    protected abstract string TexturePrefix { get; }
    protected abstract int MoveFrameCount { get; }
    protected virtual int IdleFrameCount => 1;
    /// <summary>挖掘标记时播放的帧数；为 0 时挖掘期间沿用待机帧。</summary>
    protected virtual int MineFrameCount => 0;
    protected virtual int HammerFrameCount => 0;
    protected virtual int UseFrameCount => 0;
    protected virtual int TicksPerUseFrame => 6;
    protected virtual int TicksPerIdleFrame => 12;
    protected virtual int TicksPerMoveFrame => 8;
    protected virtual int TicksPerMineFrame => 6;
    /// <summary>四个独立玩偶类型使用固定编队位置；类型随原版弹幕同步。</summary>
    public virtual int FormationSlot => 0;
    /// <summary>错落的松散队形，编号越大越靠后，避免排成方阵。</summary>
    private static readonly Vector2[] FormationOffsets = { new(40f, -34f), new(82f, -10f), new(84f, -62f), new(122f, -36f) };
    protected virtual Vector2 FollowOffset => FormationOffsets[FormationSlot % FormationOffsets.Length];
    protected virtual float TeleportDistance => 1200f;
    protected virtual float FollowSpeed => 12f;
    /// <summary>靠后的玩偶回位更慢、跟随主人速度的比例更低，移动时自然拉成一串。</summary>
    protected virtual float FollowResponsiveness => 0.11f - FormationSlot * 0.008f;
    protected virtual float FollowFeedforward => 0.8f - FormationSlot * 0.05f;
    /// <summary>
    /// 主人转身后各只依次换边，从头顶以各自高度绕过去，而不是同时翻面、挤成一团。
    /// 后排先走、前排最后走，路径不会穿过已到位同伴的落点；同一时长内远处的飞得更快。
    /// </summary>
    protected virtual int SideSwapDelay => (FormationOffsets.Length - 1 - FormationSlot % FormationOffsets.Length) * 6;
    protected virtual float SideSwapSpeed => 1f / 30f;
    private static readonly float[] SideSwapArcs = { 18f, 34f, 20f, 30f };
    protected virtual float SideSwapArc => SideSwapArcs[FormationSlot % SideSwapArcs.Length];
    protected virtual float WanderRadius => 7f;
    protected virtual float SeparationDistance => 42f;
    protected virtual float VelocitySmoothing => 0.16f;
    protected virtual float MovingBobHeight => 4f;
    protected virtual float IdleBobHeight => 1.5f;
    protected virtual int BobPeriod => 90;
    protected virtual bool SpriteFacesLeft => true;
    protected virtual float WorkSpeed => 16f;
    /// <summary>挖掘时停在目标格侧上方的偏移，X 取朝向主人的一侧。</summary>
    protected virtual Vector2 WorkOffset => new(22f, -6f);
    /// <summary>回到主人身边多近才交还收集物；略大于最近一只（编号 0）跟随位置到主人中心的距离（约 52，含漂移不超过 65）。</summary>
    protected virtual float DeliverDistance => 80f;

    private const int IdleState = 0, MoveState = 1, MineState = 2, UseState = 3, HammerState = 4, DismissState = 5;
    private Asset<Texture2D>[] idleTextures, moveTextures, mineTextures, useTextures, hammerTextures;
    private Point workTile = new(-1, -1);
    private bool workWall;
    private bool workPlace;
    private bool workGarden;
    private bool workChest, chestWaiting;
    /// <summary>送货：把主人放入的物品送进另一只玩偶开着的箱子。</summary>
    private bool workDeposit;
    /// <summary>主人端头顶显示的箱子携带物类型；不同步，其他端只看到玩偶停在箱子旁。</summary>
    private int carryType;
    private int hitTimer;
    private byte skillLevel = 1;
    internal int SkillLevel => skillLevel;
    private int ActionSpeedPercent => 100 + DollControlProgression.ActionSpeedBonusPercent(skillLevel);

    internal void SetSkillLevel(int level)
    {
        byte next = (byte)DollControlProgression.ClampLevel(level);
        if (skillLevel == next) return;
        skillLevel = next;
        Projectile.netUpdate = true;
    }
    // 跟随表现只在本地平滑，不同步；位置仍由周期性 netUpdate 纠偏。
    private float followSide, committedSide;
    private int sideSwapWait;
    private bool followSideReady;
    private float wanderTime;
    private Vector2 lastCenter;
    private bool hasLastCenter, vanishBurst;
    public bool IsMoving => Projectile.ai[1] == MoveState;
    public bool IsMining => Projectile.ai[1] == MineState;
    public bool IsHammering => Projectile.ai[1] == HammerState;
    public bool IsUsing => Projectile.ai[1] == UseState;
    public bool HasWork => workTile.X >= 0;
    /// <summary>空闲或只在照料种植盆：照料优先级最低，可以随时被开箱等指令叫走。</summary>
    internal bool IsInterruptible => !HasWork || workGarden;
    public bool IsDismissing => Projectile.ai[1] == DismissState;
    private float AppearAge => Projectile.localAI[0] - FormationSlot * DollSummonVisuals.StaggerTicks;
    /// <summary>收回时后排先走：编号越大越早开始。</summary>
    private float VanishAge => Projectile.ai[2] - (DollControlProgression.DollCount(skillLevel) - 1 - FormationSlot) * DollSummonVisuals.StaggerTicks;
    internal bool IsOpeningChest => workChest;
    public override string Texture => TexturePrefix + "_Idle_1";

    public override void SetDefaults()
    {
        Projectile.width = 24;
        Projectile.height = 32;
        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        // 不易被弹幕上限顶掉，晚加入的玩家也会收到它；每帧续命，直到主人收回、死亡或离开。
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
        // 无伤害、无召唤栏占用，也不替换原版宠物。
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;

    protected virtual bool CanStayActive(Player owner) => owner.active && !owner.dead;

    /// <summary>主人身后的跟随点：随 followSide 在两侧间过渡（经过头顶时抬高），再叠加每只独立的缓慢漂移。</summary>
    protected virtual Vector2 GetFollowAnchor(Player owner)
    {
        if (!followSideReady) ResetFollowSide(owner);
        float seed = FormationSlot * 1.7f + 0.4f;
        Vector2 wander = new(
            MathF.Sin(wanderTime * 0.021f + seed * 2.3f) * WanderRadius + MathF.Sin(wanderTime * 0.047f + seed) * WanderRadius * 0.45f,
            MathF.Sin(wanderTime * 0.017f + seed * 3.1f) * WanderRadius * 0.65f + MathF.Cos(wanderTime * 0.039f + seed * 1.3f) * WanderRadius * 0.3f);
        float y = FollowOffset.Y - (1f - followSide * followSide) * SideSwapArc + wander.Y;
        return owner.Center + new Vector2(followSide * FollowOffset.X + wander.X, owner.gravDir * y);
    }

    private void ResetFollowSide(Player owner)
    {
        followSide = committedSide = -owner.direction;
        sideSwapWait = 0;
        followSideReady = true;
    }

    private void UpdateFollowMotion(Player owner)
    {
        wanderTime++;
        if (!followSideReady) ResetFollowSide(owner);
        float targetSide = -owner.direction;
        // 主人短暂回头又转回时不必折返：延迟期内方向复原就取消这次换边。
        if (targetSide != committedSide)
        {
            if (++sideSwapWait > SideSwapDelay)
            {
                committedSide = targetSide;
                sideSwapWait = 0;
            }
        }
        else sideSwapWait = 0;
        followSide = MathHelper.Clamp(followSide + Math.Sign(committedSide - followSide) * SideSwapSpeed, -1f, 1f);
        if (Math.Abs(committedSide - followSide) < SideSwapSpeed) followSide = committedSide;
    }

    /// <summary>与同主人的其他玩偶保持一点距离，避免漂移时叠在一起。</summary>
    private Vector2 GetSeparation()
    {
        Vector2 push = Vector2.Zero;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile other = Main.projectile[i];
            if (ReferenceEquals(other, Projectile) || !other.active || other.owner != Projectile.owner ||
                other.ModProjectile is not DollPetProjectile)
                continue;
            Vector2 away = Projectile.Center - other.Center;
            float distance = away.Length();
            if (distance >= SeparationDistance) continue;
            away = distance < 0.01f ? new Vector2(FormationSlot - 1.5f, -1f) : away / distance;
            push += away * (1f - distance / SeparationDistance) * 6f;
        }
        return push;
    }

    public static DollPetProjectile FindOwned(Player owner)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active && projectile.owner == owner.whoAmI && projectile.ModProjectile is DollPetProjectile { IsDismissing: false } doll)
                return doll;
        }
        return null;
    }

    public static void DismissAll(Player owner)
    {
        if (owner.whoAmI != Main.myPlayer) return;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active && projectile.owner == owner.whoAmI && projectile.ModProjectile is DollPetProjectile doll)
                doll.Dismiss();
        }
    }

    /// <summary>由 owner 调用；召唤和距离过远共用同一重置与同步入口。</summary>
    public void RecallToOwner(Player owner)
    {
        ResetFollowSide(owner);
        Projectile.Center = GetFollowAnchor(owner);
        Projectile.velocity = Vector2.Zero;
        Projectile.ai[0] = Projectile.ai[1] = Projectile.ai[2] = 0f;
        // 保持整帧相位，确保每只都能命中 ai[0] == 0 的周期同步点。
        Projectile.ai[0] = FormationSlot * Math.Max(1, BobPeriod) / DollControlSkill.TeamSize;
        Projectile.frame = Projectile.frameCounter = 0;
        Projectile.direction = Projectile.spriteDirection = owner.direction;
        Projectile.netUpdate = true;
    }

    /// <summary>收回玩偶：先把收集物抛还给主人、放下手头任务，再依次收缩化作星屑后移除。由主人端调用。</summary>
    public void Dismiss()
    {
        if (Projectile.owner != Main.myPlayer || IsDismissing) return;
        DollCommandPlayer commands = Main.player[Projectile.owner].GetModPlayer<DollCommandPlayer>();
        commands.DeliverLoot(Projectile.Center, Projectile.GetSource_FromThis(), this);
        // 玩偶可能还远在箱子旁，箱子物品与未送达的放入物直接从主人身上交还。
        commands.ReturnDeposits(this);
        commands.DeliverChestCargo(this, Main.player[Projectile.owner].Center, Projectile.GetSource_FromThis());
        commands.ReleaseTarget(workTile, workWall || workGarden, workPlace || workGarden, this);
        workTile = new Point(-1, -1);
        workWall = workPlace = workGarden = workChest = chestWaiting = workDeposit = false;
        SetState(DismissState);
    }

    /// <summary>各端独立推进：出现计时到点时爆出星芒；可见的瞬移（远距离回到主人身边）在原地消散、新位置重新出现。</summary>
    private void UpdateSummonVisuals()
    {
        if (hasLastCenter && !IsDismissing &&
            Vector2.DistanceSquared(lastCenter, Projectile.Center) > DollSummonVisuals.TeleportJump * DollSummonVisuals.TeleportJump)
        {
            DollSummonDust.Burst(lastCenter, true);
            Projectile.localAI[0] = FormationSlot * DollSummonVisuals.StaggerTicks;
        }
        lastCenter = Projectile.Center;
        hasLastCenter = true;
        if (AppearAge == 0) DollSummonDust.Burst(Projectile.Center, false);
        Projectile.localAI[0] = Math.Min(Projectile.localAI[0] + 1, 100000);
    }

    /// <summary>收回中：停下、等轮到自己后收缩泛白，缩成星点时爆出星屑，主人端随后移除。</summary>
    private void DismissAI(bool isOwner)
    {
        Projectile.velocity *= 0.85f;
        Projectile.ai[2]++;
        if (!vanishBurst && VanishAge >= DollSummonVisuals.VanishTicks - DollSummonVisuals.VanishBurstLead)
        {
            vanishBurst = true;
            DollSummonVisuals.Appearance(AppearAge, VanishAge, out _, out _, out _, out Vector2 offset);
            DollSummonDust.Burst(Projectile.Center + offset * GravDir, true);
        }
        if (isOwner && VanishAge >= DollSummonVisuals.VanishTicks) Projectile.Kill();
    }

    private float GravDir => Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers ? Main.player[Projectile.owner].gravDir : 1f;

    public override void OnKill(int timeLeft)
    {
        // 主人死亡、离开或其他端提前收到移除时，也以星屑消散，而不是直接消失。
        if (!vanishBurst)
        {
            vanishBurst = true;
            DollSummonDust.Burst(Projectile.Center, true);
        }
        if (Projectile.owner == Main.myPlayer && Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            Main.player[Projectile.owner].GetModPlayer<DollCommandPlayer>()
                .ReleaseTarget(workTile, workWall || workGarden, workPlace || workGarden, this);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((short)workTile.X);
        writer.Write((short)workTile.Y);
        writer.Write(workWall);
        writer.Write(workPlace);
        writer.Write(workGarden);
        writer.Write(workChest);
        writer.Write(chestWaiting);
        writer.Write(workDeposit);
        writer.Write(skillLevel);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        workTile = new Point(reader.ReadInt16(), reader.ReadInt16());
        workWall = reader.ReadBoolean();
        workPlace = reader.ReadBoolean();
        workGarden = reader.ReadBoolean();
        workChest = reader.ReadBoolean();
        chestWaiting = reader.ReadBoolean();
        workDeposit = reader.ReadBoolean();
        // 远端没有主人的技能存档，工作动画使用弹幕同步来的等级。
        skillLevel = (byte)DollControlProgression.ClampLevel(reader.ReadByte());
    }

    public override void AI()
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
        {
            Projectile.Kill();
            return;
        }
        Player owner = Main.player[Projectile.owner];
        if (!CanStayActive(owner))
        {
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 2;
        bool isOwner = Projectile.owner == Main.myPlayer;
        UpdateSummonVisuals();
        if (IsDismissing)
        {
            DismissAI(isOwner);
            return;
        }
        DollCommandPlayer commands = owner.GetModPlayer<DollCommandPlayer>();
        if (isOwner) UpdateWorkTarget(commands);
        if (HasWork)
        {
            WorkAI(owner, commands, isOwner);
            return;
        }
        hitTimer = 0;

        if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > TeleportDistance * TeleportDistance && isOwner)
        {
            RecallToOwner(owner);
            return;
        }
        // 附近任务结束或远处任务暂停后，回到主人身边交还挖到的物品。
        if (isOwner && commands.HasLoot && !commands.HasNearbyTask &&
            Vector2.DistanceSquared(Projectile.Center, owner.Center) < DeliverDistance * DeliverDistance)
            commands.DeliverLoot(Projectile.Center, Projectile.GetSource_FromThis(), this);
        // 从箱子取回的物品不必等其他任务结束，带货玩偶一回来就交还。
        if (isOwner && commands.CanDeliverCargo(this) &&
            Vector2.DistanceSquared(Projectile.Center, owner.Center) < DeliverDistance * DeliverDistance)
            commands.DeliverChestCargo(this, Projectile.Center, Projectile.GetSource_FromThis());

        UpdateFollowMotion(owner);
        Vector2 anchor = GetFollowAnchor(owner);
        float anchorDistance = Vector2.Distance(Projectile.Center, anchor);
        // 浮动本身不触发移动动画。起停使用不同阈值，避免靠近玩家时反复切帧。
        bool moving = owner.velocity.LengthSquared() > 0.25f ||
            (IsMoving ? anchorDistance > 8f || Projectile.velocity.LengthSquared() > 1f : anchorDistance > 24f);
        SetState(moving ? MoveState : IdleState);

        Projectile.ai[0] = (Projectile.ai[0] + 1f) % Math.Max(1, BobPeriod);
        float phase = Projectile.ai[0] / Math.Max(1, BobPeriod) * MathHelper.TwoPi;
        float bob = MathF.Sin(phase) * (IsMoving ? MovingBobHeight : IdleBobHeight);
        Vector2 target = anchor + new Vector2(0f, bob * owner.gravDir);
        // 只部分跟随主人速度，剩余靠位置回拉：起步时落后、停下时滑过一点再回位。
        Vector2 desiredVelocity = (target - Projectile.Center) * FollowResponsiveness + owner.velocity * FollowFeedforward + GetSeparation();
        float maxSpeed = Math.Max(FollowSpeed, owner.velocity.Length() + 4f);
        if (desiredVelocity.LengthSquared() > maxSpeed * maxSpeed)
            desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, VelocitySmoothing);

        if (Math.Abs(Projectile.velocity.X) > 0.3f)
            Projectile.direction = Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
        else if (!IsMoving)
            Projectile.direction = Projectile.spriteDirection = owner.direction;

        Animate();
        // 周期性纠偏也把浮动相位与动画位置发给其他客户端、晚加入的玩家。
        if (isOwner && Projectile.ai[0] == 0f)
            Projectile.netUpdate = true;
    }

    private void UpdateWorkTarget(DollCommandPlayer commands)
    {
        Point previous = workTile;
        bool previousWall = workWall;
        bool previousPlace = workPlace;
        bool previousGarden = workGarden;
        bool previousChest = workChest, previousWaiting = chestWaiting, previousDeposit = workDeposit;
        commands.ReleaseTarget(previous, previousWall || previousGarden, previousPlace || previousGarden, this);
        workGarden = false;
        workChest = commands.TrySelectRemoteChest(this, out Point chestTarget);
        chestWaiting = workChest && commands.RemoteChestActionFinished;
        workDeposit = !workChest && commands.TrySelectChestDeposit(this, out chestTarget);
        carryType = commands.CarryType(this);
        if (workChest || workDeposit)
        {
            workTile = chestTarget;
            workWall = workPlace = false;
        }
        else if (carryType > 0 || commands.HasMiningCargo(this) && !commands.HasNearbyTask)
        {
            // 带着箱子里的物品时先回主人身边交还，不接新的挖掘或放置任务。
            workTile = new Point(-1, -1);
            workWall = workPlace = false;
        }
        else
        {
            workPlace = commands.TrySelectPlacementTarget(Projectile.Center,
                previousPlace ? previous : new Point(-1, -1), out workTile, this);
            if (workPlace) workWall = false;
            else if (!commands.TrySelectTarget(Projectile.Center, previousPlace || previousGarden ? new Point(-1, -1) : previous,
                previousWall, out workTile, out workWall, this))
            {
                workGarden = commands.TrySelectGardenTarget(Projectile.Center, previousGarden ? previous : new Point(-1, -1), out workTile, this);
                workWall = false;
                if (!workGarden) workTile = new Point(-1, -1);
            }
        }
        if (workTile != previous || workWall != previousWall || workPlace != previousPlace || workGarden != previousGarden
            || workChest != previousChest || chestWaiting != previousWaiting || workDeposit != previousDeposit)
        {
            hitTimer = 0;
            Projectile.netUpdate = true;
        }
    }

    /// <summary>飞到目标格朝向主人的一侧，到位后面向格子播放挖矿动画；主人端按工具节奏敲击。</summary>
    private void WorkAI(Player owner, DollCommandPlayer commands, bool isOwner)
    {
        Vector2 tileCenter = workTile.ToWorldCoordinates() - (workGarden ? new Vector2(0, 16) : Vector2.Zero)
            + (workChest || workDeposit ? new Vector2(8, 8) : Vector2.Zero);
        float side = tileCenter.X > owner.Center.X ? -1f : 1f;
        // 开箱玩偶守在朝向主人的一侧，送货玩偶停在另一侧，两只不叠在一起。
        if (workDeposit) side = -side;
        Vector2 spot = tileCenter + new Vector2(side * WorkOffset.X, WorkOffset.Y * owner.gravDir);
        Vector2 toSpot = spot - Projectile.Center;
        float distance = toSpot.Length();
        bool arrived = distance < 14f;
        Vector2 desired = arrived ? toSpot * 0.2f
            : toSpot.SafeNormalize(Vector2.Zero) * Math.Min(WorkSpeed, distance * 0.2f + 2f);
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.2f);

        if (arrived)
            Projectile.direction = Projectile.spriteDirection = -(int)side;
        else if (Math.Abs(Projectile.velocity.X) > 0.3f)
            Projectile.direction = Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);

        int previousState = (int)Projectile.ai[1];
        SetState(!arrived ? MoveState : chestWaiting ? IdleState : workPlace || workGarden || workChest || workDeposit
            ? UseFrameCount > 0 ? UseState : IdleState
            : workWall && HammerFrameCount > 0 ? HammerState
            : MineFrameCount > 0 ? MineState : IdleState);
        if ((workChest || workDeposit) && isOwner && previousState != (int)Projectile.ai[1]) Projectile.netUpdate = true;
        Animate();

        if (!isOwner) return;
        if (!arrived)
        {
            hitTimer = 0;
            return;
        }
        if (chestWaiting) { hitTimer = 0; return; }
        int interval = (workPlace || workGarden || workChest || workDeposit
            ? Math.Max(12, UseFrameCount * TicksPerUseFrame) : commands.HitInterval(workWall)) * 100;
        // 使用百分比整数累计工作量，保留余量；满级 +200% 速度即原用时 / 3。
        hitTimer += ActionSpeedPercent;
        if (hitTimer >= interval)
        {
            hitTimer -= interval;
            if (workChest) commands.CompleteRemoteChestGesture(this);
            else if (workDeposit) commands.CompleteChestDeposit(this);
            else if (workGarden) commands.TendGarden(workTile);
            else if (workPlace) commands.CompletePlacement(workTile);
            else commands.Hit(workTile, workWall, this);
        }
    }

    private void SetState(int state)
    {
        if (state == (int)Projectile.ai[1]) return;
        Projectile.ai[1] = state;
        Projectile.ai[2] = 0f;
        if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
    }

    private void Animate()
    {
        (int frameCount, int frameTicks) = (int)Projectile.ai[1] switch
        {
            MoveState => (MoveFrameCount, TicksPerMoveFrame),
            MineState => (MineFrameCount, TicksPerMineFrame),
            HammerState => (HammerFrameCount, TicksPerMineFrame),
            UseState => (UseFrameCount, TicksPerUseFrame),
            _ => (IdleFrameCount, TicksPerIdleFrame),
        };
        frameCount = Math.Max(1, frameCount);
        frameTicks = Math.Max(1, frameTicks);
        Projectile.frame = (int)Projectile.ai[2] / frameTicks % frameCount;
        float speed = IsMining || IsHammering || IsUsing ? ActionSpeedPercent / 100f : 1f;
        Projectile.ai[2] = (Projectile.ai[2] + speed) % (frameTicks * frameCount);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Asset<Texture2D>[] frames = (int)Projectile.ai[1] switch
        {
            MoveState => GetFrames(ref moveTextures, "Move", MoveFrameCount),
            MineState => GetFrames(ref mineTextures, "Mine", MineFrameCount),
            HammerState => GetFrames(ref hammerTextures, "Hammer", HammerFrameCount),
            UseState => GetFrames(ref useTextures, "Use", UseFrameCount),
            _ => GetFrames(ref idleTextures, "Idle", IdleFrameCount),
        };
        if (frames.Length > 0)
            texture = frames[Projectile.frame % frames.Length].Value;
        DollSummonVisuals.Appearance(AppearAge, IsDismissing ? Math.Max(0, VanishAge) : -1,
            out Vector2 appearScale, out float flash, out float alpha, out Vector2 offset);
        if (alpha <= 0) return false;
        SpriteEffects effects = (Projectile.spriteDirection == 1) == SpriteFacesLeft
            ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (GravDir < 0f)
            effects |= SpriteEffects.FlipVertically;
        Vector2 at = Projectile.Center - Main.screenPosition + offset * GravDir;
        Vector2 scale = appearScale * Projectile.scale;
        DrawFrame(Main.spriteBatch, texture, at, Projectile.GetAlpha(lightColor) * alpha, Projectile.rotation, scale, effects);
        // 出现/收回时叠加同一帧，让玩偶泛白发光；tint alpha=0 在 AlphaBlend 中为叠加。
        if (flash > 0)
            DrawFrame(Main.spriteBatch, texture, at, new Color(255, 226, 255, 0) * (flash * alpha), Projectile.rotation, scale, effects);
        if (Projectile.owner == Main.myPlayer && carryType > 0 && !IsDismissing)
            DrawCarriedItem(at - new Vector2(0f, (texture.Height * 0.5f * scale.Y + 10f) * GravDir), lightColor, alpha);
        return false;
    }

    /// <summary>头顶小图标表示正带着箱子里的物品，使用原版物品贴图。</summary>
    private void DrawCarriedItem(Vector2 center, Color lightColor, float alpha)
    {
        if (carryType >= TextureAssets.Item.Length) return;
        Main.instance.LoadItem(carryType);
        Texture2D icon = TextureAssets.Item[carryType].Value;
        Rectangle frame = Main.itemAnimations[carryType] != null
            ? Main.itemAnimations[carryType].GetFrame(icon) : icon.Frame();
        float size = Math.Max(frame.Width, frame.Height);
        float scale = size > 20f ? 20f / size : 1f;
        float bob = MathF.Sin(Projectile.ai[0] / Math.Max(1, BobPeriod) * MathHelper.TwoPi) * 1.5f;
        Main.spriteBatch.Draw(icon, new Vector2(MathF.Round(center.X), MathF.Round(center.Y + bob)), frame,
            Color.Lerp(lightColor, Color.White, 0.35f) * alpha, 0f, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }

    private Asset<Texture2D>[] GetFrames(ref Asset<Texture2D>[] cache, string action, int count)
    {
        if (cache != null) return cache;
        cache = new Asset<Texture2D>[Math.Max(0, count)];
        for (int i = 0; i < cache.Length; i++)
            cache[i] = ModContent.Request<Texture2D>($"{TexturePrefix}_{action}_{i + 1}", AssetRequestMode.ImmediateLoad);
        return cache;
    }

    // 游戏和 FNA 预览共用实际绘制入口；每帧使用整张独立 PNG，不按纵向帧表裁剪。
    internal static void DrawFrame(SpriteBatch batch, Texture2D texture, Vector2 center,
        Color color, float rotation, float scale, SpriteEffects effects)
        => DrawFrame(batch, texture, center, color, rotation, new Vector2(scale), effects);

    internal static void DrawFrame(SpriteBatch batch, Texture2D texture, Vector2 center,
        Color color, float rotation, Vector2 scale, SpriteEffects effects)
    {
        Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.5f);
        batch.Draw(texture, new Vector2(MathF.Round(center.X), MathF.Round(center.Y)), null,
            color, rotation, origin, scale, effects, 0f);
    }
}

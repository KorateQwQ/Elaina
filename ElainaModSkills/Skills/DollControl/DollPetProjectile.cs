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
/// 浮游玩偶共用的跟随、传送、挖掘与独立 PNG 帧动画。
/// 子类提供 TexturePrefix 和各动作帧数，可覆写移动参数或在 base.AI() 后添加功能。
/// 资源为 &lt;prefix&gt;_Idle_N、_Move_N、_Mine_N（N 从 1 开始），各帧以贴图中心对齐，尺寸可以不同。
/// ai[0]：浮动周期；ai[1]：待机 0 / 移动 1 / 挖矿 2；ai[2]：动画计时。三项随弹幕同步。
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
    protected virtual int TicksPerIdleFrame => 12;
    protected virtual int TicksPerMoveFrame => 8;
    protected virtual int TicksPerMineFrame => 6;
    protected virtual Vector2 FollowOffset => new(56f, -36f);
    protected virtual float TeleportDistance => 1200f;
    protected virtual float FollowSpeed => 12f;
    protected virtual float FollowResponsiveness => 0.12f;
    protected virtual float VelocitySmoothing => 0.16f;
    protected virtual float MovingBobHeight => 4f;
    protected virtual float IdleBobHeight => 1.5f;
    protected virtual int BobPeriod => 90;
    protected virtual bool SpriteFacesLeft => true;
    protected virtual float WorkSpeed => 16f;
    /// <summary>挖掘时停在目标格侧上方的偏移，X 取朝向主人的一侧。</summary>
    protected virtual Vector2 WorkOffset => new(22f, -6f);
    /// <summary>回到主人身边多近才交还收集物；略大于跟随位置到主人中心的距离（约 67）。</summary>
    protected virtual float DeliverDistance => 80f;

    private const int IdleState = 0, MoveState = 1, MineState = 2;
    private Asset<Texture2D>[] idleTextures, moveTextures, mineTextures;
    private Point workTile = new(-1, -1);
    private bool workWall;
    private int hitTimer;
    public bool IsMoving => Projectile.ai[1] == MoveState;
    public bool IsMining => Projectile.ai[1] == MineState;
    public bool HasWork => workTile.X >= 0;
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

    protected virtual Vector2 GetFollowAnchor(Player owner) => owner.Center +
        new Vector2(-owner.direction * FollowOffset.X, owner.gravDir * FollowOffset.Y);

    public static DollPetProjectile FindOwned(Player owner)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active && projectile.owner == owner.whoAmI && projectile.ModProjectile is DollPetProjectile doll)
                return doll;
        }
        return null;
    }

    /// <summary>由 owner 调用；召唤和距离过远共用同一重置与同步入口。</summary>
    public void RecallToOwner(Player owner)
    {
        Projectile.Center = GetFollowAnchor(owner);
        Projectile.velocity = Vector2.Zero;
        Projectile.ai[0] = Projectile.ai[1] = Projectile.ai[2] = 0f;
        Projectile.frame = Projectile.frameCounter = 0;
        Projectile.direction = Projectile.spriteDirection = owner.direction;
        Projectile.netUpdate = true;
    }

    /// <summary>收回玩偶：先把收集物抛还给主人，再移除弹幕。由主人端调用。</summary>
    public void Dismiss()
    {
        if (Projectile.owner != Main.myPlayer) return;
        Main.player[Projectile.owner].GetModPlayer<DollCommandPlayer>()
            .DeliverLoot(Projectile.Center, Projectile.GetSource_FromThis());
        Projectile.Kill();
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((short)workTile.X);
        writer.Write((short)workTile.Y);
        writer.Write(workWall);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        workTile = new Point(reader.ReadInt16(), reader.ReadInt16());
        workWall = reader.ReadBoolean();
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
        // 任务全部结束、回到主人身边后，把挖到的物品从玩偶身上抛给主人。
        if (isOwner && commands.HasLoot && !commands.HasTask &&
            Vector2.DistanceSquared(Projectile.Center, owner.Center) < DeliverDistance * DeliverDistance)
            commands.DeliverLoot(Projectile.Center, Projectile.GetSource_FromThis());

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
        Vector2 desiredVelocity = (target - Projectile.Center) * FollowResponsiveness + owner.velocity;
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
        if (!commands.TrySelectTarget(Projectile.Center, previous, previousWall, out workTile, out workWall))
            workTile = new Point(-1, -1);
        if (workTile != previous || workWall != previousWall)
            Projectile.netUpdate = true;
    }

    /// <summary>飞到目标格朝向主人的一侧，到位后面向格子播放挖矿动画；主人端按工具节奏敲击。</summary>
    private void WorkAI(Player owner, DollCommandPlayer commands, bool isOwner)
    {
        Vector2 tileCenter = workTile.ToWorldCoordinates();
        float side = tileCenter.X > owner.Center.X ? -1f : 1f;
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

        SetState(!arrived ? MoveState : MineFrameCount > 0 ? MineState : IdleState);
        Animate();

        if (!isOwner) return;
        if (!arrived)
        {
            hitTimer = 0;
            return;
        }
        if (++hitTimer >= commands.HitInterval(workWall))
        {
            hitTimer = 0;
            commands.Hit(workTile, workWall);
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
            _ => (IdleFrameCount, TicksPerIdleFrame),
        };
        frameCount = Math.Max(1, frameCount);
        frameTicks = Math.Max(1, frameTicks);
        Projectile.frame = (int)Projectile.ai[2] / frameTicks % frameCount;
        Projectile.ai[2] = (Projectile.ai[2] + 1f) % (frameTicks * frameCount);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Asset<Texture2D>[] frames = (int)Projectile.ai[1] switch
        {
            MoveState => GetFrames(ref moveTextures, "Move", MoveFrameCount),
            MineState => GetFrames(ref mineTextures, "Mine", MineFrameCount),
            _ => GetFrames(ref idleTextures, "Idle", IdleFrameCount),
        };
        if (frames.Length > 0)
            texture = frames[Projectile.frame % frames.Length].Value;
        SpriteEffects effects = (Projectile.spriteDirection == 1) == SpriteFacesLeft
            ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && Main.player[Projectile.owner].gravDir < 0f)
            effects |= SpriteEffects.FlipVertically;
        DrawFrame(Main.spriteBatch, texture, Projectile.Center - Main.screenPosition,
            Projectile.GetAlpha(lightColor), Projectile.rotation, Projectile.scale, effects);
        return false;
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
    {
        Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.5f);
        batch.Draw(texture, new Vector2(MathF.Round(center.X), MathF.Round(center.Y)), null,
            color, rotation, origin, scale, effects, 0f);
    }
}

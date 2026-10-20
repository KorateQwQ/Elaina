using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 浮游玩偶共用的跟随、传送与独立 PNG 帧动画。
/// 子类提供 TexturePrefix 和 MoveFrameCount，可覆写移动参数或在 base.AI() 后添加功能。
/// ai[0]：浮动周期；ai[1]：待机 0 / 移动 1；ai[2]：动画计时。三项随弹幕同步。
/// </summary>
public abstract class DollPetProjectile : ModProjectile
{
    protected abstract string TexturePrefix { get; }
    protected abstract int MoveFrameCount { get; }
    protected virtual int TicksPerMoveFrame => 8;
    protected virtual Vector2 FollowOffset => new(56f, -36f);
    protected virtual float TeleportDistance => 1200f;
    protected virtual float FollowSpeed => 12f;
    protected virtual float FollowResponsiveness => 0.12f;
    protected virtual float VelocitySmoothing => 0.16f;
    protected virtual float MovingBobHeight => 4f;
    protected virtual float IdleBobHeight => 1.5f;
    protected virtual int BobPeriod => 90;
    protected virtual bool SpriteFacesLeft => true;

    private Asset<Texture2D>[] moveTextures;
    public bool IsMoving => Projectile.ai[1] != 0f;
    public override string Texture => TexturePrefix + "_Idle";

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
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
        // 无伤害、无召唤栏占用，也不替换原版宠物。
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;

    protected virtual bool CanStayActive(Player owner) => owner.active && !owner.dead;

    protected virtual Vector2 GetFollowAnchor(Player owner) => owner.Center +
        new Vector2(-owner.direction * FollowOffset.X, owner.gravDir * FollowOffset.Y);

    /// <summary>由 owner 调用；重新施法和距离过远共用同一重置与同步入口。</summary>
    public void RecallToOwner(Player owner)
    {
        Projectile.Center = GetFollowAnchor(owner);
        Projectile.velocity = Vector2.Zero;
        Projectile.ai[0] = Projectile.ai[1] = Projectile.ai[2] = 0f;
        Projectile.frame = Projectile.frameCounter = 0;
        Projectile.direction = Projectile.spriteDirection = owner.direction;
        Projectile.netUpdate = true;
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

        if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > TeleportDistance * TeleportDistance &&
            Projectile.owner == Main.myPlayer)
        {
            RecallToOwner(owner);
            return;
        }

        Vector2 anchor = GetFollowAnchor(owner);
        float anchorDistance = Vector2.Distance(Projectile.Center, anchor);
        // 浮动本身不触发移动动画。起停使用不同阈值，避免靠近玩家时反复切帧。
        bool moving = owner.velocity.LengthSquared() > 0.25f ||
            (IsMoving ? anchorDistance > 8f || Projectile.velocity.LengthSquared() > 1f : anchorDistance > 24f);
        if (moving != IsMoving)
        {
            Projectile.ai[1] = moving ? 1f : 0f;
            Projectile.ai[2] = 0f;
            if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        }

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

        int frameTicks = Math.Max(1, TicksPerMoveFrame);
        int frameCount = Math.Max(1, MoveFrameCount);
        Projectile.frame = IsMoving ? (int)Projectile.ai[2] / frameTicks % frameCount : 0;
        Projectile.ai[2] = IsMoving ? (Projectile.ai[2] + 1f) % (frameTicks * frameCount) : 0f;
        // 周期性纠偏也把浮动相位与动画位置发给其他客户端、晚加入的玩家。
        if (Projectile.owner == Main.myPlayer && Projectile.ai[0] == 0f)
            Projectile.netUpdate = true;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        if (IsMoving && MoveFrameCount > 0)
        {
            if (moveTextures == null)
            {
                moveTextures = new Asset<Texture2D>[MoveFrameCount];
                for (int i = 0; i < moveTextures.Length; i++)
                    moveTextures[i] = ModContent.Request<Texture2D>($"{TexturePrefix}_Move_{i + 1}", AssetRequestMode.ImmediateLoad);
            }
            texture = moveTextures[Projectile.frame % moveTextures.Length].Value;
        }
        SpriteEffects effects = (Projectile.spriteDirection == 1) == SpriteFacesLeft
            ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && Main.player[Projectile.owner].gravDir < 0f)
            effects |= SpriteEffects.FlipVertically;
        DrawFrame(Main.spriteBatch, texture, Projectile.Center - Main.screenPosition,
            Projectile.GetAlpha(lightColor), Projectile.rotation, Projectile.scale, effects);
        return false;
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

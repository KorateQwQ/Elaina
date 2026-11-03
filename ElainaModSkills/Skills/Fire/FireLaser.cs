using System;
using KL.ActionsSystem;
using KL.DamageSystem;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using System.Collections.Generic;
using System.IO;
using Humanizer;
using KL.Drawing;
using KL.Dusts;
using KL.Extensions;
using KL.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

public class FireLaser : ElainaBasicProjectile
{
    private int time;
    private float maxDistance = 1000f;
    private int actionToken;
    private float manaPerPulse;
    private int intervalFrames = 18;
    private int pulseTimer;
    private int pulseSequence;
    private readonly Dictionary<int, FireBurnHit> pendingIgnitionHits = new();
    private bool damagePulse;
    private bool IsChanneling => Projectile.ai[0] == 1f;
    private Vector2[] windPoints;
    private Vector2[] windPoints2;
    private Action drawVisual;
    private int channelAge;
    private FireLaserBeamVisual beamVisual;
    private bool releaseVisualCreated;
    private SlotId chargeSound;
    private SlotId igniteSound;

    // 按下后先蓄力，期间不扣魔、不造成伤害；蓄满后爆开并开始按周期结算。
    internal static int ChargeTicks => 60;
    /// <summary>爆开后经过的 tick；蓄力期间不大于 0。</summary>
    private int FiringAge => channelAge - ChargeTicks;
    private bool IsFiring => IsChanneling && channelAge >= ChargeTicks;

    public override bool ShouldUpdatePosition() => false;

    public override void SetDefaults()
    {
        HeldInfo = new HeldProjInfo(true, 30, 0.2f, 100);
        InfusionElement = ElementType.Fire;
        Projectile.GetGlobalProjectile<ElementalGlobalProjectile>().SuppressBuildup = true;
        Projectile.width = Projectile.height = 10;
        Projectile.timeLeft = 30;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        // 每个 NPC 部位在一个付费脉冲中只命中一次；下个脉冲清理独立免疫。
        Projectile.localNPCHitCooldown = -1;
        maxDistance = 100;
        base.SetDefaults();
    }

    public void BindAction(int token, float manaCost, int tickInterval)
    {
        actionToken = token;
        manaPerPulse = Math.Max(0f, manaCost);
        intervalFrames = Math.Max(1, tickInterval);
        Projectile.netUpdate = true;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(actionToken);
        writer.Write(manaPerPulse);
        writer.Write(intervalFrames);
        base.SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        actionToken = reader.ReadInt32();
        manaPerPulse = Math.Max(0f, reader.ReadSingle());
        intervalFrames = Math.Max(1, reader.ReadInt32());
        base.ReceiveExtraAI(reader);
    }

    private Action_FireLaser BoundAction
    {
        get
        {
            ActionModPlayer actionPlayer = Owner.GetModPlayer<ActionModPlayer>();
            return actionToken > 0 && actionPlayer.CurrentActionToken == actionToken
                ? actionPlayer.CurrentAnimAction as Action_FireLaser : null;
        }
    }

    public override void OnSpawn_AllClient()
    {
        if (!Main.dedServ)
        {
            Vector2 axis = Vector2.UnitX;
            windPoints = QuickConePoints(axis * 100, -axis * 200f, 100, 200, 160f, 0.2f);
            windPoints2 = QuickConePoints(axis * 100, -axis * 150f, 100, 120, 80f, 0.2f);
        }
        Vector2 move = Projectile.velocity;
        move = move.SafeNormalize(new Vector2(Owner.direction, 0));
        Projectile.velocity = move;
        HeldInfo.Rotation = move.ToRotation();
        HeldInfo.LockPosition = Owner.MountedCenter + move * 100f;
        Projectile.Center = Owner.MountedCenter + move * HeldInfo.HeldDistance;

        base.OnSpawn_AllClient();
    }

    public override void AI()
    {
        if (!Main.dedServ && IsFiring)
        {
            KLBasicDust.SpawnDustsCircle(Projectile.Center,
                ModContent.DustType<LineSparkle>(), 1, -Projectile.velocity * 12.1f,
                6.28f, 20, new Color(255, 120, 35, 0), new Vector2(1,0.7f), 0, 0, new Vector2(0.8f, 0f), 7,attachedEntity:Projectile);
        }

        damagePulse = false;
        Projectile.friendly = false;
        if (!Owner.active || Owner.dead || Owner.CCed || Owner.noItems)
        {
            StopChannel();
            return;
        }

        if (HasAuthority())
        {
            Action_FireLaser action = BoundAction;
            if (action == null)
            {
                StopChannel();
                return;
            }
            if (action.IsChanneling && !IsChanneling)
            {
                Projectile.ai[0] = 1f;
                Projectile.netUpdate = true;
            }
            if (IsFiring)
            {
                if (pulseTimer <= 0)
                {
                    if (!Owner.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(manaPerPulse))
                    {
                        action.FinishChannel(Owner.GetModPlayer<ActionModPlayer>());
                        return;
                    }
                    pulseSequence++;
                    damagePulse = true;
                    Projectile.friendly = true;
                    Array.Clear(Projectile.localNPCImmunity);
                    pendingIgnitionHits.Clear();
                    pulseTimer = intervalFrames;
                }
                pulseTimer--;
            }
        }
        if (IsChanneling)
        {
            if (!Main.dedServ) UpdateChargeAndSound();
            channelAge++;
        }
        time++;
        base.AI();
    }

    /// <summary>蓄力时火花向杖尖收拢并逐渐加密；蓄满瞬间向外炸开，之后沿用旧版的喷火器持续音。</summary>
    private void UpdateChargeAndSound()
    {
        string sounds = "伊蕾娜/ElainaModSkills/Skills/Fire/Sounds/";
        if (channelAge == 0)
        {
            SoundStyle charge = new(sounds + "FireLaserCharge") { Volume = 0.6f, MaxInstances = 4 };
            chargeSound = SoundEngine.PlaySound(charge, Projectile.Center, sound => FollowSound(sound, fadeWhenFiring: true));
        }
        if (channelAge == ChargeTicks)
        {
            SoundStyle ignite = new(sounds + "FireLaserIgnite") { Volume = 0.6f, MaxInstances = 4 };
            igniteSound = SoundEngine.PlaySound(ignite, Projectile.Center, sound => FollowSound(sound, fadeWhenFiring: false));
            SpawnBurstSparks();
        }
        if (channelAge < ChargeTicks) SpawnChargeSparks(channelAge / (float)ChargeTicks);
        else
        {
            // 与旧版一致：每 tick 尝试播放喷火器音效，叠满上限后忽略新的，形成连续的火焰声。
            SoundStyle flame = SoundID.Item34 with { Volume = 0.5f, MaxInstances = 20, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };
            SoundEngine.PlaySound(flame, Projectile.Center);
        }
    }

    private bool FollowSound(ActiveSound sound, bool fadeWhenFiring)
    {
        // 弹幕槽位可能被复用，只跟随本实例；弹幕结束时音效随之停止。
        if (!Projectile.active || Projectile.ModProjectile != this || !IsChanneling) return false;
        sound.Position = Projectile.Center;
        if (fadeWhenFiring && IsFiring)
        {
            // 爆开后蓄力音快速淡出，交给持续音。
            float fadeTicks = 8f;
            sound.Volume -= 1f / fadeTicks;
            return sound.Volume > 0f;
        }
        return true;
    }

    private void SpawnChargeSparks(float progress)
    {
        // 可调参数（热重载）：收拢半径、密度、寿命、颜色。
        Vector2 radius = new(70f, 130f);
        float spawnPerTick = MathHelper.Lerp(0.6f, 3f, progress * progress);
        int life = 18;
        Color color = new(255, 120, 35, 0);
        Vector2 scale = new(0.9f, 0.6f);

        int count = (int)spawnPerTick + (Main.rand.NextFloat() < spawnPerTick % 1f ? 1 : 0);
        // LineSparkle 每 tick 速度乘 0.92；按衰减后的总位移让火花在寿命结束前收到杖尖附近。
        float travel = (1f - MathF.Pow(0.92f, life)) / 0.08f;
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Main.rand.NextVector2Unit() * MathHelper.Lerp(radius.X, radius.Y, Main.rand.NextFloat());
            Vector2 velocity = -offset * 0.9f / travel;
            KLBasicDust.SpawnDust(Projectile.Center + offset, ModContent.DustType<LineSparkle>(), velocity, life,
                color, scale * MathHelper.Lerp(0.7f, 1.2f, progress), attachedEntity: Projectile);
        }
    }

    private void SpawnBurstSparks()
    {
        // 可调参数（热重载）：爆开火花数量、速度、寿命。
        int count = 18;
        Vector2 speed = new(9f, 16f);
        Color color = new(255, 140, 50, 0);
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = (MathHelper.TwoPi * (i + Main.rand.NextFloat(-0.35f, 0.35f)) / count).ToRotationVector2();
            KLBasicDust.SpawnDust(Projectile.Center, ModContent.DustType<LineSparkle>(),
                direction * MathHelper.Lerp(speed.X, speed.Y, Main.rand.NextFloat()), Main.rand.Next(16, 26),
                color, new Vector2(Main.rand.NextFloat(1.2f, 1.8f), 0.8f));
        }
    }

    public override void PostAI()
    {
        if (!Projectile.active) return;
        base.PostAI();
        Projectile.velocity = HeldInfo.RealRotation.ToRotationVector2();
        Projectile.rotation = HeldInfo.RealRotation;
        Action_FireLaser action = BoundAction;
        if (action != null)
        {
            action.UpdateAimFromLaser(actionToken, HeldInfo.RealRotation);
            Projectile.Center = action.BeamOrigin;
        }
        // 几何与碰撞按游戏 tick 更新，专用服务器和屏幕外同样执行。
        if (IsChanneling)
        {
            UpdateLaserCollision();
        }
    }

    public void StopChannel()
    {
        damagePulse = false;
        Projectile.friendly = false;
        HeldInfo.IsControlling = false;
        Projectile.Kill();
    }

    public override bool? CanDamage() => damagePulse && IsChanneling && HasAuthority();

    private void UpdateLaserCollision()
    {
        float laserLength = 1500f;
        bool hitTerrain = ClipBeamLength(Projectile.Center, Projectile.velocity, ref laserLength);
        maxDistance = laserLength;
        if (hitTerrain && IsFiring && !Main.dedServ)
        {
            KLBasicDust.SpawnDustsCircle(Projectile.Center + Projectile.velocity * laserLength,
                ModContent.DustType<LineRotSparkle>(), 1, -Projectile.velocity * 12.1f,
                1.3f, 20, new Color(255, 120, 35, 255), new Vector2(1,0.6f), 0, 150, new Vector2(0.8f, 0f), 7);
        }
    }

    // 持续态与残影共用纯几何查询；不生成命中粒子，也不恢复伤害。
    private static bool ClipBeamLength(Vector2 start, Vector2 direction, ref float length)
    {
        bool blocked = LaserCollision(start, direction, 20, ref length);
        // 水面阻断由各端按本地物块液体独立计算，不同步长度。
        float waterLength = WaterBlockDistance(start, direction, length, WaterAmountAt);
        if (waterLength < length)
        {
            length = waterLength;
            blocked = true;
        }
        return blocked;
    }

    private static byte WaterAmountAt(int x, int y)
    {
        if (!WorldGen.InWorld(x, y)) return 0;
        Tile tile = Main.tile[x, y];
        return tile.LiquidType == LiquidID.Water ? tile.LiquidAmount : (byte)0;
    }

    /// <summary>
    /// 沿射线中心线逐格查找首个水体表面，返回可通过的长度；岩浆、蜂蜜等其他液体不阻挡。
    /// 液体按原版规则从格子底部向上填充，高度为 LiquidAmount / 255 格。
    /// </summary>
    internal static float WaterBlockDistance(Vector2 start, Vector2 direction, float length, Func<int, int, byte> waterAmount)
    {
        direction = direction.SafeNormalize(Vector2.UnitX);
        int x = (int)MathF.Floor(start.X / 16f);
        int y = (int)MathF.Floor(start.Y / 16f);
        int stepX = Math.Sign(direction.X);
        int stepY = Math.Sign(direction.Y);
        float deltaX = stepX != 0 ? 16f / MathF.Abs(direction.X) : float.PositiveInfinity;
        float deltaY = stepY != 0 ? 16f / MathF.Abs(direction.Y) : float.PositiveInfinity;
        float nextX = stepX > 0 ? ((x + 1) * 16f - start.X) / direction.X
            : stepX < 0 ? (x * 16f - start.X) / direction.X : float.PositiveInfinity;
        float nextY = stepY > 0 ? ((y + 1) * 16f - start.Y) / direction.Y
            : stepY < 0 ? (y * 16f - start.Y) / direction.Y : float.PositiveInfinity;
        float enter = 0f;
        while (enter <= length)
        {
            byte amount = waterAmount(x, y);
            if (amount > 0)
            {
                // 当前格内射线段 [enter, exit] 与水体矩形（surface 以下）求首个交点。
                float exit = MathF.Min(MathF.Min(nextX, nextY), length);
                float surface = (y + 1) * 16f - amount / 255f * 16f;
                float enterY = start.Y + direction.Y * enter;
                if (enterY >= surface) return enter;
                if (direction.Y > 0f)
                {
                    float hit = enter + (surface - enterY) / direction.Y;
                    if (hit <= exit) return hit;
                }
            }
            if (nextX < nextY)
            {
                enter = nextX;
                nextX += deltaX;
                x += stepX;
            }
            else
            {
                enter = nextY;
                nextY += deltaY;
                y += stepY;
            }
        }
        return length;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!IsFiring) return false;
        // HDR 颜色依赖 KL Bloom；直接在原版弹幕批次里绘制会被截断成白色且没有光晕。
        drawVisual ??= DrawVisual2;
        LayerDrawRequestSystem.RequestBloom("Elaina:FireLaser", FireLaserVisuals.Layer,
            LayerDrawRequestSystem.DrawTiming.After, drawVisual);
        return false;
    }

    void DrawVisual2()
    {
        if (!Projectile.active || !IsChanneling) return;
        // PostAI 已将弹幕中心对齐到动作的 WandCenter，包含实际手臂姿势偏移。
        beamVisual ??= new FireLaserBeamVisual();
        beamVisual.Draw(Projectile.Center, Projectile.velocity, maxDistance + 10f, time,
            FireLaserBeamVisual.OpeningWidth(channelAge - ChargeTicks));
    }

    private readonly record struct ReleaseState(FireLaserBeamVisual Visual, Vector2 Direction, float Time, float Width,
        ReleaseAnchor Anchor);

    /// <summary>残影起点：收势动作仍在时取实际杖尖（含扫帚倾斜与手臂姿势），动作结束后沿用最后的相对偏移。</summary>
    private sealed class ReleaseAnchor(int owner, int actionToken, Vector2 start)
    {
        private Vector2 offset = start - Main.player[owner].MountedCenter;
        private Vector2 fallback = start;

        public Vector2 Resolve()
        {
            Player player = Main.player[owner];
            // 玩家离开或死亡时停在最后的位置。
            if (!player.active || player.dead) return fallback;
            ActionModPlayer actionPlayer = player.GetModPlayer<ActionModPlayer>();
            if (actionPlayer.CurrentActionToken == actionToken && actionPlayer.CurrentAnimAction is Action_FireLaser action)
                offset = action.BeamOrigin - player.MountedCenter;
            return fallback = player.MountedCenter + offset;
        }
    }

    private static void DrawReleasedBeam(ReleaseState state, Vector2[] points, float remaining)
    {
        float age = 1f - remaining;
        Vector2 start = state.Anchor.Resolve();
        float length = Vector2.Distance(points[0], points[1]);
        // 起点随杖尖移动，方向仍锁定；每次绘制从当前起点重新截短，不能沿用旧墙距。
        // 此处长度已含持续态的端点补偿，不在碰撞结果后再加 10px。
        ClipBeamLength(start, state.Direction, ref length);
        state.Visual.Draw(start, state.Direction, length,
            state.Time + age * FireLaserBeamVisual.ReleaseTicks * 0.3f, state.Width, age);
    }

    private void LeaveAfterglow()
    {
        // 蓄力中松手没有射线可残留；蓄力火花是独立粒子，自然消散。
        if (Main.dedServ || !IsChanneling || FiringAge <= 0 || releaseVisualCreated) return;
        releaseVisualCreated = true;
        float width = FireLaserBeamVisual.OpeningWidth(FiringAge);
        float length = maxDistance + 10f;
        if (length <= 0f || width <= 0f) return;
        beamVisual ??= new FireLaserBeamVisual();
        // 松手通常发生在玩家更新中，弹幕中心仍是上一 tick 的杖尖；动作仍在时直接取当前杖尖。
        Vector2 start = BoundAction?.BeamOrigin ?? Projectile.Center;
        Vector2 direction = Projectile.velocity;
        TrailManager.TrailHandle handle = null;
        // 伤害弹幕照常立即 Kill；残影不依赖弹幕，起点每帧跟随杖尖，消散期间仍留在手上。
        TrailManager.CreateOrUpdateTrail(ref handle, new[] { start, start + direction * length },
            FireLaserBeamVisual.ReleaseTicks,
            new ReleaseState(beamVisual, direction, time, width, new ReleaseAnchor(Owner.whoAmI, actionToken, start)),
            DrawReleasedBeam,
            layer: FireLaserVisuals.Layer, bloom: true, shrinkPathOnExpire: false,
            order: LayerDrawRequestSystem.ProjectileOrder);
        foreach (var spark in FireLaserBeamVisual.ReleaseSparks(start, direction, length, width, Projectile.identity * 397 ^ Projectile.owner))
            KLBasicDust.SpawnDust(spark.Position, ModContent.DustType<LineSparkle>(), spark.Velocity,
                spark.Life, new Color(255, 120, 35, 0), spark.Size);
    }
    
    private void DrawVisual()
    {
        if (!Projectile.active || !IsChanneling) return;
        DrawRibbons(foreground: false);
        DrawMagicBall();
        DrawLaser();
        DrawRibbons(foreground: true);
        DrawMuzzleFlare();
        EndBeginDraw();
    }

    /// <summary>杖尖爆点：亮芯 + 多角度长短不一的尖刺射线，持续闪动，盖住火柱从法阵伸出的起点。</summary>
    void DrawMuzzleFlare()
    {
        Texture2D core = ModContent.Request<Texture2D>("KL/Dusts/Burst/glo_0281", AssetRequestMode.ImmediateLoad).Value;
        Texture2D line = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLine", AssetRequestMode.ImmediateLoad).Value;
        // 可调参数（热重载）：位置（距 MountedCenter）、尺寸（像素）、颜色、HDR 倍率、覆盖度
        float distance = 118f;
        float coreSize = 120f;
        int rayCount = 7;                    // 每条 ShotLine 两端各成一根尖刺
        Vector2 rayLength = new(90f, 230f);  // 整条光线长度范围
        Vector2 rayWidth = new(10f, 18f);
        int flickerTicks = 3;                // 每隔几帧重新排布一次尖刺
        Color coreColor = new(255, 170, 80);
        Color rayColor = new(255, 205, 140);
        float coreHdr = 1.8f, rayHdr = 2f;
        byte coreCoverage = 150, rayCoverage = 110;

        static float Hash(int n) => MathF.Abs(MathF.Sin(n * 12.9898f) * 43758.547f) % 1f;
        int step = time / flickerTicks;
        Vector2 center = Owner.MountedCenter + Projectile.velocity * distance;
        Vector2 lineTexSize = new(line.Width, line.Height);

        EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
        ReColorEffect(new Vector4(coreHdr, coreHdr, coreHdr, 1));
        float coreScale = coreSize * (.9f + .2f * Hash(step * 7 + 1)) / core.Width;
        DrawInWorld(core, center, coreColor with { A = coreCoverage }, new Vector2(coreScale), Hash(step * 7 + 2) * MathHelper.TwoPi);

        ReColorEffect(new Vector4(rayHdr, rayHdr, rayHdr, 1));
        for (int i = 0; i < rayCount; i++)
        {
            int seed = step * 31 + i * 5;
            float angle = Projectile.rotation + MathHelper.Pi * (i + Hash(seed) * .7f) / rayCount;
            float length = MathHelper.Lerp(rayLength.X, rayLength.Y, Hash(seed + 1));
            float width = MathHelper.Lerp(rayWidth.X, rayWidth.Y, Hash(seed + 2));
            // 中心沿光线偏移，两端尖刺长短不一
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (Hash(seed + 3) - .5f) * length * .35f;
            DrawInWorld(line, center + offset, rayColor with { A = rayCoverage }, new Vector2(length, width) / lineTexSize, angle);
        }
    }

    /// <summary>缎带从杖尖法阵绕到火柱末端；按深度拆成火柱后方与前方两次绘制。</summary>
    void DrawRibbons(bool foreground)
    {
        Vector2 direction = Projectile.velocity;
        Vector2 start = Owner.MountedCenter + direction * 95f;
        // 末端与 DrawLaser 的火柱主体相同。
        float length = Vector2.Dot(Projectile.Center + direction * (maxDistance + 10f) - start, direction);
        FireLaserVisuals.DrawRibbons(start, direction, length, time, foreground);
    }

    void DrawLaser()
    {
        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind3", AssetRequestMode.ImmediateLoad).Value;
        Texture2D wind2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/windNoi", AssetRequestMode.ImmediateLoad).Value;

        // 火柱主体：从杖尖法阵内侧伸出，末端与缎带相同。
        Vector2 direction = Projectile.velocity;
        Vector2 bodyStart = Owner.MountedCenter + direction * 85f;
        float bodyLength = Vector2.Dot(Projectile.Center + direction * (maxDistance + 10f) - bodyStart, direction);
        FireLaserVisuals.DrawBody(bodyStart, direction, bodyLength, time);

        float headDistance = maxDistance - 180f;
        if(headDistance<-80)headDistance = -80;

        EndBeginDraw(2,1,ss:SamplerState.LinearWrap);

        Vector2 move = new Vector2(1, 0);
        //火焰顶端
        VertexDrawEffect(wind,windPoints2,new Color(255, 50, 30,255),new Color(255, 150, 30,0),startAlpha:5.6f, endAlpha:0,blendState:1,drawTimes:1,
            uTime:new Vector2((float)(Main.timeForVisualEffects%60)/20f,0),
            attachPoint:Projectile.Center+move.RotatedBy(Projectile.rotation)*(headDistance+120),attachRotation:Projectile.rotation,
            useRforAlpha:false,debugPoint:false);
        
        //后坐力风场
        VertexDrawEffect(wind2,windPoints,Color.White, Color.White*0,startAlpha:1.0f, endAlpha:0,blendState:1,drawTimes:1,
            uTime:new Vector2((float)(Main.timeForVisualEffects%60)/15f,0),
            attachPoint:Projectile.Center-move.RotatedBy(Projectile.rotation)*(150),attachRotation:Projectile.rotation,
            useRforAlpha:false,debugPoint:false);
        EndBeginDraw();
    }
    void DrawMagicBall()
    {
        Asset<Texture2D> waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/水波");
        Effect rotEffect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/旋转", AssetRequestMode.ImmediateLoad).Value;

        Asset<Texture2D> headClip = ModContent.Request<Texture2D>("KL/Effects/Tex/background", AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> circle = ModContent.Request<Texture2D>("KL/Effects/Tex/Burst/T_Circle_01", AssetRequestMode.ImmediateLoad);

        Asset<Texture2D> trail = ModContent.Request<Texture2D>("KL/Effects/Tex/noi_1", AssetRequestMode.ImmediateLoad);

        //Vector2 time = new Vector2( (count % 120) / 40f,0);

        EndBeginDraw(2,ss:SamplerState.LinearClamp,adjustToScreen:true);
        //DrawDiamond(Owner.MountedCenter+Projectile.velocity*185f-Main.screenPosition,new Vector2(300,500)*0.9f,new Color(20,20,20,205),Projectile.rotation,650);

        /*
        DrawInWorld(circle.Value,Owner.MountedCenter+Projectile.velocity*95f,color:new Color(20,20,20,100),
            rotation:Projectile.rotation,scale:new Vector2(0.5f,1f)*0.16f);
            */

        
        Lighting.AddLight(Projectile.Center, new Vector3(1f,0.7f,0.8f)*DrawManager.FrameTime(0.5f,1.5f,60));
        
        // 原先在此给 MagicMissileEffect 写参数，但该 Effect 从未应用；魔法飞弹重写 shader 后
        // 这些参数已不存在，访问会抛 NullReferenceException 使整个射线绘制中断。
        float totalAlpha = 1;
        
        Main.graphics.GraphicsDevice.Textures[1] = waterNoise.Value;
        Main.graphics.GraphicsDevice.Textures[2] = headClip.Value;  
        
        EndBeginDraw(2,shader:rotEffect,ss:SamplerState.LinearClamp,adjustToScreen:true);
        rotEffect.Parameters["rot"].SetValue(time/20f);
        rotEffect.Parameters["scale"].SetValue(0.7f);
        rotEffect.Parameters["bloomColor"].SetValue(new Vector4(1f,0.5f,0.2f,1f)*4.5f * totalAlpha);

        float circleSize = DrawManager.FrameTime(0.9f, 1f, 100, time);

        DrawInWorld(circle.Value,Owner.MountedCenter+Projectile.velocity*95f,rotation:Projectile.rotation,scale:new Vector2(0.5f,1f)*0.2f);
        EndBeginDraw();

        
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        
        return AABBvLineCollision(targetHitbox, Projectile.Center, Projectile.Center + Projectile.velocity*maxDistance, 100);
    }
    

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }
    
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (HasAuthority() && FireBurnNPC.CaptureHit(target) is { } snapshot)
            pendingIgnitionHits[target.whoAmI] = snapshot;
        base.ModifyHitNPC(target, ref modifiers);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (damageDone > 0 && HasAuthority() && pendingIgnitionHits.Remove(target.whoAmI, out var snapshot))
            Owner.GetModPlayer<FireCyclePlayer>().RequestBeamHit(snapshot, Projectile, intervalFrames, pulseSequence, damageDone);
        base.OnHitNPC(target, hit, damageDone);
    }

    public override void OnKill(int timeLeft)
    {
        LeaveAfterglow();
        // 回调也会在弹幕失活后停止音效；这里立即停止，避免松手后多响一帧。
        if (!Main.dedServ)
        {
            if (SoundEngine.TryGetActiveSound(chargeSound, out ActiveSound charge)) charge.Stop();
            if (SoundEngine.TryGetActiveSound(igniteSound, out ActiveSound ignite)) ignite.Stop();
        }
        base.OnKill(timeLeft);
    }
}

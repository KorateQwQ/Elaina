

using System;
using System.IO;
using Terraria.ID;
using KL.ActionsSystem;
using KL.DamageSystem;
using KL.Drawing;
using KL.Extensions;
using KL.Utils;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Managers;
using 伊蕾娜.ElainaModSkills.Skills.Ice;
using DrawHelper = KL.Drawing.DrawHelper;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

public class WaterLaser : ElainaBasicProjectile
{
    public static float SplashRadius => 230f;
    public const float SplashDamageMultiplier = 0.5f;
    public const float FreezeCooldownDamageBonus = 0.3f;
    public const float OutsideFreezeCooldownDamageMultiplier = 1f / (1f + FreezeCooldownDamageBonus);
    private const float BeamWidth = 50f;

    private float maxDistance;
    private int time;
    private int actionToken;
    private float manaPerPulse;
    private int intervalFrames = 12;
    private int pulseTimer;
    private bool damagePulse;
    private int primaryTarget = -1;
    private readonly HashSet<int> hitNpcIndices = new();
    private readonly IceWaterHitBatch iceCycleBatch = new();
    private bool IsChanneling => Projectile.ai[0] == 1f;

    private readonly Vector2[] drawPoints = new Vector2[200];
    private Action drawVisual;
    
    private List<Vector2> trailPoints = new List<Vector2>(200);
    public override void SetDefaults()
    {
        HeldInfo = new HeldProjInfo(true, 30, 0.2f, 30);
        InfusionElement = ElementType.Water;
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

    private Action_WaterLaser BoundAction
    {
        get
        {
            ActionModPlayer actionPlayer = Owner.GetModPlayer<ActionModPlayer>();
            return actionToken > 0 && actionPlayer.CurrentActionToken == actionToken
                ? actionPlayer.CurrentAnimAction as Action_WaterLaser : null;
        }
    }

    public override void OnSpawn_AllClient()
    {
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
        damagePulse = false;
        Projectile.friendly = false;
        if (!Owner.active || Owner.dead || Owner.CCed || Owner.noItems)
        {
            StopChannel();
            return;
        }

        if (HasAuthority())
        {
            Action_WaterLaser action = BoundAction;
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
            if (IsChanneling)
            {
                if (pulseTimer <= 0)
                {
                    if (!Owner.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(manaPerPulse))
                    {
                        action.FinishChannel(Owner.GetModPlayer<ActionModPlayer>());
                        return;
                    }
                    damagePulse = true;
                    Projectile.friendly = true;
                    Array.Clear(Projectile.localNPCImmunity);
                    hitNpcIndices.Clear();
                    iceCycleBatch.Reset();
                    pulseTimer = intervalFrames;
                }
                pulseTimer--;
            }
        }
        time++;
        base.AI();
    }

    public override void PostAI()
    {
        if (!Projectile.active) return;
        base.PostAI();
        Projectile.velocity = HeldInfo.RealRotation.ToRotationVector2();
        Projectile.rotation = HeldInfo.RealRotation;
        Action_WaterLaser action = BoundAction;
        if (action != null)
        {
            action.UpdateAimFromLaser(actionToken, HeldInfo.RealRotation);
            Projectile.Center = action.BeamOrigin;
        }
        // 几何与碰撞按游戏 tick 更新，专用服务器和屏幕外同样执行。
        if (IsChanneling)
        {
            LaserCollisionCheck();
            if (!Main.dedServ) SpawnWaterDust();
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

    private bool CanTargetNPC(NPC target)
    {
        if (!target.IsAttackable(Owner.dontHurtCritters)
            || (target.friendly && !(target.type == NPCID.Guide && Owner.killGuide)
                && !(target.type == NPCID.Clothier && Owner.killClothier))
            || (target.aiStyle == 112 && target.ai[2] > 1f)
            || !Owner.CanNPCBeHitByPlayerOrPlayerProjectile(target, Projectile))
            return false;

        // 非伤害帧也按友方攻击检查保护规则，避免射线在脉冲之间被不可受击目标截断。
        bool wasFriendly = Projectile.friendly;
        try
        {
            Projectile.friendly = true;
            // 不调用 CombinedHooks：它会再次进入本弹幕的 CanHitNPC，并依赖尚未确定的落点。
            return PlayerLoader.CanHitNPCWithProj(Owner, Projectile, target) != false
                && NPCLoader.CanBeHitByProjectile(target, Projectile) != false;
        }
        finally
        {
            Projectile.friendly = wasFriendly;
        }
    }

    public override bool? CanHitNPC(NPC target)
    {
        if (!CanTargetNPC(target) || hitNpcIndices.Contains(target.whoAmI))
            return false;
        if (target.whoAmI == primaryTarget) return null;
        // 各个可受击部位独立检查溅射，不按 realLife 合并伤害目标。
        if (trailPoints.Count == 0) return false;
        Vector2 impact = trailPoints[^1];
        return InSplash(target.Hitbox, impact)
            && Collision.CanHitLine(impact, 1, 1, target.position, target.width, target.height)
                ? null : false;
    }

    private static bool InSplash(Rectangle hitbox, Vector2 impact) =>
        Vector2.DistanceSquared(hitbox.ClosestPointInRect(impact), impact) <= SplashRadius * SplashRadius;

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // 命中时读取目标共享的冻结 CD；冻结中尚未起算 CD，同样使用较低倍率。
        // 在 OnHitNPC 减 CD 之前结算，每个溅射目标独立判断。
        if (target.GetFreezeCooldown() <= 0)
            modifiers.SourceDamage *= OutsideFreezeCooldownDamageMultiplier;
        if (target.whoAmI != primaryTarget)
            modifiers.SourceDamage *= SplashDamageMultiplier;
        base.ModifyHitNPC(target, ref modifiers);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (trailPoints.Count > 0)
        {
            bool collide = AABBvLineCollision(targetHitbox, Projectile.Center, trailPoints[^1], BeamWidth)
                || InSplash(targetHitbox, trailPoints[^1]);
            return collide;
        }
        return false;
    }
    
    void LaserCollisionCheck()
    {
        trailPoints.Clear();
        primaryTarget = -1;
        float noiseScale = DrawManager.FrameTime(10,70,30,time); // 振幅，控制波形高度
        float timeEffect = (Main.GameUpdateCount%1800)/2f;

        float rotStrength = MathF.Abs(HeldInfo.CurRotVelocity);
        rotStrength = MathHelper.Clamp(1f - rotStrength / 0.2f, 0f, 1f);
        float laserLength = maxDistance+200f;
        if (laserLength > 1200) laserLength = 1200;
        
        float noiseFrequency = 0.01f*laserLength/1200f; // 噪声频率
        for (int i = 0; i < 200; i++)
        {
            // 使用Perlin噪声替代sin波

            float startEffect = i / 199f;
            startEffect = Math.Min(startEffect, 1f);
            
            // 使用多层分形噪声产生更自然的波动
            float noiseValue = FractalNoise(
                i * noiseFrequency, 
                timeEffect * 0.1f, 
                octaves: 4, //层次度
                persistence: 0f, //粗糙度
                scale: noiseScale*startEffect*i/100*rotStrength //强度
            );
            
            // 计算垂直于速度方向的向量
            Vector2 perpendicular = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X);
            perpendicular.Normalize();
            
            //弧线
            Vector2 point = (i / 200f * i / 200f) * perpendicular*HeldInfo.CurRotVelocity*-2000f;
            
            trailPoints.Add(Projectile.Center + Projectile.velocity * laserLength * startEffect + perpendicular * noiseValue+point);
        }
        
        if(trailPoints.Count<1)return;
        
        float candidateLength = Vector2.Distance(Projectile.Center, trailPoints[^1]);
        Vector2 toward = (trailPoints[^1] - Projectile.Center).SafeNormalize(Projectile.velocity);
        laserLength = Math.Min(candidateLength, 1200f);
        //先检测是否和岩浆碰撞
        for (int i = 0; i < trailPoints.Count; i++)
        {
            float currentLength = Vector2.Distance(Projectile.Center, trailPoints[i]);
            if(currentLength>laserLength)break;
            if (Collision.LavaCollision(trailPoints[i], 10, 10))
            {
                laserLength = Math.Min(laserLength, currentLength);
                break;
            }
        }
        
        //Main.NewText(finalLength);
        LaserCollision(Projectile.Center, toward, 5, ref laserLength, 50);
        float terrainLength = laserLength;
        float nearestNpcDistance = laserLength;
        
        foreach (var npc in Main.ActiveNPCs)
        {
            if (CanTargetNPC(npc))
            {
                float hitLength = 0f;
                bool collidingNpc = npc.Hitbox.Contains(Projectile.Center.ToPoint())
                    || Collision.CheckAABBvLineCollision(npc.position, npc.Size, Projectile.Center,
                        Projectile.Center + toward * laserLength, BeamWidth, ref hitLength);

                if(collidingNpc&&hitLength<nearestNpcDistance)
                {
                    nearestNpcDistance = hitLength;
                    primaryTarget = npc.whoAmI;
                }
            }
        }

        if (primaryTarget >= 0)
            laserLength = Math.Min(terrainLength, nearestNpcDistance + 2f);
        maxDistance = laserLength < maxDistance || primaryTarget >= 0
            ? laserLength : MathHelper.Lerp(maxDistance, laserLength, 0.3f);
        // 缩短已生成的轨迹，末端、溅射中心和本帧伤害判定始终一致。
        float lengthScale = maxDistance / Math.Max(0.001f, candidateLength);
        for (int i = 0; i < trailPoints.Count; i++)
            trailPoints[i] = Projectile.Center + (trailPoints[i] - Projectile.Center) * lengthScale;
     
    }
    
    
    
    public override bool PreDraw(ref Color lightColor)
    {
        if (!IsChanneling || trailPoints.Count < 2 || maxDistance <= 0.01f) return false;
        drawVisual ??= DrawVisual;
        LayerDrawRequestSystem.RequestBloom("Elaina:WaterLaser", WaterLaserVisuals.Layer,
            LayerDrawRequestSystem.DrawTiming.After, drawVisual);
        return false;
    }

    public void SpawnWaterDust()
    {
        if (trailPoints.Count < 2 || maxDistance <= 0.01f) return;
        Vector2 direction = Projectile.velocity;
        Vector2 normal = new(-direction.Y, direction.X);
        // 起点只在杖尖附近浮动，避免原模板向前随机偏移 50px 留下空隙。
        WaterLaserDust.Emit(Projectile.Center + normal * Main.rand.NextFloat(-3f, 3f),
            direction.RotatedBy(Main.rand.NextFloat(-0.66f, 0.66f)) * Main.rand.NextFloat(5f, 8f),
            new Vector2(40f, 18f), 15, attachedEntity: Owner);
        if (time % 5 == 0)
            WaterLaserDust.Emit(Projectile.Center, direction * 0.65f,
                new Vector2(25f, 25f), 14, foam: true, attachedEntity: Owner);

        // 随长度增加，每两帧最多四片；中段沿实际曲线切线移动。
        int middleCount = Math.Clamp((int)(maxDistance / 240f), 0, 4);
        if (time % 2 == 0)
        for (int i = 0; i < middleCount; i++)
        {
            int index = Main.rand.Next(4, trailPoints.Count - 2);
            Vector2 tangent = (trailPoints[index + 1] - trailPoints[index]).SafeNormalize(direction);
            WaterLaserDust.Emit(trailPoints[index], tangent * Main.rand.NextFloat(6f, 10f),
                new Vector2(48f, 20f), 17);
        }

        Vector2 impact = trailPoints[^1];
        Vector2 endDirection = (impact - trailPoints[^2]).SafeNormalize(direction);
        for (int i = 0; i < 2; i++)
        {
            Vector2 spray = endDirection.RotatedBy(Main.rand.NextFloat(-2.5f, 2.5f));
            WaterLaserDust.Emit(impact+Main.rand.NextVector2Circular(10,10), spray * Main.rand.NextFloat(8f, 15f),
                new Vector2(104f, 27f), Main.rand.Next(15, 22));
        }
        if (time % 4 == 0)
            WaterLaserDust.Emit(impact, Vector2.Zero, new Vector2(50f), 18, foam: true);
        Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.35f, 0.48f));
        Lighting.AddLight(impact, new Vector3(0.1f, 0.3f, 0.42f));
    }

    private void DrawVisual()
    {
        if (!Projectile.active || !IsChanneling || trailPoints.Count < 2) return;
        // 两层射线共享一个缓存数组，不再每层 ToArray 分配。
        trailPoints.CopyTo(drawPoints);
        WaterLaserVisuals.DrawBeam(drawPoints, maxDistance, time);
        WaterLaserVisuals.BeginSprites();
        //WaterLaserVisuals.DrawMuzzle(Projectile.Center, Projectile.velocity, time);
        EndBeginDraw();
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }
    
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitNpcIndices.Add(target.whoAmI);
        iceCycleBatch.OnHit(Owner, target, damageDone, waterBall: false);
        base.OnHitNPC(target, hit, damageDone);
    }

    
    public override bool ShouldUpdatePosition()
    {
        return base.ShouldUpdatePosition();
    }
}

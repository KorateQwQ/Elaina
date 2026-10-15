

using System;
using System.IO;
using System.Linq;
using KL.ActionsSystem;
using KL.DamageSystem;
using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Water;
using KL.Extensions;
using KL.Utils;
using Terraria.GameContent;
using Terraria.ID;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Managers;
using DrawHelper = KL.Drawing.DrawHelper;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

public class WaterLaser : ElainaBasicProjectile
{
    public const float SplashRadius = 80f;
    public const float SplashDamageMultiplier = 0.5f;
    public const float OutsideFreezeCooldownDamageMultiplier = 0.7f / 0.9f;
    private const float BeamWidth = 50f;

    private float maxDistance;
    private int time;
    private int actionToken;
    private float manaPerPulse;
    private int intervalFrames = 12;
    private int pulseTimer;
    private bool damagePulse;
    private int primaryTarget = -1;
    private int primaryEntity = -1;
    private readonly HashSet<int> hitEntities = new();
    private bool IsChanneling => Projectile.ai[0] == 1f;

    private float startWidth = 2f;
    private float endWidth = 7f;
    
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
        // 每个付费脉冲只命中一次；下个脉冲开始时清理独立免疫。
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
                    hitEntities.Clear();
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

    private static int DamageEntity(NPC npc) => npc.realLife >= 0 ? npc.realLife : npc.whoAmI;

    public override bool? CanHitNPC(NPC target)
    {
        int entity = DamageEntity(target);
        if (!target.IsAttackable(Owner.dontHurtCritters) || hitEntities.Contains(entity))
            return false;
        if (target.whoAmI == primaryTarget) return null;
        // 同一个 Boss 的其它身体段不能额外吃到溅射。
        if (entity == primaryEntity || trailPoints.Count == 0) return false;
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
        primaryTarget = primaryEntity = -1;
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
            if (npc.IsAttackable(Owner.dontHurtCritters))
            {
                float hitLength = 0f;
                bool collidingNpc = npc.Hitbox.Contains(Projectile.Center.ToPoint())
                    || Collision.CheckAABBvLineCollision(npc.position, npc.Size, Projectile.Center,
                        Projectile.Center + toward * laserLength, BeamWidth, ref hitLength);

                if(collidingNpc&&hitLength<nearestNpcDistance)
                {
                    nearestNpcDistance = hitLength;
                    primaryTarget = npc.whoAmI;
                    primaryEntity = DamageEntity(npc);
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
        if (!IsChanneling || trailPoints.Count == 0 || maxDistance <= 0.01f) return false;
        DrawLaser();
        EndBeginDraw();
        return base.PreDraw(ref lightColor);
    }

    public void SpawnWaterDust()
    {
        if (trailPoints.Count == 0) return;
        Vector2? lastPoint = trailPoints[^1];
        if (lastPoint!=null)
        {
            
            //前端粒子
            KLBasicDust.SpawnDustsCircle(Projectile.Center-Projectile.velocity*20, ModContent.DustType<WaterDust>(), 1, Projectile.velocity*12.1f, 
                1.3f,20, new Color(100, 156, 255,255),new Vector2(0.5f,0.5f),0,50,new Vector2(0.5f,0f),7,attachedEntity:Owner);
       
            KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust1>(), 1, Projectile.velocity*12.1f, 
                1.8f,20, new Color(100, 156, 255,255),new Vector2(0.5f,0.5f)*1.5f,0,50,new Vector2(0.5f,0f),7,attachedEntity:Owner);

            int middleCount = (int)MathHelper.Lerp(0, 5, (maxDistance - 200) / 1000f);
            //中间粒子
            for (int i = 0; i < middleCount; i++)
            {
                Vector2 randPoint = trailPoints[Main.rand.Next(0, trailPoints.Count-1)];
                KLBasicDust.SpawnDustsCircle(randPoint, ModContent.DustType<WaterDust>(), 1, Projectile.velocity*12.1f, 
                    0.0f,20, new Color(100, 156, 255,155),new Vector2(0.5f,0.5f),0,0,new Vector2(0.5f,0f),7,attachedEntity:Owner);
            }

            
            //末端粒子
            KLBasicDust.SpawnDustsCircle(lastPoint.Value, ModContent.DustType<WaterDust>(), 2, Projectile.velocity*12.1f, 
                6.28f,20, new Color(120, 156, 255,200),new Vector2(0.4f,0.7f),10,0,new Vector2(0.5f,0f),7);
            
            KLBasicDust.SpawnDustsCircle(lastPoint.Value, ModContent.DustType<ShockDust>(), 1, Projectile.velocity*5.1f, 
                6.28f,10, Color.White,new Vector2(0.2f,0.1f),0,0,new Vector2(0.2f,0f));
        }
    }
    
    void DrawLaser()
    {
        Asset<Texture2D> waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/水波",AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> waterNoise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5");
        Asset<Texture2D> trail2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/EnergyFlow", AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> trail3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/noi_1", AssetRequestMode.ImmediateLoad);

        Asset<Texture2D> waterNoise3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/T_VFX_Noise_1");
        Asset<Texture2D> waterNoise4 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/7");

        Asset<Texture2D> noise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5");
        Asset<Texture2D> headClip = ModContent.Request<Texture2D>("KL/Effects/Tex/射灯",AssetRequestMode.ImmediateLoad);//ModContent.Request<Texture2D>("KL/Effects/Tex/EnergyFlow");
        Asset<Texture2D> headClip2 = ModContent.Request<Texture2D>("KL/Effects/Tex/射灯2",AssetRequestMode.ImmediateLoad);//ModContent.Request<Texture2D>("KL/Effects/Tex/EnergyFlow");
        Asset<Texture2D> trail = ModContent.Request<Texture2D>("KL/Effects/Tex/Line", AssetRequestMode.ImmediateLoad);
        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind3", AssetRequestMode.ImmediateLoad).Value;

        float width = DrawManager.FrameTime(0.25f, 0.3f, 30, time);

        float length = 3f;
        
        Vector2 imageScale = new Vector2(0.5f/(maxDistance/1000f),1f);
        
        Vector2 uTIme = new Vector2(-time%1800/20f,0);

        startWidth = 2;
        endWidth = 8;
        
        
        TrailEffect(waterNoise2.Value,trailPoints.ToArray(),new Color(30, 180, 255,255),new Color(30, 180, 255,255),
            startWidth,endWidth,startAlpha:1f,endAlpha:1f,blendState:2,
            uTime:uTIme,imageScale:imageScale,
            clipMask:waterNoise2.Value,maskTime:uTIme,threshold:0.4f,maskScale:imageScale,
            debugPoint:false,useRforAlpha:true);
        
        imageScale = new Vector2(1.5f/(maxDistance/1000f),1f);
        
        TrailEffect(trail3.Value,trailPoints.ToArray(),new Color(255, 255, 255,255),new Color(255, 255, 255,255),
            startWidth*0.6f,endWidth*0.6f,startAlpha:1f,endAlpha:1f,
            uTime:uTIme,imageScale:imageScale,blendState:1,
            clipMask:waterNoise3.Value,maskTime:uTIme*2f,threshold:0.2f,maskScale: new Vector2(0.25f/(maxDistance/1000f),1f),
            debugPoint:false,useRforAlpha:false);
        
        EndBeginDraw();
    }
    
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }
    
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitEntities.Add(DamageEntity(target));
        // 玩家弹幕的命中钩子只在拥有者执行；冻结 CD 的权威值由服务器修改。
        if (Main.netMode == NetmodeID.MultiplayerClient)
            RPC(nameof(ReduceTargetFreezeCooldown), [target.whoAmI], KLNetModule.NetSendType.ClientToServer);
        else
            ReduceTargetFreezeCooldown(target.whoAmI);
        base.OnHitNPC(target, hit, damageDone);
    }

    public void ReduceTargetFreezeCooldown(int targetIndex)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient
            && targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex].active)
            Main.npc[targetIndex].ReduceFreezeCooldown(0.5f);
    }
    
    public override bool ShouldUpdatePosition()
    {
        return base.ShouldUpdatePosition();
    }
}

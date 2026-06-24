

using System;
using System.Linq;
using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Water;
using KL.Extensions;
using Terraria.GameContent;
using Terraria.ID;
using 伊蕾娜.Managers;
using DrawHelper = KL.Drawing.DrawHelper;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

public class WaterLaser : ElainaBasicProjectile
{
    private float maxDistance;
    private int time;

    private float startWidth = 2f;
    private float endWidth = 7f;
    
    private List<Vector2> trailPoints = new List<Vector2>(200);
    public override void SetDefaults()
    {
        HeldInfo = new HeldProjInfo(true, 30, 0.2f, 30);
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        maxDistance = 100;
        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        Vector2 move = Projectile.velocity;
        move = move.SafeNormalize(new Vector2(1));
        Projectile.velocity = move;
        
        Projectile.Center = Owner.MountedCenter + move * HeldInfo.HeldDistance;

        LaserCollisionCheck();
        LaserCollisionCheck();

        base.OnSpawn_AllClient();
    }
    
    public override void AI()
    {
        ControlLaser();
        
        startWidth = 2f;
        endWidth = 7f;

        time++;
        
        SpawnWaterDust();
        

        base.AI();
    }

    void ControlLaser()
    {
        if (HasAuthority()&&HeldInfo.IsControlling)
        {
            if (Main.mouseLeft)
            {

            }
            else
            {
                HeldInfo.IsControlling = false;
                Projectile.netUpdate = true;
            }
        }
        Owner.itemTime = 2;
        Owner.itemAnimation = 2;
        
        Projectile.velocity = new Vector2(1, 0).RotatedBy(HeldInfo.RealRotation);
        Projectile.rotation = HeldInfo.RealRotation;
        
        //只有第一个弹幕可以对武器进行控制
        if (NumOfOwingProjectiles == 0)
        {
            Owner.SetCompositeArmFront(true,stretch:Player.CompositeArmStretchAmount.Full,(Projectile.rotation*Owner.gravDir-3.14f/2));
            //根据弹幕位置控制武器位置
            Projectile.ControlWandByHeldProj();
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (trailPoints.Count > 0)
        {
            bool collide = AABBvLineCollision(targetHitbox, Projectile.Center, trailPoints.Last(), 50);
            return collide;
        }
        return base.Colliding(projHitbox, targetHitbox);
    }
    
    void LaserCollisionCheck()
    {
        if(Main.netMode==NetmodeID.Server||Main.gamePaused)return;

        trailPoints.Clear();
        float noiseScale = DrawManager.FrameTime(10,70,30,time); // 振幅，控制波形高度
        float timeEffect = (Main.GameUpdateCount%1800)/2f;

        float rotStrength = MathF.Abs(HeldInfo.CurRotVelocity);
        rotStrength = MathHelper.Lerp(1, 0, rotStrength/0.2f);
        float laserLength = maxDistance+200f;
        if (laserLength > 1200) laserLength = 1200;
        
        float noiseFrequency = 0.01f*maxDistance/1200f; // 噪声频率
        for (int i = 0; i < 200; i++)
        {
            // 使用Perlin噪声替代sin波

            float startEffect = MathHelper.Lerp(0, 1, (i) / 200f);
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
            
            trailPoints.Add(Projectile.Center + Projectile.velocity *maxDistance/200f * i + perpendicular * noiseValue+point);
        }
        
        if(trailPoints.Count<1)return;
        
        //先检测是否和岩浆碰撞
        for (int i = 0; i < trailPoints.Count; i++)
        {
            float currentLength = Vector2.Distance(Projectile.Center, trailPoints[i]);
            if(currentLength>laserLength)break;
            if (Collision.LavaCollision(trailPoints[i], 10, 10))
            {
                laserLength = currentLength+20;
                break;
            }
        }
        
        //Main.NewText(finalLength);
        bool colliding =  LaserCollision(Projectile.Center, trailPoints.Last()-Projectile.Center, 5, ref laserLength,50);


        Vector2 torward = (trailPoints.Last() - Projectile.Center);
        torward.Normalize();
        
        foreach (var npc in Main.ActiveNPCs)
        {
            if (npc.IsAttackable(Owner.dontHurtCritters))
            {
                bool collidingNpc = GetAABBvLineCollisionPoint(npc.Hitbox, Projectile.Center, Projectile.Center+torward*laserLength, 50,out var hitLength);

                if(collidingNpc&&hitLength<laserLength)
                {
                    laserLength = hitLength;
                }
            }
        }

        if (colliding) maxDistance = laserLength;
        else
        {
            float smoothingFactor = 0.7f;  // 保留 90% 当前值 + 10% 目标值
            maxDistance = maxDistance * smoothingFactor + laserLength * (1f - smoothingFactor);

            // 可选：当接近目标时直接设为目标值，避免无限接近
            if (Math.Abs(maxDistance - laserLength) < 0.1f)
            {
                maxDistance = laserLength;
            }   
        }
     
    }
    
    
    
    public override bool PreDraw(ref Color lightColor)
    {
        LaserCollisionCheck();
        DrawLaser();
        EndBeginDraw();
        return base.PreDraw(ref lightColor);
    }

    public void SpawnWaterDust()
    {
        Vector2? lastPoint = trailPoints.Last();
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
        
        base.OnHitNPC(target, hit, damageDone);
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }
    
    public override bool ShouldUpdatePosition()
    {
        return base.ShouldUpdatePosition();
    }
}
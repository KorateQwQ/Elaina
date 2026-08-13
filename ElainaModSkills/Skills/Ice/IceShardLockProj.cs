using System;
using System.Collections.Generic;
using KL.Drawing;
using KL.Drawing.ThreeD;
using KL.Dusts;
using KL.Dusts.Burst;
using KL.Dusts.Glow;
using KL.Dusts.Smoke;
using KL.Dusts.Stone;
using KL.Utils;
using Terraria.ID;


namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public class IceShardLockProj : ElainaBasicProjectile
{
    private readonly List<VisualUnit> units = new();

    private bool initialized;

    private const float SphereRadius = 200f;
    private const float IceConeSpeed = 58f;

    private int time = 0;

    private int burstTime => 110;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 120;
        Projectile.alpha = 0;
        Projectile.hide = true;

        //ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;
        base.SetDefaults();
    }

    public override void AI()
    {
        time++;
        InitializeUnits();
        VisualUnit.UpdateAll(units);

        if (units.Count <= 0 && Projectile.timeLeft < 100)
        {
            Projectile.Kill();
        }

        if (time == burstTime)
        {
            KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<SmokeDust2>(),30, rotRange:6.28f,
                velocity: new Vector2(2), lifeTime: 30,startDistance:10,startOffset:80,
                color: new Color(200, 255, 255, 150), scale: new Vector2(1.5f),velocityOffset:0.5f,scaleOffset:new Vector2(0.5f,0.5f),lifeOffset:15);
            
            KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<IceStone>(),6, rotRange:6.28f,
                velocity: new Vector2(15), lifeTime: 25,startDistance:0,startOffset:10,
                color: new Color(255, 255, 255, 255), scale: new Vector2(0.4f),scaleOffset:new Vector2(0.3f,0.3f),lifeOffset:5);
            
            KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<IceStone>(),5, rotRange:6.28f,
                velocity: new Vector2(15), lifeTime: 40,startDistance:0,startOffset:10,
                color: new Color(255, 255, 255, 255), scale: new Vector2(0.4f),lifeOffset:10);
            
            KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<GlowStone>(),10, rotRange:6.28f,
                velocity: new Vector2(15), lifeTime: 30,startDistance:0,startOffset:10,
                color: new Color(150, 220, 255, 0), scale: new Vector2(2.2f),lifeOffset:10);
            
            KLBasicDust.SpawnDust(Projectile.Center, ModContent.DustType<BurstPoint>(), velocity: Main.rand.NextVector2Circular(0.1f, 0.1f), lifeTime: 12,
                color: new Color(100, 220, 255, 0), scale: new Vector2(1.2f));
            
            KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<BurstDust>(),10, rotRange:6.28f,
                velocity: new Vector2(15), lifeTime: 16,startDistance:0,startOffset:10,
                color: new Color(150, 220, 255, 0), scale: new Vector2(2.2f,1f),lifeOffset:5);
            
            KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LineSparkle>(),10, rotRange:6.28f,
                velocity: new Vector2(25), lifeTime: 16,startDistance:0,startOffset:10,
                color: new Color(150, 220, 255, 0), scale: new Vector2(2.2f,0.2f),lifeOffset:5);
            
            KLBasicDust.SpawnDust(Projectile.Center, ModContent.DustType<GlowDust>(), velocity: Main.rand.NextVector2Circular(0.1f, 0.1f), lifeTime: 12,
                color: new Color(100, 220, 255, 0), scale: new Vector2(3.5f));
        }
        

        base.AI();
    }

    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D star = AssetManager.GetTexture("KL.Effects.Tex.Sparkle.T_StartFlare001");
        Texture2D glow = AssetManager.GetTexture("KL.Effects.Tex.background_Premultiplied");

        int starTime = 50;
        int starStartTime = 70;

        List<FrameInfo> sizeFrames = [ new(0.9f, 0.0f, starTime, TweenEase.EaseOutCubic)]; 
        List<FrameInfo> rotFrames = [ new(0.3f, 3f, starTime, TweenEase.EaseOutCubic)]; 
        List<FrameInfo> bloomFrames = [ new(3, 0f, starTime, TweenEase.SmoothStep)]; 

        float size = EvaluateTween(sizeFrames, time,starStartTime);
        float rot = EvaluateTween(rotFrames, time,starStartTime);
        float bloom = EvaluateTween(bloomFrames, time,starStartTime);
        
        
        EndBeginDraw(0,1);
        ReColorEffect(new Color(150,220,255,0).ToVector4()*bloom);

        if(time>starStartTime) DrawInWorld(star,Projectile.Center,new Color(100,200,255,0),new Vector2(size),rot);
        
        EndBeginDraw();
        List<FrameInfo> glowFrames = [ new(0, 0.7f, 20, TweenEase.SmoothStep)]; 
        int glowStartTime = 90;
        float glowAlpha = EvaluateTween(glowFrames, time,glowStartTime);

        if(time>=starStartTime)DrawInWorld(glow,Projectile.Center,new Color(100,200,255,0)*glowAlpha,new Vector2(2),0);

        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        if(time<burstTime) VisualUnit.DrawAll(units);
        overPlayers.Add(index);
    }

    public override void OnKill(int timeLeft)
    {
        VisualUnit.KillAll(units);
        base.OnKill(timeLeft);
    }

    private void InitializeUnits()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        Vector2 lockCenter = Projectile.Center;
        List<IceConeSpawnData> spawnData = BuildIceCone3DSpawnData(lockCenter, SphereRadius);
        foreach (IceConeSpawnData spawnDataEntry in spawnData)
        {
            Vector2 shootVelocity = (lockCenter - spawnDataEntry.SpawnWorldPosition)
                .SafeNormalize(Vector2.UnitX) * IceConeSpeed;
            Vector3 spawnPosition3D = new(spawnDataEntry.SpawnWorldPosition, spawnDataEntry.StartDepth);
            Vector3 targetPosition3D = new(lockCenter, 0f);
            Vector3 velocity3D = GetInitialVelocity3D(spawnPosition3D, targetPosition3D, shootVelocity.Length());
            VisualUnit.Spawn(units, new IceCone3DUnit(spawnPosition3D, targetPosition3D, velocity3D, spawnDataEntry.TriggerFrame, 135), Projectile);
        }
    }

    private static Vector3 GetInitialVelocity3D(Vector3 spawnPosition3D, Vector3 targetPosition3D, float speed)
    {
        Vector3 toTarget = targetPosition3D - spawnPosition3D;
        Vector3 direction3D = toTarget.LengthSquared() > 0.0001f
            ? Vector3.Normalize(toTarget)
            : Vector3.UnitZ;

        return direction3D * speed;
    }

    private static List<IceConeSpawnData> BuildIceCone3DSpawnData(Vector2 lockCenter, float sphereRadius)
    {
        List<IceConeSpawnData> result = new();
        List<IceConeRotationConfig> rotations = BuildIceConeFixedRotations();
        int triggerFrame = 56;

        int index = 0;
        foreach (IceConeRotationConfig rotation in rotations)
        {
            Vector3 sphereDirection = GetIceConeSphereDirection(rotation);
            Vector3 spawnOffset3D = sphereDirection * sphereRadius;
            Vector2 spawnWorldPosition = lockCenter + new Vector2(spawnOffset3D.X, spawnOffset3D.Y);
            switch (index)
            {
                case 0:
                    result.Add(new IceConeSpawnData(1, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 1:
                    result.Add(new IceConeSpawnData(15, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 2:
                    result.Add(new IceConeSpawnData(27, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 3:
                    result.Add(new IceConeSpawnData(37, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 4:
                    result.Add(new IceConeSpawnData(44, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 5:
                    result.Add(new IceConeSpawnData(49, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 6:
                    result.Add(new IceConeSpawnData(52, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                case 7:
                    result.Add(new IceConeSpawnData(54, spawnWorldPosition, spawnOffset3D.Z));
                    break;
                default:
                    result.Add(new IceConeSpawnData(triggerFrame, spawnWorldPosition, spawnOffset3D.Z));
                    triggerFrame += 2;
                    break;
            }

            index++;
        }

        return result;
    }

    private static List<IceConeRotationConfig> BuildIceConeFixedRotations()
    {
        return new List<IceConeRotationConfig>
        {
            new(-2.8f, 0.2f),
            new(-0.0f, 0.2f),
            new(2.5f, 0.3f),
            new(0.6f, -0.3f),
            new(1.9f, 0.6f),
            new(2.5f, 0.0f),
            new(3.1f, -0.8f),
            new(-0.5f, 0.3f),
            new(-0.3f, -0.6f),
            new(0.2f, 0.6f),
            new(0.3f, -0.6f),
            new(-0.3f, 0.6f),
            new(-0.8f, 0.6f),
            new(-2.6f, -0.4f),
            new(-1f, -0.2f),

        };
    }

    private static Vector3 GetIceConeSphereDirection(IceConeRotationConfig rotation)
    {
        Vector3 baseDirection = Vector3.UnitZ;
        Matrix rotationMatrix = Matrix.CreateFromYawPitchRoll(rotation.Yaw, rotation.Pitch, rotation.Roll);
        Vector3 sphereDirection = Vector3.TransformNormal(baseDirection, rotationMatrix);
        return sphereDirection.LengthSquared() > 0.0001f
            ? Vector3.Normalize(sphereDirection)
            : Vector3.UnitZ;
    }

    private readonly struct IceConeRotationConfig
    {
        public IceConeRotationConfig(float yaw, float pitch, float roll = 0f)
        {
            Yaw = yaw;
            Pitch = pitch;
            Roll = roll;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public float Roll { get; }
    }

    private readonly record struct IceConeSpawnData(int TriggerFrame, Vector2 SpawnWorldPosition, float StartDepth);

}

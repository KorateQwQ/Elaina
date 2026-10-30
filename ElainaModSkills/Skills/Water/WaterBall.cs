using System;
using System.Collections.Generic;
using System.IO;
using KL;
using KL.ActionsSystem;
using KL.DamageSystem;
using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Water;
using KL.Extensions;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;
using 伊蕾娜.Dusts;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.Managers;
using 伊蕾娜.ReProjs.Water;
using 伊蕾娜.ElainaModSkills.Skills.Ice;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

public class WaterBall : ElainaBasicProjectile
{
    private static WaterChargeFlowRenderer chargeFlowRenderer;

    public override void Unload()
    {
        WaterChargeFlowRenderer renderer = chargeFlowRenderer;
        chargeFlowRenderer = null;
        if (renderer != null) Main.QueueMainThreadAction(renderer.Dispose);
    }

    private readonly IceWaterHitBatch iceCycleBatch = new();
    private Vector2 oldPosition;

    private float lastRotation;
    
    //纹理资源声明
    private Asset<Texture2D> waterNoise;
    private Asset<Texture2D> circle;
    private Asset<Texture2D> waterNoise2;
    private Asset<Texture2D> waterNoise3;
    private Asset<Texture2D> waterNoise4;

    public enum State
    {
        Hold,
        Fly
    }


    enum DrawState
    {
        Normal,
        OverPlayer,
    }

    private DrawState drawState;


    //蓄力水球时，水球与玩家的距离
    private float HoldDistance = 70;
    private int time;
    private int time2 = 60;

    public float WaterBallRadius = 50;


    private float drawWidth = 1;
    private float drawHeight = 1;

    private Vector2[] windPoints;
    //伊蕾娜以自身魔力凝聚的水，用于计算水球大小
    private int selfChargeWater = 0;

    private bool born = false;
    private int TotalWaterBallCount = 0;
    
    public override void SetDefaults()
    {
        HoldDistance = 150;
        InfusionElement = ElementType.Water;
        state = State.Hold;
        TrailLength = 30;
        time2 = 60;

        WaterBallRadius = 50;
        Projectile.hide = true;
        Projectile.ignoreWater = true;
        Projectile.width = Projectile.height = 50;
        Projectile.timeLeft = 360;
        Projectile.friendly = true;
        Projectile.penetrate = 1;

        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
        
        selfChargeWater = 0;

        //手持弹幕配置，设置后此弹幕设置为手持属性
        HeldInfo = new HeldProjInfo(true, 360, 0.5f, 100);
        
        //初始化纹理资源
        waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/水波");
        circle = ModContent.Request<Texture2D>("KL/Effects/Tex/光圈");
        waterNoise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5");
        waterNoise3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/6");
        waterNoise4 = ModContent.Request<Texture2D>("KL/Effects/Tex/波纹");
        
        base.SetDefaults();
    }

    public State state;
    private bool needShoot = false;
    private Vector2 mousePosition;
    private int actionToken;

    public void BindAction(int token)
    {
        actionToken = token;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        //writer.WriteVector2(mousePosition);
        writer.Write(needShoot);
        writer.Write(actionToken);
        base.SendExtraAI(writer);
    }


    public override void ReceiveExtraAI(BinaryReader reader)
    {
        //mousePosition = reader.ReadVector2();
        needShoot = reader.ReadBoolean();
        actionToken = reader.ReadInt32();
        base.ReceiveExtraAI(reader);
    }

    public override bool ShouldUpdatePosition()
    {
        if (state == State.Hold) return false;
        return true;
    }
    
    public override void OnSpawn_AllClient()
    {
        Projectile.friendly = false;
        Vector2 move = Projectile.velocity;
        move = move.SafeNormalize(new Vector2(1));
        Projectile.Center = Owner.MountedCenter + move * 100;
        
        //InitChargeTrailPosition();
        base.OnSpawn_AllClient();
    }
    

    public override bool PreAI()
    {
        /*
        KLGlobalProjectile klGlobalProjectile = Projectile.GetGlobalProjectile<KLGlobalProjectile>();
        if (klGlobalProjectile != null&&state==State.Fly)
        {
            klGlobalProjectile.ShouldBeCount = false;
        }
        if (!born)
        {
            born = true;
            foreach (var projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is WaterBall { state: State.Hold })
                {
                    TotalWaterBallCount++;
                }
            }
        }*/
        return base.PreAI();
    }
    //todo: 联机同步,
    //测试多个水球的位置同步，
    //水球大小同步，
    //发射时机同步

    //蓄力至少一秒后射出水球，水球在移动一定距离后会造成锥形范围伤害，否则造成圆形范围伤害
    //水球仅靠自身凝聚成长，不受场地水源或天气影响
    public override void AI()
    {
        if(needShoot)OnShoot();

        Projectile.oldPosition = Projectile.position;

        //Main.NewText(Projectile.whoAmI+" " + Projectile.Center);
        //Main.NewText(Owner.ownedProjectileCounts[Projectile.type]);
        
        if (HasAuthority())
        {
            if (state==State.Hold)
            {
                if(time>60&&!Main.mouseLeft)
                {
                    needShoot = true;
                    HeldInfo.IsControlling = false;
                    HeldInfo.IsHeld = false;
                    Projectile.netUpdate = true;
                }
            }
        }
        
        //根据旋转位置控制弹幕位置
        if (state == State.Hold)
        {

            Projectile.Resize((int)(WaterBallRadius*0.8f), (int)(WaterBallRadius*0.8f));

            Projectile.rotation = HeldInfo.RealRotation;

            UpdateWaterBallActionRotation();
            
            Projectile.velocity = Projectile.Center - Owner.MountedCenter;
            Projectile.velocity.Normalize();

            if(selfChargeWater<=147)
            {
                selfChargeWater+=3;
            }
        }
        else
        {
            if (time2 > 0) time2--;
            if (drawHeight > 0.5f)
            {
                drawHeight -= 0.25f;
            }

            if (drawWidth < 1)
            {
                drawWidth += 0.2f;
            }

            if(Projectile.velocity.Length()>0)Projectile.rotation = Projectile.velocity.ToRotation();
        }


        if (oldPosition != Projectile.Center)
        {
            if ((oldPosition - Projectile.Center).Length() > 5f)
            {
                Vector2 dustVelocity = Projectile.velocity.SafeNormalize(new Vector2(1));
                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<BubbleDust>(), 3, dustVelocity,
                    6.28f, 60, Color.White,
                    new Vector2(1) * WaterBallRadius / 250f, startDistance: WaterBallRadius * 0.3f, startOffset: 20,
                    scaleOffset: new Vector2(0.3f));
            }

            oldPosition = Projectile.Center;
        }
        
        time++;
        WaterBallRadius = 50 + selfChargeWater;
        
        base.AI();
    }

    private void UpdateWaterBallActionRotation()
    {
        ActionModPlayer actionPlayer = Owner.GetModPlayer<ActionModPlayer>();
        if (actionPlayer.CurrentActionToken != actionToken ||
            actionPlayer.CurrentAnimAction is not Action_WaterBall waterBallAction)
        {
            return;
        }

        waterBallAction.UpdateAimFromWaterBall(actionToken, HeldInfo.RealRotation);
    }
    public override void PostAI()
    {
        base.PostAI();
    }

    void OnShoot()
    {
        Projectile.friendly = true;
        state = State.Fly;
        Projectile.velocity *= 35f;
        needShoot = false;
        Projectile.tileCollide = true;
        if (HasAuthority())
        {
            CreateRadialBlur(Projectile.Center,0.01f,4,decay:DecayType.SmoothStep);
            ShakeScreen(Projectile.Center,Projectile.velocity,10f,5f,16);
        }
        
        // 发射水花按较小半径复用锥形爆炸，分量轻于命中爆炸。
        SpawnBurstParticles(Projectile.Center, WaterBallRadius * LaunchBurstScale, Projectile.velocity.ToRotation(), true);

        Vector2 move = new Vector2(1,0);
        int arrayLength = 250;
        windPoints = QuickConePoints( move*WaterBallRadius*0.5f,-move*WaterBallRadius*2f ,arrayLength, WaterBallRadius, WaterBallRadius*0.8f,0.2f);

        drawHeight = 2.5f;
        drawWidth = 0.5f;
        

    }


    public override bool PreDraw(ref Color lightColor)
    {
        DrawChargeFlow(false);
        DrawBall();
        DrawChargeFlow(true);
        EndBeginDraw();
        return base.PreDraw(ref lightColor);
    }

    private void DrawChargeFlow(bool foreground)
    {
        if (Main.dedServ || state != State.Hold) return;
        chargeFlowRenderer ??= new WaterChargeFlowRenderer(Main.instance.GraphicsDevice,
            ModContent.Request<Effect>("伊蕾娜/Effects/Content/WaterChargeFlow", AssetRequestMode.ImmediateLoad).Value,
            path => ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value);
        Main.spriteBatch.End();
        try
        {
            // 一次性汇聚必须使用本水球年龄；VisualTime 是全局时钟，会立即超过结束时间。
            chargeFlowRenderer.Draw(Projectile.Center - Main.screenPosition, WaterBallRadius,
                time / 60f, Projectile.identity, foreground, Main.GameViewMatrix.TransformationMatrix);
        }
        finally
        {
            BeginDraw();
        }
    }

    void DrawBall()
    {
        float radius = WaterBallRadius/255;
        
        Asset<Texture2D> waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/WaterNoise");
        Asset<Texture2D> waterNoise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5");
        Asset<Texture2D> cellnoise = ModContent.Request<Texture2D>("KL/Effects/Tex/cellnoise");
        
        Asset<Texture2D> totalShapeNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/T_VFX_Noise_1");

        Asset<Texture2D> circle = ModContent.Request<Texture2D>("KL/Effects/Tex/光圈");

        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/windNoi", AssetRequestMode.ImmediateLoad).Value;

        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/WaterBallEffect", AssetRequestMode.ImmediateLoad).Value;
        
        EndBeginDraw(2,1,ss:SamplerState.LinearWrap);

        float fadeThreshold = MathF.Min(1,1-time / 60f);
        //time = (int)Main.timeForVisualEffects;
        Vector2 totalShapeNoiseTime = new Vector2(time % 12000 / 100f);
        
        float imageUTime = time % 12000 / 240f;
        float colorAlpha = 1f;
        
        //填充所有纹理采样器
        effect.SetTexture(1,circle.Value);//遮罩，直接绘制
        effect.SetTexture(2,waterNoise2.Value);//噪声，用于扰动内部形状
        effect.SetTexture(3,totalShapeNoise.Value);//噪声，用于扰动整个球体
        effect.SetTexture(4,cellnoise.Value);//消融噪声
        
        //整体绘制大小
        // 留出轮廓扰动空间，绘制缩放补偿后保持原有球体尺寸。
        const float sphereDrawSize = 0.95f;
        effect.SetValue("TotalDrawSize", new Vector2(sphereDrawSize));
        //内部扰动参数
        effect.SetValue("DistortStrength", 0.05f);
        effect.SetValue("DistortTiling", new Vector2(0.5f));
        effect.SetValue("DistortUTime",totalShapeNoiseTime);
        
        //水球整体扰动参数
        effect.SetValue("ShapeDistortStrength", 0.035f);
        effect.SetValue("ShapeDistortTiling", new Vector2(0.5f));
        effect.SetValue("ShapeDistortUTime", totalShapeNoiseTime);
        
        //水球旋转参数
        effect.SetValue("RotWorldX", 0);
        effect.SetValue("RotWorldY", MathF.PI/2f);
        effect.SetValue("RotWorldZ", -MathF.PI/3f);
        effect.SetValue("RotLocalX", 0.0f);
        effect.SetValue("RotLocalY", 0.0f);
        effect.SetValue("RotLocalZ", 0.0f);
        
        //截断效果与内部纹理大小
        effect.SetValue("clipImageX", false);
        effect.SetValue("clipImageY", false);
        effect.SetValue("ImageScale", new Vector2(4f));
        //遮罩绘制大小
        effect.SetValue("MaskScale", new Vector2(1.3f));
        
        //水基础颜色
        effect.SetValue("ColorA", new Vector4(0.8f, 0.9f, 1.0f, 1.0f)*colorAlpha);
        effect.SetValue("ColorB", new Vector4(0.4f, 0.7f, 1.0f, 1.0f)*colorAlpha);
        effect.SetValue("ColorC", new Vector4(0.2f, 0.2f, 1.0f, 1.0f)*colorAlpha);
        // 只增强外缘反射；Power 越小，边缘亮带越宽。
        effect.SetValue("FresnelStrength", 1.35f);
        effect.SetValue("FresnelPower", 5.0f);
        
        //水球内部纹理采样参数
        effect.SetValue("ColorThreshold", new Vector2(0.35f, 0.05f));
        effect.SetValue("smoothWidth", 0.2f);
        
        //内部纹理流动速度
        effect.SetValue("ImageUTime", new Vector2(0, -imageUTime));
        
        //整体消融参数
        effect.SetValue("FadeThreshold", fadeThreshold);
        effect.SetValue("FadeTiling", new Vector2(1f));
        
        effect.Apply();
        DrawInWorld(waterNoise.Value,Projectile.Center,color:Color.White*0.9f,scale:new Vector2(drawWidth,drawHeight)*radius*0.35f/sphereDrawSize,rotation:Projectile.rotation);


        if (state == State.Fly)
        {

            VertexDrawEffect(wind,windPoints,Color.White,Color.White,startAlpha:0.5f, endAlpha:0,blendState:1,
                uTime:new Vector2((float)(Main.timeForVisualEffects%60)/20f,0),
                attachPoint:Projectile.Center,attachRotation:Projectile.rotation,
                debugPoint:false);
            
            VertexDrawEffect(waterNoise2.Value,windPoints,Color.White,Color.White,startAlpha:0.5f, endAlpha:0f,blendState:1,
                uTime:new Vector2((float)(Main.timeForVisualEffects%60)/30f,0),
                clipMask:waterNoise.Value,threshold:0.9f, maskTime:new Vector2((float)(Main.timeForVisualEffects%60)/30f,0),
                useRforAlpha:true, debugPoint:false,
                attachPoint:Projectile.Center,attachRotation:Projectile.rotation);
        }

        EndBeginDraw();
        

    }

    
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (time2 <= 52&&Projectile.penetrate == -1)
        {
            Vector2 dir = new Vector2(1, 0).RotatedBy(Projectile.rotation);
            return AABBvLineCollision(targetHitbox, Projectile.Center - dir * projHitbox.Width / 2f,
                       Projectile.Center + dir * projHitbox.Width * 3.5f, Projectile.height)
                   || AABBvLineCollision(targetHitbox, Projectile.Center- dir * projHitbox.Width / 2f, Projectile.Center + dir * projHitbox.Width*1.5f,
                       Projectile.height * 3);
        }

        return base.Colliding(projHitbox, targetHitbox);
    }


    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        overPlayers.Add(index);

        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    public override void OnKill(int timeLeft)
    {
        // 保留飞行计时，让爆炸伤害与特效都按命中时的飞行阶段选择形状。
        Projectile.penetrate = -1;
        if (time2 > 52)
        {
            Projectile.Resize((int)(WaterBallRadius * 0.8f * 3), (int)(WaterBallRadius * 0.8f * 3));
        }
        Projectile.Damage();

        if ((Projectile.Center - Main.player[Main.myPlayer].Center).Length() < 1800)
        {
            bool cone = time2 <= 52;
            if (!cone) CreateRadialWaveWarp(Projectile.Center, WaterBallRadius, WaterBallRadius * 10.5f, 30, 2.5f);
            SpawnBurstParticles(Projectile.Center, WaterBallRadius, Projectile.rotation, cone);

            Vector2 bubbleVelocity = cone ? new Vector2(1, 0).RotatedBy(Projectile.rotation) * 5 : Vector2.One * 2;
            KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<BubbleDust>(), 25,
                bubbleVelocity * WaterBallRadius / 90f, cone ? 1.8f : 6.28f, 60, new Color(180, 230, 255),
                new Vector2(1) * WaterBallRadius / 200f, startDistance: 20, startOffset: 30, scaleOffset: new Vector2(0.3f),
                lifeOffset: 10, velocityOffset: 0.2f);


            float strength = MathHelper.Lerp(0, 0.01f, WaterBallRadius / 200f);
            CreateRadialBlur(Projectile.Center, strength,8);
            ShakeScreen(Projectile.Center,Projectile.velocity,100f,5f,16);
        }
        base.OnKill(timeLeft);
    }

    internal const float LaunchBurstScale = 0.6f;

    /// <summary>爆炸水花复用水流术的水花，保持同一套青蓝水体与白色浪尖。</summary>
    internal static void SpawnBurstParticles(Vector2 center, float radius, float rotation, bool cone)
    {
        // 只有蓄满（半径 200）才能发射；水花速度按 0.93 衰减，总位移约为初速的 11 倍。
        float sizeScale = 0.45f + radius / 250f;
        float reach = radius * 0.1f;
        Vector2 forward = new(MathF.Cos(rotation), MathF.Sin(rotation));
        // 锥形按 Colliding 的 T 形判定：两侧短而宽（约 1.2 倍半径），中间窄突刺约 2.8 倍半径。
        const float sideSpread = 1.15f;
        const float coreSpread = 0.2f;

        int splashCount = (int)(14 + radius / 10f);
        for (int i = 0; i < splashCount; i++)
        {
            float angle = cone
                ? Main.rand.NextFloat(-sideSpread, sideSpread)
                : MathHelper.TwoPi * (i + Main.rand.NextFloat()) / splashCount;
            Vector2 direction = forward.RotatedBy(angle);
            WaterLaserDust.Emit(center + direction * radius * 0.25f, direction * reach * Main.rand.NextFloat(0.6f, 1.1f),
                new Vector2(110f, 30f) * sizeScale * Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(18, 25));
        }

        if (cone)
        {
            // 速度分层铺开，突刺从根部连到尖端，不会脱离两侧成为独立的一团。
            int coreCount = (int)(10 + radius / 12f);
            for (int i = 0; i < coreCount; i++)
            {
                float angle = Main.rand.NextFloat(-coreSpread, coreSpread);
                Vector2 direction = forward.RotatedBy(angle);
                float layer = (i + Main.rand.NextFloat()) / coreCount;
                WaterLaserDust.Emit(center + direction * radius * 0.25f, direction * reach * MathHelper.Lerp(0.9f, 2.6f, layer),
                    new Vector2(140f, 30f) * sizeScale * Main.rand.NextFloat(0.9f, 1.2f), Main.rand.Next(20, 27));
            }
        }

        // 细小快速的水滴：圆形打散外缘，锥形压在突刺尖端。
        int dropletCount = (int)(8 + radius / 16f);
        for (int i = 0; i < dropletCount; i++)
        {
            float angle = cone
                ? Main.rand.NextFloat(-coreSpread * 0.6f, coreSpread * 0.6f)
                : Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 direction = forward.RotatedBy(angle);
            WaterLaserDust.Emit(center + direction * radius * 0.15f,
                direction * reach * (cone ? Main.rand.NextFloat(2.4f, 3f) : Main.rand.NextFloat(1.2f, 1.7f)),
                new Vector2(64f, 16f) * sizeScale, Main.rand.Next(14, 19));
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        iceCycleBatch.OnHit(Owner, target, damageDone, waterBall: true);
        base.OnHitNPC(target, hit, damageDone);
    }
}

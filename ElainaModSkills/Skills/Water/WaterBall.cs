using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KL;
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
using 伊蕾娜.Managers;
using 伊蕾娜.ReProjs.Water;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

public class WaterBall : ElainaBasicProjectile
{
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
    //以魔力吸收的水，用于计算水球大小
    private int selfChargeWater = 0;
    //从水域或者雨水中获取的水，用于计算水球大小
    public float extraChargeWater = 0;

    private bool born = false;
    private int TotalWaterBallCount = 0;
    
    public override void SetDefaults()
    {
        HoldDistance = 150;
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

    public override void SendExtraAI(BinaryWriter writer)
    {
        //writer.WriteVector2(mousePosition);
        writer.Write(needShoot);
        base.SendExtraAI(writer);
    }


    public override void ReceiveExtraAI(BinaryReader reader)
    {
        //mousePosition = reader.ReadVector2();
        needShoot = reader.ReadBoolean();
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
    //吸收水的判定同步

    //蓄力至少一秒后射出水球，水球在移动一定距离后会造成锥形范围伤害，否则造成圆形范围伤害
    //水球自我成长有上限，通过吸收水可以额外成长至原大小以及伤害1.5倍
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
                            
                if (time % 5 == 0)
                {
                    GenerateWaterTrail();
                }
            }
        }
        
        //根据旋转位置控制弹幕位置
        if (state == State.Hold)
        {

            Projectile.Resize((int)(WaterBallRadius*0.8f), (int)(WaterBallRadius*0.8f));

            //保持弹幕时间以及物品使用时间
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            
            Projectile.rotation = HeldInfo.RealRotation;

            //只有第一个弹幕可以对武器进行控制
            if (NumOfOwingProjectiles == 0)
            {
                Owner.SetCompositeArmFront(true,stretch:Player.CompositeArmStretchAmount.Full,(Projectile.rotation*Owner.gravDir-3.14f/2));
                //根据弹幕位置控制武器位置
                Projectile.ControlWandByHeldProj();
            }
            
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
        WaterBallRadius = 50 + selfChargeWater + extraChargeWater;
        
        base.AI();
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
        Projectile.damage = (int)(Projectile.damage * WaterBallRadius/200);
        Projectile.tileCollide = true;
        if (HasAuthority())
        {
            CreateRadialBlur(Projectile.Center,0.01f,4,decay:DecayType.SmoothStep);
            ShakeScreen(Projectile.Center,Projectile.velocity,10f,5f,16);
        }
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust>(), 20,
            Projectile.velocity, 1.88f, 20, Color.White,
            new Vector2(0.5f) * WaterBallRadius / 100f, startDistance: 0, startOffset: 20, scaleOffset: new Vector2(0.3f),
            lifeOffset: 10, velocityOffset: 0.2f);
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust1>(), 10,
            Projectile.velocity*1.5f, 0.58f, 20, Color.White,
            new Vector2(0.8f) * WaterBallRadius / 100f, startDistance: 0, startOffset: 50, scaleOffset: new Vector2(0.3f),
            lifeOffset: 10, velocityOffset: 0.2f);

        Vector2 move = new Vector2(1,0);
        int arrayLength = 250;
        windPoints = QuickConePoints( move*WaterBallRadius*0.5f,-move*WaterBallRadius*2f ,arrayLength, WaterBallRadius, WaterBallRadius*0.8f,0.2f);

        drawHeight = 2.5f;
        drawWidth = 0.5f;
        

    }


    //指定位置生成水流，水流终点会根据水球的半径变化。
    void GenerateWaterTrail()
    {
        Vector2 waterPosition = default;
        if (GetWaterFromSurface(Main.MouseWorld, 15, ref waterPosition))
        {
            Projectile projectile = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), waterPosition+new Vector2(0,10f), 
                new Vector2(1), ModContent.ProjectileType<WaterTrail>(), 0, 0,0,Projectile.whoAmI);
            if (projectile.ModProjectile is WaterTrail waterTrail)
            {
                Vector2 move = projectile.Center - Projectile.Center;
                move.Normalize();
                move = move.RotatedBy(Main.rand.NextFloat(-3.14f/4f,3.14f/4f));
            
                waterTrail.TargetCenter = Projectile.Center+move*WaterBallRadius*0.3f;
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        DrawBall();
        EndBeginDraw();
        return base.PreDraw(ref lightColor);
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
        effect.SetValue("TotalDrawSize", new Vector2(1f));
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
        
        //水球内部纹理采样参数
        effect.SetValue("ColorThreshold", new Vector2(0.35f, 0.05f));
        effect.SetValue("smoothWidth", 0.2f);
        
        //内部纹理流动速度
        effect.SetValue("ImageUTime", new Vector2(0, -imageUTime));
        
        //整体消融参数
        effect.SetValue("FadeThreshold", fadeThreshold);
        effect.SetValue("FadeTiling", new Vector2(1f));
        
        effect.Apply();
        DrawInWorld(waterNoise.Value,Projectile.Center,color:Color.White*0.9f,scale:new Vector2(drawWidth,drawHeight)*radius*0.35f,rotation:Projectile.rotation);


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

    /// <summary>
    /// 从指定位置，判定边长为length的正方形，从中找到最上层的可用液体
    /// </summary>
    /// <param name="position"></param>
    /// <param name="length"></param>
    bool GetWaterFromSurface(Vector2 position, int length,ref Vector2 result)
    {
        int x = (int)(position.X / 16f);
        int y = (int)(position.Y / 16f);
        
        // 使用字典存储每个x坐标对应的最上层液体瓦片
        Dictionary<int, Vector2> results = new Dictionary<int, Vector2>();
        
        for (int j = y - length/2; j < y + length/2; j++)
        {
            for (int i = x - length/2; i < x + length/2; i++)
            {
                if (i > 0 && i < (int)(Main.rightWorld / 16f) && j > 0 && j < (int)(Main.bottomWorld / 16f))
                {
                    if (IsNormalWater(Main.tile[i, j]))
                    {
                        // 如果该x坐标还没有记录，或者当前瓦片比已记录的更靠上（y值更小）
                        if (!results.ContainsKey(i))
                        {
                            results[i] = new Vector2(i, j) * 16;
                        }
                    }
                }
            }
        }

        if (results.Count > 0)
        {
            result = results.ElementAt(Main.rand.Next(results.Count)).Value;
            return true;
        }
        return false;

    }

    //判定是否为正常的水而非岩浆或蜂蜜
    bool IsNormalWater(Tile t)
    {
        return t is { LiquidType: 0, LiquidAmount: > 0 };
    }

    public override void OnKill(int timeLeft)
    {
        time2 = 20;
        Projectile.penetrate = -1;
        if (time2 > 52)
        {
            Projectile.Resize((int)(WaterBallRadius * 0.8f * 3), (int)(WaterBallRadius * 0.8f * 3));
        }
        Projectile.Damage();

        if ((Projectile.Center - Main.player[Main.myPlayer].Center).Length() < 1800)
        {
            if (time2 > 52)
            {
                CreateRadialWaveWarp(Projectile.Center, WaterBallRadius, WaterBallRadius * 10.5f, 30, 2.5f);

                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust1>(), 20,
                    Vector2.One * 8 * WaterBallRadius / 90f, 6.28f, 20, Color.White,
                    new Vector2(1) * WaterBallRadius / 100f, startDistance: 0, startOffset: 20, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.2f);


                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust>(), 20,
                    Vector2.One * 7 * WaterBallRadius / 85f, 6.28f, 20, new Color(180, 230, 255, 255),
                    new Vector2(1) * WaterBallRadius / 200f, startDistance: 20, startOffset: 30, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.2f);

                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust2>(), 20,
                    Vector2.One * 7 * WaterBallRadius / 85f, 6.28f, 20, new Color(180, 230, 255, 255),
                    new Vector2(1) * WaterBallRadius / 200f, startDistance: 20, startOffset: 30, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.2f);

                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<BubbleDust>(), 25,
                    Vector2.One * 2 * WaterBallRadius / 90f, 6.28f, 60, Color.White,
                    new Vector2(1) * WaterBallRadius / 200f, startDistance: 20, startOffset: 30, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.2f);
            }
            else
            {
                Vector2 move = new Vector2(1,0).RotatedBy(Projectile.rotation);
                /*KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust1>(), 20,
                    move * 8 * WaterBallRadius / 90f, 1.28f, 20, Color.White,
                    new Vector2(1) * WaterBallRadius / 100f, startDistance: 0, startOffset: 20, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.2f);*/


                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust>(), 10,
                    move * 10 * WaterBallRadius / 85f, 1.88f, 20, new Color(180, 230, 255, 255),
                    new Vector2(1,2) * WaterBallRadius / 200f, startDistance: 20, startOffset: 100* WaterBallRadius / 200f, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.5f);
                
                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust>(), 20,
                    move * 20 * WaterBallRadius / 85f, 0.58f, 20, new Color(180, 230, 255, 255),
                    new Vector2(1,2) * WaterBallRadius / 200f, startDistance: 20, startOffset: 200* WaterBallRadius / 200f, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.2f);

                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<WaterDust2>(), 20,
                    move * 7 * WaterBallRadius / 85f, 1.8f, 20, new Color(180, 230, 255, 255),
                    new Vector2(1) * WaterBallRadius / 200f, startDistance: 20, startOffset: 30, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.2f);

                KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<BubbleDust>(), 25,
                    move * 5 * WaterBallRadius / 90f, 1.8f, 60, Color.White,
                    new Vector2(1) * WaterBallRadius / 200f, startDistance: 20, startOffset: 30, scaleOffset: new Vector2(0.3f),
                    lifeOffset: 10, velocityOffset: 0.2f);
            }


            float strength = MathHelper.Lerp(0, 0.01f, WaterBallRadius / 200f);
            CreateRadialBlur(Projectile.Center, strength,8);
            ShakeScreen(Projectile.Center,Projectile.velocity,100f,5f,16);
        }
        base.OnKill(timeLeft);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }
}
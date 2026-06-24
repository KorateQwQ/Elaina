using System;
using System.Collections.Generic;
using System.IO;
using Humanizer;
using KL.Dusts;
using KL.Extensions;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

public class FireLaser : ElainaBasicProjectile
{
    private Effect effect;
    private int time;

    private float MagicCenterSize = 0;
    private Vector2 mousePosition;
    private float lastRotation;
    
    private Vector2[] windPoints;
    private Vector2[] windPoints2;

    float maxDistance = 1000;
    enum State
    {
        Spawn,
        Normal,
        Dead
    }
    
    public override void SendExtraAI(BinaryWriter writer)
    {
        //writer.WriteVector2(mousePosition);
        base.SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        //mousePosition = reader.ReadVector2();
        base.ReceiveExtraAI(reader);
    }

    public override void SetDefaults()
    {
        effect ??= ModContent.Request<Effect>("伊蕾娜/Effects/Content/LaserEffect", AssetRequestMode.ImmediateLoad).Value;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 30;
        
        HeldInfo = new HeldProjInfo(true, 30, 0.2f, 100);

        

        base.SetDefaults();
    }

    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    public override void OnSpawn_AllClient()
    {
        Vector2 move = new Vector2(1, 0);
        windPoints = QuickConePoints( move*100,-move*200f ,100, 200, 200*0.8f,0.2f);
        
        windPoints2 = QuickConePoints( move*100,-move*150f ,100, 120, 100*0.8f,0.2f);

        move = Projectile.velocity;
        move = move.SafeNormalize(new Vector2(1));
        Projectile.Center = Owner.MountedCenter + move * 10;
        

        base.OnSpawn_AllClient();
    }

    public override void AI()
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
        
        //只有第一个弹幕可以对武器进行控制
        if (NumOfOwingProjectiles == 0)
        {
            Owner.SetCompositeArmFront(true,stretch:Player.CompositeArmStretchAmount.Full,(Projectile.rotation*Owner.gravDir-3.14f/2));
            //根据弹幕位置控制武器位置
            Projectile.ControlWandByHeldProj();
        }
        
        //根据旋转位置控制弹幕位置
        //Projectile.Center = Owner.MountedCenter + new Vector2(1, 0).RotatedBy(Projectile.rotation) * 10;
        //根据弹幕位置控制武器位置
        
        Projectile.velocity = Projectile.Center - Owner.MountedCenter;
        Projectile.velocity.Normalize();
        Projectile.rotation = Projectile.velocity.ToRotation();
        
        //Projectile.velocity = Projectile.Center - Owner.MountedCenter;
        //Projectile.velocity.Normalize();
        
        float laserLength = 1500;
        if (LaserCollision(Projectile.Center, Projectile.velocity, 20, ref laserLength))
        {
            KLBasicDust.SpawnDustsCircle(Owner.WandCenter()+Projectile.velocity*laserLength, ModContent.DustType<LineRotSparkle>(), 3, -Projectile.velocity*12.1f, 
                1.3f,20, new Color(255, 120, 35,255),Vector2.One,0,250,new Vector2(0.8f,0f),7);
        }

        maxDistance = laserLength;
        
        

        base.AI();
    }

    void CheckWaterCollision()
    {
        if(Main.netMode==NetmodeID.Server)return;
        
    }

    public override bool PreDraw(ref Color lightColor)
    {
        time++;
        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/windNoi", AssetRequestMode.ImmediateLoad).Value;
        Asset<Texture2D> trail3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/T_Trail_01", AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/水波",AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> waterNoise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5");
        Asset<Texture2D> trail = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LineTrail", AssetRequestMode.ImmediateLoad);

        float width = DrawManager.FrameTime(0.25f, 0.3f, 30, time);

        float length = 3f;
        
        float xScale = (maxDistance-5)/(waterNoise.Width()*length);
        
        
        /*EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        CommonMagicEffect(0.0f,0,new Vector4(1f,0.6f,0.1f,1f)*1.1f,
            insideTex:TextureAssets.MagicPixel.Value, outsideTex:TextureAssets.MagicPixel.Value,
            scale:new Vector2(0.5f,1f),scaleOutside:new Vector2(1f,1),
            uTime:new Vector2(1-time / 20f,0/*1-time / 65f#1#),uTimeOut:new Vector2(0,0));

        /*
        Main.spriteBatch.Draw(trail3.Value, Projectile.Center+Projectile.velocity*15f - Main.screenPosition, new Rectangle(0,0,(int)(trail3.Size().X*xScale),(int)trail3.Size().Y),
            Color.White, Projectile.rotation, new Vector2(0,trail3.Size().Y/2f), new Vector2(length*0.25f,width*0.3f), 0, 0);
            #1#

        
        EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        CommonMagicEffect(0.0f,0,new Vector4(1f,0.6f,0.5f,1f)*1.3f,
            insideTex:waterNoise2.Value, outsideTex:TextureAssets.MagicPixel.Value,
            scale:new Vector2(1f,1f),scaleOutside:new Vector2(0.3f,1),
            uTime:new Vector2(1-time / 20f,0/*1-time / 65f#1#),uTimeOut:new Vector2(1-time / 10f,0));
        Main.spriteBatch.Draw(trail.Value, Projectile.Center+Projectile.velocity*15f - Main.screenPosition, new Rectangle(0,0,(int)(trail.Size().X*xScale),(int)trail.Size().Y), 
            Color.White, Projectile.rotation, new Vector2(0,trail.Size().Y/2f), new Vector2(length*0.5f,width*2f), 0, 0);



        VertexDrawEffect(wind,windPoints,Color.White,Color.White,startAlpha:1f, endAlpha:0,blendState:1,drawTimes:1,
            uTime:new Vector2((float)(Main.timeForVisualEffects%60)/20f,0),
            attachPoint:Projectile.Center,attachRotation:Projectile.rotation,
            useRforAlpha:false,debugPoint:false);*/

        //DrawLaser();
        
        DrawMagicBall();
        DrawLaser();
        return base.PreDraw(ref lightColor);
    }
    
    void DrawLaser()
    {
        Asset<Texture2D> waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/水波",AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> waterNoise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5");
        Asset<Texture2D> trail = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/T_GPBAR_line01_emis", AssetRequestMode.ImmediateLoad);

        Asset<Texture2D> waterNoise3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/4");

        Asset<Texture2D> noise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5");
        Asset<Texture2D> headClip = ModContent.Request<Texture2D>("KL/Effects/Tex/射灯",AssetRequestMode.ImmediateLoad);//ModContent.Request<Texture2D>("KL/Effects/Tex/EnergyFlow");
        Asset<Texture2D> headClip2 = ModContent.Request<Texture2D>("KL/Effects/Tex/射灯2",AssetRequestMode.ImmediateLoad);//ModContent.Request<Texture2D>("KL/Effects/Tex/EnergyFlow");
        Asset<Texture2D> trail2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/EnergyFlow", AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> trail3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/T_Trail_01", AssetRequestMode.ImmediateLoad);

        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind3", AssetRequestMode.ImmediateLoad).Value;
        Texture2D wind2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/windNoi", AssetRequestMode.ImmediateLoad).Value;

        float width = DrawManager.FrameTime(0.25f, 0.3f, 30, time);

        float length = 3f;
        
        float xScale = (maxDistance-5)/(waterNoise.Width()*length);
        
        EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        CommonMagicEffect(0.0f,0,new Vector4(1f,0.6f,0.1f,1f)*1.5f,
            insideTex:TextureAssets.MagicPixel.Value, outsideTex:TextureAssets.MagicPixel.Value,
            scale:new Vector2(0.5f,1f),scaleOutside:new Vector2(1f,1),
            uTime:new Vector2(1-time / 20f,0/*1-time / 65f*/),uTimeOut:new Vector2(0,0));

        /*
        Main.spriteBatch.Draw(trail3.Value, Projectile.Center+Projectile.velocity*15f - Main.screenPosition, new Rectangle(0,0,(int)(trail3.Size().X*xScale),(int)trail3.Size().Y),
            Color.White, Projectile.rotation, new Vector2(0,trail3.Size().Y/2f), new Vector2(length*0.25f,width*0.3f), 0, 0);
            */

        
        EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        CommonMagicEffect(0.0f,0,new Vector4(1f,0.6f,0.3f,1f)*2.2f,
            insideTex:waterNoise2.Value, outsideTex:TextureAssets.MagicPixel.Value,
            scale:new Vector2(0.5f,1f),scaleOutside:new Vector2(0.3f,1),
            uTime:new Vector2(1-time / 20f,0/*1-time / 65f*/),uTimeOut:new Vector2(1-time / 10f,0));
        Main.spriteBatch.Draw(trail.Value, Projectile.Center+Projectile.velocity*15f - Main.screenPosition, new Rectangle(0,0,(int)(trail.Size().X*xScale),(int)trail.Size().Y), 
            Color.White, Projectile.rotation, new Vector2(0,trail.Size().Y/2f), new Vector2(length*1f,width*1.5f), 0, 0);


        
        EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        CommonMagicEffect(0.3f,0,new Vector4(1f,0.5f,0.1f,1f)*2.5f,
            insideTex:waterNoise2.Value, outsideTex:trail2.Value,
            scale:new Vector2(0.8f,1.5f),scaleOutside:new Vector2(0.3f,1),
            uTime:new Vector2(1-time / 20f,1-time / 65f/*1-time / 65f*/),uTimeOut:new Vector2(1-time / 10f,0));
        
            Main.spriteBatch.Draw(waterNoise3.Value, Projectile.Center+Projectile.velocity*15f - Main.screenPosition, new Rectangle(0,0,(int)(waterNoise.Size().X*xScale),(int)waterNoise.Size().Y), 
            Color.White, Projectile.rotation, new Vector2(0,waterNoise.Size().Y/2f), new Vector2(length,width), 0, 0);

        
        
        
        float headDistance = maxDistance - 180f;
        if(headDistance<-80)headDistance = -80;
        

        
        
        Vector2 move = new Vector2(1, 0);
        //火焰顶端
        VertexDrawEffect(wind,windPoints2,new Color(255, 150, 30,255),new Color(255, 150, 30,0),startAlpha:3.6f, endAlpha:0,blendState:1,drawTimes:1,
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
        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/MagicMissileEffect", AssetRequestMode.ImmediateLoad).Value;
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
        
        float clipValue = 0.1f;
        float totalAlpha = 1;
        effect.Parameters["uTime"].SetValue(time/60f);
        effect.Parameters["clipValue"].SetValue(clipValue);
        effect.Parameters["clipValue2"].SetValue(clipValue);

        effect.Parameters["Edge"].SetValue(0.2f);
        effect.Parameters["EdgeColor"].SetValue(new Vector4(1f,0.7f,0.8f,1)*1);
        effect.Parameters["imageColor"].SetValue(new Vector4(1f,0.7f,0.3f,1f)*2.8f * totalAlpha);
        
        Main.graphics.GraphicsDevice.Textures[1] = waterNoise.Value;
        Main.graphics.GraphicsDevice.Textures[2] = headClip.Value;  
        
        EndBeginDraw(2,shader:null,ss:SamplerState.LinearWrap,adjustToScreen:true);
        
        /*Main.spriteBatch.Draw(waterNoise.Value, Owner.MountedCenter+Projectile.velocity*85f - Main.screenPosition, waterNoise.Value.GetRec(),
            Color.White, Projectile.rotation, waterNoise.Value.Origin(), new Vector2(1)* 0.3f, 0, 0);*/

        EndBeginDraw(2,1,ss:SamplerState.LinearWrap,adjustToScreen:true);
        CommonMagicEffect(0.8f,0,insideTex:waterNoise.Value,outsideTex:headClip.Value,
            imageColor:new Vector4(1f,0.7f,0.3f,1f)*3.7f*totalAlpha,uTime:new Vector2(time/60f));
        
        Main.spriteBatch.Draw(waterNoise.Value, Owner.MountedCenter+Projectile.velocity*95f - Main.screenPosition, waterNoise.Value.GetRec(),
            Color.White, Projectile.rotation, waterNoise.Value.Origin(), new Vector2(1)* 0.3f, 0, 0);
        
        /*
        KLBasicDust.SpawnDustsCircle(Owner.WandCenter()+Projectile.velocity*25f, ModContent.DustType<LineSparkle>(), 1, Projectile.velocity*18.1f, 
            MathHelper.Pi*2f,20, new Color(255, 120, 35,255),new Vector2(0.8f,1f),0,70,new Vector2(0.5f,0f),7,attachedEntity:Owner);
            */
        
        
        
        EndBeginDraw(2,shader:rotEffect,ss:SamplerState.LinearClamp,adjustToScreen:true);
        rotEffect.Parameters["rot"].SetValue(time/20f);
        rotEffect.Parameters["scale"].SetValue(0.7f);
        rotEffect.Parameters["bloomColor"].SetValue(new Vector4(1f,0.4f,0.2f,1f)*5.5f * totalAlpha);

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
    
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }
}
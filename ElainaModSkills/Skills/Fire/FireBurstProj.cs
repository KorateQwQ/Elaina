using System.Collections.Generic;
using KL.Drawing;
using KL.Dusts;
using KL.Extensions;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

public class FireBurstProj : ElainaBasicProjectile
{
    private int time;
    List<Vector2> windPoints;

    public override void SetDefaults()
    {
        Projectile.timeLeft = 30;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.hide = true;

        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.localNPCHitCooldown = 30;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.height = Projectile.width = 50;

        base.SetDefaults();
    }
    
    public override void OnSpawn_AllClient()
    {
        KLBasicDust.SpawnDustsCircle(Projectile.Center+new Vector2(0,30), ModContent.DustType<ShockDust>(), 10, new Vector2(0,-18), 
            1.9f,20, new Color(255, 120, 35,255),new Vector2(0.3f,0.15f),
            0,20,new Vector2(0.8f,0f),7);
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center+new Vector2(0,50), ModContent.DustType<ShockBlackDust>(), 20, new Vector2(0,-25), 
            1.3f,20, new Color(0, 0, 0,255),new Vector2(0.3f,0.15f),
            0,20,new Vector2(0.8f,0f),7);

        
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LineSparkle>(), 30, new Vector2(0,-22), 
            1.9f,20, new Color(255, 120, 35,255),new Vector2(2f,3.5f),
            0,50,new Vector2(0.8f,0f),7);

        CreateRadialBlur(Projectile.Center,0.005f,16,iteration:5);
        base.OnSpawn_AllClient();
    }
    
    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    public override void AI()
    {
        time++;
        //windPoints = GetCircleRingVertices(0,200, 30);

        Lighting.AddLight(Projectile.Center+new Vector2(0,50),new Vector3(1,0.5f,0.1f)*1f/time);
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D noise1  =  ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5", AssetRequestMode.ImmediateLoad).Value;
        Texture2D HeadClip = ModContent.Request<Texture2D>("KL/Effects/Tex/lightMask", AssetRequestMode.ImmediateLoad).Value;
        Texture2D trail = ModContent.Request<Texture2D>("KL/Effects/Tex/光柱", AssetRequestMode.ImmediateLoad).Value;
        Texture2D wave = ModContent.Request<Texture2D>("KL/Effects/Tex/光圈", AssetRequestMode.ImmediateLoad).Value;

        List<FrameInfo> heightInfos =
        [
            new(0.5f, 4, 10),
            new(4, 3, 25),
            new(3, 0, 30)
        ];

        List<FrameInfo> widthInfos =
        [
            new(7, 1, 7, FrameType.SmoothStep),
            new(1, 1, 15),
            new(1, 0, 25)
        ];

        float height = GetFrameValue(heightInfos,time,clamp:true);
        float width = GetFrameValue(widthInfos,time,clamp:true);

        
        List<FrameInfo> heightInfos2 =
        [
            new(3f, 4, 10),
            new(4, 10, 25),
        ];

        List<FrameInfo> widthInfos2 =
        [
            new(8, 1, 10, FrameType.SmoothStep),
            new(1, 0, 15),
        ];
        
        float height2 = GetFrameValue(heightInfos2,time,clamp:true);
        float width2 = GetFrameValue(widthInfos2,time,clamp:true);
        
        EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        CommonMagicEffect(0.0f, 0.15f, new Vector4(1f, 0.5f, 0.1f, 1f) * 2.5f, 
            insideTex: noise1, outsideTex: HeadClip,
            scale: new Vector2(1),scaleOutside:new Vector2(1),
            uTime:new Vector2(time / 45f,0));
            
        DrawInWorld(new TextureInfo(trail,originOffset:new Vector2(trail.Width/2,0)),Projectile.Center,scale:new Vector2(height,width),rotation:3.14f/2);

        DrawInWorld(new TextureInfo(trail,originOffset:new Vector2(trail.Width/2,0)),Projectile.Center,scale:new Vector2(height2,width2),rotation:3.14f/2);

        EndBeginDraw();

        DrawBlackDust();
        
        return base.PreDraw(ref lightColor);
    }
    
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        behindNPCsAndTiles.Add(index);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }


    void DrawBlackDust()
    {
        Texture2D noise1  = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5", AssetRequestMode.ImmediateLoad).Value;
        Texture2D noise2  =  ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/6", AssetRequestMode.ImmediateLoad).Value;

        Texture2D HeadClip = ModContent.Request<Texture2D>("KL/Effects/Tex/lightMask", AssetRequestMode.ImmediateLoad).Value;
        Texture2D wave = ModContent.Request<Texture2D>("KL/Effects/Tex/光圈", AssetRequestMode.ImmediateLoad).Value;

        List<FrameInfo> alphaInfos = new List<FrameInfo>();
        alphaInfos.Add(new FrameInfo(0f,10,10));
        alphaInfos.Add(new FrameInfo(10,1,15));
        alphaInfos.Add(new FrameInfo(1f,0,30));

        List<FrameInfo> scaleInfos = new List<FrameInfo>();
        scaleInfos.Add(new FrameInfo(0f,0.8f,15));
        scaleInfos.Add(new FrameInfo(0.8f,1,30));
        
        float alpha =  GetFrameValue(alphaInfos,time,clamp:true);
        float scale = GetFrameValue(scaleInfos,time,clamp:true);

        EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
        CommonMagicEffect(0.2f, 0.1f, new Vector4(0f, 0.0f, 0.0f, alpha), 
            insideTex: noise2, outsideTex: HeadClip,
            scale:new Vector2(0.8f),scaleOutside:new Vector2(1),
            uTime:new Vector2(time / 25f,0));
            
        DrawInWorld(new TextureInfo(noise1,originOffset:new Vector2(noise1.Width/2f,0)),Projectile.Center,scale:new Vector2(3,1),rotation:3.14f/2);
        
        
        EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
        CommonMagicEffect(0.8f, 0.3f, new Vector4(0f, 0.0f, 0.0f, alpha), 
            insideTex: noise2, outsideTex: HeadClip,
            scale:new Vector2(0.8f),scaleOutside:new Vector2(1),
            uTime:new Vector2((time+20) / 25f,0));
            
        DrawInWorld(new TextureInfo(noise1,originOffset:new Vector2(noise1.Width/2f,0)),Projectile.Center+new Vector2(-50,-200),scale:new Vector2(3,1),rotation:3.14f/2);
        DrawInWorld(new TextureInfo(noise1,originOffset:new Vector2(noise1.Width/2f,0)),Projectile.Center+new Vector2(50,-300),scale:new Vector2(3,1),rotation:3.14f/2);

        
        /*CircleRingVertexEffect(noise1,windPoints.ToArray(),50,Color.White,Color.White,
            debugPoint:true,attachPoint:Projectile.Center);*/

        EndBeginDraw();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return GamePlayStatic.AABBvLineCollision(targetHitbox,Projectile.Center,Projectile.Center+new Vector2(0,-600),250);
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
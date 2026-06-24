using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

public class FireBurstStarProj : ElainaBasicProjectile
{
    private int time = 0;
    public override void SetDefaults()
    {
        Projectile.tileCollide = false;
        Projectile.timeLeft = 30;
        Projectile.friendly = false;

        Projectile.rotation = Main.rand.NextFloat(-MathF.PI, MathF.PI);
        Projectile.hide = true;

        Projectile.width = Projectile.height = 10;
        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        base.OnSpawn_AllClient();
    }
    
    public override void AI()
    {
        time++;
        Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.5f, 0.2f)*Projectile.timeLeft/30f);
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Draw();
        EndBeginDraw();

        return base.PreDraw(ref lightColor);
    }
    

    void Draw()
    {
        Texture2D line  =  ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/HShotA", AssetRequestMode.ImmediateLoad).Value;
        Texture2D circle  =  ModContent.Request<Texture2D>("KL/Effects/Tex/光圈渐变", AssetRequestMode.ImmediateLoad).Value;

        List<FrameInfo> lengthInfo = new List<FrameInfo>();
        lengthInfo.Add(new FrameInfo(1.8f,0.6f,10));
        lengthInfo.Add(new FrameInfo(0.6f,0.5f,30));

        List<FrameInfo> widthInfo = new List<FrameInfo>();
        widthInfo.Add(new FrameInfo(0.2f,0.0f,30));


        List<FrameInfo> rotateInfos = new List<FrameInfo>();
        rotateInfos.Add(new FrameInfo(0.01f,1.5f,30));
        
        float length = GetFrameValue(lengthInfo, time);
        float width = GetFrameValue(widthInfo, time)*0.5f;

        float rotate = GetFrameValue(rotateInfos, time);

        float totalAlpha = DrawManager.FrameTime(1, 2, 20, time);
        
        if (time > 12 && time % 4 == 0)
        {
            
        }
        else
        {
            EndBeginDraw(2,1);
            ReColorEffect(new Vector4(1f, 0.5f, 0.3f, 1f) * 3.5f*totalAlpha);
            DrawInWorld(line,Projectile.Center,Color.White,scale: new Vector2(length,width),Projectile.rotation+rotate);
            DrawInWorld(line,Projectile.Center,Color.White,scale: new Vector2(length,width),Projectile.rotation+rotate+MathF.PI/2f);
        }
        int startFrame = 0;
        if (time > startFrame)
        {
            EndBeginDraw(2,1);
            int frame = (int)MathHelper.Lerp(0, 3, time / 30f);
            if(frame > 3)frame = 3;

        
            List<FrameInfo> scaleInfos2 = new List<FrameInfo>();
            scaleInfos2.Add(new FrameInfo(0.0f,0.2f,10));
            scaleInfos2.Add(new FrameInfo(0.2f,0.25f,30));

        
            List<FrameInfo> alphaInfos = new List<FrameInfo>();
            alphaInfos.Add(new FrameInfo(5f,2.5f,10));
            alphaInfos.Add(new FrameInfo(2.5f,0.0f,30));
        
            float alpha =GetFrameValue(alphaInfos, time,startFrame);
            ReColorEffect(new Vector4(1f, 0.5f, 0.1f, 1f) * alpha*totalAlpha);
        
            float scale2 = GetFrameValue(scaleInfos2, time,startFrame)*1f;

            List<FrameInfo> rotateInfos2 = new List<FrameInfo>();
            rotateInfos2.Add(new FrameInfo(0.01f,2.5f,30));
        
            float rotate2 = GetFrameValue(rotateInfos2, time,startFrame);

        
            DrawInWorld(new TextureInfo(circle,frame,2,2),Projectile.Center,Color.White,scale: new Vector2(2.5f)*scale2,
                Projectile.rotation + rotate2 +MathF.PI/2f);
        }
    }

    
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        overPlayers.Add(index);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }
    
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }

    public override void OnKill(int timeLeft)
    {
        Projectile projectile = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),Projectile.Center ,
            Vector2.Zero, ModContent.ProjectileType<FireBurstProj>(), Projectile.damage, Projectile.knockBack);
        base.OnKill(timeLeft);
    }
    
    public override bool ShouldUpdatePosition()
    {
        return false;
    }
}
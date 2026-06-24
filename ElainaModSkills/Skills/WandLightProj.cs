using System;
using System.Collections.Generic;
using KL.Utils;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;
using 伊蕾娜.Items;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaModSkills.Skills;

public class WandLightProj : KLProjectile
{
    private int time = 0;
    private Asset<Texture2D> Line;
    private Asset<Texture2D> subCircle;
    private Asset<Texture2D> trail;

    private Color currentColor = new Color(255, 160, 239);
    private float totalAlpha = 1;
    private Vector2 posVec;
    public override void SetDefaults()
    {
        Line ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLine", AssetRequestMode.ImmediateLoad);
        subCircle ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/HShotA", AssetRequestMode.ImmediateLoad);
        trail ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LightTrail", AssetRequestMode.ImmediateLoad);

        Projectile.GetGlobalProjectile<TimeStopManager.TimeStopGlobalProjectile>().ImmuneTimeStop = true;
        Projectile.timeLeft = 30;
        Projectile.friendly = false;
        Projectile.hide = true;
        TrailLength = 30;
        Projectile.tileCollide = false;
        base.SetDefaults();
    }

    public override void OnSpawn(IEntitySource source)
    {
        base.OnSpawn(source);
    }

    public override void AI()
    {
        if (Owner.ownedProjectileCounts[Projectile.type] > 1)
        {
            foreach(Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == Projectile.type&&p!=Projectile)
                {
                    p.Kill();
                }
            }
        }
        time++;
        if (Owner.itemAnimation > 0)
        {
            if (Owner.HeldItem.type == ModContent.ItemType<ElainaWand>())
            {
                Projectile.timeLeft = 60;
                Projectile.Center =Owner.MountedCenter+ new Vector2(1*Owner.direction, 0).RotatedBy(Owner.itemRotation)*35f;
                posVec = Projectile.Center-Owner.MountedCenter;
            }
        }
        else
        {
            Projectile.Center = Owner.MountedCenter + posVec;
        }


        if (Projectile.timeLeft >= 60)
        {
            totalAlpha = 1;
        }
        else
        {
            totalAlpha = Projectile.timeLeft / 60f;
        }
        currentColor = new Color(255, 160, 239,155);//Normalmagic
        //currentColor = new Color(30, 116, 224,255);//water
        //currentColor = new Color(255, 150, 30,255);//fire
        Lighting.AddLight(Projectile.Center, currentColor.ToVector3()*totalAlpha);

        //永续看看效果（
        /*Projectile.timeLeft = 60;
        RotateToPosition(Main.MouseWorld);
        //根据旋转位置控制弹幕位置
        Projectile.Center = Owner.MountedCenter + new Vector2(1, 0).RotatedBy(Projectile.rotation) * 50;
        //根据弹幕位置控制武器位置
        Projectile.ControlWand(Projectile.Center);
        Projectile.rotation = 0;*/
        
        base.AI();
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        overPlayers.Add(index);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        base.PreDraw(ref lightColor);
        time = (int)Main.timeForVisualEffects;
        EndBeginDraw(1,1);
        float timeEffect = DrawManager.FrameTime(0, 1, 30,time);
        Vector2 scale = Vector2.Lerp(new Vector2(0.75f, 0.05f), new Vector2(0.78f, 0.1f),timeEffect);
        float alpha = MathHelper.Lerp(0.5f, 0.6f, timeEffect)*totalAlpha;
        Color c = currentColor;
        c.A = (byte)(c.A*alpha);
        ReColorEffect(new Vector4(1)*3.5f);
        DrawInWorld(Line.Value,Projectile.Center,c,scale);
        
        DrawShortLine2();
        DrawRotLine();
        
        if (OldCenter != null)
        {
            TrailEffect(trail.Value,OldCenter,currentColor * totalAlpha,currentColor*0f,5.5f,0,
                totalAlpha*1.5f,totalAlpha*5.5f,drawTimes:1,blendState:1);
        }

        
        EndBeginDraw();
        
        
        return false;
    }
    

    void DrawRotLine()
    {
        //MiscHelper.WandCenter(Owner);
        EndBeginDraw(1,1);
        
        ReColorEffect(Color.White.ToVector4() * 1.5f);

        float time1 = ((time+5) % 30) / 30f;
        float rot1 = MathHelper.Lerp(0.7f, 2.2f,time1 );
        float alpha1 = MathHelper.Lerp(1, 0.0f,time1)*totalAlpha;
        Vector2 scale1 = new Vector2(1.2f, 0.5f);
        
        float time2 = (time % 50) / 50f;
        float rot2 = MathHelper.Lerp(1.2f, 2.2f,time2 );
        float alpha2 = MathHelper.Lerp(1, 0,time2)*totalAlpha;
        Vector2 scale2 = new Vector2(1.4f, 0.5f);

        /*
        for (int i = 0; i < 5; i++)
        {
            DrawInWorld(Line.Value,Projectile.Center,Color.White*alpha1,scale1,rot1);
        }
        
        
        for (int i = 0; i < 5; i++)
        {
            DrawInWorld(Line.Value,Projectile.Center,Color.White*alpha2,new Vector2(1.2f,0.05f),rot2);
        }
        */
        
        Vector2 toward = new Vector2(1, 0).RotatedBy(Projectile.rotation+rot1);
        float radius = 100;
        int count = 1;
        float totalScale = 0.5f;
        float middleHeight = 1f;
        float subHeight = 0.1f;
        Color finalColor1 = currentColor;
        Color finalColor2 = currentColor;
        finalColor1.A = (byte)(255*alpha1);
        finalColor2.A = (byte)(255*alpha2);

        for (int i = 0; i < count; i++)
        {
            float eachHeight = MathHelper.Lerp(middleHeight,subHeight, Math.Abs((float)i / count - 0.5f) * 10f);

            DrawInWorld(Line.Value, Projectile.Center + toward*(i+0.5f)*radius/count - toward*radius/2f, finalColor1, 
                alpha1*new Vector2(1f,0.5f*eachHeight) * totalScale*scale1,rot1);
        }   
        toward = new Vector2(1, 0).RotatedBy(Projectile.rotation+rot2);
        radius = 30;
        count = 7;
        for (int i = 0; i < count; i++)
        {
            float eachHeight = MathHelper.Lerp(middleHeight,subHeight, Math.Abs((float)i / count - 0.5f) * 2f);

            DrawInWorld(Line.Value, Projectile.Center + toward*(i+0.5f)*radius/count - toward*radius/2f, finalColor2, 
                alpha2*new Vector2(1f,0.5f*eachHeight) * totalScale*scale2,rot2);
        }   
    }
    void DrawShortLine()
    {
        //MiscHelper.WandCenter(Owner);
        EndBeginDraw(1,1);
        
        ReColorEffect(currentColor.ToVector4() * 4f, ReColorState.NoBlack);


        float timeEffect1 = DrawManager.FrameTime(0, 1, 38,time+5);
        Vector2 scale1 = Vector2.Lerp(new Vector2(0.3f, 0.07f), new Vector2(0.4f, 0.07f),timeEffect1);
        float alpha1 = MathHelper.Lerp(0.5f, 1, timeEffect1)*totalAlpha;

        float timeEffect2 = DrawManager.FrameTime(0, 1, 28,time+15);
        Vector2 scale2 = Vector2.Lerp(new Vector2(0.35f, 0.05f), new Vector2(0.5f, 0.05f),timeEffect2);
        float alpha2 = MathHelper.Lerp(0.5f, 1, timeEffect2)*totalAlpha;
        
        DrawInWorld(Line.Value,Projectile.Center,Color.White*alpha1,scale1,0.5f);
        
        DrawInWorld(Line.Value,Projectile.Center,Color.White*alpha2,scale2,1.8f);
        
    }

    void DrawShortLine2()
    {
        EndBeginDraw(1,1);
        ReColorEffect(Color.White.ToVector4() * 2.2f);

        float timeEffect1 = DrawManager.FrameTime(0, 1, 38,time+5);
        Vector2 scale1 = Vector2.Lerp(new Vector2(0.3f, 1f), new Vector2(0.4f, 0.6f),timeEffect1);
        float alpha1 = MathHelper.Lerp(0.5f, 1, timeEffect1)*totalAlpha;
        
        float timeEffect2 = DrawManager.FrameTime(0, 1, 28,time+15);
        Vector2 scale2 = Vector2.Lerp(new Vector2(0.35f, 0.8f), new Vector2(0.5f, 0.3f),timeEffect2);
        float alpha2 = MathHelper.Lerp(0.5f, 1, timeEffect2)*totalAlpha;
        
        
        Vector2 toward = new Vector2(1, 0).RotatedBy(Projectile.rotation+0.5f);
        float radius = 70;
        int count = 17;
        float totalScale = 0.5f;
        float middleHeight = 1f;
        float subHeight = 0.1f;
                
        for (int i = 0; i < count; i++)
        {
            float eachHeight = MathHelper.Lerp(middleHeight,subHeight, Math.Abs((float)i / count - 0.5f) * 2f);

            DrawInWorld(Line.Value, Projectile.Center + toward*(i+0.5f)*radius/count - toward*radius/2f, currentColor*alpha1, 
                alpha1*new Vector2(1f,0.2f*eachHeight) * totalScale*scale1,0.5f);
        }   
        
        toward = new Vector2(1, 0).RotatedBy(Projectile.rotation+1.8f);
        
        count = 13; 
        radius = 50;

        for (int i = 0; i < count; i++)
        {
            float eachHeight = MathHelper.Lerp(middleHeight,subHeight, Math.Abs((float)i / count - 0.5f) * 2f);

            DrawInWorld(Line.Value, Projectile.Center + toward*(i+0.5f)*radius/count - toward*radius/2f, currentColor*alpha2, 
                alpha2*new Vector2(0.8f,0.3f*eachHeight) * totalScale*scale2,1.8f);   
        }   
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }

    public override bool ShouldUpdatePosition() => false;
}
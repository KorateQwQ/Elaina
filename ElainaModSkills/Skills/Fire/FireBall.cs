using System.Collections.Generic;
using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Fire;
using KL.Dusts.Smoke;
using KL.Dusts.Water;
using KL.Extensions;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

public class FireBall : KLProjectile
{
    private int count = 0;
    private bool draw = false;

    private float scale = 0.5f;

    private int time = 0;


    public override void SetDefaults()
    {
        //projectile.ignoreWater = true;//无视水
        Projectile.friendly = true; //可以攻击敌人
        Projectile.ownerHitCheck = false;
        //Projectile.penetrate = 1; // 穿透数量
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false; //瓷砖碰撞
        //projectile.timeLeft=30;
        //projectile.extraUpdates=1;
        Projectile.width = 35;
        Projectile.height = 35;
        Projectile.damage = 15;
        Projectile.timeLeft = 300;
        Projectile.extraUpdates = 1;
        Projectile.alpha = 0;
        Projectile.friendly = true;

        TrailLength = 30;
        scale = 0f;

        count = Main.rand.Next(0, 60);
        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.rotation);
        
        /*
        KLBasicDust.SpawnDustsCircle(Projectile.Center-move*20, ModContent.DustType<WaterDust3>(), 15,
            move*25, 0.88f, 15, new Color(255, 180, 50,255), new Vector2(1f,1f)*1f,
            0, 20, scaleOffset: new Vector2(1f, 0.2f), 10);*/
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center-move*20, ModContent.DustType<FireDust2>(), 10,
            move*15, 0.98f, 15, new Color(255, 255, 255,255), new Vector2(1f,1f)*1f,
            0, 20, scaleOffset: new Vector2(1f, 0.2f), 10);
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center+move*20, ModContent.DustType<FireDust3>(), 5,
            move*15, 0.88f, 25, new Color(255, 255, 255,255), new Vector2(1f,1f)*1f,
            0, 20, scaleOffset: new Vector2(1f, 0.2f), 10);
        base.OnSpawn_AllClient();
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, 伊蕾娜.skillColor[伊蕾娜.SkillType.MagicMissile].ToVector3());
        //Projectile.velocity = Vector2.Zero;
        time++;

        if (scale < 0.4)
        {
            scale = MathHelper.SmoothStep(0, 0.4f, time / 30f);
        }
        
        count ++;
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        base.PreDraw(ref lightColor);
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.rotation);

        Texture2D waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/1", AssetRequestMode.ImmediateLoad).Value;
        Texture2D waterNoise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/水波", AssetRequestMode.ImmediateLoad).Value;
        Texture2D waterNoise3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/Eff_Noise_11", AssetRequestMode.ImmediateLoad).Value;

        Texture2D ball = ModContent.Request<Texture2D>("KL/Effects/Tex/射灯_alpha", AssetRequestMode.ImmediateLoad).Value;
        
        EndBeginDraw(2,1);
        RadialDissolve(new Vector4(new Vector3(1f, 0.4f, 0.0f)*5.0f, 1), waterNoise, 0.2f,
            new Vector2((float)Main.timeForVisualEffects % 360 / 120f,0), new Vector2(1), 0.58f, 0.52f, -25f,sweepDirection:new Vector2(1,0),
            imageTex:waterNoise3,internalTextureOffset:new Vector2((float)Main.timeForVisualEffects % 360 / 40f,0),internalTextureScale:new Vector2(1.5f));
        
        DrawInWorld(ball,Projectile.Center,Color.White,new Vector2(1.0f,0.5f)*1,Projectile.rotation);

        RadialDissolve(new Vector4(new Vector3(1.0f, 0.7f, 0.5f)*3.0f, 1), waterNoise, 0.2f,
            new Vector2((float)Main.timeForVisualEffects % 360 / 120f,0), new Vector2(1), 0.58f, 0.52f, -25f,sweepDirection:new Vector2(1,0),
            imageTex:waterNoise2,internalTextureOffset:new Vector2((float)Main.timeForVisualEffects % 360 / 40f,0),internalTextureScale:new Vector2(1.5f));
        
        DrawInWorld(ball,Projectile.Center+move*40f,Color.White,new Vector2(0.8f,0.3f)*1,Projectile.rotation);
        
        EndBeginDraw();

        return false;
    }
    

    void DrawFireBall(float alpha = 1)
    {

        float clipValue = 0.1f;

        Vector2 time = new Vector2((count % 120) / 80f, 0);
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.rotation) * 35f;
        
        EndBeginDraw(0,1,ss: SamplerState.LinearWrap);
        
        /*CommonMagicEffect(insideTex:水波.Value,outsideTex:HeadClip.Value,colorMask:colorMask.Value,
            clipValueInside:0.2f,clipValueOutside:0.2f,imageColor:new Vector4(1f,1f,1f,1)*2.5f*alpha,
            uTime:time);
        
        Main.spriteBatch.Draw(水波.Value, Projectile.Center + move - Main.screenPosition, 水波.Value.GetRec(),
            Color.White, Projectile.rotation, new Vector2(水波.Size().X, 水波.Size().Y / 2f),
            new Vector2(4.5f, 2.2f) * 0.6f * scale, 0, 0);
        
        EndBeginDraw(1,1,ss: SamplerState.LinearWrap);
        
        CommonMagicEffect(insideTex:noise.Value,outsideTex:HeadClip2.Value,colorMask:colorMask.Value,scale:new Vector2(0.5f,1.5f),
            clipValueInside:0.2f,clipValueOutside:0.1f,imageColor:new Vector4(1f,1,1,1)*1.2f*alpha,
            uTime:time);
        
        Texture2D drawTex= noise2.Value;
        Main.spriteBatch.Draw(drawTex, Projectile.Center + move*7.2f*scale - Main.screenPosition, drawTex.GetRec(),
            Color.White, Projectile.rotation, new Vector2(drawTex.Size().X, drawTex.Size().Y / 2f),
            new Vector2(4.5f, 2.2f) * 1.6f * scale, 0, 0);*/
        
        
        EndBeginDraw();
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        //overPlayers.Add(index);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return base.Colliding(projHitbox, targetHitbox);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }
}
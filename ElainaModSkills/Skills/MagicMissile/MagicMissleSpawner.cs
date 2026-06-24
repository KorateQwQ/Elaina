using System;
using System.Collections.Generic;
using KL.Extensions;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

public class MagicMissleSpawner : KLProjectile
{
    enum State
    {
        Spawn,
        Normal,
        Dead
    }
    
    State state = State.Spawn;
    private int time = 0;
    private Vector2 traceToward;
    
    public override void SetDefaults()
    {
        //TrailLength = 100;
        Projectile.width = 100;
        Projectile.height = 100;
        Projectile.timeLeft = 60;
        Projectile.rotation = Main.rand.NextFloat(-3.14f, 3.14f);
        Projectile.tileCollide = false;
        Projectile.friendly = false;
        TrailLength = 30;
        base.SetDefaults();
    }

    public override void AI()
    {
        //Projectile.Center = Main.MouseWorld;
        //Projectile.rotation = (Projectile.Center - Owner.MountedCenter).ToRotation();
        time++;
        switch (state)
        {
            case State.Spawn:
            {
                if(time>20)
                {
                    state = State.Normal;
                    time = 0;
                }
            }break;
            case State.Normal:
            {
                Projectile.timeLeft = 60;
                if (time > 10000) time = 0;
                FindTarget();
            }break;
            case State.Dead:
            {
                Projectile.velocity = Vector2.Zero;
                if(time>20)Projectile.Kill();
            }break;
        }

        if(state!=State.Dead)TraceLocation();
        base.AI();
    }

    void FindTarget()
    {
        if(!HasAuthority())return;
        NPC npc = null;
        int id = Projectile.FindTargetWithLineOfSight(1000);
        if(id<0||id>Main.npc.Length)return;
        npc = Main.npc[id];
        
        if (npc!=null&& npc.IsAttackable(true, true))
        {
            state = State.Dead;
            time = 0;
            Vector2 velocity = (npc.Center - Projectile.Center).SafeNormalize(Vector2.One);
            
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, velocity*25f, ModContent.ProjectileType<MagicMissile>(), 10, 1);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        EndBeginDraw(1,1);

        /*switch (state)
        {
            case State.Spawn:
            {
                DrawStartStar();
                DrawStartCircle();
            }break;
            case State.Normal:
            {
                
            }break;
            case State.Dead:
            {
                DrawStartStar();
                DrawStartCircle();
            }break;
        }*/
        
        switch (state)
        {
            case State.Spawn:
            {
                EndBeginDraw(1,1);
                ReColorEffect(new Vector4(1)*1.9f);

                DrawStartStar();
                DrawStartCircle();
                if(time>10)DrawMagicBall();
            }break;
            case State.Normal:
            {
                DrawMagicBall();

            }break;
            case State.Dead:
            {
                EndBeginDraw(1,1);
                ReColorEffect(new Vector4(1)*1.9f);
                DrawStartStar();
                DrawStartCircle();
            }break;
        }
        EndBeginDraw();
        EndBeginDraw();

        return base.PreDraw(ref lightColor);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    void DrawStartStar()
    {
        Asset<Texture2D> line = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLine");
        Asset<Texture2D> line2 = TextureAssets.Extra[98];
        float rotation = MathHelper.Lerp(0.7f, 1.3f, time/20f);
        Color color = new Color(255, 160, 239,155);
        float timeEffect = DrawManager.FrameTime(0, 1, 20,time);

        float timeEffect2 = MathHelper.Lerp(0, 1, time / 20f);
        
        float timeEffect3 = MathHelper.Lerp(1, 0, time / 20f);

        Vector2 toward = new Vector2(1, 0).RotatedBy(Projectile.rotation+rotation);
        float radius = MathHelper.Lerp(100, 300, time / 20f);
        int count = (int)(25*timeEffect3);
        
        float totalScale = 1f;
        float middleHeight = 1f;
        float subHeight = 0.1f;
                

        for (int i = 0; i < count; i++)
        {
            float eachHeight = MathHelper.Lerp(middleHeight,subHeight, Math.Abs((float)i / count - 0.5f) * 2f);

            DrawInWorld(line.Value, Projectile.Center + toward*(i+0.5f)*radius/count - toward*radius/2f, color, new Vector2(0.2f,0.5f*eachHeight) * totalScale * timeEffect3,Projectile.rotation+ rotation);

        }
        
        rotation += 3.14f/2f;
        toward = new Vector2(1, 0).RotatedBy(Projectile.rotation+rotation);
        radius = DrawManager.FrameTime(70, 150, 20,time);
        for (int i = 0; i < count; i++)
        {
            float eachHeight = MathHelper.Lerp(middleHeight,subHeight, Math.Abs((float)i / count - 0.5f) * 2f);

            DrawInWorld(line.Value, Projectile.Center + toward*(i+0.5f)*radius/count - toward*radius/2f, color, new Vector2(0.2f,0.5f*eachHeight) * totalScale * timeEffect3,Projectile.rotation+ rotation);

        }
    }


    void DrawStartCircle()
    {
        Asset<Texture2D> line = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLine");
        Color color = new Color(255, 160, 239,155);
        float radius = MathHelper.Lerp(10, 50, time / 20f);

        float count = 35 * MathHelper.Lerp(1, 0, time / 20f);
        
        float length =MathHelper.Lerp(1, 0, time / 20f);
        
        for(int i =0; i<count; i++) 
        {
            float eachAngle = 3.14f*2f/count;
            Vector2 toward = new Vector2(1, 0).RotatedBy(Projectile.rotation+eachAngle*i);
            DrawInWorld(line.Value, Projectile.Center + toward*radius, color, new Vector2(0.2f*length,0.23f*length),Projectile.rotation+ eachAngle*i+3.14f/2f);
        }

    }

    void DrawMagicBall()
    {
        Asset<Texture2D> waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/水波");
        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/MagicMissileEffect", AssetRequestMode.ImmediateLoad).Value;
        Asset<Texture2D> headClip = ModContent.Request<Texture2D>("KL/Effects/Tex/background", AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> trail = ModContent.Request<Texture2D>("KL/Effects/Tex/noi_1", AssetRequestMode.ImmediateLoad);

        //Vector2 time = new Vector2( (count % 120) / 40f,0);

        Lighting.AddLight(Projectile.Center, new Vector3(1f,0.7f,0.8f)*DrawManager.FrameTime(0.5f,1.5f,60));
        
        float clipValue = 0.1f;
        float totalAlpha = 1;
        effect.Parameters["uTime"].SetValue(time/60f);
        effect.Parameters["clipValue"].SetValue(clipValue);
        effect.Parameters["clipValue2"].SetValue(clipValue);

        effect.Parameters["Edge"].SetValue(0.2f);
        effect.Parameters["EdgeColor"].SetValue(new Vector4(1f,0.7f,0.8f,1)*1);
        effect.Parameters["imageColor"].SetValue(new Vector4(1f,0.5f,0.8f,1)*2.5f * totalAlpha);
        
        Main.graphics.GraphicsDevice.Textures[1] = waterNoise.Value;
        Main.graphics.GraphicsDevice.Textures[2] = headClip.Value;  
        
        EndBeginDraw(1,shader:effect,ss:SamplerState.LinearWrap,adjustToScreen:true);

        Vector2 offset = Vector2.Zero;
        if (Projectile.velocity.Length() < 10f)
        {
            float offsetTime = DrawManager.FrameTime(0, 1, 150, time);

            offset.Y = MathHelper.SmoothStep(-10, 10, offsetTime);
        }
        
        
        Main.spriteBatch.Draw(waterNoise.Value, Projectile.Center+offset - Main.screenPosition, waterNoise.Value.GetRec(),
            Color.White, Projectile.rotation, new Vector2(waterNoise.Size().X/2f,waterNoise.Size().Y/2f), new Vector2(1)* 0.3f, 0, 0);
        
        if(OldCenter!=null)TrailEffect(trail.Value,OldCenter,new Color(255, 160, 239,255),Color.White*0f,7,0.1f,drawTimes:1,
            uTime:new Vector2(1-(time % 120) / 30f,0),startAlpha:2f,endAlpha:0.2f,blendState:1);
        
    }
    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }

    void TraceLocation()
    {
        Vector2 target = Owner.MountedCenter + traceToward;
        Vector2 targetVec = target - Projectile.Center;
        if (targetVec.Length()<1f)
        {
            Projectile.velocity = Vector2.Zero;
            return;
        }

        if (targetVec.Length() > 2000f)
        {
            Projectile.Center = target;
            return;
        }
        // 计算基础速度，距离越远速度越快，但不超过最大值20
        float speed = MathHelper.SmoothStep(1f, Math.Max(Owner.velocity.Length()*0.8f,20f) , Math.Min(targetVec.Length() / 200f, 1f));
        Projectile.velocity = Vector2.Normalize(targetVec) * speed;
    }
    public override void OnSpawn(IEntitySource source)
    {
        traceToward = Projectile.Center - Owner.MountedCenter;
        
        base.OnSpawn(source);
    }
}
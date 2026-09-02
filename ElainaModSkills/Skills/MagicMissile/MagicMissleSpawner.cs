using System;
using System.Collections.Generic;
using KL.Drawing;
using KL.Extensions;
using KL.Utils;
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

    // 使用ai[0]来存储状态，方便外部技能读取
    // 0 = Spawn, 1 = Normal, 2 = Dead
    State CurrentState
    {
        get => (State)Projectile.ai[0];
        set => Projectile.ai[0] = (int)value;
    }
    
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

        // 从ai[0]恢复状态
        state = CurrentState;

        switch (state)
        {
            case State.Spawn:
            {
                if(time>20)
                {
                    state = State.Normal;
                    CurrentState = state; // 同步到ai[0]
                    //time = 0;
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
                if(time>40)Projectile.Kill();
            }break;
        }

        if(state!=State.Dead)TraceLocation();
        base.AI();
    }

    void FindTarget()
    {
        if(!HasAuthority()||time<40)return;
        NPC npc = null;
        int id = Projectile.FindTargetWithLineOfSight(1000);
        if(id<0||id>Main.npc.Length)return;
        npc = Main.npc[id];

        if (npc!=null&& npc.IsAttackable(true, true))
        {

            RPC("ToDeadState",KLNetModule.NetSendType.ClientToAll);
            Vector2 velocity = (npc.Center - Projectile.Center).SafeNormalize(Vector2.One);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, velocity*25f, ModContent.ProjectileType<MagicMissile>(), Projectile.damage, 2);
        }
    }

    public void ToDeadState()
    {            
        time = 0;
        state = State.Dead;
        CurrentState = state; // 同步到ai[0]
        
    }

    public override bool PreDraw(ref Color lightColor)
    {
        int startTime = 5;
        switch (state)
        {
            case State.Spawn:
            {
                DrawMagicBall();

                if(time>startTime)
                {
                    DrawStartStar(startTime);
                    DrawStartCircle(startTime);
                }
            }break;
            case State.Normal:
            {
                DrawStartStar(startTime);
                DrawMagicBall();

            }break;
            case State.Dead:
            {
                DrawStartStar(startTime);
                DrawStartCircle(startTime);
            }break;
        }
        EndBeginDraw();

        return base.PreDraw(ref lightColor);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    void DrawStartStar(int start)
    {
        Texture2D line = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLine").Value;
        Texture2D noise = AssetManager.GetTexture("KL.Effects.Tex.voronoiOrigin");
        Effect effect = AssetManager.GetEffect("KL.Effects.Content.FuzzyEdge");

        int startTime = time - start;
        if(startTime>40)return;
        Asset<Texture2D> line2 = TextureAssets.Extra[98];
        
        float progress = startTime / 20f;
        float easedProgress = 1f - (1f - progress) * (1f - progress);
        float rotation = MathHelper.Lerp(0.7f, 2.2f, easedProgress);
        if (startTime > 20f)
        {
            rotation = MathHelper.Lerp(2.2f, 2.6f, (startTime-20)/20f);
        }
        
        Color color = new Color(255, 160, 239,155);

        float timeEffect = MathHelper.Lerp(1, 0, startTime / 20f);

        Vector2 toward = new Vector2(1, 0).RotatedBy(Projectile.rotation+rotation);
        float radius = MathHelper.Lerp(100, 300, startTime / 20f);
        int count = (int)(25*timeEffect);

        Vector2 totalScale =new Vector2(MathHelper.Lerp(0.35f, 1.2f, startTime / 20f),MathHelper.Lerp(0.2f, 0.2f, startTime / 20f));
        float middleHeight = 1f;
        float subHeight = 0.1f;

        // FuzzyEdge 参数：保持 + 消融效果
        Vector4 imageColor = new Color(255, 160, 239, 155).ToVector4() * MathHelper.Lerp(4.5f, 2.5f, startTime / 20f);

        // edgeNoiseStrength 控制消融：前 12 帧保持完整，后 8 帧慢慢消融
        float edgeNoiseStrength;
        int keepTime = 5;
        if (startTime < keepTime)
        {
            // 保持阶段：噪声强度保持很低，星星完整显示
            edgeNoiseStrength = 0.0f;
        }
        else
        {
            // 消融阶段：噪声强度从 0 增加到 0.7，使用缓动函数让消融更自然
            float fadeProgress = (startTime - keepTime) / 30f; // 0-1 的消融进度

            edgeNoiseStrength = MathHelper.Lerp(0.0f, 0.6f, 1f - (1f - fadeProgress) * (1f - fadeProgress));
        }
        Vector2 edgeNoiseScale = new Vector2(1f,0.5f);
        Vector2 edgeNoiseOffset = new Vector2(0.4f, 0);
        int edgeDirection = 0;

        EndBeginDraw(1,1);
        effect.SetValue("ImageColor", imageColor);
        effect.SetValue("Intensity", edgeNoiseStrength);
        effect.SetValue("NoiseOffset", new Vector2(0.0f,0));
        effect.SetValue("NoiseScale", edgeNoiseScale);
        effect.SetValue("UseErosion", true);
        effect.SetValue("Symmetric", false);

        effect.SetTexture(1, noise);
        effect.Apply();
        //ClipEffect(edgeNoiseStrength,imageColor:imageColor,mask:noise,maskScale: edgeNoiseScale);


        DrawInWorld(line,Projectile.Center,color,totalScale,Projectile.rotation+rotation);

        EndBeginDraw(1,1);
        effect.SetValue("NoiseOffset", new Vector2(0.5f,0));
        effect.Apply();

        DrawInWorld(line,Projectile.Center,color,totalScale,Projectile.rotation+rotation+3.14f/2);

    }


    void DrawStartCircle(int start)
    {
        Texture2D line = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.MagicMissile.noi_1");
        Texture2D noise = AssetManager.GetTexture("KL.Effects.Tex.cellnoise");

        Color color = new Color(255, 160, 239,155);
        int startTime = time - start;

        float radius = MathHelper.Lerp(20, 40, startTime / 20f)*0.01f;
        float noiseStr = MathHelper.Lerp(0.0f, 0.25f, startTime / 40f);

        float count = 35 * MathHelper.Lerp(1, 0, startTime / 20f);

        float length =MathHelper.Lerp(1, 0, startTime / 20f);
        EndBeginDraw(2,1);
        CircleRingEffect(0.06f,outerRadius:radius,ringColor:new Vector4(new Vector3(0.5f),1),texScale:new Vector2(0.5f,4),
            swapUV:true,noiseTex:noise,edgeNoiseStrength:noiseStr);
        DrawInWorld(line,Projectile.Center);

        EndBeginDraw(1,1);
        CircleRingEffect(0.06f,outerRadius:radius,ringColor:color.ToVector4()*(DrawSystem.GetShouldBloom()?2.5f:1.5f),texScale:new Vector2(0.5f,4),
            swapUV:true,noiseTex:noise,edgeNoiseStrength:noiseStr);
        DrawInWorld(line,Projectile.Center);

    }

    void DrawMagicBall()
    {

        Texture2D waterNoise = ModContent.Request<Texture2D>("KL/Effects/Tex/水波").Value;
        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/MagicMissileEffect", AssetRequestMode.ImmediateLoad).Value;
        Asset<Texture2D> headClip = ModContent.Request<Texture2D>("KL/Effects/Tex/background", AssetRequestMode.ImmediateLoad);
        Asset<Texture2D> trail = ModContent.Request<Texture2D>("KL/Effects/Tex/air", AssetRequestMode.ImmediateLoad);

        //Vector2 time = new Vector2( (count % 120) / 40f,0);
        if(OldCenter!=null&&OldCenter.Length>2)
        {
            TrailEffect(trail.Value, OldCenter, new Color(0, 0, 0, 255), Color.White * 0f, 7, 0.1f, drawTimes: 1,
                uTime: new Vector2(1 - (time % 120) / 30f, 0), startAlpha: 1f, endAlpha: 0.0f, blendState: 2);
            
            TrailEffect(trail.Value, OldCenter, new Color(255, 107, 239, 255), Color.White * 0f, 7, 0.1f, drawTimes: 1,
                uTime: new Vector2(1 - (time % 120) / 30f, 0), startAlpha: 2.5f, endAlpha: 0.0f, blendState: 1);
        }
        Lighting.AddLight(Projectile.Center, new Vector3(1f,0.7f,0.8f)*DrawManager.FrameTime(0.5f,1.5f,60));

        // 新的 shader 参数（根据截图）
        float circleRadius = 0.4f;
        Vector2 circleCenter = new Vector2(0.5f, 0.5f);
        float circleSoftness = 0.05f;
        Vector4 circleColor = new Vector4(1f, 0.22f, 0.72f, 1f);
        float distortionStrength = 0.01f;
        Vector2 uTime = new Vector2(0f, 0f);
        float clipValue = 0.0f;

        // 设置 shader 参数
        effect.Parameters["CircleRadius"].SetValue(circleRadius);
        effect.Parameters["CircleCenter"].SetValue(circleCenter);
        effect.Parameters["CircleSoftness"].SetValue(circleSoftness);
        effect.Parameters["DistortionStrength"].SetValue(distortionStrength);
        effect.Parameters["uTime"].SetValue(new Vector2(0, VisualTime * 0.8f / 60f)); // 使用截图中的动画参数 (0.2, 0.2)
        effect.Parameters["clipValue"].SetValue(clipValue);

        //Main.graphics.GraphicsDevice.Textures[1] = headClip.Value;  // clipImage

        // Q弹缩放动画：从0放大撑开后回到0.2
        float totalScale = 0.20f;
        int startTime = time;
        if (state == State.Spawn && startTime <= 15)
        {
            float progress = startTime / 15f; // 0-1 的进度
            // 使用弹性函数：先超调到更大值，然后回弹
            float overshoot = MathF.Sin(progress * MathF.PI); // 先增大后减小
            float elastic = progress * (1f + 0.8f * overshoot); // 添加弹性效果
            totalScale = 0.20f * elastic;
        }

        EndBeginDraw(2, shader: effect, ss: SamplerState.LinearWrap);
        effect.Parameters["CircleColor"].SetValue(new Vector4(0,0,0,1));
        DrawInWorld(waterNoise, Projectile.Center, scale: new Vector2(totalScale*1.00f));
        
        
        EndBeginDraw(1, shader: effect, ss: SamplerState.LinearWrap);
        effect.Parameters["CircleColor"].SetValue(circleColor*1.8f);
        DrawInWorld(waterNoise, Projectile.Center, scale: new Vector2(totalScale));
        
        EndBeginDraw(2, shader: effect, ss: SamplerState.LinearWrap);
        effect.Parameters["CircleColor"].SetValue(Vector4.One*1.0f);
        effect.Parameters["DistortionStrength"].SetValue(0.04f);

        DrawInWorld(waterNoise, Projectile.Center, scale: new Vector2(totalScale*0.8f));
    }
    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }

    void TraceLocation()
    {
        int slotIndex = (int)Projectile.ai[1] - 1;
        Vector2 target;

        if (slotIndex >= 0 && slotIndex < 5)
        {
            // 目标使用不受玩家旋转影响的固定相对槽位，再通过速度追踪制造滞后。
            target = MultiMissileSkill.GetSlotPosition(Owner, slotIndex);
        }
        else
        {
            // 兼容没有槽位编号的其他生成方式。
            target = Owner.MountedCenter + traceToward;

            if (Owner.gravDir < 0)
            {
                Vector2 relativeOffset = traceToward;
                relativeOffset.Y = -relativeOffset.Y;
                target = Owner.MountedCenter + relativeOffset;
            }
        }

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

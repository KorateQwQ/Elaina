using System;
using KL.DamageSystem;
using KL.Drawing;
using KL.Drawing.ThreeD;
using KL.Dusts;
using KL.Dusts.Burst;
using KL.Dusts.Glow;
using KL.Dusts.Smoke;
using KL.Dusts.Stone;
using KL.Projectiles;
using KL.Utils;
using Terraria.ID;
using 伊蕾娜.ElainaModSkills.ElainaDamageClass;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public class IceConeProj : ElainaBasicProjectile
{
    static ObjModel iceCone;
    static Texture2D texture;

    private int BackwardFlyTime => 25;
    private int BurstFlyTime => 8;
    private float BackwardSpeedMultiplier => 0.5f;
    private float BurstSpeedMultiplier => 3f;

    private int time;
    private Vector2 forwardVelocity;
    private TrailManager.TrailHandle managedTrail;
    public override void Load()
    {
        iceCone ??= ObjModel.Load("伊蕾娜.Models.IceCone2");
        texture ??= AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.Ice.IceConeProj2");
        
        base.Load();
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.timeLeft = 600;
        TrailLength = 30;
        TrailMaxLength = 30;
        TrailSampleCount = 50;
        
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.localNPCHitCooldown = 60;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.DamageType = ModContent.GetInstance<ElainaBasicDamage>();
        // 冰锥命中时携带冰元素标签，由 KL 的积蓄系统按命中次数累积冻结
        InfusionElement = ElementType.Ice;
        //ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2000;

        base.SetDefaults();
    }

    public override void AI()
    {
        if (time == 0)
        {
            forwardVelocity = Projectile.velocity;
        }

        UpdateMovement();

        if (time < BackwardFlyTime)
        {
            Vector2 playerMovement = Owner.position - Owner.oldPosition;
            Projectile.position += playerMovement;
            if (time == BackwardFlyTime - 1)
            {
                Vector2 dir = new Vector2(1, 0).RotatedBy(Projectile.rotation);
                KLBasicDust.SpawnDust(Projectile.Center+dir*-50,ModContent.DustType<BurstCircle>(),velocity:dir,lifeTime:12,
                    color:new Color(150,220,255,0),scale:new Vector2(0.3f,1.5f)*1.5f);
            }
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        time++;
        
        Lighting.AddLight(Projectile.Center, new Color(50, 220, 255).ToVector3());
        base.AI();
    }

    private void UpdateMovement()
    {
        if (forwardVelocity == Vector2.Zero)
        {
            return;
        }

        float fixedSpeed = forwardVelocity.Length();
        Vector2 forwardDirection = Vector2.Normalize(forwardVelocity);

        if (time < BackwardFlyTime)
        {
            float backwardProgress = time / (float)(BackwardFlyTime - 1);
            float currentSpeed = MathHelper.Lerp(fixedSpeed * BackwardSpeedMultiplier, 0f, backwardProgress);
            Projectile.velocity = -forwardDirection * currentSpeed;
            return;
        }

        if (time < BackwardFlyTime + BurstFlyTime)
        {
            float burstProgress = (time - BackwardFlyTime) / (float)BurstFlyTime;
            float currentSpeed = MathHelper.Lerp(fixedSpeed * 4, fixedSpeed, burstProgress);
            Projectile.velocity = forwardDirection * currentSpeed;
            return;
        }

        //Projectile.velocity = forwardDirection * fixedSpeed;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // 必须先采样本帧的 Center，TrailManager 随后复制的 OldCenter 才是最新的一帧
        base.PreDraw(ref lightColor);

        Texture2D trailTexture = AssetManager.GetTexture("KL.Effects.Tex.Trail.streak_801_4r");
        if (time >= BackwardFlyTime && trailTexture != null && ValidTrailArray())
        {
            var trailState = (
                Texture: trailTexture,
                Direction: new Vector2(1, 0).RotatedBy(Projectile.rotation),
                TimeOffset: -(float)time % 1200 / 40);

            TrailManager.CreateOrUpdateTrail(
                ref managedTrail,
                OldCenter,
                lifetime: 10,
                trailState,
                DrawManagedTrail,
                bloom: true);
        }

        Color drawLightColor = lightColor;

        LayerDrawRequestSystem.RequestBloom("Elaina:IceCone", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, () => DrawVisual(drawLightColor));
        return false;
    }

    private static void DrawManagedTrail((Texture2D Texture, Vector2 Direction, float TimeOffset) state, Vector2[] points, float remainingRatio)
    {
        if (points.Length < 2)
            return;

        TrailEffect(state.Texture, points, new Vector4(new Color(200,220,255,255).ToVector3()*2,1),  new Vector4(new Color(200,220,255,255).ToVector3()*1,1),
            endAlpha:1,maxWidth:7,endWidth:0,blendState:2,debugPoint:false,attachPoint:state.Direction*-100,imageScale:new Vector2(0.5f,1),
            uTime:new Vector2(state.TimeOffset,0));
    }

    private void DrawVisual(Color lightColor)
    {
        Effect iceConeEffect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/ThreeD/IceCone3D", AssetRequestMode.ImmediateLoad).Value;
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        VertexBuffer vertexBuffer = iceCone.GetOrCreateVertexBuffer(gd);
        Vector3 position = new(Projectile.Center, 0f);
        Vector2 drawDirection = time <= BackwardFlyTime && forwardVelocity != Vector2.Zero ? forwardVelocity : Projectile.velocity;
        Vector3 rotation3D = new(0, time * 0.1f, drawDirection.ToRotation() + MathF.PI / 2f);
        Vector3 scale3D = new(20);
        Vector3 cameraPosition = GraphicsUtils.CameraPos(MathF.PI / 3f);
        Vector3 sunPosition = new(new Vector2(Main.screenWidth, Main.screenHeight) / 4f + Main.screenPosition, -1000);
        Vector3 lightDirection = Vector3.Normalize(position - sunPosition);
        // HDR brightness belongs in RGB; alpha remains coverage in the 0-1 range.
        Vector4 baseColor = new(new Color(200, 220, 255).ToVector3() * 1.10f, 1f);
        Vector3 fresnelColor = (new Color(255, 255, 255)).ToVector3();
        //底光
        float ambientStrength = 0.8f;
        //光照影响强度
        float diffuseStrength = 1f;
        //菲涅尔强度
        float fresnelStrength = 2f;

        // 消融参数
        float dissolveNoiseScale = 1.0f;
        float dissolveEdgeWidth = 0.2f;

        // 后撤阶段通过消融效果逐渐出现，后撤到一半时完全显现
        float dissolveThreshold;
        if (time < BackwardFlyTime)
        {
            float progress = time / (float)BackwardFlyTime;
            float appearProgress = MathHelper.Clamp(progress * 1.5f, 0f, 1f); // 一半时间达到完全显现
            dissolveThreshold = MathHelper.Lerp(1.2f, 0f, appearProgress);
        }
        else
        {
            dissolveThreshold = 0f;
        }

        Matrix modelMatrix = Matrix.CreateScale(scale3D) *
                             Matrix.CreateRotationX(rotation3D.X) *
                             Matrix.CreateRotationY(rotation3D.Y) *
                             Matrix.CreateRotationZ(rotation3D.Z) *
                             Matrix.CreateTranslation(position);
        Matrix viewProjectionMatrix = GraphicsUtils.GetVPMatrix();

        EndBeginDraw();

        iceConeEffect.Parameters["uWorld"].SetValue(modelMatrix);
        iceConeEffect.Parameters["uViewProjection"].SetValue(viewProjectionMatrix);
        iceConeEffect.Parameters["uLightDirection"].SetValue(lightDirection);
        iceConeEffect.Parameters["uLightColor"].SetValue(lightColor.ToVector3());
        iceConeEffect.Parameters["uCameraPosition"].SetValue(cameraPosition);
        iceConeEffect.Parameters["uBaseColor"].SetValue(baseColor);
        iceConeEffect.Parameters["uFresnelColor"].SetValue(fresnelColor);
        iceConeEffect.Parameters["uDissolveEdgeColor"].SetValue(new Vector4(new Color(100, 200, 255).ToVector3() * 4.5f, 1f));
        iceConeEffect.Parameters["uAmbientStrength"].SetValue(ambientStrength);
        iceConeEffect.Parameters["uDiffuseStrength"].SetValue(diffuseStrength);
        iceConeEffect.Parameters["uFresnelStrength"].SetValue(fresnelStrength);
        iceConeEffect.Parameters["uDissolveNoiseScale"].SetValue(dissolveNoiseScale);
        iceConeEffect.Parameters["uDissolveThreshold"].SetValue(dissolveThreshold);
        iceConeEffect.Parameters["uDissolveEdgeWidth"].SetValue(dissolveEdgeWidth);
        iceConeEffect.Parameters["uDepthClipSide"].SetValue(0f);

        gd.BlendState = BlendState.NonPremultiplied;// ;IceConeDepthRenderer.ModelBlend;
        gd.DepthStencilState = DepthStencilState.None;
        gd.SamplerStates[0] = SamplerState.LinearWrap;
        gd.SamplerStates[1] = SamplerState.LinearWrap;
        gd.RasterizerState = RasterizerState.CullClockwise;

        gd.Textures[0] = texture;
        gd.Textures[1] = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/4", AssetRequestMode.ImmediateLoad).Value;

        gd.SetVertexBuffer(vertexBuffer);
        iceConeEffect.CurrentTechnique.Passes["Base"].Apply();
        gd.DrawPrimitives(PrimitiveType.TriangleList, 0, iceCone.PrimitiveCount);

        EndBeginDraw();
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers,
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
        Vector2 dir = new Vector2(1, 0).RotatedBy(Projectile.rotation);
        
        KLBasicDust.SpawnDust(Projectile.Center,ModContent.DustType<BurstPoint>(),velocity:Main.rand.NextVector2Circular(0.1f,0.1f),lifeTime:12,
            color:new Color(150,220,255,0),scale:new Vector2(1.2f));
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center+Main.rand.NextVector2Circular(50,50), ModContent.DustType<IceStone>(), 5, -dir*15.1f, 
            3.28f,28, new Color(255, 255, 255, 255),new Vector2(0.4f),
            0,20,lifeOffset:2,scaleOffset:new Vector2(0.2f,0.2f));
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center+Main.rand.NextVector2Circular(10,10), ModContent.DustType<BurstDust>(), 5, -dir*10.1f, 
            1.88f,15, new Color(180, 220, 255, 150),new Vector2(1.2f,0.8f)*0.5F,
            0,50,lifeOffset:5,scaleOffset:new Vector2(0.5f,0.2f));
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center+Main.rand.NextVector2Circular(10,10), ModContent.DustType<SmokeDust2>(), 5, -dir*2.1f, 
            6.28f,40, new Color(150, 220, 255, 0)*0.3f,new Vector2(1.0f,1f)*2.0F,
            0,100,lifeOffset:5,scaleOffset:new Vector2(0.5f,0.5f));
        
        KLBasicDust.SpawnDust(Projectile.Center,ModContent.DustType<GlowDust>(),velocity:Main.rand.NextVector2Circular(0.1f,0.1f),lifeTime:12,
            color:new Color(150,220,255,0),scale:new Vector2(1.5f));
        
        base.OnKill(timeLeft);
    }
}

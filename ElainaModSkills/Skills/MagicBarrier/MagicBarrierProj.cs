using System;
using KL.Drawing;
using KL.Drawing.ThreeD;
using KL.Utils;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

public class MagicBarrierProj : ElainaBasicProjectile
{
    private static ObjModel sphere;
    private static Effect barrierEffect;
    private static Texture2D flowNoise;
    private static Texture2D cellNoise;
    private static Texture2D dissolveNoise;

    private float BarrierRadius => 55f;
    private Vector2 BarrierScale => Vector2.One;
    private float BarrierFov => MathF.PI / 3f;
    private float BarrierDepth => 0f;
    private float HaloWidth => 0f;
    private float RotationSpeed => 0.035f;
    private float RevealDuration => 12f;
    private float FadeDuration => 28f;
    private float TransitionWidth => 0.13f;
    private Vector2 TransitionScale => new(0.72f, 0.52f);
    private float visualRevealProgress;
    private float visualFadeProgress;
    private Vector2 dissolveOffset;

    private Color BaseColor => new(50, 100, 230);
    private Color RimColor => new(150, 200, 255);
    private Color FlowColor => new(102, 215, 255);
    private Color HighlightColor => new(205, 238, 255);

    public override void Load()
    {
        if (!Main.dedServ)
        {
            sphere ??= ObjModel.Load("KL.Models.Sphere");
            barrierEffect = ModContent.Request<Effect>(
                "伊蕾娜/Effects/Content/ThreeD/MagicBarrier3D",
                AssetRequestMode.ImmediateLoad).Value;
            flowNoise = AssetManager.GetTexture("KL.Effects.Tex.Noise.1");

            cellNoise = AssetManager.GetTexture("KL.Effects.Tex.Noise.1");
            dissolveNoise = AssetManager.GetTexture("KL.Effects.Tex.Noise.Eff_Noise_105");
        }

        base.Load();
    }

    public override void SetDefaults()
    {
        Projectile.width = (int)(BarrierRadius * BarrierScale.X * 2f);
        Projectile.height = (int)(BarrierRadius * BarrierScale.Y * 2f);
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hide = true;

        Projectile.timeLeft = 60;
        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        dissolveOffset = new Vector2(
            Main.rand.NextFloat(-2f, 2f),
            Main.rand.NextFloat(-2f, 2f));
        base.OnSpawn_AllClient();
    }

    public override void AI()
    {
        Projectile.Center = Owner.VisualCenter();
        Projectile.localAI[0] = MathF.Min(Projectile.localAI[0] + 1f, RevealDuration + FadeDuration + 1f);
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        base.PreDraw(ref lightColor);
        DrawBarrier(0);
        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        overPlayers.Add(index);
    }

    private void RequestBarrierDraw(LayerDrawRequestSystem.DrawTargetLayer layer, float depthClipSide)
    {

    }

    private void DrawBarrier(float depthClipSide)
    {
        GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
        VertexBuffer vertexBuffer = sphere.GetOrCreateVertexBuffer(graphicsDevice);

        BlendState oldBlendState = graphicsDevice.BlendState;
        DepthStencilState oldDepthStencilState = graphicsDevice.DepthStencilState;
        RasterizerState oldRasterizerState = graphicsDevice.RasterizerState;
        SamplerState oldSamplerState0 = graphicsDevice.SamplerStates[0];
        SamplerState oldSamplerState1 = graphicsDevice.SamplerStates[1];
        SamplerState oldSamplerState2 = graphicsDevice.SamplerStates[2];
        Texture oldTexture0 = graphicsDevice.Textures[0];
        Texture oldTexture1 = graphicsDevice.Textures[1];
        Texture oldTexture2 = graphicsDevice.Textures[2];
        VertexBufferBinding[] oldVertexBuffers = graphicsDevice.GetVertexBuffers();

        try
        {
            graphicsDevice.DepthStencilState = DepthStencilState.None;
            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;
            graphicsDevice.Textures[0] = flowNoise;
            graphicsDevice.Textures[1] = cellNoise;
            graphicsDevice.Textures[2] = dissolveNoise;
            graphicsDevice.SetVertexBuffer(vertexBuffer);

            float time = VisualTime / 60f;
            float revealProgress = MathHelper.Clamp(Projectile.localAI[0] / RevealDuration, 0f, 1f);
            float fadeProgress = MathHelper.Clamp((FadeDuration - Projectile.timeLeft) / FadeDuration, 0f, 1f);
            visualRevealProgress = revealProgress;
            visualFadeProgress = fadeProgress;
            float revealScale = MathHelper.SmoothStep(0.84f, 1f, revealProgress);
            float fadeScale = MathHelper.Lerp(1f, 0.84f, fadeProgress);
            float transitionScale = revealScale * fadeScale;
            Matrix haloWorld = CreateWorldMatrix((BarrierRadius + HaloWidth) * transitionScale, time);

            graphicsDevice.BlendState = BlendState.Additive;
            DrawSphere(haloWorld, new Vector3(0.3f,0.5f,1f),new Vector3(0.5f,0.7f,1f)*3f,depthClipSide, time,
                opacity: 0.5f,
                bodyAlpha: 0.4f,
                rimStrength: 0.85f,
                lowerRimStrength: 0.15f,
                flowStrength: 0.88f,
                highlightStrength: 0.65f);
        }
        finally
        {
            graphicsDevice.BlendState = oldBlendState;
            graphicsDevice.DepthStencilState = oldDepthStencilState;
            graphicsDevice.RasterizerState = oldRasterizerState;
            graphicsDevice.SamplerStates[0] = oldSamplerState0;
            graphicsDevice.SamplerStates[1] = oldSamplerState1;
            graphicsDevice.SamplerStates[2] = oldSamplerState2;
            graphicsDevice.Textures[0] = oldTexture0;
            graphicsDevice.Textures[1] = oldTexture1;
            graphicsDevice.Textures[2] = oldTexture2;
            graphicsDevice.SetVertexBuffers(oldVertexBuffers);
        }
    }

    private Matrix CreateWorldMatrix(float radius, float time)
    {
        return Matrix.CreateScale(
                   radius * BarrierScale.X,
                   radius * BarrierScale.Y,
                   radius) *
               Matrix.CreateRotationZ(time * RotationSpeed) *
               Matrix.CreateTranslation(new Vector3(Projectile.Center, BarrierDepth));
    }

    private void DrawSphere(Matrix world,Vector3 baseColor,Vector3 rimColor, float depthClipSide, float time,
        float opacity, float bodyAlpha, float rimStrength, float lowerRimStrength,
        float flowStrength, float highlightStrength)
    {
        barrierEffect.Parameters["uWorld"].SetValue(world);
        barrierEffect.Parameters["uWorldInverseTranspose"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
        barrierEffect.Parameters["uViewProjection"].SetValue(
            GraphicsUtils.GetVPMatrix(ProjectionMode.Perspective, BarrierFov));
        barrierEffect.Parameters["uCameraPosition"].SetValue(GraphicsUtils.CameraPos(BarrierFov));
        barrierEffect.Parameters["uBaseColor"].SetValue(baseColor);
        barrierEffect.Parameters["uRimColor"].SetValue(rimColor);
        barrierEffect.Parameters["uFlowColor"].SetValue(FlowColor.ToVector3());
        barrierEffect.Parameters["uHighlightColor"].SetValue(HighlightColor.ToVector3());
        barrierEffect.Parameters["uFlowScale"].SetValue(new Vector2(2.2f, 1.55f));
        barrierEffect.Parameters["uTransitionScale"].SetValue(TransitionScale);
        barrierEffect.Parameters["uDissolveOffset"]?.SetValue(dissolveOffset);
        barrierEffect.Parameters["uTime"].SetValue(time);
        barrierEffect.Parameters["uCenterDepth"].SetValue(BarrierDepth);
        barrierEffect.Parameters["uDepthClipSide"].SetValue(depthClipSide);
        barrierEffect.Parameters["uOpacity"].SetValue(opacity);
        barrierEffect.Parameters["uBodyAlpha"].SetValue(bodyAlpha);
        barrierEffect.Parameters["uRimPower"].SetValue(3.15f);
        barrierEffect.Parameters["uRimStrength"].SetValue(rimStrength);
        barrierEffect.Parameters["uLowerRimStrength"].SetValue(lowerRimStrength);
        barrierEffect.Parameters["uFlowStrength"].SetValue(flowStrength);
        barrierEffect.Parameters["uFlowWidth"].SetValue(0.045f);
        barrierEffect.Parameters["uHighlightStrength"].SetValue(highlightStrength);
        barrierEffect.Parameters["uRevealProgress"].SetValue(visualRevealProgress);
        barrierEffect.Parameters["uFadeProgress"].SetValue(visualFadeProgress);
        barrierEffect.Parameters["uTransitionWidth"].SetValue(TransitionWidth);
        float revealGlowRamp = MathHelper.SmoothStep(0.12f, 0.55f, visualRevealProgress);
        float transitionStrength = MathHelper.Lerp(0.55f, 1.65f, revealGlowRamp);
        transitionStrength *= MathHelper.Lerp(1f, 0.8f, visualFadeProgress);
        barrierEffect.Parameters["uTransitionStrength"].SetValue(transitionStrength);

        barrierEffect.CurrentTechnique.Passes["Barrier"].Apply();
        Main.instance.GraphicsDevice.DrawPrimitives(
            PrimitiveType.TriangleList,
            0,
            sphere.PrimitiveCount);
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }
}

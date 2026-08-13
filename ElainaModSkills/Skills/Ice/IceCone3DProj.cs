using System;
using System.Collections.Generic;
using System.IO;
using KL.Drawing;
using KL.Drawing.ThreeD;
using KL.Dusts;
using KL.Dusts.Burst;
using KL.Dusts.Glow;
using KL.Utils;
using Terraria.GameContent;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public class IceCone3DProj : ElainaBasicProjectile
{
    static ObjModel iceCone;
    static Texture2D texture;
    static BasicEffect trailEffect;
    static Texture2D trailTexture;

    private const float DefaultFov = MathF.PI / 3f;
    private ProjectionMode ProjectionMode => ProjectionMode.Perspective;
    private float fov => MathF.PI / 6f;

    private float DefaultDepth => 900f;
    private static int TrailPointCount => 30;

    private int BackwardFlyTime => 25;
    private int BurstFlyTime => 5;
    private float BackwardSpeedMultiplier => 0.5f;

    private bool initialized;
    private Vector3 currentPosition3D;
    private Vector3 targetPosition3D;
    private Vector3 velocity3D;
    private Vector3 forwardVelocity3D;
    private Vector3[] trailPositions3D = new Vector3[TrailPointCount];
    private int trailRecordedCount;
    private int time;
    private float startDepth;
    private float targetDepth;
    private Vector2 targetWorldPosition;

    private float TrailMaxWidth => 13f;
    private float TrailMinWidth => 0f;
    private Color TrailStartColor => new(180, 200, 255, 255);
    private Color TrailEndColor => new(180, 200, 255, 0);
    private float TrailDepthFade => 0.35f;
    private float TrailDelayDistance => 0f;
    private float TrailFlowSpeed => 0.03f;

    public override void Load()
    {
        iceCone ??= ObjModel.Load("伊蕾娜.Models.IceCone2");
        texture ??= AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.Ice.IceConeProj2");
        trailTexture ??= AssetManager.GetTexture("KL.Effects.Tex.Trail.235653rnwhwlbv2vm828gm");

        base.Load();
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = false;
        Projectile.penetrate = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 120;
        Projectile.alpha = 0;
        Projectile.hide = true;
        TrailLength = 20;

        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;
        base.SetDefaults();
    }

    public void SetupFlight(Vector2 targetWorldPosition, float startDepth, float targetDepth = 0f)
    {
        this.targetWorldPosition = targetWorldPosition;
        this.startDepth = startDepth;
        this.targetDepth = targetDepth;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(targetWorldPosition);
        writer.Write(startDepth);
        writer.Write(targetDepth);
        base.SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        targetWorldPosition = reader.ReadVector2();
        startDepth = reader.ReadSingle();
        targetDepth = reader.ReadSingle();
        base.ReceiveExtraAI(reader);
    }

    public override void AI()
    {
        InitializeFlight();
        UpdateMovement();

        Projectile.Center = GetModelOriginScreenPosition();

        Vector3 drawVelocity3D = time < BackwardFlyTime && forwardVelocity3D.LengthSquared() > 0.001f ? forwardVelocity3D : velocity3D;
        Vector2 planarVelocity = new Vector2(drawVelocity3D.X, drawVelocity3D.Y);
        if (planarVelocity.LengthSquared() > 0.001f)
        {
            Projectile.rotation = planarVelocity.ToRotation();
        }

        if (time >= BackwardFlyTime)
        {
            RecordTrailPosition();
        }
        Lighting.AddLight(Projectile.Center, new Color(255, 255, 255).ToVector3());
        time++;

        if (time == BackwardFlyTime + BurstFlyTime-1)
        {
            SpawnIceDust();
        }
        base.AI();
    }

    void SpawnIceDust()
    {
        /*KLBasicDust.SpawnDust(Projectile.Center,ModContent.DustType<GlowDust>(),velocity:Main.rand.NextVector2Circular(0.1f,0.1f),lifeTime:12,
            color:new Color(0,0,0,100)*1f,scale:new Vector2(1.5f));*/
        
        Vector2 dir = new Vector2(velocity3D.X,velocity3D.Y).SafeNormalize(Vector2.One);
        KLBasicDust.SpawnDust(Projectile.Center,ModContent.DustType<BurstPoint>(),velocity:Main.rand.NextVector2Circular(0.1f,0.1f),lifeTime:12,
            color:new Color(100,220,255,0)*1.0f,scale:new Vector2(0.7f));
        
        KLBasicDust.SpawnDust(Projectile.Center+dir*50f,ModContent.DustType<BurstDust>(),velocity:dir*0.01f,lifeTime:12,
            color:new Color(150,220,255,0),scale:new Vector2(2.5f,0.8f));
        
        KLBasicDust.SpawnDust(Projectile.Center,ModContent.DustType<GlowDust>(),velocity:Main.rand.NextVector2Circular(0.1f,0.1f),lifeTime:12,
            color:new Color(150,220,255,0)*0.5f,scale:new Vector2(1.5f));
    }

    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    private void InitializeFlight()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        if (startDepth == 0f)
        {
            startDepth = DefaultDepth;
        }

        currentPosition3D = new Vector3(Projectile.Center.X, Projectile.Center.Y, startDepth);
        targetPosition3D = new Vector3(targetWorldPosition.X, targetWorldPosition.Y, targetDepth);
        forwardVelocity3D = GetInitialVelocity3D();
        velocity3D = forwardVelocity3D;
        Projectile.netUpdate = true;

        for (int i = 0; i < trailPositions3D.Length; i++)
        {
            trailPositions3D[i] = currentPosition3D;
        }

        trailRecordedCount = 1;
    }

    private void UpdateMovement()
    {
        if (forwardVelocity3D.LengthSquared() <= 0.0001f)
        {
            return;
        }

        float fixedSpeed = forwardVelocity3D.Length();
        Vector3 forwardDirection = Vector3.Normalize(forwardVelocity3D);

        if (time < BackwardFlyTime)
        {
            float backwardProgress = time / (float)(BackwardFlyTime - 1);
            float currentSpeed = MathHelper.Lerp(fixedSpeed * BackwardSpeedMultiplier, 0f, backwardProgress);
            velocity3D = -forwardDirection * currentSpeed;
            currentPosition3D += velocity3D;
            return;
        }

        if (time < BackwardFlyTime + BurstFlyTime)
        {
            float burstProgress = (time - BackwardFlyTime) / (float)BurstFlyTime;
            float currentSpeed = MathHelper.Lerp(fixedSpeed * 3.4f, fixedSpeed, burstProgress);
            velocity3D = forwardDirection * currentSpeed;
            currentPosition3D += velocity3D;
            return;
        }

        /*velocity3D = forwardDirection * fixedSpeed;
        if(time<5) currentPosition3D += velocity3D;*/
    }

    private Vector3 GetInitialVelocity3D()
    {
        Vector3 toTarget = targetPosition3D - currentPosition3D;
        Vector3 direction3D = toTarget.LengthSquared() > 0.0001f
            ? Vector3.Normalize(toTarget)
            : Vector3.UnitZ;

        float speed = Projectile.velocity.Length();
        return direction3D * speed;
    }

    private void RecordTrailPosition()
    {
        for (int i = trailPositions3D.Length - 1; i > 0; i--)
        {
            trailPositions3D[i] = trailPositions3D[i - 1];
        }

        trailPositions3D[0] = GetDelayedTrailPosition3D();
        trailRecordedCount = Math.Min(trailRecordedCount + 1, trailPositions3D.Length);
    }

    private Matrix GetModelMatrix()
    {
        float toward = -1;
        if (time < BackwardFlyTime)toward = 1;
        Vector3 scale3D = new Vector3(20f);
        Vector3 forward =Vector3.Normalize(velocity3D);
        Matrix directionRotation = CreateRotationFromTo(toward*Vector3.UnitY, forward);
        return Matrix.CreateScale(scale3D) * directionRotation * Matrix.CreateTranslation(currentPosition3D);
    }

    private static Matrix CreateRotationFromTo(Vector3 from, Vector3 to)
    {
        from = Vector3.Normalize(from);
        to = Vector3.Normalize(to);

        float dot = Vector3.Dot(from, to);
        if (dot >= 0.9999f)
        {
            return Matrix.Identity;
        }

        if (dot <= -0.9999f)
        {
            Vector3 fallbackAxis = Vector3.Cross(from, Vector3.UnitX);
            if (fallbackAxis.LengthSquared() <= 0.0001f)
            {
                fallbackAxis = Vector3.Cross(from, Vector3.UnitY);
            }

            fallbackAxis.Normalize();
            return Matrix.CreateFromAxisAngle(fallbackAxis, MathF.PI);
        }

        Vector3 axis = Vector3.Cross(from, to);
        axis.Normalize();
        float angle = MathF.Acos(MathHelper.Clamp(dot, -1f, 1f));
        return Matrix.CreateFromAxisAngle(axis, angle);
    }

    private Vector2 ProjectWorldToScreen(Vector3 worldPosition)
    {
        return GraphicsUtils.ProjectWorldToScreen(worldPosition, ProjectionMode, fov);
    }

    private float GetPerspectiveScale(Vector3 worldPosition)
    {
        if (ProjectionMode == ProjectionMode.Orthographic)
        {
            return 1f;
        }

        Vector3 cameraPosition = GraphicsUtils.CameraPos(fov);
        float relativeZ = cameraPosition.Z - worldPosition.Z;
        if (MathF.Abs(relativeZ) <= 0.0001f)
        {
            return 1f;
        }

        return MathF.Abs(cameraPosition.Z / relativeZ);
    }

    private Vector2 GetModelOriginScreenPosition()
    {
        return ProjectWorldToScreen(currentPosition3D);
    }

    private Vector3 GetDelayedTrailPosition3D()
    {
        if (velocity3D.LengthSquared() <= 0.0001f)
        {
            return currentPosition3D;
        }

        return currentPosition3D - Vector3.Normalize(velocity3D) * TrailDelayDistance;
    }

    private void EnsureTrailEffect(GraphicsDevice gd)
    {
        if (trailEffect != null && !trailEffect.IsDisposed)
        {
            return;
        }

        trailEffect = new BasicEffect(gd)
        {
            VertexColorEnabled = true,
            TextureEnabled = true
        };
    }

    private void DrawTrail3D(GraphicsDevice gd)
    {
        if (trailRecordedCount < 2)
        {
            return;
        }

        List<Vector3> points = new(trailRecordedCount);

        for (int i = 0; i < trailRecordedCount; i++)
        {
            points.Add(trailPositions3D[i]);
        }

        if (points.Count < 2)
        {
            return;
        }

        int segmentCount = points.Count - 1;
        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[points.Count * 2];
        short[] indices = new short[segmentCount * 6];
        float totalSegments = Math.Max(1, points.Count - 1);
        float flowOffset = -(int)Main.timeForVisualEffects * TrailFlowSpeed;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 current = points[i];
            Vector3 previous = i == 0 ? points[i] + (points[i] - points[i + 1]) : points[i - 1];
            Vector3 next = i == points.Count - 1 ? points[i] + (points[i] - points[i - 1]) : points[i + 1];

            Vector3 tangent = previous - next;
            if (tangent.LengthSquared() <= 0.0001f)
            {
                tangent = Vector3.UnitY;
            }

            tangent.Normalize();

            Vector3 viewDirection = GraphicsUtils.GetViewDirection(ProjectionMode, current, fov);

            Vector3 side = Vector3.Cross(tangent, viewDirection);
            if (side.LengthSquared() <= 0.0001f)
            {
                side = Vector3.Cross(tangent, Vector3.UnitZ);
                if (side.LengthSquared() <= 0.0001f)
                {
                    side = Vector3.Cross(tangent, Vector3.UnitX);
                }
            }

            side.Normalize();

            float progress = i / totalSegments;
            float width = MathHelper.Lerp(TrailMaxWidth, TrailMinWidth, progress);
            float perspectiveScale = GetPerspectiveScale(current);
            float depthFade = MathHelper.Lerp(1f, TrailDepthFade, progress);
            float finalWidth = width; // Math.Max(perspectiveScale, 0.0001f) * depthFade;

            Color color = Color.Lerp(TrailStartColor, TrailEndColor, progress);
            int vertexIndex = i * 2;
            float uvU = progress + flowOffset;

            vertices[vertexIndex] =
                new VertexPositionColorTexture(current + side * finalWidth, color, new Vector2(uvU, 0f));
            vertices[vertexIndex + 1] =
                new VertexPositionColorTexture(current - side * finalWidth, color, new Vector2(uvU, 1f));

            if (i >= points.Count - 1)
            {
                continue;
            }

            int index = i * 6;
            short baseVertex = (short)vertexIndex;
            indices[index] = baseVertex;
            indices[index + 1] = (short)(baseVertex + 1);
            indices[index + 2] = (short)(baseVertex + 2);
            indices[index + 3] = (short)(baseVertex + 1);
            indices[index + 4] = (short)(baseVertex + 3);
            indices[index + 5] = (short)(baseVertex + 2);
        }

        trailTexture = AssetManager.GetTexture("KL.Effects.Tex.Trail.235653rnwhwlbv2vm828gm");

        EnsureTrailEffect(gd);
        trailEffect.World = Matrix.Identity;
        trailEffect.View = Matrix.Identity;
        trailEffect.Projection = GraphicsUtils.GetVPMatrix(ProjectionMode, fov);
        trailEffect.Texture = trailTexture;

        BlendState oldBlendState = gd.BlendState;
        DepthStencilState oldDepthStencilState = gd.DepthStencilState;
        RasterizerState oldRasterizerState = gd.RasterizerState;
        SamplerState oldSamplerState0 = gd.SamplerStates[0];

        gd.BlendState = BlendState.Additive;
        gd.DepthStencilState = DepthStencilState.Default;
        gd.RasterizerState = RasterizerState.CullNone;
        gd.SamplerStates[0] = SamplerState.LinearWrap;

        foreach (EffectPass pass in trailEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            gd.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                vertices,
                0,
                vertices.Length,
                indices,
                0,
                segmentCount * 2);
        }

        gd.BlendState = oldBlendState;
        gd.DepthStencilState = oldDepthStencilState;
        gd.RasterizerState = oldRasterizerState;
        gd.SamplerStates[0] = oldSamplerState0;
        
    }

    public override bool PreDraw(ref Color lightColor)
    {
        base.PreDraw(ref lightColor);
        return false;
    }


    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        behindNPCsAndTiles.Add(index);

        bool drawTrailBehind = GetDrawLayerZ() >= 0f;
        RequestIceConeDraw(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCs, 1f, drawTrailBehind);
        RequestIceConeDraw(LayerDrawRequestSystem.DrawTargetLayer.OverPlayers, -1f, !drawTrailBehind);
    }

    private void RequestIceConeDraw(LayerDrawRequestSystem.DrawTargetLayer layer, float depthClipSide,
        bool drawTrail)
    {
        LayerDrawRequestSystem.RequestBefore("icedraw", layer, ctx =>
        {
            Color lightColor = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
            Effect iceConeEffect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/ThreeD/IceCone3D", AssetRequestMode.ImmediateLoad).Value;
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            VertexBuffer vertexBuffer = iceCone.GetOrCreateVertexBuffer(gd);
            Vector3 cameraPosition = GraphicsUtils.CameraPos(fov);
            Vector3 sunPosition = new(new Vector2(Main.screenWidth, Main.screenHeight) / 4f + Main.screenPosition,
                -1000f);
            Vector3 lightDirection = Vector3.Normalize(currentPosition3D - sunPosition);

            if (ctx.IsFirst)
            {
                BeginDraw3D();
            }

            if (drawTrail)
            {
                DrawTrail3D(gd);
            }

            iceConeEffect.Parameters["uWorld"].SetValue(GetModelMatrix());
            iceConeEffect.Parameters["uViewProjection"].SetValue(GraphicsUtils.GetVPMatrix(ProjectionMode, fov));
            iceConeEffect.Parameters["uLightDirection"].SetValue(lightDirection);
            iceConeEffect.Parameters["uLightColor"].SetValue(lightColor.ToVector3());
            iceConeEffect.Parameters["uCameraPosition"].SetValue(cameraPosition);
            iceConeEffect.Parameters["uBaseColor"].SetValue(new Color(200, 220, 255, 255).ToVector4());
            iceConeEffect.Parameters["uFresnelColor"].SetValue(new Color(255, 255, 255).ToVector3() * 0.5f);
            iceConeEffect.Parameters["uAmbientStrength"].SetValue(0.8f);
            iceConeEffect.Parameters["uDiffuseStrength"].SetValue(1f);
            iceConeEffect.Parameters["uFresnelStrength"].SetValue(2f);

            float dissolveThreshold = 0f;
            if (time < BackwardFlyTime)
            {
                float progress = time / (float)BackwardFlyTime;
                float appearProgress = MathHelper.Clamp(progress * 1.5f, 0f, 1f);
                dissolveThreshold = MathHelper.Lerp(1.2f, 0f, appearProgress);
            }

            iceConeEffect.Parameters["uDissolveNoiseScale"].SetValue(1f);
            iceConeEffect.Parameters["uDissolveThreshold"].SetValue(dissolveThreshold);
            iceConeEffect.Parameters["uDissolveEdgeWidth"].SetValue(0.2f);
            iceConeEffect.Parameters["uDissolveEdgeColor"].SetValue(new Color(100, 200, 255, 255).ToVector4() * 4.5f);
            iceConeEffect.Parameters["uDepthClipSide"].SetValue(depthClipSide);

            gd.BlendState = BlendState.NonPremultiplied;
            gd.DepthStencilState = DepthStencilState.Default;
            gd.SamplerStates[0] = SamplerState.LinearWrap;
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            gd.RasterizerState = RasterizerState.CullClockwise;
            gd.Textures[0] = texture;
            gd.Textures[1] = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/4", AssetRequestMode.ImmediateLoad).Value;
            gd.SetVertexBuffer(vertexBuffer);
            iceConeEffect.CurrentTechnique.Passes[0].Apply();
            gd.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount);

            if (ctx.IsLast)
            {
                Main.spriteBatch.End();
            }
        });
    }

    private float GetDrawLayerZ()
    {
        return initialized ? currentPosition3D.Z : startDepth;
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }
}

using System;
using System.Collections.Generic;
using KL.Drawing;
using KL.Drawing.ThreeD;
using KL.Dusts;
using KL.Dusts.Burst;
using KL.Dusts.Glow;
using KL.Utils;
using ReLogic.Content;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public class IceCone3DUnit : VisualUnit
{
    private static ObjModel iceCone;
    private static Texture2D texture;
    private static BasicEffect trailEffect;
    private static Texture2D trailTexture;
    private static Effect iceConeEffect;
    private static Texture2D dissolveNoiseTexture;

    private const int TrailPointCount = 30;
    private const int BackwardFlyTime = 25;
    private const int BurstFlyTime = 5;
    private const float BackwardSpeedMultiplier = 0.5f;
    private const float TrailMaxWidth = 13f;
    private const float TrailMinWidth = 0f;
    private const float TrailDelayDistance = 108f;
    private const float TrailFlowSpeed = 0.03f;

    private readonly Vector3[] trailPositions3D = new Vector3[TrailPointCount];
    private Vector3 targetPosition3D;
    private Vector3 forwardVelocity3D;
    private Vector3 currentVelocity3D;
    private int trailRecordedCount;

    private ProjectionMode ProjectionMode => ProjectionMode.Perspective;
    private float Fov => MathF.PI / 6f;

    public IceCone3DUnit()
    {
    }

    public IceCone3DUnit(Vector3 position3D, Vector3 targetPosition3D, Vector3 velocity3D, int delay, int timeLeft)
        : base(position3D, Vector3.Zero, delay, timeLeft)
    {
        this.targetPosition3D = targetPosition3D;
        forwardVelocity3D = velocity3D;
        currentVelocity3D = velocity3D;
    }

    public override void OnLoad(Mod mod)
    {
        iceCone ??= ObjModel.Load("伊蕾娜.Models.IceCone2");
        texture ??= AssetManager.GetTexture("伊蕾娜.ElainaModSkills.Skills.Ice.IceConeProj2");
        trailTexture ??= AssetManager.GetTexture("KL.Effects.Tex.Trail.235653rnwhwlbv2vm828gm");
        iceConeEffect ??= ModContent.Request<Effect>("伊蕾娜/Effects/Content/ThreeD/IceCone3D", AssetRequestMode.ImmediateLoad).Value;
        dissolveNoiseTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/4", AssetRequestMode.ImmediateLoad).Value;
        base.OnLoad(mod);
    }

    public override void OnSpawn()
    {
        for (int i = 0; i < trailPositions3D.Length; i++)
        {
            trailPositions3D[i] = Position3D;
        }

        trailRecordedCount = 1;
        base.OnSpawn();
    }

    public override void Update()
    {
        if (!Active)
        {
            return;
        }

        Timer++;
        if (!Started)
        {
            return;
        }

        int time = Timer - Delay;
        UpdateMovement(time);

        Vector3 drawVelocity3D = time < BackwardFlyTime && forwardVelocity3D.LengthSquared() > 0.001f
            ? forwardVelocity3D
            : currentVelocity3D;
        Vector2 planarVelocity = new(drawVelocity3D.X, drawVelocity3D.Y);
        if (planarVelocity.LengthSquared() > 0.001f)
        {
            Rotation = planarVelocity.ToRotation();
        }

        if (time >= BackwardFlyTime)
        {
            RecordTrailPosition();
        }

        Lighting.AddLight(GetModelOriginScreenPosition(), new Color(255, 255, 255).ToVector3());

        if (time == BackwardFlyTime + BurstFlyTime - 1)
        {
            SpawnIceDust();
        }

        if (time >= TimeLeft)
        {
            Kill();
        }
    }

    public override void Draw()
    {
        if (iceCone == null || texture == null || iceConeEffect == null || dissolveNoiseTexture == null)
        {
            return;
        }

        int time = Timer - Delay;
        bool drawAuxiliaryBehind = Position3D.Z >= 0f;
        RequestIceConeDraw(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCsAndTiles, time, 1f,
            drawAuxiliaryBehind);
        RequestIceConeDraw(LayerDrawRequestSystem.DrawTargetLayer.OverPlayers, time, -1f,
            !drawAuxiliaryBehind);
    }

    private void RequestIceConeDraw(LayerDrawRequestSystem.DrawTargetLayer layer, int time,
        float depthClipSide, bool drawAuxiliary)
    {
        LayerDrawRequestSystem.RequestBefore("ice_shard_lock_draw", layer, ctx =>
        {
            Vector2 lightPosition = GetModelOriginScreenPosition();
            Color lightColor = Lighting.GetColor((int)(lightPosition.X / 16f), (int)(lightPosition.Y / 16f));
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            VertexBuffer vertexBuffer = iceCone.GetOrCreateVertexBuffer(gd);
            Vector3 cameraPosition = GraphicsUtils.CameraPos(Fov);
            Vector3 sunPosition = new(new Vector2(Main.screenWidth, Main.screenHeight) / 4f + Main.screenPosition, -1000);
            Vector3 lightDirection = Vector3.Normalize(Position3D - sunPosition);

            if (ctx.IsFirst)
            {
                BeginDraw3D();
            }

            if (drawAuxiliary&&time >= BackwardFlyTime)
            {
                DrawTrail3D(gd);
            }
            
            iceConeEffect.Parameters["uWorld"].SetValue(GetModelMatrix(time));
            iceConeEffect.Parameters["uViewProjection"].SetValue(GraphicsUtils.GetVPMatrix(ProjectionMode, Fov));
            iceConeEffect.Parameters["uLightDirection"].SetValue(lightDirection);
            iceConeEffect.Parameters["uLightColor"].SetValue(lightColor.ToVector3());
            iceConeEffect.Parameters["uCameraPosition"].SetValue(cameraPosition);
            iceConeEffect.Parameters["uBaseColor"].SetValue(new Color(200, 220, 255, 255).ToVector4());
            iceConeEffect.Parameters["uFresnelColor"].SetValue(new Color(255, 255, 255).ToVector3() * 0.5f);
            iceConeEffect.Parameters["uAmbientStrength"].SetValue(0.8f);
            iceConeEffect.Parameters["uDiffuseStrength"].SetValue(1f);
            iceConeEffect.Parameters["uFresnelStrength"].SetValue(2f);
            iceConeEffect.Parameters["uDissolveNoiseScale"].SetValue(1f);
            iceConeEffect.Parameters["uDissolveThreshold"].SetValue(GetDissolveThreshold(time));
            iceConeEffect.Parameters["uDissolveEdgeWidth"].SetValue(0.2f);
            iceConeEffect.Parameters["uDissolveEdgeColor"].SetValue(new Color(100, 200, 255, 255).ToVector4() * 4.5f);
            iceConeEffect.Parameters["uDepthClipSide"].SetValue(depthClipSide);
            iceConeEffect.Parameters["uOutlineWidth"].SetValue(4f);
            iceConeEffect.Parameters["uScreenSize"].SetValue(new Vector2(gd.Viewport.Width, gd.Viewport.Height));
            iceConeEffect.Parameters["uOutlineColor"].SetValue(new Color(255, 255, 255, 255).ToVector4());

            gd.BlendState = BlendState.NonPremultiplied;
            gd.DepthStencilState = DepthStencilState.DepthRead;
            gd.SamplerStates[0] = SamplerState.LinearWrap;
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            gd.Textures[0] = texture;
            gd.Textures[1] = dissolveNoiseTexture;
            gd.SetVertexBuffer(vertexBuffer);

            gd.RasterizerState = RasterizerState.CullCounterClockwise;
            iceConeEffect.CurrentTechnique.Passes["Outline"].Apply();
            gd.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount);

            gd.DepthStencilState = DepthStencilState.Default;
            gd.RasterizerState = RasterizerState.CullClockwise;
            iceConeEffect.CurrentTechnique.Passes["Base"].Apply();
            gd.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount);
            
            if (ctx.IsLast)
            {
                Main.spriteBatch.End();
            }
        });
    }

    private void UpdateMovement(int time)
    {
        if (forwardVelocity3D.LengthSquared() <= 0.0001f)
        {
            return;
        }

        float fixedSpeed = forwardVelocity3D.Length();
        Vector3 forwardDirection = Vector3.Normalize(forwardVelocity3D);
        if (time < BackwardFlyTime)
        {
            float progress = time / (float)(BackwardFlyTime - 1);
            currentVelocity3D = -forwardDirection * MathHelper.Lerp(fixedSpeed * BackwardSpeedMultiplier, 0f, progress);
            Position3D += currentVelocity3D;
            return;
        }

        if (time < BackwardFlyTime + BurstFlyTime)
        {
            float progress = (time - BackwardFlyTime) / (float)BurstFlyTime;
            currentVelocity3D = forwardDirection * MathHelper.Lerp(fixedSpeed * 3.2f, fixedSpeed, progress);
            Position3D += currentVelocity3D;

        }
        

    }

    private void SpawnIceDust()
    {
        Vector2 center = GetModelOriginScreenPosition();
        Vector2 dir = new Vector2(currentVelocity3D.X, currentVelocity3D.Y).SafeNormalize(Vector2.One);
        KLBasicDust.SpawnDust(center, ModContent.DustType<BurstPoint>(), velocity: Main.rand.NextVector2Circular(0.1f, 0.1f), lifeTime: 12,
            color: new Color(100, 220, 255, 0), scale: new Vector2(0.7f));
        
        KLBasicDust.SpawnDust(center + dir * 10f, ModContent.DustType<BurstDust>(), velocity: dir * 0.01f, lifeTime: 12,
            color: new Color(150, 220, 255, 0), scale: new Vector2(3.5f, 0.8f));
        
        KLBasicDust.SpawnDust(center, ModContent.DustType<GlowDust>(), velocity: Main.rand.NextVector2Circular(0.1f, 0.1f), lifeTime: 12,
            color: new Color(150, 220, 255, 0) * 0.5f, scale: new Vector2(1.5f));
    }

    private Matrix GetModelMatrix(int time)
    {
        bool isBackwardFlying = time < BackwardFlyTime;
        float toward = isBackwardFlying ? 1f : -1f;
        Vector3 forward = GetStableModelForward(isBackwardFlying);
        return Matrix.CreateScale(new Vector3(20f)) * CreateRotationFromTo(toward * Vector3.UnitY, forward) * Matrix.CreateTranslation(Position3D);
    }

    private Vector3 GetStableModelForward(bool isBackwardFlying)
    {
        if (currentVelocity3D.LengthSquared() > 0.0001f)
        {
            return Vector3.Normalize(currentVelocity3D);
        }

        if (forwardVelocity3D.LengthSquared() > 0.0001f)
        {
            Vector3 forwardDirection = Vector3.Normalize(forwardVelocity3D);
            return isBackwardFlying ? -forwardDirection : forwardDirection;
        }

        return isBackwardFlying ? -Vector3.UnitY : Vector3.UnitY;
    }

    private float GetDissolveThreshold(int time)
    {
        if (time >= BackwardFlyTime)
        {
            return 0f;
        }

        float progress = time / (float)BackwardFlyTime;
        return MathHelper.Lerp(1.2f, 0f, MathHelper.Clamp(progress * 1.5f, 0f, 1f));
    }

    private Vector2 GetModelOriginScreenPosition()
    {
        return GraphicsUtils.ProjectWorldToScreen(Position3D, ProjectionMode, Fov);
    }

    private Vector3 GetDelayedTrailPosition3D()
    {
        return currentVelocity3D.LengthSquared() <= 0.0001f
            ? Position3D
            : Position3D - Vector3.Normalize(currentVelocity3D) * TrailDelayDistance;
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
        if (trailRecordedCount < 2 || trailTexture == null)
        {
            return;
        }

        List<Vector3> points = new(trailRecordedCount);
        for (int i = 0; i < trailRecordedCount; i++)
        {
            points.Add(trailPositions3D[i]);
        }

        int segmentCount = points.Count - 1;
        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[points.Count * 2];
        short[] indices = new short[segmentCount * 6];
        float flowOffset = -(int)Main.timeForVisualEffects * TrailFlowSpeed;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 previous = i == 0 ? points[i] + (points[i] - points[i + 1]) : points[i - 1];
            Vector3 next = i == points.Count - 1 ? points[i] + (points[i] - points[i - 1]) : points[i + 1];
            Vector3 tangent = previous - next;
            if (tangent.LengthSquared() <= 0.0001f)
            {
                tangent = Vector3.UnitY;
            }

            tangent.Normalize();
            Vector3 side = Vector3.Cross(tangent, GraphicsUtils.GetViewDirection(ProjectionMode, points[i], Fov));
            if (side.LengthSquared() <= 0.0001f)
            {
                side = Vector3.UnitX;
            }

            side.Normalize();
            float progress = i / Math.Max(1f, points.Count - 1f);
            float width = MathHelper.Lerp(TrailMaxWidth, TrailMinWidth, progress);
            Color color = Color.Lerp(new Color(180, 200, 255, 255), new Color(180, 200, 255, 0), progress);
            int vertexIndex = i * 2;
            vertices[vertexIndex] = new VertexPositionColorTexture(points[i] + side * width, color, new Vector2(progress + flowOffset, 0f));
            vertices[vertexIndex + 1] = new VertexPositionColorTexture(points[i] - side * width, color, new Vector2(progress + flowOffset, 1f));

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

        EnsureTrailEffect(gd);
        trailEffect.World = Matrix.Identity;
        trailEffect.View = Matrix.Identity;
        trailEffect.Projection = GraphicsUtils.GetVPMatrix(ProjectionMode, Fov);
        trailEffect.Texture = trailTexture;

        BlendState oldBlendState = gd.BlendState;
        DepthStencilState oldDepthStencilState = gd.DepthStencilState;
        RasterizerState oldRasterizerState = gd.RasterizerState;
        SamplerState oldSamplerState0 = gd.SamplerStates[0];
        gd.BlendState = BlendState.Additive;
        gd.DepthStencilState = DepthStencilState.None;
        gd.RasterizerState = RasterizerState.CullNone;
        gd.SamplerStates[0] = SamplerState.LinearWrap;

        foreach (EffectPass pass in trailEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, segmentCount * 2);
        }

        gd.BlendState = oldBlendState;
        gd.DepthStencilState = oldDepthStencilState;
        gd.RasterizerState = oldRasterizerState;
        gd.SamplerStates[0] = oldSamplerState0;
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
        return Matrix.CreateFromAxisAngle(axis, MathF.Acos(MathHelper.Clamp(dot, -1f, 1f)));
    }
}

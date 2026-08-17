using System;
using System.Collections.Generic;
using KL.Drawing;
using KL.Drawing.ThreeD;
using KL.Extensions;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaModSkills.Skills.Lightning;

public class LightningModelTest : ElainaBasicProjectile
{
    static ObjModel lightningModel;
    static ObjModel lightningModel2;
    static ObjModel lightningModel3;
    static ObjModel lightningModel4;

    static Effect lightningEffect;

    private readonly List<VisualUnit> units = new();

    private int ProjectileLifeTime => 180;
    private int LightningSpawnInterval => 5;
    private int LightningLifeTime => 25;

    public override void Load()
    {
        lightningModel ??= BuildOutlineModel(ObjModel.Load("伊蕾娜.Models.Lightning_1"));
        lightningModel2 ??= BuildOutlineModel(ObjModel.Load("伊蕾娜.Models.Lightning_2"));
        lightningModel3 ??= BuildOutlineModel(ObjModel.Load("伊蕾娜.Models.Lightning_3"));
        lightningModel4 ??= BuildOutlineModel(ObjModel.Load("伊蕾娜.Models.Lightning_4"));
        lightningEffect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/ThreeD/LightningEffect", AssetRequestMode.ImmediateLoad).Value;
        
        base.Load();
    }

    static ObjModel BuildOutlineModel(ObjModel source)
    {
        Vertex3D[] vertices = (Vertex3D[])source.Vertices.Clone();
        Dictionary<Vector3, Vector2> normalSums = new();
        Dictionary<(Vector3, Vector3), (int Count, Vector2 Normal)> edges = new();

        for (int i = 0; i < vertices.Length; i += 3)
        {
            Vector3 a = vertices[i].Position;
            Vector3 b = vertices[i + 1].Position;
            Vector3 c = vertices[i + 2].Position;
            float winding = MathF.Sign((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X));
            AddOutlineEdge(edges, a, b, winding);
            AddOutlineEdge(edges, b, c, winding);
            AddOutlineEdge(edges, c, a, winding);
        }

        foreach ((Vector3 start, Vector3 end) in edges.Keys)
        {
            (int count, Vector2 edgeNormal) = edges[(start, end)];
            if (count != 1)
                continue;

            normalSums[start] = normalSums.GetValueOrDefault(start) + edgeNormal;
            normalSums[end] = normalSums.GetValueOrDefault(end) + edgeNormal;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 normal = normalSums.GetValueOrDefault(vertices[i].Position);
            if (normal.LengthSquared() > 0f)
                normal.Normalize();
            vertices[i].Normal = new Vector3(normal, 0f);
        }

        return new ObjModel(vertices);
    }

    static void AddOutlineEdge(Dictionary<(Vector3, Vector3), (int Count, Vector2 Normal)> edges, Vector3 start, Vector3 end, float winding)
    {
        (Vector3, Vector3) key = ComparePosition(start, end) <= 0 ? (start, end) : (end, start);
        Vector2 direction = new(end.X - start.X, end.Y - start.Y);
        Vector2 outwardNormal = winding >= 0f
            ? new Vector2(direction.Y, -direction.X)
            : new Vector2(-direction.Y, direction.X);
        if (outwardNormal.LengthSquared() > 0f)
            outwardNormal.Normalize();

        (int count, Vector2 normal) = edges.GetValueOrDefault(key);
        edges[key] = (count + 1, outwardNormal);
    }

    static int ComparePosition(Vector3 left, Vector3 right)
    {
        int x = left.X.CompareTo(right.X);
        if (x != 0)
            return x;
        int y = left.Y.CompareTo(right.Y);
        return y != 0 ? y : left.Z.CompareTo(right.Z);
    }

    public override void SetDefaults()
    {
        Projectile.timeLeft = ProjectileLifeTime;
        base.SetDefaults();
    }

    public override void AI()
    {
        if ((ProjectileLifeTime - Projectile.timeLeft) % LightningSpawnInterval == 0)
            SpawnLightningUnit();
        
        if(Main.GameUpdateCount%Main.rand.Next(5,8)==0) SpawnMainLightning();
        
        VisualUnit.UpdateAll(units);
        base.AI();
    }

    private void SpawnLightningUnit()
    {
        ObjModel model = lightningModel;
        Vector2 modelScale = new(
            Main.rand.NextFloat(0.85f, 1.15f),
            Main.rand.NextFloat(0.7f, 2.2f));
        
        
        float azimuth = Main.rand.NextFloat(0f, MathHelper.TwoPi);
        float coneAngle = Main.rand.NextFloat(-0.6f, 0.6f);
        Vector3 rotation = new(
            coneAngle,
            azimuth,
            0f);
        Vector2 position = Projectile.Center;

        VisualUnit.Spawn(units,
            new LightningVisualUnit(model, modelScale, rotation, position, LightningLifeTime+Main.rand.Next(5, 10)), Projectile);
    }
    
    void SpawnMainLightning()
    {
        int lightningType = Main.rand.Next(4);
        ObjModel model = lightningType switch
        {
            0 => lightningModel,
            1 => lightningModel2,
            2 => lightningModel3,
            _ => lightningModel4
        };
        float scaleMultiplier = lightningType is 0 or 2 ? 2f : 1f;
        Vector2 modelScale = new Vector2(0.7f,0.8f)*scaleMultiplier * new Vector2(
            Main.rand.NextFloat(0.75f, 1.2f),
            Main.rand.NextFloat(2.7f, 3.2f));
        Vector3 rotation = new(
            Main.rand.NextFloat(-0.1f, 0.1f),
            Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi),
            Main.rand.NextFloat(-0.1f, 0.1f));

        VisualUnit.Spawn(units,
            new LightningVisualUnit(model, modelScale, rotation, Projectile.Center, Main.rand.Next(12, 16)), Projectile);
    }

    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (lightningModel == null || lightningModel2 == null || lightningEffect == null)
            return false;

        EndBeginDraw3D(2);
        VisualUnit.DrawAll(units);
        EndBeginDraw();
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        VisualUnit.KillAll(units);
        base.OnKill(timeLeft);
    }
    
    private static void DrawMainLightning(ObjModel model, Vector2 modelScale, Vector3 rotation, Vector2 position,
        int elapsedTime, int lifeTime)
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        VertexBuffer vertexBuffer = model.GetOrCreateVertexBuffer(gd);
        Vector2 uDissolveNoiseScale = new Vector2(1f, 15f)*0.1f;

        const float outlineWidth = 3f;
        Matrix world = Matrix.CreateScale(new Vector3(modelScale, 1f)) *
                       Matrix.CreateRotationX(rotation.X) *
                       Matrix.CreateRotationY(rotation.Y) *
                       Matrix.CreateRotationZ(-MathHelper.Pi + rotation.Z) *
                       Matrix.CreateTranslation(new Vector3(position, 0f));
        Texture2D whiteTexture = TextureAssets.MagicPixel.Value;
        Texture2D dissolveNoise = PerLinNoiseX;// ModContent.Request<Texture2D>("KL/Effects/Tex/noi_1", AssetRequestMode.ImmediateLoad).Value;

        const int dissolveStartTime = 0;
        float dissolveProgress = MathHelper.Clamp(
            (elapsedTime - dissolveStartTime) / (float)(lifeTime - dissolveStartTime), 0f, 1f);
        lightningEffect.SetValue("uViewProjection", GraphicsUtils.GetVPMatrix(ProjectionMode.Orthographic));
        lightningEffect.SetValue("uBaseColor", Color.Black.ToVector4());
        lightningEffect.SetValue("uDissolveNoiseScale", uDissolveNoiseScale);
        lightningEffect.SetValue("uDissolveThreshold", dissolveProgress * 1.05f);
        lightningEffect.SetValue("uDissolveEdgeWidth", 0.0f);
        lightningEffect.SetValue("uDissolveEdgeColor", Color.Black.ToVector4() * 1f);
        
        gd.Textures[0] = whiteTexture;
        gd.Textures[1] = dissolveNoise;

        gd.SetVertexBuffer(vertexBuffer);

        RasterizerState baseRasterizerState = gd.RasterizerState;

        gd.DepthStencilState = DepthStencilState.None;
        gd.RasterizerState = RasterizerState.CullNone;

        lightningEffect.SetValue("uWorld", world);
        lightningEffect.SetValue("uWorldInverseTranspose", Matrix.Transpose(Matrix.Invert(world)));
        lightningEffect.SetValue("uOutlineWidth", outlineWidth);
        lightningEffect.CurrentTechnique.Passes[0].Apply();
        gd.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount);

        lightningEffect.SetValue("uOutlineWidth", 0f);
        lightningEffect.SetValue("uDissolveEdgeWidth", 0.05f);
        lightningEffect.SetValue("uDissolveEdgeColor", Color.Black.ToVector4() * 1f);
        lightningEffect.SetValue("uBaseColor", new Color(100, 200, 255, 255).ToVector4() * 1.3f);
        lightningEffect.CurrentTechnique.Passes[0].Apply();
        gd.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount);

        gd.RasterizerState = baseRasterizerState;
    }

    private sealed class LightningVisualUnit : VisualUnit
    {
        private readonly ObjModel model;
        private readonly Vector2 modelScale;
        private readonly Vector3 modelRotation;

        public LightningVisualUnit(ObjModel model, Vector2 modelScale, Vector3 modelRotation,
            Vector2 position, int timeLeft)
            : base(position, Vector2.Zero, 0, timeLeft)
        {
            this.model = model;
            this.modelScale = modelScale;
            this.modelRotation = modelRotation;
        }

        public override void Draw()
        {
            DrawMainLightning(model, modelScale, modelRotation, Position2D, Timer, TimeLeft);
        }
    }
}
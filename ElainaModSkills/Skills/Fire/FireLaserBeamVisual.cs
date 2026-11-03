using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KL.Drawing;
using static KL.Drawing.DrawHelper;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>持续射线的原有两层亮线；入场只调宽度，离场按固定噪声局部收缩截面。</summary>
internal sealed class FireLaserBeamVisual
{
    internal static int EntranceTicks => 31;
    internal static int ReleaseTicks => 42;

    // 入场：先展开，再快速鼓起，峰值停留后回落；这些参数只控制 OpeningWidth。
    private static float EntranceGrowTicks => 1f;
    private static float EntrancePeakTick => 5f;
    private static float EntrancePeakHoldTicks => 5f;
    private static float EntrancePeakWidth => 2.2f;

    // 收缩时长（游戏 tick）：越小越快。线体应先于头部收完，避免露出截断端。
    private static float ReleaseLineShrinkTicks => 31f;
    // 缓出指数：1 为匀速，2 为先快后慢；越大，前段收得越快。
    private static float ReleaseLineShrinkPower => 1f;
    private static float ReleaseHeadShrinkTicks => 33f;

    // 退场调参：噪声周期越大，断点越少、残留线段越长（单位：世界像素）。
    private static float ReleaseNoisePeriod => 1300f;
    // 局部收缩速度差：越接近 1 越整齐、越晚断开；越大越早出现缺口。
    private static float ReleaseMaxShrinkRate => 1.25f;
    // 粒子初速度范围（像素 / tick）：前进为主，侧向分量控制张开的角度。
    private static Vector2 ReleaseSparkForwardSpeed => new(3.5f, 6f);
    private static Vector2 ReleaseSparkSideSpeed => new(0.6f, 1.6f);
    // 平均每多少世界像素一颗：越小越密。位置独立随机，少量聚集，不按格子或两侧配对。
    private static float ReleaseSparkSpacing => 24f;
    private static Vector2 ReleaseSparkLengthScale => new(0.4f, 0.9f);
    private readonly Vector2[] points = new Vector2[300];
    private readonly Vector2[] head = new Vector2[100];
    private readonly Vector2[] originalHead = QuickConePoints(Vector2.UnitX * 100, -Vector2.UnitX * 150, 100, 70, 50f, 0.2f);
    private readonly BeamVertex[] releaseVertices = new BeamVertex[299 * 6];

    internal static float OpeningWidth(float ticks)
    {
        if (ticks <= 0f) return 0f;
        if (ticks < EntranceGrowTicks) return MathHelper.SmoothStep(0f, 1f, ticks / EntranceGrowTicks);
        if (ticks < EntrancePeakTick) return MathHelper.SmoothStep(1f, EntrancePeakWidth,
            (ticks - EntranceGrowTicks) / Math.Max(1f, EntrancePeakTick - EntranceGrowTicks));
        float settleStart = EntrancePeakTick + EntrancePeakHoldTicks;
        if (ticks < settleStart) return EntrancePeakWidth;
        return MathHelper.SmoothStep(EntrancePeakWidth, 1f, (ticks - settleStart) / Math.Max(1f, EntranceTicks - settleStart));
    }

    // 前半段连续收细，后半段由 PerlinX 让不同位置先后归零，形成分离的细线。
    private static float ReleaseLineProgress(float progress) =>
        MathHelper.Clamp(progress * ReleaseTicks / Math.Max(1f, ReleaseLineShrinkTicks), 0f, 1f);

    // 短射线也覆盖完整噪声起伏，避免只取到同一波峰而整体收没。
    internal static float ReleaseNoiseScale(float length) => Math.Max(1f, length / Math.Max(1f, ReleaseNoisePeriod));

    internal static float ReleaseShrink(float progress) =>
        1f - MathF.Pow(1f - ReleaseLineProgress(progress), Math.Max(1f, ReleaseLineShrinkPower));

    internal static float ReleaseHeadWidth(float progress) => 1f - MathHelper.SmoothStep(0f, 1f,
        MathHelper.Clamp(progress * ReleaseTicks / Math.Max(1f, ReleaseHeadShrinkTicks), 0f, 1f));

    private static Texture2D Texture(string path) => ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;

    internal void Draw(Vector2 start, Vector2 direction, float length, float ticks, float width, float release = -1f)
    {
        if (length <= 0f || width <= 0f || release >= 1f) return;
        Texture2D trail = Texture("KL/Effects/Tex/Trail/Eff_Trail_1679");
        Texture2D core = Texture("KL/Effects/Tex/Trail/Eff_Trail_2014");
        Vector2 flow = new(-(ticks % 1200f) / 30f, 0f);
        Vector2 imageScale = new(500f / length, 1f);
        Vector4 outerColor = new(new Color(255, 100, 20).ToVector3() * 3.8f, 1f);
        Vector4 coreColor = new(new Color(255, 200, 150).ToVector3() * 1.8f, 1f);

        if (release < 0f)
        {
            for (int i = 0; i < points.Length; i++)
                points[i] = start + direction * (length * i / (points.Length - 1));
            TrailEffect(trail, points, outerColor, outerColor, maxWidth: 2f * width, endWidth: 15f * width,
                startAlpha: 1f, endAlpha: 1f, drawTimes: 1, blendState: 2, uTime: flow, imageScale: imageScale,
                useRforAlpha: false, debugPoint: false);
            TrailEffect(core, points, coreColor, coreColor, maxWidth: 2f * width, endWidth: 6f * width,
                startAlpha: 1f, endAlpha: 1f, drawTimes: 1, blendState: 2, uTime: flow, imageScale: imageScale,
                useRforAlpha: true, debugPoint: false);
        }
        else
        {
            Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/FireLaserRelease", AssetRequestMode.ImmediateLoad).Value;
            Texture2D noise = Texture("KL/Effects/Tex/PerlinX");
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            Main.spriteBatch.End();
            try
            {
                device.BlendState = DrawSystem.GetBlendStateForBloom(BlendState.NonPremultiplied);
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = RasterizerState.CullNone;
                device.SamplerStates[0] = device.SamplerStates[1] = SamplerState.LinearWrap;
                device.Textures[1] = noise;
                Matrix model = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f) * Main.Transform;
                effect.Parameters["uTransform"].SetValue(model * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
                effect.Parameters["ImageScale"].SetValue(imageScale);
                effect.Parameters["Flow"].SetValue(flow.X);
                effect.Parameters["Shrink"].SetValue(ReleaseShrink(release));
                effect.Parameters["NoiseScale"].SetValue(ReleaseNoiseScale(length));
                effect.Parameters["MaxShrinkRate"].SetValue(Math.Max(1f, ReleaseMaxShrinkRate));
                DrawReleaseLayer(device, effect, trail, start, direction, length, width, 15f, outerColor, false);
                DrawReleaseLayer(device, effect, core, start, direction, length, width, 6f, coreColor, true);
            }
            finally { BeginDraw(); }
        }

        // 火焰头随主体一起展开，退场稍晚收完，遮住正在收细消融的线体末端。
        float headWidth = release < 0f ? 1f : ReleaseHeadWidth(release);
        if (headWidth <= 0f) return;
        for (int i = 0; i < head.Length; i++)
            head[i] = new Vector2(originalHead[i].X, originalHead[i].Y * headWidth);
        // 头部同样通过截面收缩消失，保留仍可见部分的发光强度。
        float headAlpha = Math.Min(1f, width);
        VertexDrawEffect(Texture("KL/Effects/Tex/Wind/wind3"), head,
            new Color(255, 50, 30, 255), new Color(255, 150, 30, 0),
            startAlpha: 4.6f * headAlpha, endAlpha: 0, blendState: 1, drawTimes: 1,
            uTime: new Vector2((float)(Main.timeForVisualEffects % 60) / 20f, 0),
            attachPoint: start + direction * (length - 70f), attachRotation: direction.ToRotation(),
            useRforAlpha: false, debugPoint: false);
    }

    private void DrawReleaseLayer(GraphicsDevice device, Effect effect, Texture2D texture, Vector2 start,
        Vector2 direction, float length, float width, float endWidth, Vector4 color, bool redAlpha)
    {
        // 与 TrailEffect 相同的端点 UV / 宽度插值，释放首帧不跳变，噪声固定在世界中的整条射线上。
        Vector2 normal = new(direction.Y, -direction.X);
        normal *= Main.GameViewMatrix.TransformationMatrix.M11 * width;
        var previousTop = new BeamVertex(start + normal * 2f, color, new Vector3(0, 1, 1));
        var previousBottom = new BeamVertex(start - normal * 2f, color, new Vector3(0, 0, 1));
        for (int i = 1; i < points.Length; i++)
        {
            float u = i / (float)points.Length;
            Vector2 center = start + direction * (length * i / (points.Length - 1));
            Vector2 offset = normal * MathHelper.Lerp(2f, endWidth, u);
            var top = new BeamVertex(center + offset, color, new Vector3(u, 1, 1));
            var bottom = new BeamVertex(center - offset, color, new Vector3(u, 0, 1));
            int index = (i - 1) * 6;
            releaseVertices[index] = previousTop; releaseVertices[index + 1] = top; releaseVertices[index + 2] = previousBottom;
            releaseVertices[index + 3] = previousBottom; releaseVertices[index + 4] = top; releaseVertices[index + 5] = bottom;
            previousTop = top; previousBottom = bottom;
        }
        device.Textures[0] = texture;
        effect.Parameters["UseRedAlpha"].SetValue(redAlpha);
        effect.CurrentTechnique.Passes[0].Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, releaseVertices, 0, releaseVertices.Length / 3);
    }

    internal readonly record struct Spark(Vector2 Position, Vector2 Velocity, Vector2 Size, int Life);

    private readonly struct BeamVertex(Vector2 position, Vector4 color, Vector3 uv) : IVertexType
    {
        public readonly Vector2 Position = position;
        public readonly Vector4 Color = color;
        public readonly Vector3 UV = uv;
        private static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0));
        public VertexDeclaration VertexDeclaration => Declaration;
    }

    internal static Spark[] ReleaseSparks(Vector2 start, Vector2 direction, float length, float width, int seed)
    {
        if (length <= 0f || width <= 0.05f) return Array.Empty<Spark>();
        int count = Math.Clamp((int)MathF.Ceiling(length / Math.Max(1f, ReleaseSparkSpacing)), 6, 80);
        var result = new Spark[count];
        var random = new Random(seed);
        float Next(float a, float b) => MathHelper.Lerp(a, b, (float)random.NextDouble());
        Vector2 normal = new(-direction.Y, direction.X);
        float[] clusters = { Next(0.05f, 0.95f), Next(0.05f, 0.95f), Next(0.05f, 0.95f) };
        int firstSide = random.Next(2) == 0 ? -1 : 1;
        for (int i = 0; i < count; i++)
        {
            float along = Next(0.01f, 0.99f);
            if (random.NextDouble() < 0.35)
                along = MathHelper.Clamp(clusters[random.Next(clusters.Length)] + Next(-0.07f, 0.07f), 0.01f, 0.99f);
            Vector2 position = start + direction * (length * along);
            // 前两颗只保证两侧都有粒子，位置仍独立；其余随机选择一侧。
            int side = i == 0 ? firstSide : i == 1 ? -firstSide : random.Next(2) == 0 ? -1 : 1;
            float forwardSpeed = Next(ReleaseSparkForwardSpeed.X, ReleaseSparkForwardSpeed.Y);
            float sideSpeed = Next(ReleaseSparkSideSpeed.X, ReleaseSparkSideSpeed.Y);
            Vector2 velocity = direction * forwardSpeed + normal * (side * sideSpeed);
            result[i] = new Spark(position, velocity,
                new Vector2(Next(ReleaseSparkLengthScale.X, ReleaseSparkLengthScale.Y), Next(0.24f, 0.5f)) * Math.Min(1f, width),
                random.Next(28, 47));
        }
        return result;
    }
}

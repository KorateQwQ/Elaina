using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>
/// 炎爆领域的范围圈：沿领域边界燃烧的火环，火舌沿半径向外舔出。仅绘制，不推进状态；预览直接编译本文件。
/// 着色在 FireBurstFlame.fx 的 DomainRing 通道（与火柱相同的三阈值配色），噪声为 KL 的 PerlinX。
/// 展开时火环从中心迅速扩到边界，火舌随后长到全长；领域结束前火势减弱熄灭。
/// </summary>
internal static class FireBurstDomainRingDrawing
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Vertex : IVertexType
    {
        private static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
        public Vector2 Position;
        public Vector4 Color;
        public Vector2 Local;
        public readonly VertexDeclaration VertexDeclaration => Declaration;
    }

    private static Vertex[] vertices = new Vertex[1024];
    private static short[] indices = new short[3072];

    /// <param name="screenCenter">领域中心的屏幕坐标（世界坐标 - Main.screenPosition）。</param>
    /// <param name="radius">领域半径（像素）。</param>
    /// <param name="age">领域已存在帧数。</param>
    /// <param name="remaining">距领域结束的帧数。</param>
    public static void Draw(GraphicsDevice device, Matrix transform, Vector2 screenCenter, float radius, float age, float remaining)
    {
        // ---- 可调参数 ----
        float openTicks = 24f;               // 火环从中心扩到边界的帧数
        float growTicks = 30f;               // 火舌长到全长的帧数
        float fadeTicks = 40f;               // 结束前火势减弱的帧数
        float flameHeight = 80f;             // 火舌基准长度（像素）
        float innerWidth = 9f;               // 根部向圆内的余辉
        float rootLevel = .6f;               // 根部火势
        Vector2 flowScale = new(1f / 800f, 1f / 420f);
        float flowSpeed = .0035f;            // 约 2.8 像素 / 帧
        float outerClip = .06f, midClip = .15f, innerClip = .42f;  // 同火柱
        Vector4 outer = new(2f, .46f, .06f, 1f), mid = new(2f, .82f, .09f, 1f), inner = new(3.6f, 2.23f, .65f, 1f);
        int segments = 360;

        float open = Math.Clamp(age / openTicks, 0f, 1f);
        float ring = radius * (1f - MathF.Pow(1f - open, 3f));
        float fade = MathHelper.SmoothStep(0f, 1f, remaining / fadeTicks);
        float strength = Math.Min(1f, age / 10f) * fade;
        float height = flameHeight * MathHelper.SmoothStep(.35f, 1f, Math.Min(1f, age / growTicks))
            * MathHelper.SmoothStep(.3f, 1f, remaining / fadeTicks);
        if (strength <= 0f || ring < 1f) return;

        // 圆环网格覆盖 边界内余辉 ～ 最长火舌（1.5 倍基准长度）
        float rIn = Math.Max(0f, ring - innerWidth * 3f), rOut = ring + height * 1.5f + 8f;
        int needVertices = (segments + 1) * 2, needIndices = segments * 6;
        if (vertices.Length < needVertices) Array.Resize(ref vertices, needVertices);
        if (indices.Length < needIndices) Array.Resize(ref indices, needIndices);
        int vertexCount = 0, indexCount = 0;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * MathHelper.TwoPi / segments;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            vertices[vertexCount++] = new Vertex { Position = screenCenter + direction * rIn, Color = Vector4.One, Local = direction * rIn };
            vertices[vertexCount++] = new Vertex { Position = screenCenter + direction * rOut, Color = Vector4.One, Local = direction * rOut };
            if (i == segments) break;
            short b = (short)(i * 2);
            indices[indexCount++] = b;
            indices[indexCount++] = (short)(b + 2);
            indices[indexCount++] = (short)(b + 1);
            indices[indexCount++] = (short)(b + 1);
            indices[indexCount++] = (short)(b + 2);
            indices[indexCount++] = (short)(b + 3);
        }

        Texture2D noise = ModContent.Request<Texture2D>("KL/Effects/Tex/PerlinX", AssetRequestMode.ImmediateLoad).Value;
        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/FireBurstFlame", AssetRequestMode.ImmediateLoad).Value;
        EffectParameterCollection p = effect.Parameters;
        p["uTransform"].SetValue(transform);
        p["BandOuter"].SetValue(outer);
        p["BandMid"].SetValue(mid);
        p["BandInner"].SetValue(inner);
        p["ClipOuter"].SetValue(outerClip);
        p["ClipMid"].SetValue(midClip);
        p["ClipInner"].SetValue(innerClip);
        p["RingTime"].SetValue(age);
        p["RingRadius"].SetValue(ring);
        p["FlameHeight"].SetValue(height);
        p["InnerWidth"].SetValue(innerWidth);
        p["FlowScale"].SetValue(flowScale);
        p["FlowSpeed"].SetValue(flowSpeed);
        p["RootLevel"].SetValue(rootLevel);
        p["RingStrength"].SetValue(strength);

        // shader 输出预乘 RGB 与覆盖度，黄层 HDR 由同层 Bloom 晕出。
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        effect.CurrentTechnique.Passes["DomainRing"].Apply();
        device.Textures[0] = noise;
        device.SamplerStates[0] = SamplerState.LinearWrap;
        device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount, indices, 0, indexCount / 3);
    }
}

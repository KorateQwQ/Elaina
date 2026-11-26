using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>沿冰锥轴线投影的双面气流条带；蓝色内面在后，白色外面在前。绘制不推进状态。</summary>
internal static class IceConeWindDrawing
{
    private const int Segments = 96;
    public const int Lifetime = 16;
    public const string TexturePath = "伊蕾娜/Mounts/Assets/BroomDashWind";
    public const string EffectPath = "伊蕾娜/Effects/Content/FireBurstFlame";

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Vertex : IVertexType
    {
        private static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
        public Vector2 Position;
        public Vector4 Color;
        public Vector2 TexCoord;
        public readonly VertexDeclaration VertexDeclaration => Declaration;
    }

    // 固定观察方向的两个半圆；旋转只移动缺口和 UV，不旋转前后面的分界。
    private static readonly Vertex[] Vertices = new Vertex[(Segments + 2) * 4];
    private static readonly short[] Indices = new short[(Segments + 2) * 6];

    public static void Draw(GraphicsDevice device, Effect effect, Texture2D texture, Matrix transform,
        Vector2 origin, float age, bool front, float axisRotation, float startAngle)
    {
        if (age <= 0 || age >= Lifetime) return;
        float progress = age / Lifetime;
        float fade = Math.Min(1f, age / 2f) * MathHelper.SmoothStep(1f, 0f, progress);
        float radius = MathHelper.Lerp(40f, 86f, MathF.Sqrt(progress));
        const float pitch = .2f;
        float vertical = MathF.Sqrt(1f - pitch * pitch);
        float thickness = MathHelper.SmoothStep(18f, 0f, progress);
        const float arc = MathHelper.TwoPi - .85f;
        float rotation = (startAngle - age * .17f) % MathHelper.TwoPi;
        if (rotation < 0f) rotation += MathHelper.TwoPi;
        Vector2 axis = new(MathF.Cos(axisRotation), MathF.Sin(axisRotation));
        Vector2 side = new(-axis.Y, axis.X);
        // 前后由深度符号决定，发射角度与缺口旋转不会交换两面。
        float halfStart = front ? 0f : MathF.PI;
        // AlphaBlend 要求颜色预乘淡出覆盖度；只减 Alpha 会让蓝面叠加泛白。
        Vector4 faceColor = front
            ? new Vector4(new Vector3(.95f), 1f)
            : new Vector4(new Vector3(.62f, .82f, 1f) * fade, fade);

        int vertexCount = 0, count = 0;
        for (int i = 0; i < Segments / 2; i++)
        {
            float from = halfStart + i * MathHelper.TwoPi / Segments;
            float to = halfStart + (i + 1) * MathHelper.TwoPi / Segments;
            // 与旋转的有纹理条带求交。跨 2π 的部分拆开，但几何始终留在指定的上下半圆。
            for (int wrap = -1; wrap <= 0; wrap++)
            {
                float arcStart = rotation + wrap * MathHelper.TwoPi;
                float clippedFrom = Math.Max(from, arcStart);
                float clippedTo = Math.Min(to, arcStart + arc);
                if (clippedTo - clippedFrom > 1e-6f) AddSection(clippedFrom, clippedTo, arcStart);
            }
        }

        void AddSection(float from, float to, float arcStart)
        {
            int start = vertexCount;
            AddEdge(from, (from - arcStart) / arc);
            AddEdge(to, (to - arcStart) / arc);
            Indices[count++] = (short)start;
            Indices[count++] = (short)(start + 2);
            Indices[count++] = (short)(start + 1);
            Indices[count++] = (short)(start + 1);
            Indices[count++] = (short)(start + 2);
            Indices[count++] = (short)(start + 3);
        }

        void AddEdge(float angle, float phase)
        {
            float tip = MathHelper.SmoothStep(0f, 1f, Math.Clamp(Math.Min(phase, 1f - phase) / .14f, 0f, 1f));
            for (int edge = 0; edge < 2; edge++)
            {
                Vector3 point = new(radius * MathF.Cos(angle), (.5f - edge) * thickness * tip,
                    radius * MathF.Sin(angle));
                Vertices[vertexCount++] = new Vertex
                {
                    Position = origin + side * point.X + axis * (point.Z * pitch + point.Y * vertical),
                    Color = faceColor,
                    TexCoord = new Vector2(phase * 2f, edge)
                };
            }
        }
        if (count == 0) return;
        effect.Parameters["uTransform"].SetValue(transform);
        // 非零权重保证 D3D11 首次上传像素常量，避免全零参数留下未初始化 GPU 数据。
        effect.Parameters["WindMaskWeights"].SetValue(Vector2.UnitX);
        // shader 再将 RGB 和 Alpha 同乘纹理遮罩；两面均不经过 Bloom。
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        effect.CurrentTechnique.Passes["WindRing"].Apply();
        device.Textures[0] = texture;
        device.SamplerStates[0] = SamplerState.LinearWrap;
        device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, vertexCount,
            Indices, 0, count / 3);
    }
}

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>绕火柱弯曲的竖直条带：朝火柱的内表面橙红，背向火柱的外表面黑色。仅绘制，不推进状态。</summary>
internal static class FireBurstWindDrawing
{
    private const int Segments = 96;
    private const float Lifetime = 24f;

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
        Vector2 origin, float age, bool front, Vector2 uvScale, Vector2 uvOffset, float maskFromRed,
        Vector4 innerColor, Vector4 outerColor, float radiusStart, float radiusEnd, float riseSpeed, float startAngle)
    {
        if (age <= 0 || age >= Lifetime) return;
        float progress = age / Lifetime;
        float fade = Math.Min(1f, age / 2f) * MathF.Pow(1f - progress, .8f);
        float radius = MathHelper.Lerp(radiusStart, radiusEnd, MathF.Sqrt(progress));
        float pitch = .27f; // 俯视投影：地面纵深压缩，条带宽度沿世界竖直方向展开。
        float vertical = MathF.Sqrt(1f - pitch * pitch);
        // 两圈各按自己的年龄逐渐收细，末端平滑缩到零。
        float thickness = MathHelper.SmoothStep(28f, 0f, progress);
        float opening = .9f;
        float arc = MathHelper.TwoPi - opening;
        float rotation = (startAngle - age * .115f) % MathHelper.TwoPi;
        if (rotation < 0f) rotation += MathHelper.TwoPi;
        Vector2 center = origin - new Vector2(0, 18f + age * riseSpeed);
        // z>0 投影到下半圈，为背光黑色外表面；z<0 投影到上半圈，为受火柱照亮的内表面。
        float halfStart = front ? 0f : MathF.PI;
        Vector4 faceColor = front ? outerColor : innerColor;
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
                    Position = center + new Vector2(point.X, point.Z * pitch - point.Y * vertical),
                    Color = new Vector4(new Vector3(faceColor.X,faceColor.Y,faceColor.Z),1),
                    TexCoord = new Vector2(phase * uvScale.X, (edge - .5f) * uvScale.Y + .5f) + uvOffset
                };
            }
        }
        if (count == 0) return;
        effect.Parameters["uTransform"].SetValue(transform);
        effect.Parameters["MaskFromRed"].SetValue(maskFromRed);
        // shader 输出已预乘的 RGB 和覆盖度：黑色外表面参与遮挡，内表面的 HDR 颜色参与 Bloom。
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

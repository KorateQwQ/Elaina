using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

/// <summary>保留消亡时的轨迹，PerlinX 决定局部收缩速度，先归零的位置自然断开；游戏与 FNA 预览共用。</summary>
public sealed class MagicMissileTrailVisuals
{
    public const int FadeTicks = 22;
    private TrailVertex[] vertices = Array.Empty<TrailVertex>();
    private readonly Vector2 noiseOffset;
    private readonly int headFrame;
    private readonly float headSpin;
    public bool Expiring;
    public float TextureOffset;
    /// <summary>消亡时的入射方向，头部爆点据此对齐；路径缩短后末段方向不再可靠。</summary>
    public float HeadRotation;
    public Vector2 NoiseOffset => noiseOffset;

    public MagicMissileTrailVisuals(int seed)
    {
        var random = new Random(seed);
        noiseOffset = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
        headFrame = random.Next(4);
        headSpin = (float)random.NextDouble() * MathHelper.TwoPi;
    }

    /// <summary>
    /// 原版撞墙时把速度改为沿墙滑动，消亡位置会沿墙偏出一截，末段拐成 L 形。
    /// 按被挡住的轴在本次更新中走过的比例，沿入射方向找回真正的接触点。start 为本次移动前的中心。
    /// </summary>
    public static Vector2 WallContact(Vector2 start, Vector2 incoming, Vector2 collided)
    {
        float t = 1f;
        if (MathF.Abs(incoming.X) > .001f && MathF.Abs(incoming.X - collided.X) > .001f)
            t = Math.Min(t, collided.X / incoming.X);
        if (MathF.Abs(incoming.Y) > .001f && MathF.Abs(incoming.Y - collided.Y) > .001f)
            t = Math.Min(t, collided.Y / incoming.Y);
        return start + incoming * Math.Clamp(t, 0f, 1f);
    }

    /// <summary>绘制采样晚于碰撞更新；把最后一次绘制的路径接到真正消亡位置，并保持长度上限。</summary>
    public static Vector2[] FinishPath(Vector2[] previous, Vector2 end, float maxLength)
    {
        var points = new List<Vector2> { end };
        if (previous == null) return points.ToArray();
        float remaining = maxLength;
        foreach (Vector2 point in previous)
        {
            Vector2 last = points[^1];
            float length = Vector2.Distance(last, point);
            if (length < 0.01f) continue;
            if (length >= remaining)
            {
                points.Add(Vector2.Lerp(last, point, remaining / length));
                break;
            }
            points.Add(point);
            remaining -= length;
        }
        return points.ToArray();
    }

    /// <summary>
    /// 消亡结束时从尾端缩掉的长度比例，按 age² 集中在后段；0 为完全不缩短、纯靠截面消融。
    /// TrailManager 的线性缩短会在半程砍掉一半长度，提交时关闭，改由 BuildGeometry 只截掉尾端几何。
    /// </summary>
    public const float DefaultLengthShrink = .45f;
    public float LengthShrink = DefaultLengthShrink;

    // 独立几何入口供预览检查：几何中心线和原始宽度保持不变，局部收缩交给 PerlinX Shader。
    // 缩短时只停止生成尾端几何，U 仍按完整路径计算：先截路径再重算 U 会让噪声图案在每次越过采样点时整体跳动。
    internal int BuildGeometry(Vector2[] points, float remaining, float viewWidthScale, MagicMissileTrailNoise noise)
    {
        if (points.Length < 2 || remaining <= 0) return 0;
        int subdivisions = 0;
        float length = 0;
        for (int i = 1; i < points.Length; i++)
        {
            float segmentLength = Vector2.Distance(points[i - 1], points[i]);
            length += segmentLength;
            subdivisions += Expiring ? Math.Max(1, (int)MathF.Ceiling(segmentLength / 1.5f)) : 1;
        }
        if (length < .01f) return 0;
        float age = Expiring ? 1f - Math.Clamp(remaining, 0f, 1f) : 0f;
        float visibleLength = length * (1f - LengthShrink * age * age);
        int count = 3 + subdivisions * 6;
        if (vertices.Length < count) vertices = new TrailVertex[count];
        float distance = 0;
        int output = 3;
        for (int i = 0; i < points.Length - 1; i++)
        {
            float segmentLength = Vector2.Distance(points[i], points[i + 1]);
            int steps = Expiring ? Math.Max(1, (int)MathF.Ceiling(segmentLength / 1.5f)) : 1;
            // 可见长度在本段内结束时，保留原有细分点，只在截断处补一个顶点，保留部分完全不动
            float end = segmentLength > 0f && distance + segmentLength > visibleLength
                ? Math.Clamp((visibleLength - distance) / segmentLength, 0f, 1f) : 1f;
            bool clipped = false;
            // 插值旧网格的两侧边，而非重算法线；增加采样不会改变未收缩时的轮廓。
            Vector2 offsetA = Offset(points, i, viewWidthScale);
            Vector2 offsetB = Offset(points, i + 1, viewWidthScale);
            for (int j = 0; j <= steps; j++)
            {
                float t = j / (float)steps;
                if (t >= end)
                {
                    t = end;
                    clipped = true;
                }
                Vector2 center = Vector2.Lerp(points[i], points[i + 1], t);
                Vector2 offset = Vector2.Lerp(offsetA, offsetB, t);
                float u = (i + t) / points.Length;
                Vector4 color = Vector4.Lerp(new Vector4(1.5f, .75f, 1.35f, 1), new Vector4(1, .4f, .9f, 0), u);
                float alpha = MathHelper.Lerp(1, 3, u);
                var top = new TrailVertex(center + offset, color, new Vector3(u, 1, alpha));
                var bottom = new TrailVertex(center - offset, color, new Vector3(u, 0, alpha));
                if (j == 0)
                {
                    previousTop = top;
                    previousBottom = bottom;
                    if (i == 0)
                    {
                        vertices[0] = top;
                        vertices[1] = bottom;
                        vertices[2] = new TrailVertex(center, Vector4.One, new Vector3(0, .5f, 1));
                    }
                    continue;
                }
                vertices[output++] = previousTop;
                vertices[output++] = top;
                vertices[output++] = previousBottom;
                vertices[output++] = previousBottom;
                vertices[output++] = top;
                vertices[output++] = bottom;
                previousTop = top;
                previousBottom = bottom;
                if (clipped) break;
            }
            if (end < 1f) break;
            distance += segmentLength;
        }
        return output;
    }

    private TrailVertex previousTop, previousBottom;
    internal ReadOnlySpan<TrailVertex> Geometry(int count) => vertices.AsSpan(0, count);

    private static Vector2 Offset(Vector2[] points, int i, float viewWidthScale)
    {
        Vector2 delta = i < points.Length - 1 ? points[i] - points[i + 1] : points[i - 1] - points[i];
        float length = delta.Length();
        Vector2 normal = length > .001f ? new Vector2(-delta.Y, delta.X) / length : Vector2.UnitY;
        return normal * MathHelper.Lerp(6, 2, i / (float)points.Length) * viewWidthScale;
    }

    /// <summary>调用方结束 SpriteBatch 并恢复批次。固定贴图只读，顶点宽度完成全部消亡效果。</summary>
    public void Draw(GraphicsDevice device, Effect effect, Texture2D texture, MagicMissileTrailNoise noise, Vector2[] points,
        float remaining, Matrix transform, BlendState blend, float viewWidthScale = 1f)
    {
        int count = BuildGeometry(points, remaining, viewWidthScale, noise);
        if (count == 0) return;
        device.BlendState = blend;
        device.DepthStencilState = DepthStencilState.Default;
        device.RasterizerState = RasterizerState.CullNone;
        device.SamplerStates[0] = SamplerState.LinearWrap;
        device.Textures[0] = texture;
        var viewport = device.Viewport;
        effect.Parameters["uTransform"].SetValue(transform * Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1));
        effect.Parameters["uTimex"].SetValue(TextureOffset);
        effect.Parameters["NoiseOffset"].SetValue(noiseOffset);
        effect.Parameters["NoiseScale"].SetValue(new Vector2(0.46f, 0.55f));
        float age = Expiring ? 1f - Math.Clamp(remaining, 0, 1) : 0f;
        // 收缩最快的部位约在 40% 寿命归零，最慢的约在 70%，此后路径已全部断开消失
        effect.Parameters["TrailAge"].SetValue(Expiring ? Math.Min(1f, .12f + age * 1.25f) : 0f);
        effect.Parameters["MaxShrinkRate"].SetValue(1.65f);
        effect.Parameters["ShrinkExponent"].SetValue(1.0f);
        effect.Parameters["UseShrink"].SetValue(Expiring);
        device.Textures[1] = noise.Texture;
        effect.CurrentTechnique.Passes[0].Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, count / 3);
    }

    /// <summary>
    /// 飞弹消失后在拖尾头部画一帧 HitSword 爆点，盖住拖尾的截断端口；随拖尾消亡由外向内被噪声消融，前沿提亮。
    /// 与拖尾相同：调用方结束 SpriteBatch，transform 与 head 使用拖尾的坐标系。stamp 为 2×2 图集，每发固定一帧和角度。
    /// 不经 SpriteBatch：只带像素着色器的通道会沿用此前绑定的顶点着色器，游戏中紧接拖尾绘制时位置不可靠，曾导致头部完全不显示。
    /// </summary>
    public void DrawHead(GraphicsDevice device, Effect effect, Texture2D stamp, Texture2D noise, Vector2 head,
        float remaining, Matrix transform, BlendState blend)
    {
        float life = Math.Clamp(remaining, 0f, 1f);
        // 整个拖尾消融期间匀速消失：一开始画大，盖过命中爆点，之后尺寸、亮度、消融同时推进
        float progress = Math.Clamp((1f - life) / HeadDissolveLife, 0f, 1f);
        if (progress >= 1f) return;
        float size = MathHelper.Lerp(HeadStartSize, 1f, progress);
        float hdr = MathHelper.Lerp(HeadStartHdr, HeadEndHdr, progress);
        // 尺寸与亮度的下降已带走一部分可见量，其余由消融补足，使总可见量 ∝ 1 − progress
        float visible = Math.Min(1f, (1f - progress) * HeadStartSize * HeadStartSize * HeadStartHdr / (size * size * hdr));
        float dissolve = DissolveForVisible(visible);

        int frameSize = stamp.Width / 2;
        var source = new Rectangle(headFrame % 2 * frameSize, headFrame / 2 * frameSize, frameSize, frameSize);
        device.BlendState = blend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        device.Textures[0] = stamp;
        device.SamplerStates[0] = SamplerState.LinearClamp;
        device.Textures[1] = noise;
        device.SamplerStates[1] = SamplerState.LinearWrap;
        var frame = new Vector4(source.X / (float)stamp.Width, source.Y / (float)stamp.Height,
            source.Width / (float)stamp.Width, source.Height / (float)stamp.Height);
        var viewport = device.Viewport;
        effect.Parameters["uTransform"].SetValue(transform * Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1));
        effect.Parameters["HeadFrame"].SetValue(frame);
        effect.Parameters["HeadDissolve"].SetValue(dissolve);
        effect.Parameters["HeadEdge"].SetValue(0.08f);
        effect.Parameters["HeadNoiseScale"].SetValue(new Vector2(HeadNoiseScale));
        effect.Parameters["NoiseOffset"].SetValue(noiseOffset);
        effect.Parameters["HeadNoiseInvert"].SetValue(1f);
        effect.Parameters["HeadNoiseWeight"].SetValue(.5f);
        // 残片随消融降低 HDR，避免 Bloom 把碎片晕成一团白
        effect.Parameters["HeadColor"].SetValue(new Vector3(1f, .47f, .94f) * hdr);
        effect.Parameters["HeadEdgeColor"].SetValue(new Vector3(1f, .62f, .96f) * (hdr + .3f));
        effect.Techniques[0].Passes["Head"].Apply();

        // 帧内爆点可见范围约 140 px。顶点 alpha 为覆盖度，少量压暗背景以免白天糊成一片
        float half = frameSize / 2f * 0.34f * size;
        float rotation = HeadRotation + headSpin;
        Vector2 axisX = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * half;
        Vector2 axisY = new Vector2(-axisX.Y, axisX.X);
        var color = new Vector4(1, 1, 1, 60 / 255f);
        TrailVertex Corner(float x, float y) => new(head + axisX * x + axisY * y, color,
            new Vector3(frame.X + frame.Z * (x + 1) / 2, frame.Y + frame.W * (y + 1) / 2, 1));
        TrailVertex topLeft = Corner(-1, -1), topRight = Corner(1, -1), bottomLeft = Corner(-1, 1), bottomRight = Corner(1, 1);
        headVertices[0] = topLeft; headVertices[1] = topRight; headVertices[2] = bottomLeft;
        headVertices[3] = bottomLeft; headVertices[4] = topRight; headVertices[5] = bottomRight;
        device.DrawUserPrimitives(PrimitiveType.TriangleList, headVertices, 0, 2);
    }

    private readonly TrailVertex[] headVertices = new TrailVertex[6];

    // 头部在拖尾寿命的前 72% 内消尽，与截面收缩最慢的部位同时归零
    private const float HeadDissolveLife = .72f;
    private const float HeadStartSize = 1.4f, HeadStartHdr = 1.9f, HeadEndHdr = 1.5f;

    // 消融噪声 KL Noise/5（胞状），缩放 1.8、反相：暗块最后消失，爆点被切成分离的圆润碎块
    private const float HeadNoiseScale = 1.8f;

    // Tools/MagicMissileHitPreview/head_curve.py 复刻 Head 通道测得：消融值与可见亮度的关系极不均匀（低值只蚀去外圈淡晕）。
    // 下表为剩余可见量 1.0, 0.9 … 0 对应的消融值，按它反查才能匀速消失；改噪声、缩放、反相或 Shader 消融场后需重测。
    private static readonly float[] DissolveByVisibleLoss = [.21f, .413f, .488f, .56f, .614f, .658f, .698f, .735f, .774f, .817f, .917f];

    internal static float DissolveForVisible(float visible)
    {
        float[] table = DissolveByVisibleLoss;
        float loss = Math.Clamp(1f - visible, 0f, 1f) * (table.Length - 1);
        int i = Math.Min((int)loss, table.Length - 2);
        return MathHelper.Lerp(table[i], table[i + 1], loss - i);
    }

    internal readonly struct TrailVertex(Vector2 position, Vector4 color, Vector3 uv) : IVertexType
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
}

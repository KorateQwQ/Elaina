using System;
using ReLogic.Content;
using Terraria.ModLoader;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>
/// 回城法阵外观。只绘制，不负责传送、配对或计时；预览直接链接本文件。
/// 全部笔画拼成一批顶点带，以 KL VertexDraw.fx 一次绘制；顶点颜色可大于 1，交给 KL bloom 提取过曝部分。
/// </summary>
internal static class RecallPortalDrawing
{
    public static bool Contains(Vector2 offset, float radius) =>
        offset.X * offset.X / (radius * radius) + offset.Y * offset.Y / (radius * radius * .09f) <= 1;

    // ===== 亮度调参 =====
    // KL bloom 只提取感知亮度 dot(rgb, (0.3, 0.59, 0.11)) 大于 1 的像素，光晕形状就是这些线条本身。
    // 超过阈值的像素按完整颜色参与模糊，静止线条一旦全部越过阈值就会糊成一片雾，
    // 所以常态线条留在阈值以下，整体微光只来自花体字流光波峰与悬停外环。
    /// <summary>所有线条的 HDR 倍数，常态下保持在阈值以下。</summary>
    const float BaseGlow = 1.5f;
    /// <summary>花体字流光波峰的过曝程度（0..1，同笔尖 heat），形成沿文字流动的微弱光点。</summary>
    const float GlintHeat = .15f;
    /// <summary>笔尖处额外叠加的倍数，沿笔画向后衰减回 BaseGlow，形成拖尾式的过曝渐变。</summary>
    const float TipGlow = 5f;
    /// <summary>笔尖过曝的衰减长度（屏幕像素）：距笔尖每隔这么远亮度降为 1/e。</summary>
    const float TipLength = 14f;
    /// <summary>笔画到达终点后，笔尖过曝淡出所用的帧数。</summary>
    const float TipFade = 18f;
    /// <summary>线条两侧各留出的柔边宽度（像素），由线条贴图从实心渐变到透明。</summary>
    const float Feather = .8f;

    // ===== 完成爆发 =====
    // 画完瞬间整个法阵向上拖出光幕与光束，像把纹路沿竖直方向做了一次径向模糊，随后快速淡出；
    // 每次有玩家经法阵往返时两端再放一次，作为传送特效。
    // 光幕与光束为纯加色（Alpha 为 0），只增亮不遮挡，过曝部分交给 bloom。
    /// <summary>爆发持续帧数，从 burstStart 开始计。</summary>
    const float BurstTicks = 50f;
    /// <summary>光束最高可达的高度（以法阵半径为单位）；光幕约为其一半。</summary>
    const float BurstHeight = 1.1f;
    /// <summary>每条纹路向上拖出的光幕底部亮度倍数。</summary>
    const float SheetGlow = .45f;
    /// <summary>细光束底部亮度倍数，明显越过 bloom 阈值。</summary>
    const float RayGlow = 6f;
    /// <summary>纹路上每个采样点长出细光束的概率。</summary>
    const float RayDensity = .1f;

    // 与 VertexDraw.fx 的输入一致：位置、Vector4 颜色（可超过 1）、纹理坐标（z 为透明度）。
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Vertex : IVertexType
    {
        static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0));
        public Vector2 Position;
        public Vector4 Color;
        public Vector3 TexCoord;
        public Vertex(Vector2 position, Vector4 color, Vector3 texCoord) { Position = position; Color = color; TexCoord = texCoord; }
        readonly VertexDeclaration IVertexType.VertexDeclaration => Declaration;
    }

    static Vertex[] vertices = new Vertex[8192];
    static int[] indices = new int[16384];
    static int vertexCount, indexCount;

    // 正在收集的一条笔画；结束时统一计算法线与笔尖亮度再写入顶点。
    static Vector2[] stripPoints = new Vector2[512];
    static Color[] stripTints = new Color[512];
    static float[] stripHeat = new float[512];
    static int stripCount;
    static float stripWidth;

    // 爆发状态：burstHeight 为 0 时不画；burstSeed 按笔画递增，让每帧同一笔画的光束位置一致。
    static float burstHeight, burstIntensity;
    /// <summary>当前绘制部分的光幕、光束倍率：花体字笔迹密集重叠，只保留少量光束，避免糊成一团。</summary>
    static float burstSheet, burstRays;
    static int burstSeed;

    /// <summary>线条横截面：中间实心、两侧柔边，预乘白色。沿 v 方向（0..1）采样。</summary>
    static Texture2D lineTexture;

    static void EnsureTexture() => lineTexture ??=
        ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/Skills/Recall/Assets/PortalLine", AssetRequestMode.ImmediateLoad).Value;

    // 借用资源系统的预制线条，只清理引用。
    public static void Unload() => lineTexture = null;

    static void BeginStroke(float width)
    {
        stripCount = 0;
        stripWidth = width + Feather * 2;
    }

    /// <param name="heat">笔尖过曝程度 0..1；-1 表示由 EndStroke 按到笔尖的距离计算。</param>
    static void AddPoint(Vector2 position, Color tint, float heat = 0)
    {
        if (stripCount > 0 && Vector2.DistanceSquared(stripPoints[stripCount - 1], position) < 1e-4f) return;
        if (stripCount == stripPoints.Length)
        {
            Array.Resize(ref stripPoints, stripCount * 2);
            Array.Resize(ref stripTints, stripCount * 2);
            Array.Resize(ref stripHeat, stripCount * 2);
        }
        stripPoints[stripCount] = position;
        stripTints[stripCount] = tint;
        stripHeat[stripCount] = heat;
        stripCount++;
    }

    static void Reserve(int vertexNeed, int indexNeed)
    {
        if (vertexCount + vertexNeed > vertices.Length) Array.Resize(ref vertices, Math.Max(vertices.Length * 2, vertexCount + vertexNeed));
        if (indexCount + indexNeed > indices.Length) Array.Resize(ref indices, Math.Max(indices.Length * 2, indexCount + indexNeed));
    }

    /// <summary>
    /// 结束当前笔画并写入顶点；tip 大于 0 时最后一点为笔尖，过曝程度为 tip，向后按距离衰减。
    /// 爆发期间同时为这条笔画写入向上的光幕与光束。
    /// </summary>
    static void EndStroke(float tip = 0)
    {
        int n = stripCount;
        stripCount = 0;
        if (n < 2) return;
        if (tip > 0)
        {
            float distance = 0;
            stripHeat[n - 1] = Math.Max(stripHeat[n - 1], tip);
            for (int i = n - 2; i >= 0; i--)
            {
                distance += Vector2.Distance(stripPoints[i], stripPoints[i + 1]);
                stripHeat[i] = Math.Max(stripHeat[i], tip * MathF.Exp(-distance / TipLength));
            }
        }
        Reserve(n * 2, (n - 1) * 6);
        int start = vertexCount;
        float half = stripWidth / 2;
        for (int i = 0; i < n; i++)
        {
            Vector2 direction = stripPoints[Math.Min(i + 1, n - 1)] - stripPoints[Math.Max(i - 1, 0)];
            if (direction.LengthSquared() < 1e-8f) direction = Vector2.UnitX;
            direction.Normalize();
            Vector2 normal = new Vector2(-direction.Y, direction.X) * half;
            Vector4 color = Ink(stripTints[i], stripHeat[i]);
            vertices[vertexCount++] = new Vertex(stripPoints[i] + normal, color, new Vector3(0, 0, 1));
            vertices[vertexCount++] = new Vertex(stripPoints[i] - normal, color, new Vector3(0, 1, 1));
        }
        for (int i = 0; i < n - 1; i++)
        {
            int a = start + i * 2;
            indices[indexCount++] = a;
            indices[indexCount++] = a + 2;
            indices[indexCount++] = a + 1;
            indices[indexCount++] = a + 1;
            indices[indexCount++] = a + 2;
            indices[indexCount++] = a + 3;
        }
        if (burstHeight > 0) AddBurst(n);
    }

    /// <summary>
    /// 把刚写入的笔画向上拖出：每个采样点竖起一条高度不一的渐隐光幕（相邻点连成锯齿状的光芒），
    /// 少数点再长出更高更亮的细光束。颜色从底部本色过曝渐变到顶部透明。
    /// </summary>
    static void AddBurst(int n)
    {
        int seed = burstSeed++;
        if (burstSheet > 0) AddSheet(n, seed);
        AddRays(n, seed);
    }

    static void AddSheet(int n, int seed)
    {
        Reserve(n * 3, (n - 1) * 12);
        int start = vertexCount;
        for (int i = 0; i < n; i++)
        {
            Vector2 p = stripPoints[i];
            float h = burstHeight * .5f * (.35f + .65f * Hash(seed, i));
            Vector4 glow = Glow(stripTints[i], SheetGlow * burstSheet * burstIntensity, .2f);
            // 中段放在 30% 高度、亮度降到 35%，让光幕底部集中、上方拖得细长。
            vertices[vertexCount++] = new Vertex(p, glow, new Vector3(0, .5f, 1));
            vertices[vertexCount++] = new Vertex(p - new Vector2(0, h * .3f), glow * .35f, new Vector3(0, .5f, 1));
            vertices[vertexCount++] = new Vertex(p - new Vector2(0, h), Vector4.Zero, new Vector3(0, .5f, 1));
        }
        for (int i = 0; i < n - 1; i++)
        for (int row = 0; row < 2; row++)
        {
            int a = start + i * 3 + row, b = a + 3;
            indices[indexCount++] = a;
            indices[indexCount++] = b;
            indices[indexCount++] = a + 1;
            indices[indexCount++] = a + 1;
            indices[indexCount++] = b;
            indices[indexCount++] = b + 1;
        }
    }

    static void AddRays(int n, int seed)
    {
        for (int i = 0; i < n; i++)
        {
            if (Hash(seed, i + 4096) >= RayDensity * burstRays) continue;
            Vector2 p = stripPoints[i];
            float h = burstHeight * (.5f + .5f * Hash(seed, i + 8192));
            Vector4 glow = Glow(stripTints[i], RayGlow * burstIntensity, .5f);
            Vector2 side = new(1.2f, 0);
            Reserve(6, 12);
            int a = vertexCount;
            vertices[vertexCount++] = new Vertex(p - side, glow, new Vector3(0, 0, 1));
            vertices[vertexCount++] = new Vertex(p + side, glow, new Vector3(0, 1, 1));
            vertices[vertexCount++] = new Vertex(p - side - new Vector2(0, h * .25f), glow * .3f, new Vector3(0, 0, 1));
            vertices[vertexCount++] = new Vertex(p + side - new Vector2(0, h * .25f), glow * .3f, new Vector3(0, 1, 1));
            vertices[vertexCount++] = new Vertex(p - side - new Vector2(0, h), Vector4.Zero, new Vector3(0, 0, 1));
            vertices[vertexCount++] = new Vertex(p + side - new Vector2(0, h), Vector4.Zero, new Vector3(0, 1, 1));
            for (int row = 0; row < 2; row++)
            {
                int b = a + row * 2;
                indices[indexCount++] = b;
                indices[indexCount++] = b + 2;
                indices[indexCount++] = b + 1;
                indices[indexCount++] = b + 1;
                indices[indexCount++] = b + 2;
                indices[indexCount++] = b + 3;
            }
        }
    }

    /// <summary>加色光：预乘颜色向白色偏移 white 后乘 k，Alpha 为 0，在 AlphaBlend 下只增亮不遮挡。</summary>
    static Vector4 Glow(Color tint, float k, float white)
    {
        Vector4 c = tint.ToVector4();
        Vector3 rgb = Vector3.Lerp(new Vector3(c.X, c.Y, c.Z), new Vector3(c.W), white) * k;
        return new Vector4(rgb, 0);
    }

    /// <summary>确定性的 0..1 伪随机数，同一笔画同一点每帧结果相同。</summary>
    static float Hash(int a, int b)
    {
        uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return (h & 0xFFFFFF) / 16777216f;
    }

    /// <summary>预乘颜色转 HDR：整体乘 BaseGlow；笔尖偏白并额外增亮。Alpha 保持覆盖度不变。</summary>
    static Vector4 Ink(Color tint, float heat)
    {
        Vector4 c = tint.ToVector4();
        Vector3 rgb = Vector3.Lerp(new Vector3(c.X, c.Y, c.Z), new Vector3(c.W), heat * .6f) * (BaseGlow + heat * TipGlow);
        return new Vector4(rgb, c.W);
    }

    /// <summary>
    /// 绘制回城魔法阵；本方法只负责外观，不负责传送、配对或推进计时。
    /// </summary>
    /// <param name="device">图形设备；调用前应结束或刷新当前 SpriteBatch，本方法直接设置设备状态并绘制顶点。</param>
    /// <param name="effect">KL 的 VertexDraw.fx（KL/Effects/Content/VertexDraw）。</param>
    /// <param name="transform">屏幕坐标到裁剪空间的矩阵：游戏缩放矩阵 × 屏幕正交投影。</param>
    /// <param name="center">
    /// 法阵的屏幕绘制中心，已由 Projectile.Center - Main.screenPosition 得到，
    /// 不要再次减去摄像机位置；游戏缩放由 transform 处理。
    /// </param>
    /// <param name="radius">
    /// 绘制半径，当前传入 160 像素。椭圆横向半径为 radius，纵向半径为 radius * 0.3f。
    /// 仅改变绘制大小不会改变传送判定；更改外形时应同步调整 Contains 的鼠标命中区域。
    /// </param>
    /// <param name="age">
    /// 总计时进度，单位为游戏更新帧（每秒 60 帧），不是实际绘制次数。
    /// 起点与出口同时生成、年龄一致，两端一起从 0 开始描绘。
    /// </param>
    /// <param name="delay">首次群体传送的蓄力时间，当前为 90 帧（1.5 秒）；age 小于 delay 时正在蓄力。</param>
    /// <param name="lifetime">
    /// 激活后的残留时间，当前为 1800 帧（30 秒），不包含蓄力；age 达到 delay + lifetime 时到期。
    /// </param>
    /// <param name="destination">是否为出生地出口：false 为施法地点，true 为出生地，可用于区分两端特效。</param>
    /// <param name="hovered">鼠标是否处于法阵的椭圆区域，可用于高亮；不代表距离、队伍等交互条件已满足。</param>
    /// <param name="burstStart">最近一次向上爆发开始时的 age：画完时为 delay，之后每次有人经法阵传送时更新。</param>
    public static void Draw(GraphicsDevice device, Effect effect, Matrix transform, Vector2 center, float radius,
        float age, int delay, int lifetime, bool destination, bool hovered, float burstStart)
    {
        // 描绘进度即蓄力进度：两端同时生成，一起从零画出纹路。
        float build = Math.Max(0, age) / delay;
        float opacity = Math.Min(Math.Clamp(age / 15, 0, 1), Math.Clamp((delay + lifetime - age) / 30, 0, 1));
        if (opacity <= 0) return;
        Color color = (destination ? new Color(123, 215, 240) : new Color(205, 160, 246)) * opacity;
        Vector2 Point(float angle, float r) => center + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r * .3f);
        // 笔画在 finishedAt 帧到达终点后，笔尖过曝在 TipFade 帧内平滑淡出。
        float TipAfterglow(float finishedAt)
        {
            float f = Math.Clamp(1 - (age - finishedAt) / TipFade, 0, 1);
            return f * f;
        }

        vertexCount = indexCount = 0;
        // 爆发：光束迅速升高并继续缓慢上升，亮度在头 3 帧冲到最高后衰减。
        float burst = (age - burstStart) / BurstTicks;
        burstSeed = 0;
        burstHeight = burstIntensity = 0;
        if (burst >= 0 && burst < 1)
        {
            burstHeight = radius * BurstHeight * (1 - MathF.Pow(1 - Math.Min(1, burst * 1.6f), 3));
            burstIntensity = Math.Min(1, (age - burstStart + 1) / 3) * (1 - burst) * (1 - burst) * opacity;
        }
        burstSheet = burstRays = 1;
        DrawPattern();
        burstSheet = 0;
        burstRays = .25f;
        DrawScript(Math.Min(build, 1));
        if (indexCount == 0) return;

        EnsureTexture();
        effect.Parameters["uTransform"].SetValue(transform);
        effect.Parameters["ImageScale"].SetValue(Vector2.One);
        effect.Parameters["useTime"].SetValue(false);
        effect.Parameters["shouldClip"].SetValue(false);
        effect.Parameters["useRforAlpha"].SetValue(false);
        // 顶点颜色为预乘值，使用 AlphaBlend；KL bloom 捕获期间该状态保持不变。
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        device.SamplerStates[0] = SamplerState.LinearClamp;
        device.Textures[0] = lineTexture;
        effect.CurrentTechnique.Passes[0].Apply();
        device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount, indices, 0, indexCount / 3);

        // 内部纹路：每条笔画在自己的时间窗口内逐段画出，正在描绘的笔画头部过曝。
        void DrawPattern()
        {
            ReadOnlySpan<Vector2> points = RecallPortalPattern.Points;
            Span<Vector2> spin = stackalloc Vector2[3];
            for (int layer = 0; layer < 3; layer++)
            {
                float angle = layer switch { 1 => age * .012f, 2 => -age * .02f, _ => 0 };
                spin[layer] = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            }
            Vector2 Project(Vector2 p, Vector2 r) =>
                center + new Vector2((p.X * r.X - p.Y * r.Y) * radius, (p.X * r.Y + p.Y * r.X) * radius * .3f);
            foreach (RecallPortalPattern.Stroke stroke in RecallPortalPattern.Strokes)
            {
                float t = Math.Clamp((build - stroke.Begin) / (stroke.End - stroke.Begin), 0, 1);
                if (t <= 0) continue;
                Vector2 r = spin[stroke.Layer];
                Color tint = color * (stroke.Rim && hovered ? 1 : stroke.Alpha);
                BeginStroke(stroke.Rim && hovered ? stroke.Width + 1 : stroke.Width);
                float reach = t * (stroke.Count - 1);
                int full = (int)reach;
                for (int i = 0; i <= full; i++) AddPoint(Project(points[stroke.Start + i], r), tint);
                bool drawing = full < stroke.Count - 1;
                if (drawing)
                    AddPoint(Vector2.Lerp(Project(points[stroke.Start + full], r), Project(points[stroke.Start + full + 1], r), reach - full), tint);
                EndStroke(drawing ? 1 : TipAfterglow(stroke.End * delay));
            }
        }

        // 蓄力进度条：外圈花体字随进度逐笔写出，笔尖过曝；写完后整圈缓慢反向旋转并有流光。
        void DrawScript(float written)
        {
            float start = MathHelper.Pi - age * .002f;
            float flash = Math.Clamp(1 - (age - delay) / 40f, 0, 1) * (written >= 1 ? 1 : 0);
            ReadOnlySpan<RecallPortalScript.Vertex> path = RecallPortalScript.Path;
            Vector2 Place(RecallPortalScript.Vertex v) => Point(RecallPortalScript.EllipseAngle(start + v.Angle), radius * v.Radius);
            float Wave(RecallPortalScript.Vertex v) => MathF.Sin(v.Angle * 5 - age * .06f);
            Color Tint(RecallPortalScript.Vertex v) => color * (hovered ? 1 : .85f) * (.6f + .4f * Wave(v));
            // 刚写下的笔迹按书写进度差过曝，写完后这段余辉逐渐淡去；完成瞬间整圈闪亮一次；流光波峰处轻微过曝。
            float trail = written < 1 ? 1 : TipAfterglow(delay);
            float Heat(RecallPortalScript.Vertex v) => Math.Max(Math.Max(
                Math.Clamp(1 - (written - v.Progress) / .03f, 0, 1) * trail, flash * .15f),
                MathF.Pow(Math.Max(0, Wave(v)), 12) * GlintHeat);
            BeginStroke(1.5f);
            AddPoint(Place(path[0]), Tint(path[0]), Heat(path[0]));
            for (int i = 1; i < path.Length; i++)
            {
                RecallPortalScript.Vertex a = path[i - 1], b = path[i];
                if (a.Progress >= written) break;
                if (!b.PenDown)
                {
                    EndStroke();
                    BeginStroke(1.5f);
                    AddPoint(Place(b), Tint(b), Heat(b));
                    continue;
                }
                float t = b.Progress > a.Progress ? Math.Min(1, (written - a.Progress) / (b.Progress - a.Progress)) : 1;
                if (t < 1)
                {
                    AddPoint(Vector2.Lerp(Place(a), Place(b), t), Tint(b), 1);
                    break;
                }
                AddPoint(Place(b), Tint(b), Heat(b));
            }
            EndStroke();
        }
    }
}

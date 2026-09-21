using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Shared verbatim with the FNA preview. Caller owns the batch and render-target boundaries.
internal sealed class ConstellationBookRenderer : IDisposable
{
    internal const int StripCount = 32;
    private readonly BasicEffect _effect;
    private readonly VertexPositionColorTexture[] _quad = new VertexPositionColorTexture[6];
    private readonly Vector3[] _edge = new Vector3[StripCount + 1];
    private readonly float[] _angle = new float[StripCount];
    private readonly (int Strip, float Z)[] _order = new (int, float)[StripCount];

    internal ConstellationBookRenderer(GraphicsDevice device)
        => _effect = new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true, LightingEnabled = false };

    internal static void PaintCover(ConstellationDrawing draw)
    {
        draw.Image("BookCoverFront", 0, 0, 1100, 800);
        draw.Text("魔女手札", 550, 512, 29, new Color(226, 208, 233), .5f, spacing: 7, serif: true);
    }

    internal static void PaintDock(ConstellationDrawing draw, string key, bool hover, float glow)
    {
        draw.Box(0, 0, 96, 30, hover ? new Color(81, 64, 90) : new Color(33, 29, 44) * .86f);
        draw.Frame(0, 0, 96, 30, new Color(161, 133, 178) * (hover ? .8f : .4f));
        if (glow > 0)
        {
            draw.Image("Glow", -15, -15, 126, 60, new Color(189, 161, 221) * (glow * .45f));
            draw.Frame(-3, -3, 102, 36, new Color(216, 192, 237) * glow);
        }
        draw.Image("StudyBook", 8, 7, 16, 16, new Color(215, 197, 174));
        draw.Text("手札", 30, 8, 12, new Color(216, 197, 231), spacing: 1);
        draw.FittedText(key, 79, 9, 25, 10, new Color(196, 173, 201), .5f, serif: true);
    }

    internal void Draw(GraphicsDevice device, Texture2D page, Texture2D front, Texture2D back, Texture2D pixel,
        ConstellationBookMotion motion, Vector2 origin, float scale, Vector2 dockCenter, Vector2 dockSize)
    {
        Vector2 dock = (dockCenter - origin) / scale - new Vector2(550, 400);
        float dockScale = Math.Min(1, Math.Min(dockSize.X / (1100 * scale), dockSize.Y / (800 * scale))) * .82f;
        Matrix rotation = motion.Rotation(dock);
        Matrix pose = motion.Pose(dock, dockScale);
        _effect.World = pose * Matrix.CreateScale(scale, scale, 1) * Matrix.CreateTranslation(origin.X, origin.Y, 0);
        _effect.View = Matrix.Identity;
        var projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, 0, 1);
        // Preserve perspective-correct UV interpolation, without clipping the CSS z coordinates.
        projection.M33 = 0; projection.M43 = .5f;
        _effect.Projection = projection;
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        device.SamplerStates[0] = SamplerState.LinearClamp;

        // The live SUI capture is screen-sized; map it back to the same design plane.
        Vector2 low = -origin / scale, high = (new Vector2(page.Width, page.Height) - origin) / scale;
        Quad(device, page, new Vector3(low, 0), new Vector3(high.X, low.Y, 0),
            new Vector3(high, 0), new Vector3(low.X, high.Y, 0), 0, 1,
            Color.White * (motion.SceneAlpha * motion.PageAlpha), Color.White * (motion.SceneAlpha * motion.PageAlpha));

        float width = 1100f / StripCount, bend = 32 * MathF.Sin(MathF.PI * motion.Turn) * motion.CurlDirection;
        float right = 0;
        _edge[0] = Vector3.Zero;
        for (int i = 0; i < StripCount; i++)
        {
            float u = (i + .5f) / StripCount;
            float angle = MathHelper.ToRadians(-104 * motion.Turn - bend * (1 - 2 * u));
            _angle[i] = angle;
            _edge[i + 1] = _edge[i] + new Vector3(width * MathF.Cos(angle), 0, -width * MathF.Sin(angle));
            right = Math.Max(right, _edge[i + 1].X);
            _order[i] = (i, Vector3.Transform((_edge[i] + _edge[i + 1]) * .5f + new Vector3(0, 400, 0), rotation).Z);
        }
        float shadowWidth = Math.Max(.12f, right / 1100) * 1100;
        float shadowAlpha = motion.ShadowAlpha * motion.SceneAlpha;
        Quad(device, pixel, Vector3.Zero, new Vector3(shadowWidth * .58f, 0, 0),
            new Vector3(shadowWidth * .58f, 800, 0), new Vector3(0, 800, 0), 0, 1,
            new Color(9, 6, 18) * (.7f * shadowAlpha), new Color(41, 26, 59) * (.4f * shadowAlpha));
        Quad(device, pixel, new Vector3(shadowWidth * .58f, 0, 0), new Vector3(shadowWidth, 0, 0),
            new Vector3(shadowWidth, 800, 0), new Vector3(shadowWidth * .58f, 800, 0), 0, 1,
            new Color(41, 26, 59) * (.4f * shadowAlpha), Color.Transparent);
        if (motion.CoverAlpha <= 0) return;

        // Insertion sort is allocation-free and keeps transparent strips back-to-front.
        for (int i = 1; i < StripCount; i++)
        {
            var item = _order[i]; int j = i - 1;
            while (j >= 0 && _order[j].Z > item.Z) { _order[j + 1] = _order[j]; j--; }
            _order[j + 1] = item;
        }
        foreach (var item in _order)
        {
            int i = item.Strip;
            Vector3 a = _edge[i], b = _edge[i + 1], c = b + new Vector3(0, 800, 0), d = a + new Vector3(0, 800, 0);
            Vector3 normal = Vector3.TransformNormal(Vector3.Cross(b - a, d - a), rotation);
            Vector3 toCamera = new Vector3(385, 384, 5000) - Vector3.Transform((a + c) * .5f, rotation);
            bool frontFacing = Vector3.Dot(normal, toCamera) >= 0;
            float shade = Math.Abs(MathF.Sin(_angle[i])) * .2f;
            float u0 = i / (float)StripCount, u1 = (i + 1) / (float)StripCount;
            // Back art mirrors across the whole sheet, not each individual strip.
            var tint = frontFacing ? Color.Lerp(Color.White, new Color(13, 10, 27), shade)
                : Color.Lerp(Color.White, new Color(225, 212, 241), shade * .35f);
            tint *= motion.CoverAlpha * motion.SceneAlpha;
            Quad(device, frontFacing ? front : back, a, b, c, d,
                frontFacing ? u0 : 1 - u0, frontFacing ? u1 : 1 - u1, tint, tint);
        }
    }

    private void Quad(GraphicsDevice device, Texture2D texture, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        float u0, float u1, Color left, Color right)
    {
        _quad[0] = new(a, left, new(u0, 0)); _quad[1] = new(b, right, new(u1, 0)); _quad[2] = new(c, right, new(u1, 1));
        _quad[3] = _quad[0]; _quad[4] = _quad[2]; _quad[5] = new(d, left, new(u0, 1));
        _effect.Texture = texture;
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, _quad, 0, 2);
        }
    }

    public void Dispose() => _effect.Dispose();
}

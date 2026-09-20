using System;
using System.Collections.Generic;
using System.Text.Json;
using KL.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview;

// Coordinates here are design pixels. The same scale/offset is used for SUI hit regions.
// Original assets are ModContent-owned. Grayscale variants are cached once and released on exit.
internal sealed class PreviewDrawing
{
    internal const string Root = "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets/";
    internal static readonly Color Ink = new(238, 233, 245);
    internal static readonly Color Muted = new(178, 168, 195);
    internal static readonly Color Lavender = new(202, 178, 237);
    internal static readonly Color Gold = new(230, 206, 165);
    internal static readonly Color LineColor = new(76, 62, 88);
    private readonly Dictionary<string, Texture2D> _textures = [];
    private readonly Dictionary<(string, float), Texture2D> _desaturated = [];
    private static DynamicSpriteFont GetFont(bool serif) => serif
        ? FontManager.NotoSerifSC.Value : FontManager.HarmonyOS_Sans_SC.Value;
    private readonly Dictionary<string, int[]> _icons;
    private readonly Dictionary<(int Aspect, int Waist, bool Filled), Rectangle[]> _stars = [];
    private sealed class StarRegion
    {
        public int Aspect { get; set; }
        public int Waist { get; set; }
        public bool Filled { get; set; }
        public int[][] Levels { get; set; }
    }
    internal SpriteBatch Batch;
    internal Vector2 Origin;
    internal float Scale = 1;

    internal PreviewDrawing(Mod mod)
    {
        const string path = "ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets/";
        _icons = JsonSerializer.Deserialize<Dictionary<string, int[]>>(mod.GetFileBytes(path + "Icons.json"));
        foreach (var star in JsonSerializer.Deserialize<StarRegion[]>(mod.GetFileBytes(path + "Stars.json")))
        {
            var levels = new Rectangle[star.Levels.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                int[] r = star.Levels[i];
                levels[i] = new Rectangle(r[0], r[1], r[2], r[3]);
            }
            _stars.Add((star.Aspect, star.Waist, star.Filled), levels);
        }
    }

    internal Texture2D Texture(string name)
        => LoadTexture(Root + name);

    private Texture2D LoadTexture(string path)
    {
        if (!_textures.TryGetValue(path, out var result))
            _textures[path] = result = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
        return result;
    }

    internal Vector2 At(float x, float y) => Origin + new Vector2(x, y) * Scale;
    internal void Image(string name, float x, float y, float width, float height, Color? tint = null, float rotation = 0)
    {
        var tex = Texture(name);
        Batch.Draw(tex, At(x + width / 2, y + height / 2), null, tint ?? Color.White,
            rotation, new Vector2(tex.Width, tex.Height) / 2,
            new Vector2(width / tex.Width, height / tex.Height) * Scale, SpriteEffects.None, 0);
    }

    internal void Box(float x, float y, float w, float h, Color color)
        => Batch.Draw(TextureAssets.MagicPixel.Value, At(x, y), new Rectangle(0, 0, 1, 1), color,
            0, Vector2.Zero, new Vector2(w, h) * Scale, SpriteEffects.None, 0);

    internal void ActiveFilterMarker(Vector2 center)
    {
        // HTML active filter: a fading rule and a violet shadow behind the diamond.
        // Glow.png already contains premultiplied RGB for tML's rawimg loader.
        Image("Glow", center.X - 32, center.Y - 4, 64, 8, new Color(192, 154, 220) * .14f);
        Image("Glow", center.X - 16, center.Y - 14, 32, 28, new Color(207, 157, 250) * .34f);
        const int width = 58;
        for (int i = 0; i < width; i++)
        {
            float opacity = 1 - Math.Abs((i + .5f) / width * 2 - 1);
            Box(center.X - width / 2 + i, center.Y - .5f, 1, 1,
                new Color(192, 154, 220) * (.4f * opacity));
        }
        CrossStar(center, new Vector2(7), new Color(215, 186, 239), .5f);
    }

    internal void Line(Vector2 a, Vector2 b, Color color, float width = 1)
    {
        var delta = b - a;
        if (delta.LengthSquared() < .001f) return;
        Batch.Draw(TextureAssets.MagicPixel.Value, Origin + a * Scale, new Rectangle(0, 0, 1, 1), color,
            MathF.Atan2(delta.Y, delta.X), new Vector2(0, .5f), new Vector2(delta.Length(), width) * Scale,
            SpriteEffects.None, 0);
    }

    internal void Dash(Vector2 a, Vector2 b, Color color, float width = 1, float dash = 4, float gap = 5)
    {
        float length = Vector2.Distance(a, b);
        if (length < .01f) return;
        var dir = (b - a) / length;
        for (float p = 0; p < length; p += dash + gap)
            Line(a + dir * p, a + dir * Math.Min(p + dash, length), color, width);
    }

    // Stretch only the middle of a preblurred stroke: cap falloff stays round at any length.
    private void ProfileLine(string textureName, Vector2 a, Vector2 b, float width, float padding,
        float zoom, Color color)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < .01f) return;
        var texture = Texture(textureName);
        Vector2 direction = delta / length;
        float rotation = MathF.Atan2(delta.Y, delta.X);
        float cap = padding * 2 * zoom, height = (width + padding * 2) * zoom;
        void Slice(Rectangle source, float offset, float span)
            => Batch.Draw(texture, Origin + (a + direction * offset) * Scale, source, color, rotation,
                new Vector2(0, source.Height / 2f),
                new Vector2(span / source.Width, height / source.Height) * Scale, SpriteEffects.None, 0);
        if (length < cap)
        {
            Slice(new Rectangle(0, 0, texture.Width, texture.Height), -padding * zoom,
                length + padding * 2 * zoom);
            return;
        }
        int half = texture.Width / 2;
        Slice(new Rectangle(0, 0, half, texture.Height), -padding * zoom, cap);
        if (length > cap)
            Slice(new Rectangle(half, 0, 1, texture.Height), padding * zoom, length - cap);
        Slice(new Rectangle(half, 0, texture.Width - half, texture.Height), length - padding * zoom, cap);
    }

    internal void SkillLink(Vector2 a, Vector2 b, string status, bool related, bool dim, float zoom)
    {
        bool learned = status == "learned", ready = status == "available", hidden = status == "hidden";
        float opacity = dim ? .09f : related ? 1 : ready ? .65f : hidden ? .35f : 1;
        Color color = (related ? new Color(228, 201, 152) : learned ? new Color(191, 172, 216)
            : ready ? new Color(189, 157, 218) : new Color(102, 89, 115)) * opacity;
        float width = related ? 1.8f : learned ? 1.2f : 1;
        string core = related ? "LinkRelated" : learned ? "LinkLearned" : "LinkLocked";
        Vector2 delta = b - a;
        float length = delta.Length();
        if (length < .01f) return;
        Vector2 direction = delta / length;
        if (ready || hidden)
        {
            float dash = (hidden ? 2 : 4) * zoom, gap = (hidden ? 7 : 5) * zoom;
            // SVG drop-shadow follows the dashed alpha mask; do not draw a solid halo underneath.
            if (related)
            {
                var texture = Texture("LinkRelatedDashGlow");
                float rotation = MathF.Atan2(delta.Y, delta.X);
                for (float p = 0; p < length; p += dash + gap)
                {
                    float span = Math.Min(dash, length - p);
                    Batch.Draw(texture, Origin + (a + direction * (p + span / 2)) * Scale, null,
                        new Color(227, 192, 145) * (opacity / 3), rotation,
                        new Vector2(texture.Width, texture.Height) / 2,
                        new Vector2((span + 18 * zoom) / texture.Width, 19.8f * zoom / texture.Height) * Scale,
                        SpriteEffects.None, 0);
                }
            }
            for (float p = 0; p < length; p += dash + gap)
                ProfileLine(core, a + direction * p, a + direction * Math.Min(p + dash, length), width, 1.5f, zoom, color);
        }
        else
        {
            if (related || learned)
                ProfileLine(related ? "LinkRelatedGlow" : "LinkLearnedGlow", a, b, width, related ? 9 : 12, zoom,
                    (related ? new Color(227, 192, 145) : new Color(185, 157, 222)) * (opacity / 3));
            ProfileLine(core, a, b, width, 1.5f, zoom, color);
        }
    }

    internal void Ring(Vector2 center, float rx, float ry, Color color, float width = 1, bool dashed = false, float rotation = 0)
    {
        const int segments = 160;
        Vector2 Point(int i)
        {
            float angle = i * MathHelper.TwoPi / segments;
            var v = new Vector2(MathF.Cos(angle) * rx, MathF.Sin(angle) * ry);
            float c = MathF.Cos(rotation), s = MathF.Sin(rotation);
            return center + new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }
        for (int i = 0; i < segments; i++)
            if (!dashed || i % 4 == 0) Line(Point(i), Point(i + 1), color, width);
    }

    internal void Icon(string name, Vector2 center, float size, Color color)
    {
        if (!_icons.TryGetValue(name, out var r)) r = _icons["star"];
        Batch.Draw(Texture("Icons"), Origin + center * Scale, new Rectangle(r[0], r[1], r[2], r[3]),
            color, 0, new Vector2(r[2], r[3]) / 2, size * Scale / r[2], SpriteEffects.None, 0);
    }

    internal void SkillIcon(PreviewSkill skill, Vector2 center, float size, Color color, bool hidden = false, float grayscale = 0)
    {
        if (hidden)
        {
            // The reference's crescent and four-point star, with a transparent cutout.
            // Scanline spans avoid painting an opaque circle over the retained starfield.
            float radius = size * .35f;
            var moon = center + new Vector2(-size * .08f, size * .06f);
            var cut = moon + new Vector2(size * .17f, -size * .15f);
            const int rows = 40;
            float step = radius * 2 / rows;
            for (int i = 0; i < rows; i++)
            {
                float y = moon.Y - radius + (i + .5f) * step;
                float half = MathF.Sqrt(Math.Max(0, radius * radius - (y - moon.Y) * (y - moon.Y)));
                float left = moon.X - half, right = moon.X + half;
                float dy = y - cut.Y;
                if (Math.Abs(dy) < radius)
                    right = Math.Min(right, cut.X - MathF.Sqrt(radius * radius - dy * dy));
                if (right > left) Box(left, y - step / 2, right - left, step, color * .8f);
            }
            CrossStar(center + new Vector2(size * .3f, -size * .3f), new Vector2(size * .35f), color, .2f);
            return;
        }
        var texture = LoadTexture("伊蕾娜/ElainaModSkills/Icon/" + skill.icon);
        if (grayscale > 0)
        {
            var key = (skill.icon, grayscale);
            if (!_desaturated.TryGetValue(key, out var muted))
            {
                var pixels = new Color[texture.Width * texture.Height];
                texture.GetData(pixels);
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    float gray = p.R * .299f + p.G * .587f + p.B * .114f;
                    pixels[i] = new Color((byte)MathHelper.Lerp(p.R, gray, grayscale),
                        (byte)MathHelper.Lerp(p.G, gray, grayscale), (byte)MathHelper.Lerp(p.B, gray, grayscale), p.A);
                }
                muted = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);
                muted.SetData(pixels);
                _desaturated.Add(key, muted);
            }
            texture = muted;
        }
        float factor = size / Math.Max(texture.Width, texture.Height);
        // Use the project's original skill artwork with its established zero-alpha tint.
        Batch.Draw(texture, Origin + center * Scale, null, new Color(color.R, color.G, color.B, (byte)0), 0,
            new Vector2(texture.Width, texture.Height) / 2, factor * Scale, SpriteEffects.None, 0);
    }

    internal void DisposeGeneratedTextures()
    {
        foreach (var texture in _desaturated.Values) texture.Dispose();
        _desaturated.Clear();
    }

    internal string[] WrapParagraph(string text, float width, float size)
    {
        var lines = new List<string>();
        string line = "";
        foreach (char c in text)
        {
            if (c == '\r') continue;
            if (c == '\n')
            {
                lines.Add(line); line = ""; continue;
            }
            if (line.Length > 0 && Measure(line + c, size) > width)
            {
                // Keep closing punctuation with the preceding glyph without drawing outside the clip.
                if (line.Length > 1 && "，。！？；：、”」）".Contains(c))
                {
                    lines.Add(line[..^1]); line = line[^1].ToString();
                }
                else { lines.Add(line); line = ""; }
            }
            line += c;
        }
        if (line.Length > 0 || lines.Count == 0) lines.Add(line);
        return lines.ToArray();
    }

    internal void Frame(float x, float y, float width, float height, Color color)
    {
        Line(new Vector2(x, y), new Vector2(x + width, y), color);
        Line(new Vector2(x + width, y), new Vector2(x + width, y + height), color);
        Line(new Vector2(x + width, y + height), new Vector2(x, y + height), color);
        Line(new Vector2(x, y + height), new Vector2(x, y), color);
    }

    // Offline KL DrawCrossStar output; all shapes and prefiltered sizes share one texture.
    internal void CrossStar(Vector2 center, Vector2 size, Color color, float waist = .2f, bool filled = true)
    {
        if (size.X <= 0 || size.Y <= 0) return;
        var key = ((int)MathF.Round(size.X / size.Y * 1000),
            (int)MathF.Round(Math.Clamp(waist, .05f, .49f) * 1000), filled);
        Rectangle[] levels = _stars[key];
        // PNG assets have no automatic mipmaps. Select a prefiltered tile close to screen size.
        float extent = Math.Max(size.X, size.Y) * 128f / 112;
        float pixels = extent * Scale * Main.UIScale;
        int level = 0;
        while (level + 1 < levels.Length && pixels < MathF.Sqrt(levels[level].Width * levels[level + 1].Width)) level++;
        Rectangle source = levels[level];
        Batch.Draw(Texture("Stars"), Origin + center * Scale, source, color, 0,
            new Vector2(source.Width, source.Height) / 2, extent * Scale / source.Width, SpriteEffects.None, 0);
    }

    // Exact paths from the reference's 32x32 corner symbol, rotated around its center.
    internal void PageCorner(Vector2 topLeft, float rotation)
    {
        const float scale = 34f / 32;
        var color = new Color(193, 171, 202);
        Vector2 AtCorner(float x, float y) => topLeft + new Vector2(17)
            + Vector2.Transform(new Vector2(x - 16, y - 16) * scale, Matrix.CreateRotationZ(rotation));
        void Stroke(float x1, float y1, float x2, float y2, float width = 1)
            => Line(AtCorner(x1, y1), AtCorner(x2, y2), color, width * scale);
        Stroke(1, 31, 1, 1); Stroke(1, 1, 31, 1);
        Stroke(6, 22, 6, 6); Stroke(6, 6, 22, 6);
        Stroke(1, 1, 10, 10, .7f);
        Stroke(13, 3, 16, 6, .7f); Stroke(16, 6, 13, 9, .7f);
        Stroke(13, 9, 10, 6, .7f); Stroke(10, 6, 13, 3, .7f);
        var dot = AtCorner(6, 6);
        Image("Disc", dot.X - 2 * scale, dot.Y - 2 * scale, 4 * scale, 4 * scale, color);
    }

    // Open corners may rotate independently of the rectangular, unmasked artwork.
    internal void Corners(Vector2 center, float size, float arm, Color color, float rotation = 0)
    {
        Vector2 Transform(Vector2 v) => center + Vector2.Transform(v, Matrix.CreateRotationZ(rotation));
        foreach (int x in new[] { -1, 1 })
        foreach (int y in new[] { -1, 1 })
        {
            var corner = new Vector2(x * size / 2, y * size / 2);
            Line(Transform(corner), Transform(corner - new Vector2(x * arm, 0)), color);
            Line(Transform(corner), Transform(corner - new Vector2(0, y * arm)), color);
        }
    }

    internal void FittedText(string text, float x, float y, float width, float size, Color color,
        float align = 0, bool serif = false, float spacing = 0)
    {
        float measured = Measure(text, size, spacing, serif);
        float fit = measured > width ? width / measured : 1;
        Text(text, x, y, size * fit, color, align, spacing * fit, serif);
    }

    // Use the font's full-width Chinese advance as one design em, not a fixed atlas resolution.
    private static float FontScale(DynamicSpriteFont font, float size) => size / Math.Max(1f, font.MeasureString("国").X);

    // Gelasio is baked at 36pt / 48px; it contains ASCII only, so do not measure a CJK em.
    internal float SlotLabelWidth(string text, float size)
        => FontManager.Gelasio.Value.MeasureString(text).X * size / 48f;

    internal void SlotLabel(string text, float x, float y, float size, Color color)
        => Batch.DrawString(FontManager.Gelasio.Value, text, At(x, y), color, 0, Vector2.Zero,
            size / 48f * Scale, SpriteEffects.None, 0);

    // Header Latin text uses the project's Georgia-compatible face, not CJK serif digits.
    internal float LatinWidth(string text, float size, float spacing = 0)
        => SlotLabelWidth(text, size) + Math.Max(0, text.Length - 1) * spacing;

    internal void LatinText(string text, float x, float y, float size, Color color, float align = 0, float spacing = 0)
    {
        float start = x - LatinWidth(text, size, spacing) * align;
        if (spacing == 0) { SlotLabel(text, start, y, size, color); return; }
        foreach (char c in text)
        {
            string glyph = c.ToString();
            SlotLabel(glyph, start, y, size, color);
            start += SlotLabelWidth(glyph, size) + spacing;
        }
    }

    internal void NotebookTitle(float x, float y)
    {
        var color = new Color(240, 231, 244);
        // Reinforce coverage at exactly the same sample position. Unlike an offset shadow,
        // this gives thin bitmap strokes more ink without blur, new glyphs or wider layout.
        Text("魔女手札", x, y, 30, color, spacing: 3, serif: true);
        Text("魔女手札", x, y, 30, color * .45f, spacing: 3, serif: true);
    }

    internal float Measure(string text, float size, float spacing = 0, bool serif = false)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var font = GetFont(serif);
        float factor = FontScale(font, size);
        if (spacing == 0) return font.MeasureString(text).X * factor;
        float width = 0;
        foreach (char c in text)
            width += font.MeasureString(c.ToString()).X * factor + spacing;
        return Math.Max(0, width - spacing);
    }

    internal void Text(string text, float x, float y, float size, Color color,
        float align = 0, float spacing = 0, bool serif = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        var font = GetFont(serif);
        float factor = FontScale(font, size);
        float start = x - Measure(text, size, spacing, serif) * align;
        if (spacing == 0)
        {
            Batch.DrawString(font, text, At(start, y), color, 0, Vector2.Zero,
                factor * Scale, SpriteEffects.None, 0);
            return;
        }
        foreach (char c in text)
        {
            string glyph = c.ToString();
            Batch.DrawString(font, glyph, At(start, y), color, 0, Vector2.Zero,
                factor * Scale, SpriteEffects.None, 0);
            start += font.MeasureString(glyph).X * factor + spacing;
        }
    }

    internal float Paragraph(string text, float x, float y, float width, float size, Color color,
        float lineHeight = 24)
    {
        foreach (string line in WrapParagraph(text, width, size))
        {
            Text(line, x, y, size, color);
            y += lineHeight;
        }
        return y;
    }
}

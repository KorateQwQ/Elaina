using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KL.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.UI;

// Design-space drawing shared by the native SUI controls and the offline FNA preview.
// Font advances match the existing constellation notebook. Textures are owned by the game or ModContent.
internal sealed class AlchemyDrawing
{
    internal static readonly Color Ink = new(238, 233, 245);
    internal static readonly Color Muted = new(178, 168, 195);
    internal static readonly Color Lavender = new(202, 178, 237);
    internal static readonly Color Gold = new(230, 206, 165);
    internal static readonly Color LineColor = new(76, 62, 88);
    private const string SurfacePath = "伊蕾娜/ElainaModAlchemy/UI/Assets/NotebookSurface";
    private const string GlowPath = "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets/Glow";
    private const string DiscPath = "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets/Disc";
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.Ordinal);
    internal SpriteBatch Batch;
    internal Vector2 Origin;
    internal float Scale = 1;

    private static DynamicSpriteFont GetFont(bool serif) => serif
        ? FontManager.NotoSerifSC.Value : FontManager.HarmonyOS_Sans_SC.Value;
    private static float FontScale(DynamicSpriteFont font, float size)
        => size / Math.Max(1f, font.MeasureString("国").X);
    internal Vector2 At(float x, float y) => Origin + new Vector2(x, y) * Scale;

    internal Texture2D Texture(string path)
    {
        if (!_textures.TryGetValue(path, out var texture))
            _textures[path] = texture = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
        return texture;
    }

    internal void Image(string path, float x, float y, float width, float height, Color? tint = null, float rotation = 0)
    {
        if (path.StartsWith(AlchemyCatalog.VanillaItemRoot, StringComparison.Ordinal))
        {
            int itemType = int.Parse(path[AlchemyCatalog.VanillaItemRoot.Length..]);
            Terraria.Main.GetItemDrawFrame(itemType, out var itemTexture, out var frame);
            float itemFit = Math.Min(width / frame.Width, height / frame.Height);
            DrawPixel(itemTexture, At(x + width / 2, y + height / 2), tint ?? Color.White,
                rotation, itemFit * Scale, frame);
            return;
        }
        var texture = Texture(path);
        float fit = Math.Min(width / texture.Width, height / texture.Height);
        if (path.EndsWith("_Pixel", StringComparison.Ordinal))
        {
            DrawPixel(texture, At(x + width / 2, y + height / 2), tint ?? Color.White, rotation, fit * Scale);
            return;
        }
        Batch.Draw(texture, At(x + width / 2, y + height / 2), null, tint ?? Color.White,
            rotation, new Vector2(texture.Width, texture.Height) / 2, fit * Scale, SpriteEffects.None, 0);
    }

    // Material slots keep their layout while the centered icon uses 75% of its fitted size.
    internal void MaterialIcon(string path, float x, float y, float width, float height)
        => Image(path, x + width * .125f, y + height * .125f, width * .75f, height * .75f);

    // Show catalog pixel art at its native size, independent of the old pencil bounds.
    internal void ItemIcon(string path, float centerX, float centerY, float rotation = 0)
    {
        var texture = Texture(path);
        DrawPixel(texture, At(centerX, centerY), Color.White, rotation, Scale);
    }

    private void DrawPixel(Texture2D texture, Vector2 center, Color tint, float rotation, float scale,
        Rectangle? sourceFrame = null)
    {
        // Capture Begin's actual state: in Deferred mode the device may still hold
        // the previous batch's state. Keep SUI's transform and clipping intact.
        var sort = BatchSortMode(Batch);
        var blend = BatchBlendState(Batch);
        var sampler = BatchSamplerState(Batch);
        var depth = BatchDepthState(Batch);
        var rasterizer = BatchRasterizerState(Batch);
        var effect = BatchEffect(Batch);
        var transform = BatchTransform(Batch);
        Batch.End();
        Batch.Begin(SpriteSortMode.Deferred, blend, SamplerState.PointClamp, depth, rasterizer, effect, transform);
        try
        {
            var frame = sourceFrame ?? new Rectangle(0, 0, texture.Width, texture.Height);
            Batch.Draw(texture, center, frame, tint, rotation,
                new Vector2(frame.Width, frame.Height) / 2, scale, SpriteEffects.None, 0);
        }
        finally
        {
            Batch.End();
            Batch.GraphicsDevice.SamplerStates[0] = sampler;
            Batch.Begin(sort, blend, sampler, depth, rasterizer, effect, transform);
        }
    }

    // FNA has no public batch-state snapshot API. These accessors target the
    // installed FNA fields without per-frame reflection or state allocations.
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sortMode")]
    private static extern ref SpriteSortMode BatchSortMode(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "blendState")]
    private static extern ref BlendState BatchBlendState(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "samplerState")]
    private static extern ref SamplerState BatchSamplerState(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "depthStencilState")]
    private static extern ref DepthStencilState BatchDepthState(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "rasterizerState")]
    private static extern ref RasterizerState BatchRasterizerState(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "customEffect")]
    private static extern ref Effect BatchEffect(SpriteBatch batch);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "transformMatrix")]
    private static extern ref Matrix BatchTransform(SpriteBatch batch);

    private void Stretch(string path, float x, float y, float width, float height, Color color)
    {
        var texture = Texture(path);
        Batch.Draw(texture, At(x, y), null, color, 0, Vector2.Zero,
            new Vector2(width / texture.Width, height / texture.Height) * Scale, SpriteEffects.None, 0);
    }

    internal void Glow(float x, float y, float width, float height, Color color)
        => Stretch(GlowPath, x, y, width, height, color);

    internal void Box(float x, float y, float width, float height, Color color)
        => Batch.Draw(TextureAssets.MagicPixel.Value, At(x, y), new Rectangle(0, 0, 1, 1), color,
            0, Vector2.Zero, new Vector2(width, height) * Scale, SpriteEffects.None, 0);

    internal void RoundedBar(float x, float y, float width, float height, Color color)
    {
        if (width <= 0 || height <= 0) return;
        if (width <= height) { Stretch(DiscPath, x, y, width, height, color); return; }
        float radius = height / 2;
        // Clip each disc to its outer half, avoiding translucent overlap with the center.
        var disc = Texture(DiscPath);
        int half = disc.Width / 2;
        Batch.Draw(disc, At(x, y), new Rectangle(0, 0, half, disc.Height), color, 0, Vector2.Zero,
            new Vector2(radius / half, height / disc.Height) * Scale, SpriteEffects.None, 0);
        Box(x + radius, y, width - height, height, color);
        Batch.Draw(disc, At(x + width - radius, y), new Rectangle(half, 0, disc.Width - half, disc.Height), color, 0, Vector2.Zero,
            new Vector2(radius / (disc.Width - half), height / disc.Height) * Scale, SpriteEffects.None, 0);
    }

    internal void Line(Vector2 a, Vector2 b, Color color, float width = 1)
    {
        Vector2 delta = b - a;
        if (delta.LengthSquared() < .001f) return;
        Batch.Draw(TextureAssets.MagicPixel.Value, Origin + a * Scale, new Rectangle(0, 0, 1, 1), color,
            MathF.Atan2(delta.Y, delta.X), new Vector2(0, .5f), new Vector2(delta.Length(), width) * Scale,
            SpriteEffects.None, 0);
    }

    internal void Frame(float x, float y, float width, float height, Color color)
    {
        Line(new Vector2(x, y), new Vector2(x + width, y), color);
        Line(new Vector2(x + width, y), new Vector2(x + width, y + height), color);
        Line(new Vector2(x + width, y + height), new Vector2(x, y + height), color);
        Line(new Vector2(x, y + height), new Vector2(x, y), color);
    }

    internal void DashedFrame(float x, float y, float width, float height, Color color)
    {
        const float dash = 2, step = 4;
        for (float offset = 0; offset < width; offset += step)
        {
            float end = Math.Min(offset + dash, width);
            Line(new(x + offset, y), new(x + end, y), color);
            Line(new(x + offset, y + height), new(x + end, y + height), color);
        }
        for (float offset = 0; offset < height; offset += step)
        {
            float end = Math.Min(offset + dash, height);
            Line(new(x, y + offset), new(x, y + end), color);
            Line(new(x + width, y + offset), new(x + width, y + end), color);
        }
    }

    internal void CornerBrackets(float x, float y, float width, float height, Color color)
    {
        const float arm = 9;
        for (int i = 0; i < 4; i++)
        {
            bool right = (i & 1) != 0, bottom = (i & 2) != 0;
            Vector2 corner = new(x + (right ? width : 0), y + (bottom ? height : 0));
            Line(corner, corner + new Vector2(right ? -arm : arm, 0), color);
            Line(corner, corner + new Vector2(0, bottom ? -arm : arm), color);
        }
    }

    internal void DashedRing(Vector2 center, float radius, Color color)
    {
        const int segments = 24;
        for (int i = 0; i < segments; i += 2)
        {
            float start = i * MathHelper.TwoPi / segments, end = (i + 1) * MathHelper.TwoPi / segments;
            Line(center + new Vector2(MathF.Cos(start), MathF.Sin(start)) * radius,
                center + new Vector2(MathF.Cos(end), MathF.Sin(end)) * radius, color);
        }
    }

    internal void Ring(Vector2 center, float rx, float ry, Color color, float width = 1)
    {
        const int segments = 160;
        Vector2 Point(int index)
        {
            float angle = index * MathHelper.TwoPi / segments;
            return center + new Vector2(MathF.Cos(angle) * rx, MathF.Sin(angle) * ry);
        }
        for (int i = 0; i < segments; i++) Line(Point(i), Point(i + 1), color, width);
    }

    internal void Diamond(Vector2 center, float size, Color color, bool filled = false)
    {
        if (filled)
        {
            float side = size * MathF.Sqrt(2);
            Batch.Draw(TextureAssets.MagicPixel.Value, Origin + center * Scale, new Rectangle(0, 0, 1, 1),
                color, MathHelper.PiOver4, new Vector2(.5f), new Vector2(side * Scale), SpriteEffects.None, 0);
            return;
        }
        Vector2 top = center - new Vector2(0, size), right = center + new Vector2(size, 0);
        Vector2 bottom = center + new Vector2(0, size), left = center - new Vector2(size, 0);
        Line(top, right, color); Line(right, bottom, color); Line(bottom, left, color); Line(left, top, color);
    }

    internal void Gradient(float x, float y, float width, float height, Color left, Color right)
    {
        int count = Math.Max(1, (int)MathF.Ceiling(width));
        for (int i = 0; i < count; i++)
            Box(x + i * width / count, y, width / count, height, Color.Lerp(left, right, (i + .5f) / count));
    }

    internal float Measure(string text, float size, float spacing = 0, bool serif = false)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var font = GetFont(serif);
        float factor = FontScale(font, size);
        if (spacing == 0) return font.MeasureString(text).X * factor;
        float width = 0;
        foreach (char c in text) width += font.MeasureString(c.ToString()).X * factor + spacing;
        return Math.Max(0, width - spacing);
    }

    internal void Text(string text, float x, float y, float size, Color color,
        float align = 0, float spacing = 0, bool serif = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        var font = GetFont(serif);
        float factor = FontScale(font, size), start = x - Measure(text, size, spacing, serif) * align;
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

    internal void FittedText(string text, float x, float y, float width, float size, Color color,
        float align = 0, bool serif = false, float spacing = 0)
    {
        float measured = Measure(text, size, spacing, serif);
        float fit = measured > width ? width / measured : 1;
        Text(text, x, y, size * fit, color, align, spacing * fit, serif);
    }

    internal float LatinWidth(string text, float size, float spacing = 0)
        => string.IsNullOrEmpty(text) ? 0 : FontManager.Gelasio.Value.MeasureString(text).X * size / 48f
            + Math.Max(0, text.Length - 1) * spacing;

    internal void LatinText(string text, float x, float y, float size, Color color, float align = 0, float spacing = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
        var font = FontManager.Gelasio.Value;
        float start = x - LatinWidth(text, size, spacing) * align;
        if (spacing == 0)
        {
            Batch.DrawString(font, text, At(start, y), color, 0, Vector2.Zero,
                size / 48f * Scale, SpriteEffects.None, 0);
            return;
        }
        foreach (char c in text)
        {
            string glyph = c.ToString();
            Batch.DrawString(font, glyph, At(start, y), color, 0, Vector2.Zero,
                size / 48f * Scale, SpriteEffects.None, 0);
            start += LatinWidth(glyph, size) + spacing;
        }
    }

    internal string[] WrapParagraph(string text, float width, float size)
    {
        var lines = new List<string>();
        string line = "";
        foreach (char c in text ?? "")
        {
            if (c == '\r') continue;
            if (c == '\n') { lines.Add(line); line = ""; continue; }
            if (line.Length > 0 && Measure(line + c, size) > width)
            {
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

    internal float Paragraph(string text, float x, float y, float width, float size, Color color, float lineHeight = 24)
    {
        foreach (string line in WrapParagraph(text, width, size))
        {
            Text(line, x, y, size, color);
            y += lineHeight;
        }
        return y;
    }

    // Same purple paper, exposed cover and leaf edges as ConstellationDrawing.NotebookPage.
    internal void NotebookPage(float width, float height)
    {
        Color paper = Lavender, leather = new(26, 23, 35);
        Box(-7, 4, width + 16, height + 8, leather);
        Line(new Vector2(-8, 4), new Vector2(-8, height + 5), paper * .42f);
        Line(new Vector2(2, height + 12), new Vector2(width + 5, height + 12), Color.Black * .35f, 2);
        Line(new Vector2(width + 10, 12), new Vector2(width + 10, height + 7), Color.Black * .3f, 2);
        Box(5, height, width - 6, 9, new Color(39, 32, 51));
        Box(width, 7, 7, height - 9, new Color(39, 32, 51));
        for (int leaf = 0; leaf < 3; leaf++)
        {
            float inset = leaf * 3;
            Color edge = paper * (.38f - leaf * .045f);
            Line(new Vector2(5, height + inset + 1), new Vector2(width - 1, height + inset + 1), edge);
            Line(new Vector2(width + inset + 1, 9), new Vector2(width + inset + 1, height - 1), edge);
            Line(new Vector2(width - 1, height + inset + 1), new Vector2(width + inset + 1, height - 1), edge);
        }
        Stretch(SurfacePath, 0, 0, width, height, Color.White);
        Frame(1, 1, width - 2, height - 2, Lavender * .5f);
        Frame(10, 10, width - 20, height - 20, Lavender * .13f);
        Line(new Vector2(12, 2), new Vector2(width - 12, 2), Lavender * .1f);
        Gradient(2, 22, 5, height - 44, new Color(13, 13, 23) * .8f, LineColor * .35f);
        Gradient(7, 22, 5, height - 44, LineColor * .35f, new Color(13, 13, 23) * .65f);
        PageCorner(new Vector2(10, 10), 0);
        PageCorner(new Vector2(width - 35, 10), MathHelper.PiOver2);
        PageCorner(new Vector2(10, height - 35), -MathHelper.PiOver2);
        PageCorner(new Vector2(width - 35, height - 35), MathHelper.Pi);
    }

    internal void PageCorner(Vector2 topLeft, float rotation)
    {
        const float scale = 25f / 32;
        var color = new Color(193, 171, 202);
        Vector2 AtCorner(float x, float y) => topLeft + new Vector2(12.5f)
            + Vector2.Transform(new Vector2(x - 16, y - 16) * scale, Matrix.CreateRotationZ(rotation));
        void Stroke(float x1, float y1, float x2, float y2, float width = 1)
            => Line(AtCorner(x1, y1), AtCorner(x2, y2), color, width * scale);
        Stroke(1, 31, 1, 1); Stroke(1, 1, 31, 1);
        Stroke(6, 22, 6, 6); Stroke(6, 6, 22, 6);
        Stroke(1, 1, 10, 10, .7f);
        Stroke(13, 3, 16, 6, .7f); Stroke(16, 6, 13, 9, .7f);
        Stroke(13, 9, 10, 6, .7f); Stroke(10, 6, 13, 3, .7f);
        var dot = AtCorner(6, 6);
        Image(DiscPath, dot.X - 2 * scale, dot.Y - 2 * scale, 4 * scale, 4 * scale, color);
    }
}

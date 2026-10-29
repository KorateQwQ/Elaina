using static 伊蕾娜.Localization.UIText;
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
internal sealed partial class AlchemyDrawing
{
    internal static readonly Color Ink = new(238, 233, 245);
    internal static readonly Color Muted = new(178, 168, 195);
    internal static readonly Color Lavender = new(202, 178, 237);
    internal static readonly Color Gold = new(230, 206, 165);
    internal static readonly Color LineColor = new(76, 62, 88);
    private const string SurfacePath = "伊蕾娜/ElainaModAlchemy/UI/Assets/NotebookSurface";
    private const string GlowPath = "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/Glow";
    private const string DiscPath = "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/Disc";
    private const string MagicIconPath = "伊蕾娜/Snippets/MagicStarIcon";
    private const string MagicTagPrefix = "[klicon/16,0:" + MagicIconPath + "][c/4C91FFFF:";
    private const string ChineseMagicTag = MagicTagPrefix + "魔力]";
    private const string EnglishMagicTag = MagicTagPrefix + "Magic Power]";
    private static readonly Color MagicColor = new(76, 145, 255);

    // Treat the resource icon and its name as one inline unit for measuring and wrapping.
    // These private markers never enter localization or the rendered text.
    private static string PackMagicTags(string text) => text.Replace(ChineseMagicTag, "\uE000").Replace(EnglishMagicTag, "\uE001");
    private static string UnpackMagicTags(string text) => text.Replace("\uE000", ChineseMagicTag).Replace("\uE001", EnglishMagicTag);
    private static bool IsMagicMarker(char c) => c is '\uE000' or '\uE001';
    private static string MagicLabel(char c) => c == '\uE000' ? "魔力" : "Magic Power";

    private float MagicWidth(char marker, float size, float spacing, bool serif)
        => size + size * .12f + spacing + Measure(MagicLabel(marker), size, spacing, serif);
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
    {
        Texture2D texture;
        Rectangle frame;
        if (path.StartsWith(AlchemyCatalog.VanillaItemRoot, StringComparison.Ordinal))
            Terraria.Main.GetItemDrawFrame(int.Parse(path[AlchemyCatalog.VanillaItemRoot.Length..]), out texture, out frame);
        else { texture = Texture(path); frame = new(0, 0, texture.Width, texture.Height); }
        float fit = Math.Min(1, Math.Min(width / frame.Width, height / frame.Height));
        DrawPixel(texture, At(x + width / 2, y + height / 2), Color.White, 0, fit * Scale, frame, Outline(path));
    }

    private Texture2D Outline(string path) => Texture(AlchemyNotebookPresentation.AssetRoot + "Outlines/" + path[(path.LastIndexOf('/') + 1)..]);

    internal void OutlinedItem(string path, float x, float y, Color? tint = null)
    {
        var texture = Texture(path);
        DrawPixel(texture, At(x, y), tint ?? Color.White, 0, Scale, outline: Outline(path));
    }

    // Show catalog pixel art at its native size, independent of the old pencil bounds.
    internal void ItemIcon(string path, float centerX, float centerY, float rotation = 0, Color? tint = null)
    {
        var texture = Texture(path);
        DrawPixel(texture, At(centerX, centerY), tint ?? Color.White, rotation, Scale);
    }

    // Atlas cell centered on a design-space point; texelSize is design units per texel.
    internal void Sprite(string path, Rectangle source, Vector2 center, float texelSize, Color color, float rotation = 0)
        => Batch.Draw(Texture(path), At(center.X, center.Y), source, color, rotation,
            new Vector2(source.Width, source.Height) / 2, texelSize * Scale, SpriteEffects.None, 0);

    private void DrawPixel(Texture2D texture, Vector2 center, Color tint, float rotation, float scale,
        Rectangle? sourceFrame = null, Texture2D outline = null)
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
            if (outline != null)
            {
                int index = frame.Y / (frame.Height + 2);
                var ring = new Rectangle(0, index * (frame.Height + 2), frame.Width + 2, frame.Height + 2);
                Batch.Draw(outline, center, ring, new Color(236, 229, 247) * (.62f * tint.A / 255f), rotation,
                    new Vector2(ring.Width, ring.Height) / 2, scale, SpriteEffects.None, 0);
            }
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

    internal readonly record struct BatchFrame(SpriteSortMode Sort, BlendState Blend, SamplerState Sampler,
        DepthStencilState Depth, RasterizerState Rasterizer, Effect Effect, Matrix Transform);

    // Rotate and scale everything drawn until PopFrame about a design-space pivot.
    // The extra matrix is applied before SUI's own transform; screen-space clipping is unchanged,
    // and nested pixel sprites pick up the frame through DrawPixel's state capture.
    internal BatchFrame PushFrame(Vector2 pivot, float rotation, float scale)
    {
        var saved = new BatchFrame(BatchSortMode(Batch), BatchBlendState(Batch), BatchSamplerState(Batch),
            BatchDepthState(Batch), BatchRasterizerState(Batch), BatchEffect(Batch), BatchTransform(Batch));
        Vector2 p = At(pivot.X, pivot.Y);
        var local = Matrix.CreateTranslation(-p.X, -p.Y, 0) * Matrix.CreateRotationZ(rotation)
            * Matrix.CreateScale(scale, scale, 1) * Matrix.CreateTranslation(p.X, p.Y, 0);
        Batch.End();
        Batch.Begin(saved.Sort, saved.Blend, saved.Sampler, saved.Depth, saved.Rasterizer, saved.Effect, local * saved.Transform);
        return saved;
    }

    internal void PopFrame(BatchFrame saved)
    {
        Batch.End();
        Batch.Begin(saved.Sort, saved.Blend, saved.Sampler, saved.Depth, saved.Rasterizer, saved.Effect, saved.Transform);
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
        // Clip each disc to its outer half, avoiding translucent overlap with the center.
        var disc = Texture(DiscPath);
        if (height > width * 2)
        {
            // Upright pill, e.g. a scroll thumb.
            float cap = width / 2;
            int top = disc.Height / 2;
            Batch.Draw(disc, At(x, y), new Rectangle(0, 0, disc.Width, top), color, 0, Vector2.Zero,
                new Vector2(width / disc.Width, cap / top) * Scale, SpriteEffects.None, 0);
            Box(x, y + cap, width, height - width, color);
            Batch.Draw(disc, At(x, y + height - cap), new Rectangle(0, top, disc.Width, disc.Height - top), color, 0, Vector2.Zero,
                new Vector2(width / disc.Width, cap / (disc.Height - top)) * Scale, SpriteEffects.None, 0);
            return;
        }
        if (width <= height) { Stretch(DiscPath, x, y, width, height, color); return; }
        float radius = height / 2;
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
        string packed = PackMagicTags(text);
        if (packed.IndexOfAny(['\uE000', '\uE001']) >= 0)
        {
            float total = 0;
            int start = 0;
            for (int i = 0; i < packed.Length; i++)
            {
                if (!IsMagicMarker(packed[i])) continue;
                if (i > start) total += Measure(packed[start..i], size, spacing, serif) + spacing;
                total += MagicWidth(packed[i], size, spacing, serif);
                if (i + 1 < packed.Length) total += spacing;
                start = i + 1;
            }
            if (start < packed.Length) total += Measure(packed[start..], size, spacing, serif);
            return total;
        }
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
        string packed = PackMagicTags(text);
        if (packed.IndexOfAny(['\uE000', '\uE001']) >= 0)
        {
            int segmentStart = 0;
            for (int i = 0; i < packed.Length; i++)
            {
                if (!IsMagicMarker(packed[i])) continue;
                if (i > segmentStart)
                {
                    string segment = packed[segmentStart..i];
                    Text(segment, start, y, size, color, spacing: spacing, serif: serif);
                    start += Measure(segment, size, spacing, serif) + spacing;
                }
                var icon = Texture(MagicIconPath);
                float centerY = y + font.MeasureString("国").Y * factor * .5f;
                DrawPixel(icon, At(start + size * .5f, centerY), Color.White * (color.A / 255f), 0,
                    size / Math.Max(icon.Width, icon.Height) * Scale);
                Text(MagicLabel(packed[i]), start + size * 1.12f + spacing, y, size,
                    MagicColor * (color.A / 255f), spacing: spacing, serif: serif);
                start += MagicWidth(packed[i], size, spacing, serif) + spacing;
                segmentStart = i + 1;
            }
            if (segmentStart < packed.Length) Text(packed[segmentStart..], start, y, size, color, spacing: spacing, serif: serif);
            return;
        }
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

    // A short label turned about its own center, e.g. an ink stamp. Shrinks to fit width.
    internal void RotatedText(string text, Vector2 center, float width, float size, Color color, float rotation, bool serif = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        var font = GetFont(serif);
        float measured = Measure(text, size, 0, serif);
        if (measured > width) size *= width / measured;
        Batch.DrawString(font, text, At(center.X, center.Y), color, rotation, font.MeasureString(text) / 2,
            FontScale(font, size) * Scale, SpriteEffects.None, 0);
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
        var lines = WrapUIParagraph(PackMagicTags(text ?? ""), width, value => Measure(value, size));
        for (int i = 0; i < lines.Length; i++) lines[i] = UnpackMagicTags(lines[i]);
        return lines;
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

}

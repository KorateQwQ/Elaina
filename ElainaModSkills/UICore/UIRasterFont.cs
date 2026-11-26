using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace 伊蕾娜.ElainaModSkills.UICore;

// Area-filtered glyph coverage baked from the existing 48px fonts. These PNGs retain 8-bit alpha;
// loading builds glyph metadata only, never textures. Shared by notebooks, HUDs and radial menus.
internal class UIRasterFont(string family, DynamicSpriteFont master, int minimum, int maximum,
    Func<string, byte[]> readBytes, Func<string, Texture2D> loadTexture)
{
    private readonly Dictionary<int, DynamicSpriteFont> _sizes = [];

    private DynamicSpriteFont Resolve(string text, float em)
    {
        int pixels = (int)MathF.Round(em, MidpointRounding.AwayFromZero);
        if (pixels < minimum || pixels > maximum) return null;
        if (!_sizes.TryGetValue(pixels, out var font))
        {
            string path = $"ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/FontRaster/{family}/{pixels}";
            using var stream = new MemoryStream(readBytes(path + ".bin"));
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported constellation font atlas.");
            font = new DynamicSpriteFont(0, reader.ReadInt32(), '?');
            int count = reader.ReadInt32();
            var texture = loadTexture(path);
            for (int i = 0; i < count; i++)
            {
                char character = (char)reader.ReadUInt16();
                var region = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                var padding = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), region.Width, region.Height);
                var kerning = new Vector3(0, reader.ReadInt32(), 0);
                font.SpriteCharacters.Add(character, new(texture, region, padding, kerning));
            }
            _sizes.Add(pixels, font);
        }
        return font.AreCharactersSupported(text) ? font : null;
    }

    internal float Measure(string text, float em)
        => Resolve(text, em)?.MeasureString(text).X ?? master.MeasureString(text).X * em / 48f;

    internal float Height(string text, float em)
        => Resolve(text, em)?.MeasureString(text).Y ?? master.MeasureString(text).Y * em / 48f;

    internal void Draw(SpriteBatch batch, string text, Vector2 position, Color color, float em, Matrix transform,
        float? referenceScale = null, float rotation = 0, bool centered = false, bool smoothTransform = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        var font = Resolve(text, em);
        float scale = referenceScale ?? MathF.Sqrt(transform.M11 * transform.M11 + transform.M12 * transform.M12);
        float factor = font == null ? em / 48f : 1;
        bool native = rotation == 0 && !centered && Math.Abs(transform.M12) < .00001f &&
            Math.Abs(transform.M21) < .00001f && Math.Abs(transform.M11 - scale) < .00001f && Math.Abs(transform.M22 - scale) < .00001f;
        if (smoothTransform && !native && font != null)
        {
            DrawTransformed(batch, text, position, color, em, scale, rotation, centered, font);
            return;
        }
        // Card transforms keep their continuous rotation/scale and stable layout font. Only the
        // untransformed static path snaps to native device pixels; movement is never quantized here.
        if (font != null && native)
        {
            position.X = (MathF.Round(position.X * scale + transform.M41) - transform.M41) / scale;
            position.Y = (MathF.Round(position.Y * scale + transform.M42) - transform.M42) / scale;
        }
        font ??= master;
        batch.DrawString(font, text, position, color, rotation, centered ? font.MeasureString(text) / 2 : Vector2.Zero,
            factor / scale, SpriteEffects.None, 0);
    }

    // Rotated paper must not interpolate an already screen-sized coverage mask again.
    // Use a denser shipped atlas, retaining the layout font's advances and line breaks.
    private void DrawTransformed(SpriteBatch batch, string text, Vector2 position, Color color, float em,
        float scale, float rotation, bool centered, DynamicSpriteFont layout)
    {
        int pixels = (int)MathF.Round(em, MidpointRounding.AwayFromZero);
        int sourcePixels = Math.Min(maximum, pixels * 2);
        var source = Resolve(text, sourcePixels) ?? throw new InvalidDataException("Font atlas character sets differ between sizes.");
        float ratio = (float)pixels / sourcePixels;
        Vector2 pen = centered ? -layout.MeasureString(text) / 2 : Vector2.Zero;
        float start = pen.X;
        var turn = Matrix.CreateRotationZ(rotation);
        foreach (char c in text)
        {
            if (c == '\r') continue;
            if (c == '\n') { pen.X = start; pen.Y += layout.LineSpacing; continue; }
            var target = layout.SpriteCharacters[c];
            var glyph = source.SpriteCharacters[c];
            float horizontal = glyph.Kerning.Y > 0 ? target.Kerning.Y / glyph.Kerning.Y : ratio;
            var offset = pen + new Vector2(glyph.Padding.X * horizontal, glyph.Padding.Y * ratio);
            batch.Draw(glyph.Texture, position + Vector2.Transform(offset / scale, turn), glyph.Glyph, color,
                rotation, Vector2.Zero, new Vector2(horizontal, ratio) / scale, SpriteEffects.None, 0);
            pen.X += target.Kerning.Y + target.Kerning.Z + layout.CharacterSpacing;
        }
    }
}

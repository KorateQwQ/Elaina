using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Area-filtered glyph coverage baked from the existing 48px fonts. These PNGs retain 8-bit alpha;
// loading builds glyph metadata only, never textures. Other interfaces keep their original fonts.
internal sealed class ConstellationFont(Mod mod, string family, Asset<DynamicSpriteFont> master, int minimum, int maximum)
{
    private readonly Dictionary<int, DynamicSpriteFont> _sizes = [];

    private DynamicSpriteFont Resolve(string text, float em)
    {
        int pixels = (int)MathF.Round(em, MidpointRounding.AwayFromZero);
        if (pixels < minimum || pixels > maximum) return null;
        if (!_sizes.TryGetValue(pixels, out var font))
        {
            string path = $"ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/FontRaster/{family}/{pixels}";
            using var stream = new MemoryStream(mod.GetFileBytes(path + ".bin"));
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported constellation font atlas.");
            font = new DynamicSpriteFont(0, reader.ReadInt32(), '?');
            int count = reader.ReadInt32();
            var texture = ModContent.Request<Texture2D>("伊蕾娜/" + path, AssetRequestMode.ImmediateLoad).Value;
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
        => Resolve(text, em)?.MeasureString(text).X ?? master.Value.MeasureString(text).X * em / 48f;

    internal float Height(string text, float em)
        => Resolve(text, em)?.MeasureString(text).Y ?? master.Value.MeasureString(text).Y * em / 48f;

    internal void Draw(SpriteBatch batch, string text, Vector2 position, Color color, float em, Matrix transform,
        float? referenceScale = null, float rotation = 0, bool centered = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        var font = Resolve(text, em);
        float scale = referenceScale ?? MathF.Sqrt(transform.M11 * transform.M11 + transform.M12 * transform.M12);
        float factor = font == null ? em / 48f : 1;
        // Card transforms keep their continuous rotation/scale and stable layout font. Only the
        // untransformed static path snaps to native device pixels; movement is never quantized here.
        if (font != null && rotation == 0 && !centered && Math.Abs(transform.M12) < .00001f &&
            Math.Abs(transform.M21) < .00001f && Math.Abs(transform.M11 - scale) < .00001f && Math.Abs(transform.M22 - scale) < .00001f)
        {
            position.X = (MathF.Round(position.X * scale + transform.M41) - transform.M41) / scale;
            position.Y = (MathF.Round(position.Y * scale + transform.M42) - transform.M42) / scale;
        }
        font ??= master.Value;
        batch.DrawString(font, text, position, color, rotation, centered ? font.MeasureString(text) / 2 : Vector2.Zero,
            factor / scale, SpriteEffects.None, 0);
    }
}

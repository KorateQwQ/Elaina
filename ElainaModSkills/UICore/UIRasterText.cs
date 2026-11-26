using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.UICore;

internal static class UIRasterText
{
    internal static float Pixels(SpriteBatch batch, float size, float minimum = 9)
        => Math.Max(minimum, MathF.Round(size * UIBatch.Scale(batch), MidpointRounding.AwayFromZero));

    // Measure the same selected atlas that will be drawn. Long text keeps a readable minimum.
    internal static string Fit(UIRasterFont font, string text, float widthPixels, ref float pixels, float minimum = 9)
    {
        while (pixels > minimum && font.Measure(text, pixels) > widthPixels) pixels--;
        if (font.Measure(text, pixels) <= widthPixels) return text;
        int length = text.Length;
        while (length > 0)
        {
            length--;
            if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;
            string shown = text[..length] + "…";
            if (font.Measure(shown, pixels) <= widthPixels) return shown;
        }
        return "";
    }

    internal static void Draw(SpriteBatch batch, UIRasterFont font, string text, Vector2 position,
        Color color, float pixels, Vector2 anchor, Color outline = default, float spacing = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
        float scale = UIBatch.Scale(batch);
        var matrix = UIBatch.Transform(batch);
        Vector2 extent = new(font.Measure(text, pixels) + Math.Max(0, text.Length - 1) * spacing * scale, font.Height(text, pixels));
        Vector2 topLeft = position - extent * anchor / scale;
        void Paint(Vector2 at, Color ink)
        {
            if (spacing == 0) { font.Draw(batch, text, at, ink, pixels, matrix); return; }
            foreach (char c in text)
            {
                string glyph = c.ToString();
                font.Draw(batch, glyph, at, ink, pixels, matrix);
                at.X += font.Measure(glyph, pixels) / scale + spacing;
            }
        }
        if (outline.A > 0)
        {
            // Exactly one device pixel, even when the widget is reduced to half size.
            Paint(topLeft + new Vector2(-1, 0) / scale, outline);
            Paint(topLeft + new Vector2(1, 0) / scale, outline);
            Paint(topLeft + new Vector2(0, -1) / scale, outline);
            Paint(topLeft + new Vector2(0, 1) / scale, outline);
        }
        Paint(topLeft, color);
    }
}

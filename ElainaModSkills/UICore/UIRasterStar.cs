using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.UICore;

// Share the notebook's dense prefiltered star atlas instead of stretching an 8px tile to 10px.
internal sealed class UIRasterStar
{
    private readonly Texture2D _texture;
    private readonly Rectangle[] _levels;
    internal UIRasterStar(Texture2D texture, byte[] manifest)
    {
        _texture = texture;
        using var document = JsonDocument.Parse(manifest);
        var entry = document.RootElement.EnumerateArray().Single(e => e.GetProperty("Aspect").GetInt32() == 1000 &&
            e.GetProperty("Waist").GetInt32() == 200 && e.GetProperty("Filled").GetBoolean());
        _levels = entry.GetProperty("Levels").EnumerateArray().Select(r => new Rectangle(r[0].GetInt32(), r[1].GetInt32(), r[2].GetInt32(), r[3].GetInt32())).ToArray();
    }
    internal void Draw(SpriteBatch batch, Vector2 center, float size, Color color)
    {
        float scale = UIBatch.Scale(batch);
        float pixels = Math.Max(4, MathF.Round(size * scale * 128 / 112));
        Rectangle source = _levels[0];
        foreach (Rectangle candidate in _levels)
            if (Math.Abs(MathF.Log(candidate.Width / pixels)) < Math.Abs(MathF.Log(source.Width / pixels))) source = candidate;
        var matrix = UIBatch.Transform(batch);
        Vector2 screen = Vector2.Transform(center, matrix) - new Vector2(pixels / 2);
        center = Vector2.Transform(new Vector2(MathF.Round(screen.X), MathF.Round(screen.Y)) + new Vector2(pixels / 2), Matrix.Invert(matrix));
        batch.Draw(_texture, center, source, color, 0, new Vector2(source.Width, source.Height) / 2, pixels / scale / source.Width, SpriteEffects.None, 0);
    }
}

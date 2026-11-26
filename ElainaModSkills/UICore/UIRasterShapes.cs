using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.UICore;

// Analytic screen coverage; dynamic sizes never require generating a texture.
internal sealed class UIRasterShapes(Texture2D pixel, Effect effect)
{
    internal void Line(SpriteBatch batch, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 delta = b - a;
        if (delta.LengthSquared() < .000001f || width <= 0) return;
        Shape(batch, (a + b) / 2, new Vector2(delta.Length(), width) / 2, color, 0, -1, 0, MathF.Atan2(delta.Y, delta.X));
    }
    internal void Ring(SpriteBatch batch, Vector2 center, Vector2 radii, float width, Color color, float rotation = 0)
    {
        if (width <= 0) return;
        Shape(batch, center, radii, color, 1, width / 2, rotation: rotation);
    }
    internal void Arc(SpriteBatch batch, Vector2 center, float radius, float from, float sweep, float width, Color color)
    {
        if (sweep <= 0 || width <= 0) return;
        Shape(batch, center, new Vector2(radius), color, 1, width / 2, from: MathHelper.ToRadians(from),
            sweep: Math.Min(MathHelper.TwoPi, MathHelper.ToRadians(sweep)));
    }
    internal void Disc(SpriteBatch batch, Vector2 center, float radius, Color color)
        => Shape(batch, center, new Vector2(radius), color, 1, -1);
    internal void Diamond(SpriteBatch batch, Vector2 center, Vector2 size, Color color)
        => Shape(batch, center, size / 2, color, 2, -1);
    internal void RoundRect(SpriteBatch batch, Vector2 position, Vector2 size, float radius, Color fill, Color border, float width = 1)
    {
        Vector2 half = size / 2;
        float r = Math.Min(radius, Math.Min(half.X, half.Y));
        if (fill.A > 0) Shape(batch, position + half, half, fill, 0, -1, r);
        if (border.A > 0 && width > 0) Shape(batch, position + half, half, border, 0, width / 2, r);
    }
    private void Shape(SpriteBatch batch, Vector2 center, Vector2 half, Color color, int mode, float halfWidth,
        float corner = 0, float rotation = 0, float from = 0, float sweep = MathHelper.TwoPi)
    {
        if (half.X <= 0 || half.Y <= 0 || color == Color.Transparent) return;
        var saved = UIBatch.Capture(batch);
        float scale = UIBatch.Scale(batch);
        Vector2 extent = half + new Vector2(Math.Max(0, halfWidth) + 2 / scale);
        batch.End();
        var viewport = batch.GraphicsDevice.Viewport;
        effect.Parameters["MatrixTransform"].SetValue(saved.Transform * Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, -1));
        effect.Parameters["Center"].SetValue(center);
        effect.Parameters["HalfSize"].SetValue(half);
        effect.Parameters["Corner"].SetValue(corner);
        effect.Parameters["HalfWidth"].SetValue(halfWidth);
        effect.Parameters["Mode"].SetValue((float)mode);
        effect.Parameters["Rotation"].SetValue(new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)));
        effect.Parameters["ArcStart"].SetValue(new Vector2(MathF.Cos(from), MathF.Sin(from)));
        effect.Parameters["ArcEnd"].SetValue(new Vector2(MathF.Cos(from + sweep), MathF.Sin(from + sweep)));
        effect.Parameters["Sweep"].SetValue(sweep);
        saved.Begin(batch, effect, replaceEffect: true);
        batch.Draw(pixel, center, new Rectangle(0, 0, 1, 1), color, rotation, new Vector2(.5f), extent * 2, SpriteEffects.None, 0);
        batch.End();
        saved.Begin(batch);
    }
}

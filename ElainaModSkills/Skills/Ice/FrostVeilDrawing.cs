using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>Temporary rotating ice shields; shared with the offline FNA preview.</summary>
public static class FrostVeilDrawing
{
    public static void Draw(SpriteBatch batch, Texture2D pixel, Vector2 center, int shields, float seconds, float gravity = 1f)
    {
        for (int i = 0; i < Math.Clamp(shields, 0, 3); i++)
        {
            float angle = seconds * 1.8f + i * MathHelper.TwoPi / 3f;
            float depth = MathF.Sin(angle);
            Vector2 anchor = center + new Vector2(MathF.Cos(angle) * 43f, depth * 19f * gravity);
            float scale = .8f + (depth + 1f) * .14f;
            Color edge = new Color(136, 231, 255) * (.6f + (depth + 1f) * .2f);
            Vector2 top = anchor + new Vector2(0, -16 * gravity) * scale;
            Vector2 left = anchor + new Vector2(-10, -4 * gravity) * scale;
            Vector2 right = anchor + new Vector2(10, -4 * gravity) * scale;
            Vector2 bottom = anchor + new Vector2(0, 17 * gravity) * scale;
            Line(batch, pixel, top, left, edge, 2f);
            Line(batch, pixel, left, bottom, edge, 2f);
            Line(batch, pixel, bottom, right, edge, 2f);
            Line(batch, pixel, right, top, edge, 2f);
            Line(batch, pixel, top, bottom, edge * .7f, 1f);
            Line(batch, pixel, left, right, edge * .7f, 1f);
            Line(batch, pixel, anchor + new Vector2(-4, 0), anchor + new Vector2(4, 0), Color.White * .9f, 2f);
        }
    }

    private static void Line(SpriteBatch batch, Texture2D pixel, Vector2 from, Vector2 to, Color color, float width)
    {
        Vector2 delta = to - from;
        // The real MagicPixel is 1 x 1000; use a 1 x 1 source region explicitly.
        batch.Draw(pixel, from, new Rectangle(0, 0, 1, 1), color, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0f);
    }
}

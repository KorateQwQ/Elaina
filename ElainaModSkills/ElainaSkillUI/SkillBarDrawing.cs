using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

// Shared by the live SUI controls and the offline FNA preview.
internal static class SkillBarDrawing
{
    public const int Width = 646;
    public const int Height = 94;
    private const int Tile = 192;
    private const float ArrivalDuration = .45f;
    private static readonly Vector2[] Orbit = CreateOrbit();

    public static void DrawRack(SpriteBatch batch, Texture2D rack, Vector2 position) =>
        batch.Draw(rack, position - new Vector2(24), null, Color.White, 0,
            Vector2.Zero, .5f, SpriteEffects.None, 0);

    private static void Ornament(SpriteBatch batch, Texture2D atlas, int tile, Vector2 center, Color color) =>
        batch.Draw(atlas, center, new Rectangle(tile * Tile, 0, Tile, Tile), color,
            0, new Vector2(Tile / 2), .5f, SpriteEffects.None, 0);

    public static void DrawSlotBackground(SpriteBatch batch, Texture2D atlas,
        Vector2 center, bool selected, bool occupied, float hover, float seconds)
    {
        Ornament(batch, atlas, selected ? 2 : 4, center, Color.White);
        if (selected)
            Ornament(batch, atlas, 2, center, new Color(255, 235, 255, 0) * (2.2f * ArrivalPulse(seconds)));
        if (hover > 0 && !selected) Ornament(batch, atlas, 5, center, Color.White * hover);
        if (!occupied)
            Ornament(batch, atlas, 3, center, selected ? new Color(218, 197, 238) :
                Color.Lerp(new Color(177, 154, 195) * (113f / 255f), new Color(224, 203, 238), hover));
    }

    public static void DrawSlotFrame(SpriteBatch batch, Texture2D atlas,
        Vector2 center, bool selected, bool occupied, float hover, float seconds)
    {
        if (!selected)
        {
            DrawRestingCorners(batch, atlas, center,
                Color.White * MathHelper.Lerp(occupied ? .48f : .26f, .72f, hover));
            return;
        }
        float pulse = ArrivalPulse(seconds);
        Ornament(batch, atlas, 1, center, Color.White * (.84f + .16f * pulse));
        if (pulse > 0)
        {
            // Alpha-zero tints add light under the existing premultiplied batch.
            Ornament(batch, atlas, 1, center, new Color(245, 222, 255, 0) * (.9f * pulse));
            float progress = Math.Clamp(seconds / ArrivalDuration, 0, 1);
            float spread = 1 - MathF.Pow(1 - progress, 3);
            batch.Draw(atlas, center, new Rectangle(0, 0, Tile, Tile),
                new Color(223, 192, 255, 0) * (.65f * pulse), 0, new Vector2(Tile / 2),
                .5f * (1 + .16f * spread), SpriteEffects.None, 0);

        }

        // CSS conic-gradient: clockwise from twelve o'clock, one revolution in 2.8 seconds.
        float rotation = seconds / 2.8f * 360;
        for (int i = 0; i < Orbit.Length - 1; i++)
        {
            Vector2 a = Orbit[i], b = Orbit[i + 1], middle = (a + b) * .5f;
            float angle = (MathHelper.ToDegrees(MathF.Atan2(middle.X, -middle.Y)) - rotation) % 360;
            if (angle < 0) angle += 360;
            Color color = OrbitColor(angle);
            if (color.A == 0) continue;
            Vector2 delta = b - a;
            batch.Draw(atlas, center + a, new Rectangle(0, Tile, 4, 8), color,
                MathF.Atan2(delta.Y, delta.X), new Vector2(0, 4),
                new Vector2(delta.Length() / 4, .25f), SpriteEffects.None, 0);
        }
        DrawSelectionMarkers(batch, atlas, center, seconds, pulse);
    }

    private static void DrawSelectionMarkers(SpriteBatch batch, Texture2D atlas,
        Vector2 center, float seconds, float pulse)
    {
        float breath = .5f + .5f * MathF.Sin(seconds * MathHelper.TwoPi / 3.6f);
        Vector2 starPosition = center + new Vector2(0, -37.5f);
        Vector2 diamondPosition = center + new Vector2(0, 34.5f);
        float starScale = 1 + .25f * pulse;
        DrawMarker(batch, atlas, 1, starPosition, new Color(255, 255, 255, 0) * (.55f + .25f * breath + pulse), starScale);
        DrawMarker(batch, atlas, 0, starPosition, Color.White, starScale);
        DrawMarker(batch, atlas, 0, starPosition, new Color(255, 241, 255, 0) * (.45f * pulse), starScale);

        // A short delayed response below the slot reinforces the main star's arrival.
        float reveal = MathHelper.SmoothStep(0, 1, Math.Clamp((seconds - .065f) / .10f, 0, 1));
        float diamondPulse = seconds < .10f ? 0 : ArrivalPulse(seconds - .10f);
        DrawMarker(batch, atlas, 3, diamondPosition,
            new Color(255, 255, 255, 0) * reveal * (.35f + .15f * breath + .5f * diamondPulse), 1);
        DrawMarker(batch, atlas, 2, diamondPosition, Color.White * (.82f * reveal), 1);
        DrawMarker(batch, atlas, 2, diamondPosition,
            new Color(255, 241, 255, 0) * (.35f * diamondPulse * reveal), 1);
    }

    private static void DrawMarker(SpriteBatch batch, Texture2D atlas, int marker,
        Vector2 position, Color color, float scale) =>
        batch.Draw(atlas, position, new Rectangle(16 + marker * 64, 192, 64, 64),
            color, 0, new Vector2(32), .5f * scale, SpriteEffects.None, 0);

    private static void DrawRestingCorners(SpriteBatch batch, Texture2D atlas, Vector2 center, Color color)
    {
        // Crop four 12px patches from the existing 2x frame; retain the full tile for arrival pulses.
        const int near = 36;
        const int far = 132;
        const int cornerSize = 24;
        for (int row = 0; row < 2; row++)
        for (int column = 0; column < 2; column++)
        {
            int x = column == 0 ? near : far;
            int y = row == 0 ? near : far;
            batch.Draw(atlas, center + new Vector2(x - Tile / 2, y - Tile / 2) * .5f,
                new Rectangle(x, y, cornerSize, cornerSize), color,
                0, Vector2.Zero, .5f, SpriteEffects.None, 0);
        }
    }

    private static float ArrivalPulse(float seconds)
    {
        if (seconds >= ArrivalDuration) return 0;
        const float attack = .055f;
        if (seconds < attack) return MathHelper.Lerp(.65f, 1, Math.Clamp(seconds / attack, 0, 1));
        float decay = (seconds - attack) / (ArrivalDuration - attack);
        return (1 - decay) * (1 - decay);
    }

    private static Color OrbitColor(float angle)
    {
        if (angle < 235) return Color.Transparent;
        if (angle < 275) return Color.Lerp(Color.Transparent, new Color(179, 142, 211) * (59f / 255), (angle - 235) / 40);
        if (angle < 312) return Color.Lerp(new Color(179, 142, 211) * (59f / 255), new Color(215, 185, 243) * (179f / 255), (angle - 275) / 37);
        if (angle < 342) return Color.Lerp(new Color(215, 185, 243) * (179f / 255), new Color(250, 241, 255), (angle - 312) / 30);
        if (angle < 355) return Color.Lerp(new Color(250, 241, 255), new Color(234, 215, 255), (angle - 342) / 13);
        return Color.Lerp(new Color(234, 215, 255), Color.Transparent, (angle - 355) / 5);
    }

    private static Vector2[] CreateOrbit()
    {
        // The 57px CSS ring has a 1.25px inset stroke and 2.5px outer corner radius.
        var points = new Vector2[225];
        const float half = 28.5f - .625f;
        const float radius = 2.5f - .625f;
        for (int i = 0; i <= 224; i++)
        {
            float angle = i / 224f * MathHelper.TwoPi;
            Vector2 ray = new(MathF.Sin(angle), -MathF.Cos(angle));
            Vector2 hit = ray * (half / Math.Max(Math.Abs(ray.X), Math.Abs(ray.Y)));
            if (Math.Abs(hit.X) > half - radius && Math.Abs(hit.Y) > half - radius)
            {
                Vector2 corner = new(MathF.CopySign(half - radius, ray.X), MathF.CopySign(half - radius, ray.Y));
                float dot = Vector2.Dot(ray, corner);
                hit = ray * (dot + MathF.Sqrt(Math.Max(0, dot * dot - corner.LengthSquared() + radius * radius)));
            }
            points[i] = hit;
        }
        return points;
    }
}

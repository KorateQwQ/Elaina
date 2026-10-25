using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.System.UI;

// Two stacked chevrons beside a clipped page, pointing at lines still hidden above or below.
// A glow runs through them toward the hidden side while the pair drifts that way.
// Drawn with the page's own line primitive, so it needs no texture of its own.
internal static class ScrollHint
{
    internal const float Period = 1.4f, Drift = 1f, Spacing = 4f;

    // Fades out over the last stretch of travel, so reaching an edge hides it smoothly.
    internal static float Amount(float hidden) => Math.Clamp(hidden / 12f, 0, 1);

    // direction: 1 points down, -1 points up; center is the middle of the pair.
    internal static void Draw(Action<Vector2, Vector2, Color, float> line, Vector2 center, int direction,
        float seconds, Color color, float amount)
    {
        if (amount <= 0) return;
        float wave = seconds / Period;
        float drift = (1 - MathF.Cos(wave * MathHelper.TwoPi)) * .5f * Drift;
        Vector2 arm = new(5.5f, -2.75f * direction);
        for (int i = 0; i < 2; i++)
        {
            float glow = .5f + .5f * MathF.Cos((wave - i * .25f) * MathHelper.TwoPi);
            Vector2 tip = center + new Vector2(0, ((i - .5f) * Spacing + 1.4f + drift) * direction);
            Color tint = color * (amount * (.35f + .65f * glow));
            line(tip + new Vector2(-arm.X, arm.Y), tip, tint, 1.3f);
            line(tip, tip + arm, tint, 1.3f);
        }
    }
}

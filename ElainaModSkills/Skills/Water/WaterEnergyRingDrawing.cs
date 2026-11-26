using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

internal static class WaterEnergyRingDrawing
{
    internal static float SizeMultiplier => 1.25f;
    // X is the maximum sweep, Y is the clockwise offset from twelve o'clock.
    internal static Vector2 ArcDegrees => new(130f, 25f);
    internal static float Curvature => .65f; // 0 = straight, 1 = original circular arc.
    internal static Vector2 CursorOffset => new(9f, 0f); // UI pixels, positive right/down.
    private static float CursorPadding => 5f;
    private static float HitCrestStrength => .36f;
    private static float SpendCrestStrength => .85f;
    internal static float BaseWidth => 2.45f;
    private static float PeakWidth => 4.5f;
    private static float HeldWidth => 3.35f;
    private static float RiseSeconds => .10f;
    private static float SettleSeconds => .24f;

    internal static float FullWidth(float age) => age < RiseSeconds
        ? MathHelper.SmoothStep(BaseWidth, PeakWidth, age / RiseSeconds)
        : MathHelper.SmoothStep(PeakWidth, HeldWidth, (age - RiseSeconds) / SettleSeconds);

    internal static (Vector2 Center, float Scale) AroundCursor(Vector2 topLeft, Vector2 glyphSize, float uiScale, Vector2 cursorOffset)
    {
        Vector2 halfSize = glyphSize * .5f;
        float clearRadius = halfSize.Length() + CursorPadding * uiScale;
        return (topLeft + halfSize + cursorOffset * uiScale, Math.Max(SizeMultiplier * uiScale, clearRadius / 13f));
    }

    internal static void Draw(SpriteBatch batch, Texture2D water, Effect effect, Vector2 center,
        Vector2 arcDegrees, float curvature, float scale, float fill, float phase, float surge, bool full, float visibility = 1f, float fullAge = 0f)
    {
        float fraction = full ? 1f : fill;
        effect.CurrentTechnique = effect.Techniques[full ? "FullEnergyRing" : "WaterEnergyRing"];
        effect.Parameters["RingWidth"].SetValue(full ? FullWidth(fullAge) : BaseWidth);
        float crest = surge < 0 ? SpendCrestStrength : surge * HitCrestStrength;
        float crestPosition = surge < 0 ? fraction : fraction * (1f - surge);
        effect.Parameters["RingState"].SetValue(new Vector4(fraction, phase, crest, crestPosition));
        float extent = Math.Clamp(arcDegrees.X / 360f, 0f, 1f);
        float bend = Math.Clamp(curvature, 0f, 1f);
        // Bound the distance from the original centre to either end of the straightened strip.
        float side = bend == 1f ? 64f : Math.Max(64f, MathF.Ceiling(50f + MathHelper.TwoPi * 20f * extent));
        effect.Parameters["ArcGeometry"].SetValue(new Vector4(extent, arcDegrees.Y / 360f, bend, side));
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
            DepthStencilState.None, RasterizerState.CullNone, effect);
        batch.Draw(water, center, null, Color.White * visibility, 0, new Vector2(water.Width, water.Height) * .5f,
            new Vector2(side * scale / water.Width, side * scale / water.Height), SpriteEffects.None, 0);
        batch.End();
    }
}

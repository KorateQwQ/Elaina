using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace 伊蕾娜.ElainaAttribute;

// Shared verbatim by the game and the FNA preview. Coordinates are logical UI pixels relative to the portrait centre.
// Layer order and constants follow drawPortraitHud in Tools/ResourceBarPreview/bars.html.
internal sealed class PortraitHudDrawing
{
    public const float Radius = 25, Band = 27.6f, Outer = 31.6f;
    // Centre relative to the life bar origin (HTML: PR = {x:-45, y:37} against a bar top of 18).
    public static readonly Vector2 Offset = new(-45, 19);
    private const float LevelSize = 9, LevelY = 7.5f, FlourishGap = 1.8f, FontEm = 48;
    // Canvas "middle" baseline sits this far below the top of a DynamicSpriteFont line, in em units.
    private const float TextMiddle = .695f;
    private static readonly Color LevelInk = new(238, 233, 245), CappedInk = new(207, 200, 218), Outline = new(12, 9, 23);
    private readonly Layer @base, rim, fill, glow, capped, mask, pulse, glint, flourish;
    private readonly Texture2D nebula, star;
    private readonly DynamicSpriteFont font;

    private sealed class Layer
    {
        public readonly Texture2D[] Lods;
        public readonly Vector2 Size, Origin;
        public Layer(Func<string, Texture2D> texture, string name, float width, float height, float x, float y, bool lod)
        {
            var full = texture(name);
            Lods = lod ? [texture(name + "S1"), texture(name + "S15"), texture(name + "S2"), full] : [full, full, full, full];
            Size = new Vector2(width, height); Origin = new Vector2(x, y);
        }
    }

    public PortraitHudDrawing(Func<string, Texture2D> texture, DynamicSpriteFont font)
    {
        this.font = font;
        // Boxes match PORTRAIT in Tools/ResourceBarPreview/bake.cjs (also recorded in manifest.json).
        @base = new(texture, "PortraitBase", 66, 66, 33, 33, true);
        rim = new(texture, "PortraitRim", 66, 72, 33, 39, true);
        fill = new(texture, "PortraitXp", 62, 62, 31, 31, true);
        glow = new(texture, "PortraitXpGlow", 62, 62, 31, 31, true);
        capped = new(texture, "PortraitXpCapped", 62, 62, 31, 31, true);
        mask = new(texture, "PortraitXpMask", 62, 62, 31, 31, true);
        pulse = new(texture, "PortraitPulse", 64, 64, 32, 32, false);
        glint = new(texture, "PortraitGlint", 6, 6, 3, 3, false);
        flourish = new(texture, "LevelFlourish", 13, 10, 1.5f, 5, true);
        nebula = texture("Nebula"); star = texture("Star");
    }

    public static Vector2 Position(float logicalScreenWidth) => ResourceBarDrawing.LifePosition(logicalScreenWidth) + Offset;
    public static bool Contains(Vector2 centre, Vector2 point) => Math.Abs(point.X - centre.X) + Math.Abs(point.Y - centre.Y) <= Outer;
    public static float LowLifePulse(float time) => .28f + .24f * MathF.Sin(time * 6);

    // Caller must End its batch first. This method owns its Begin/End pair and never switches targets.
    public void Draw(SpriteBatch batch, Matrix ui, Vector2 centre, PortraitHudState state, float lifeFraction, float time)
    {
        float scale = new Vector2(ui.M11, ui.M12).Length();
        int lod = scale <= 1.25f ? 0 : scale <= 1.75f ? 1 : scale <= 2.5f ? 2 : 3;
        float frac = state.Fraction;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, ui);
        Draw(batch, @base, lod, centre, Color.White);
        if (lifeFraction < .25f) Draw(batch, pulse, lod, centre, new Color(255, 122, 172) * LowLifePulse(time));
        if (frac > 0)
        {
            // Both arms rise to the same height, so a horizontal cut of the full band reveals the progress.
            float cut = frac >= 1 ? float.NegativeInfinity : Band * (1 - 2 * frac);
            if (!state.Capped) Draw(batch, glow, lod, centre, new Color(255, 255, 255, 0), cut);
            Draw(batch, state.Capped ? capped : fill, lod, centre, Color.White, cut);
            if (!state.Capped && frac < 1)
                foreach (int side in new[] { 1, -1 })
                    Draw(batch, glint, lod, centre + PortraitHudState.ArmPoint(Band, frac, side), Color.White);
        }
        if (state.Flash > 0) Draw(batch, mask, lod, centre, Color.White * (state.Flash * .85f));
        Draw(batch, rim, lod, centre, Color.White);
        Level(batch, lod, centre + new Vector2(0, Outer), state);
        foreach (var s in state.Sparks)
        {
            float age = s.Age / s.Duration;
            float alpha = Math.Clamp(age < .15f ? age / .15f : 1 - (age - .15f) / .85f, 0, 1);
            batch.Draw(star, centre + s.Position, null, new Color(238, 230, 255, 0) * alpha, 0, new Vector2(32),
                new Vector2(s.Size, s.Size * 1.4f) / 24, SpriteEffects.None, 0);
        }
        batch.End();
    }

    private void Level(SpriteBatch batch, int lod, Vector2 anchor, PortraitHudState state)
    {
        float k = 1 + state.Pulse * .18f;
        if (state.Pulse > 0)
            batch.Draw(nebula, anchor + new Vector2(0, 3) * k, null, new Color(214, 196, 255) * (.5f * state.Pulse), 0,
                new Vector2(64), 16f / 64 * k, SpriteEffects.None, 0);
        string text = state.Shown.ToString();
        float size = LevelSize / FontEm, width = font.MeasureString(text).X * size, e = width / 2 + FlourishGap;
        Draw(batch, flourish, lod, anchor + new Vector2(e, LevelY) * k, Color.White, scale: k);
        Draw(batch, flourish, lod, anchor + new Vector2(-e, LevelY) * k, Color.White, scale: k, flip: true);
        var topLeft = anchor + new Vector2(-width / 2, LevelY + .4f - TextMiddle * LevelSize) * k;
        // Canvas strokes a 1.5px outline under the fill; eight offsets reproduce it without a shader.
        for (int i = 0; i < 8; i++)
        {
            var offset = new Vector2(MathF.Cos(i * MathF.PI / 4), MathF.Sin(i * MathF.PI / 4)) * .75f * k;
            batch.DrawString(font, text, topLeft + offset, Outline * .9f, 0, Vector2.Zero, size * k, SpriteEffects.None, 0);
        }
        batch.DrawString(font, text, topLeft, state.Capped ? CappedInk : LevelInk, 0, Vector2.Zero, size * k, SpriteEffects.None, 0);
    }

    // Draws a baked layer whose logical origin lands on anchor; rows above cut (logical, layer-relative) are skipped.
    private static void Draw(SpriteBatch batch, Layer layer, int lod, Vector2 anchor, Color color,
        float cut = float.NegativeInfinity, float scale = 1, bool flip = false)
    {
        var texture = layer.Lods[lod];
        var texel = new Vector2(texture.Width / layer.Size.X, texture.Height / layer.Size.Y);
        int row = cut > -layer.Origin.Y ? (int)MathF.Floor((cut + layer.Origin.Y) * texel.Y) : 0;
        if (row >= texture.Height) return;
        float left = flip ? -(layer.Size.X - layer.Origin.X) : -layer.Origin.X;
        var position = anchor + new Vector2(left, row / texel.Y - layer.Origin.Y) * scale;
        batch.Draw(texture, position, new Rectangle(0, row, texture.Width, texture.Height - row), color, 0, Vector2.Zero,
            new Vector2(scale / texel.X, scale / texel.Y), flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
    }
}

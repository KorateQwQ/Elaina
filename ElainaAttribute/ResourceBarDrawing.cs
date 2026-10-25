using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaAttribute;

// Shared verbatim by the game and the FNA preview. All coordinates are logical UI pixels.
internal sealed class ResourceBarDrawing
{
    public const float Length = 262;
    public const float LifeHeight = 8;
    public const float ManaHeight = 7;
    public const float Gap = 28;
    private const float Tau = MathF.PI * 2;
    private readonly Effect clip;
    private readonly Texture2D pixel, line, petal, star, dot, nebula;
    private readonly Art life, mana;
    private readonly PetalSeed[] petals = new PetalSeed[26];
    private readonly StarSeed[] stars = new StarSeed[70];
    private readonly NebulaSeed[] clouds = new NebulaSeed[5];
    private readonly (int A, int B)[] links = new (int, int)[6];

    private sealed class Art
    {
        public readonly Texture2D[] Frames = new Texture2D[4], Slots = new Texture2D[4];
        public readonly Texture2D Base, Shade, Head;
        public Art(Func<string, Texture2D> texture, string prefix)
        {
            string[] suffixes = ["S1", "S15", "S2", ""];
            for (int i = 0; i < suffixes.Length; i++)
            { Frames[i] = texture(prefix + "Frame" + suffixes[i]); Slots[i] = texture(prefix + "Slot" + suffixes[i]); }
            Base = texture(prefix + "Base"); Shade = texture(prefix + "Shade"); Head = texture(prefix + "Head");
        }
    }
    private readonly record struct PetalSeed(float X, float Y, float Size, float Rotation, float Spin, float Alpha);
    private readonly record struct StarSeed(float X, float Y, float Size, float Phase, bool Big);
    private readonly record struct NebulaSeed(float X, float Y, float Radius);
    private sealed class Seed
    {
        private long value;
        public Seed(int value) { this.value = value; }
        public float Next() { value = value * 16807 % 2147483647; return (float)(value / 2147483647.0); }
    }

    public ResourceBarDrawing(Func<string, Texture2D> texture, Effect clip)
    {
        this.clip = clip;
        life = new Art(texture, "Life"); mana = new Art(texture, "Mana");
        pixel = texture("Pixel"); line = texture("Line"); petal = texture("Petal");
        star = texture("Star"); dot = texture("Dot"); nebula = texture("Nebula");
        var r = new Seed(17);
        for (int i = 0; i < petals.Length; i++)
            petals[i] = new(r.Next() * (Length + 30), r.Next(), 1.4f + r.Next() * 1.6f,
                r.Next() * Tau, (r.Next() - .5f) * .6f, .16f + r.Next() * .3f);
        r = new Seed(29);
        for (int i = 0; i < stars.Length; i++)
            stars[i] = new(r.Next() * (Length + 40), r.Next(), .35f + r.Next() * r.Next() * .9f,
                r.Next() * Tau, r.Next() < .1f);
        for (int i = 0; i < clouds.Length; i++)
        {
            clouds[i] = new(r.Next() * (Length + 40), .2f + r.Next() * .6f, 10 + r.Next() * 18);
            r.Next(); // The HTML seed consumes a color choice even though the palette overrides it.
        }
        for (int i = 0; i < links.Length; i++)
        {
            int a = (int)(r.Next() * stars.Length), b = -1;
            for (int j = 0; j < stars.Length; j++)
                if (j != a && Math.Abs(stars[j].X - stars[a].X) < 22) { b = j; break; }
            links[i] = (a, b);
        }
    }

    public static Vector2 LifePosition(float logicalScreenWidth) => new(logicalScreenWidth - 16 - 20 - Length, 18);
    public static Vector2 ManaPosition(float logicalScreenWidth) => LifePosition(logicalScreenWidth) + new Vector2(0, Gap);

    // Caller must End its batch first. This method owns its Begin/End pairs and never switches targets.
    public void Draw(SpriteBatch batch, Matrix ui, Vector2 origin, bool isMana, float time, ResourceBarState state)
    {
        float h = isMana ? ManaHeight : LifeHeight;
        var art = isMana ? mana : life;
        float scale = new Vector2(ui.M11, ui.M12).Length();
        int lod = scale <= 1.25f ? 0 : scale <= 1.75f ? 1 : scale <= 2.5f ? 2 : 3;
        var frame = art.Frames[lod]; var slot = art.Slots[lod];
        float width = Math.Clamp(state.Value, 0, 1) * Length;
        float ghost = Math.Clamp(state.Ghost, state.Value, 1) * Length;
        Begin(batch, ui);
        batch.Draw(slot, origin - new Vector2(32, 16), null, Color.White, 0, Vector2.Zero, 322f / slot.Width, SpriteEffects.None, 0);
        batch.End();

        BeginClipped(batch, ui, origin, Length, h);
        Material(batch, origin, art, isMana, time, Length, h, .13f);
        Rect(batch, pixel, origin, Length, h, new Color(12, 8, 20) * .35f);
        batch.End();
        if (ghost > width)
        {
            BeginClipped(batch, ui, origin, ghost, h);
            // Darker and more saturated than the fill, brighter than the empty slot, so the real value edge stays readable.
            Rect(batch, pixel, origin, ghost, h, isMana ? new Color(132, 112, 236) * .9f : new Color(160, 52, 92) * .85f);
            batch.End();
        }
        if (width > 0)
        {
            BeginClipped(batch, ui, origin, width, h);
            Material(batch, origin, art, isMana, time, width, h, 1);
                        batch.End();
        }
        Begin(batch, ui);
        batch.Draw(frame, origin - new Vector2(32, 16), null, Color.White, 0, Vector2.Zero, 322f / frame.Width, SpriteEffects.None, 0);
        foreach (var p in state.Particles)
        {
            float age = p.Age / p.Duration;
            float alpha = Math.Clamp(age < .15f ? age / .15f : 1 - (age - .15f) / .85f, 0, 1);
            var color = p.Star ? new Color(238, 239, 255) : new Color(245, 198, 221);
            if (p.Star) color.A = 0; // HTML particles use lighter blending; material stars remain alpha blended.
            Sprite(batch, p.Star ? star : petal, origin + p.Position, p.Size, p.Star ? p.Size * 1.4f : p.Size,
                p.Star ? 0 : p.Rotation, color * alpha);
        }
        batch.End();
    }

    private static void Begin(SpriteBatch batch, Matrix ui, Effect effect = null) =>
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, effect, ui);

    internal static Matrix ClipTransform(Matrix ui, int width, int height) =>
        ui * Matrix.CreateOrthographicOffCenter(0, width, height, 0, 0, -1);

    private void BeginClipped(SpriteBatch batch, Matrix ui, Vector2 origin, float width, float height)
    {
        var viewport = batch.GraphicsDevice.Viewport;
        // Match FNA SpriteEffect exactly: it does not apply a D3D9 half-pixel offset.
        // The baked frame uses that SpriteEffect, so a separate offset would shift only the fill.
        clip.Parameters["MatrixTransform"].SetValue(ClipTransform(ui, viewport.Width, viewport.Height));
        clip.Parameters["BarOrigin"].SetValue(origin);
        clip.Parameters["BarSize"].SetValue(new Vector2(width, height));
        Begin(batch, ui, clip);
    }

    private void Material(SpriteBatch batch, Vector2 origin, Art art, bool isMana, float time, float width, float h, float opacity)
    {
        // Base stays anchored to the full 262px length; it must not stretch with the current resource value.
        Rect(batch, art.Base, origin, Length, h, Color.White * opacity);
        if (isMana)
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                var n = clouds[i];
                var position = origin + new Vector2(Wrap(n.X - time * 2, Length + 40) - 20, n.Y * h);
                var color = (i % 2 == 0 ? new Color(186, 156, 255) : new Color(140, 162, 255)) * (.3f * opacity);
                batch.Draw(nebula, position, null, color, 0, new Vector2(64), n.Radius / 64, SpriteEffects.None, 0);
            }
            Vector2 At(StarSeed s) => new(Wrap(s.X - time * 4, Length + 40) - 20, s.Y * h);
            foreach (var (a, b) in links)
            {
                if (b < 0) continue;
                var from = At(stars[a]); var to = At(stars[b]);
                if (Math.Abs(from.X - to.X) <= 30)
                    Line(batch, origin + from, origin + to, .4f, new Color(240, 241, 255) * (.22f * opacity));
            }
            foreach (var s in stars)
            {
                var position = At(s);
                if (position.X > width + 2) continue;
                float alpha = .45f + .55f * Math.Max(0, MathF.Sin(time * 2 + s.Phase));
                Sprite(batch, s.Big ? star : dot, origin + position, s.Big ? 1.6f : s.Size, s.Big ? 1.6f : s.Size,
                    0, (s.Big ? Color.White : new Color(240, 241, 255)) * (alpha * opacity));
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 At(float x) => origin + new Vector2(x, h * (.24f + .26f * i) + MathF.Sin(x * .05f + time * (.7f + .2f * i) + i * 2.1f) * h * .16f);
                for (float x = 2; x <= width; x += 2)
                    Line(batch, At(x - 2), At(x), .6f, new Color(255, 220, 237) * ((.16f + .08f * i) * opacity));
            }
            foreach (var q in petals)
            {
                float x = Wrap(q.X - time * 6, Length + 30) - 14;
                if (x > width + 4) continue;
                Sprite(batch, petal, origin + new Vector2(x, q.Y * h), q.Size, q.Size, q.Rotation + time * q.Spin,
                    new Color(255, 230, 243) * (q.Alpha * opacity));
            }
        }
        Rect(batch, art.Shade, origin, width, h, Color.White * opacity);
        Rect(batch, art.Head, origin + new Vector2(width - 40, 0), 40, h, Color.White * opacity);
    }

    private static float Wrap(float x, float length) => (x % length + length) % length;
    private static void Rect(SpriteBatch batch, Texture2D texture, Vector2 origin, float width, float height, Color color) =>
        batch.Draw(texture, origin, null, color, 0, Vector2.Zero, new Vector2(width / texture.Width, height / texture.Height), SpriteEffects.None, 0);
    private static void Sprite(SpriteBatch batch, Texture2D texture, Vector2 center, float rx, float ry, float rotation, Color color) =>
        batch.Draw(texture, center, null, color, rotation, new Vector2(32), new Vector2(rx, ry) / 24, SpriteEffects.None, 0);
    private void Line(SpriteBatch batch, Vector2 a, Vector2 b, float width, Color color)
    {
        var delta = b - a;
        batch.Draw(line, a, null, color, MathF.Atan2(delta.Y, delta.X), new Vector2(0, 8),
            new Vector2(delta.Length() / 8, width / 4), SpriteEffects.None, 0);
    }
}

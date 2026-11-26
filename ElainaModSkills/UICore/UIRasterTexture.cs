using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.UICore;

internal sealed class UIRasterTexture(Texture2D master, Func<int, Texture2D> loadLevel)
{
    private readonly Texture2D[] _levels = new Texture2D[6];
    private static readonly float[] Factors = [.25f, .375f, .5f, .625f, .75f, 1];

    internal void Draw(SpriteBatch batch, Vector2 position, Rectangle? region, Color color, float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects flip, float depth)
    {
        float requested = Math.Max(scale.X, scale.Y) * UIBatch.Scale(batch);
        int level = 0;
        while (level < Factors.Length - 1 && requested > MathF.Sqrt(Factors[level] * Factors[level + 1])) level++;
        var texture = level == Factors.Length - 1 ? master : _levels[level] ??= loadLevel(level);
        Vector2 ratio = new(texture.Width / (float)master.Width, texture.Height / (float)master.Height);
        Rectangle source = region ?? new Rectangle(0, 0, master.Width, master.Height);
        int x = (int)MathF.Round(source.X * ratio.X), y = (int)MathF.Round(source.Y * ratio.Y);
        var rect = new Rectangle(x, y, Math.Max(1, (int)MathF.Round(source.Right * ratio.X) - x),
            Math.Max(1, (int)MathF.Round(source.Bottom * ratio.Y) - y));
        // Tiny line-strip regions retain their original padded source; the art tiles use LODs.
        if (source.Width < 16 || source.Height < 16) { texture = master; rect = source; ratio = Vector2.One; }
        var saved = UIBatch.Capture(batch);
        bool changeSampler = saved.Sampler != SamplerState.LinearClamp;
        if (changeSampler) { batch.End(); saved.Begin(batch, sampler: SamplerState.LinearClamp); }
        batch.Draw(texture, position, rect, color, rotation, origin * ratio, scale / ratio, flip, depth);
        if (changeSampler) { batch.End(); saved.Begin(batch); }
    }
}

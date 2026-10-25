using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

/// <summary>
/// 灰赝尘提示文字的扫光与星点。只画字身与星点，黑影由调用方按原版提示方式绘制；
/// 不依赖 Terraria 状态，离线预览直接复用。星点用技能星图同款四芒星（炼金手记 NotebookStars 第一行）
/// 叠一层 Glow 柔光。
/// </summary>
internal static class FacsimileTooltipEffect
{
    private const float SweepPeriod = 2f;
    private const float SparklePeriod = 1f;
    private const int SparkleCount = 3;
    private static readonly Color HaloColor = new(214, 190, 250), StarColor = new(244, 238, 255);

    /// <param name="stars">NotebookStars 图集：64/32/16/8/4 预过滤，长轴占格子 112/128。</param>
    /// <param name="pixelScale">每个 UI 单位对应的屏幕像素，用于挑选预过滤层级。</param>
    /// <param name="phase">0~1 的错相，让同一提示里的多行不同步扫光。</param>
    internal static void Draw(SpriteBatch batch, DynamicSpriteFont font, Texture2D stars, Texture2D glow, string text,
        Vector2 position, Color color, Vector2 scale, float pixelScale, float time, float phase)
    {
        float width = font.MeasureString(text).X * scale.X;
        // 两秒一次，从行首前一段扫到行尾后一段。
        float sweep = ((time / SweepPeriod + phase) % 1f) * (width + 160 * scale.X) - 80 * scale.X;
        float x = 0;
        for (int i = 0; i < text.Length; i++)
        {
            string glyph = text[i].ToString();
            float band = Band(x + 8 * scale.X, sweep, 48 * scale.X);
            Color glyphColor = Color.Lerp(color, Color.White * (color.A / 255f), band);
            batch.DrawString(font, glyph, position + new Vector2(x, 0), glyphColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            x = font.MeasureString(text[..(i + 1)]).X * scale.X;
        }

        float alpha = color.A / 255f;
        for (int s = 0; s < SparkleCount; s++)
        {
            // 每颗星一秒闪一次，每次换到文字上的另一个位置。
            float t = time / SparklePeriod + phase + s / (float)SparkleCount;
            int cycle = (int)MathF.Floor(t);
            float twinkle = MathF.Sin((t - cycle) * MathF.PI);
            twinkle *= twinkle * alpha;
            float u = Hash(cycle, s, phase);
            float v = Hash(cycle, s + 17, phase);
            var center = position + new Vector2(4 + (width - 8) * u, (2 + 18 * v) * scale.Y);
            Sparkle(batch, stars, glow, center, twinkle, pixelScale, (1 - twinkle) * 0.5f);
        }
    }

    private static float Band(float x, float center, float radius)
    {
        float d = MathF.Abs(x - center) / radius;
        return d >= 1 ? 0 : (1 - d * d) * 0.95f;
    }

    private static float Hash(int cycle, int star, float phase)
    {
        uint h = (uint)(cycle * 73856093) ^ (uint)(star * 19349663) ^ (uint)(phase * 1000) * 83492791u;
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return (h & 0xffff) / 65535f;
    }

    private static void Sparkle(SpriteBatch batch, Texture2D stars, Texture2D glow, Vector2 center, float strength,
        float pixelScale, float rotation)
    {
        if (strength <= 0.01f)
            return;
        float size = 6 + 9 * strength;
        float halo = size * 1.1f;
        batch.Draw(glow, center, null, HaloColor * (0.6f * strength), 0f, new Vector2(glow.Width, glow.Height) / 2,
            halo * 2 / glow.Width, SpriteEffects.None, 0f);

        int side = 64, x = 0;
        float extent = size * 128 / 112;
        while (side > 4 && extent * pixelScale < side * .7071f) { x += side; side /= 2; }
        batch.Draw(stars, center, new Rectangle(x, 0, side, side), StarColor * strength, rotation, new Vector2(side / 2f),
            extent / side, SpriteEffects.None, 0f);
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

/// <summary>共享实际绘制的小型生长箭头；不分配纹理，不切换 SpriteBatch。</summary>
internal static class RainPlantIndicatorDrawing
{
    private static readonly Rectangle Pixel = new(0, 0, 1, 1);

    public static void Draw(SpriteBatch batch, Texture2D pixel, Vector2 plantTop, float ticks, float opacity, int seed)
    {
        float phase = seed * .73f;
        float bob = MathF.Sin(ticks * .045f + phase) * 1.5f;
        Vector2 center = new(MathF.Round(plantTop.X), MathF.Round(plantTop.Y - 10 - bob));
        float strength = MathHelper.Clamp(opacity, 0, 1) * (.84f + MathF.Sin(ticks * .055f + phase) * .12f);
        if (strength <= 0) return;
        Color outline = new Color(25, 44, 57) * (.90f * strength);
        Color fill = new Color(122, 232, 189) * strength;
        Color highlight = new Color(219, 255, 239) * strength;

        // 深色外轮廓在日间和雨幕上保留可读性；9×11 左右的尺寸不遮住植物。
        Rect(batch, pixel, center, -1, -6, 3, 3, outline);
        Rect(batch, pixel, center, -2, -5, 5, 3, outline);
        Rect(batch, pixel, center, -3, -4, 7, 3, outline);
        Rect(batch, pixel, center, -4, -3, 9, 3, outline);
        Rect(batch, pixel, center, -2, -1, 5, 6, outline);
        Rect(batch, pixel, center, 0, -5, 1, 1, highlight);
        Rect(batch, pixel, center, -1, -4, 3, 1, fill);
        Rect(batch, pixel, center, -2, -3, 5, 1, fill);
        Rect(batch, pixel, center, -3, -2, 7, 1, fill);
        Rect(batch, pixel, center, -1, -1, 3, 5, fill);
        Rect(batch, pixel, center, 0, -4, 1, 7, highlight * .65f);
    }

    private static void Rect(SpriteBatch batch, Texture2D pixel, Vector2 center, int x, int y, int width, int height, Color tint)
        => batch.Draw(pixel, new Rectangle((int)center.X + x, (int)center.Y + y, width, height), Pixel, tint);
}

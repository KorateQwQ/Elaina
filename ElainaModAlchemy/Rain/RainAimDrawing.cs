using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

internal readonly record struct RainAimPlant(Vector2 Anchor, bool InPlanter);

/// <summary>世界坐标预览；复用已有白像素，范围与折线随当前地形变化。</summary>
internal static class RainAimDrawing
{
    private static readonly Rectangle Pixel = new(0, 0, 1, 1);
    private static readonly Vector2[] LandingCorners = [new(0, -7), new(7, 0), new(0, 7), new(-7, 0)];

    internal static void Draw(SpriteBatch batch, Texture2D pixel, Vector2 camera,
        IReadOnlyList<Vector2> path, Vector2 impact, Vector2 cloud, float[] lengths,
        IReadOnlyList<RainAimPlant> plants)
    {
        Color blue = new(133, 211, 248);
        float startY = cloud.Y + 16;
        float left = MathF.Floor((cloud.X - RainCloudDrawing.Width * .5f + 8) / 16) * 16;
        for (int i = 0; i < lengths.Length; i++)
        {
            float x = left + i * 16;
            float bottom = startY + lengths[i];
            Fill(batch, pixel, new Vector2(x, startY) - camera, new Vector2(16, lengths[i]), blue * .065f);
            Line(batch, pixel, new Vector2(x, bottom) - camera, new Vector2(x + 16, bottom) - camera, 2, blue * .75f);
            if (i > 0)
                Line(batch, pixel, new Vector2(x, startY + lengths[i - 1]) - camera,
                    new Vector2(x, bottom) - camera, 1, blue * .55f);
        }
        Line(batch, pixel, new Vector2(left, startY) - camera,
            new Vector2(left + lengths.Length * 16, startY) - camera, 1, blue * .65f);
        Dashes(batch, pixel, new Vector2(left, startY) - camera,
            new Vector2(left, startY + lengths[0]) - camera, blue * .7f);
        Dashes(batch, pixel, new Vector2(left + lengths.Length * 16, startY) - camera,
            new Vector2(left + lengths.Length * 16, startY + lengths[^1]) - camera, blue * .7f);

        for (int i = 1; i < path.Count; i += 2)
            Line(batch, pixel, path[i - 1] - camera, path[i] - camera, 2, blue * .75f);
        Dashes(batch, pixel, impact - camera, cloud - camera, blue * .4f);
        Vector2 landing = impact - camera;
        Color gold = new(255, 222, 150);
        // 空心菱形保留鼠标下方的物块可见性。
        for (int i = 0; i < LandingCorners.Length; i++)
        {
            Line(batch, pixel, landing + LandingCorners[i], landing + LandingCorners[(i + 1) % 4], 4, new Color(35, 43, 57));
            Line(batch, pixel, landing + LandingCorners[i], landing + LandingCorners[(i + 1) % 4], 2, gold);
        }
        foreach (RainAimPlant plant in plants)
        {
            Vector2 top = plant.Anchor - camera;
            Color green = new(142, 245, 190);
            float bottom = plant.InPlanter ? 32 : 18;
            // 种植盆/黏土盆与其中的植物一起框出；空盆不生成标记。
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 a = top + new Vector2(side * 10, -2);
                Vector2 b = top + new Vector2(side * 10, bottom);
                Line(batch, pixel, a, a + new Vector2(-side * 5, 0), 2, green);
                Line(batch, pixel, a, a + new Vector2(0, 5), 2, green);
                Line(batch, pixel, b, b + new Vector2(-side * 5, 0), 2, green);
                Line(batch, pixel, b, b - new Vector2(0, 5), 2, green);
            }
            RainPlantIndicatorDrawing.Draw(batch, pixel, top, 0, 1, 0);
        }
    }

    private static void Dashes(SpriteBatch batch, Texture2D pixel, Vector2 from, Vector2 to, Color tint)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length < .01f) return;
        Vector2 direction = delta / length;
        for (float at = 0; at < length; at += 10)
            Line(batch, pixel, from + direction * at, from + direction * Math.Min(at + 4, length), 1, tint);
    }

    private static void Fill(SpriteBatch batch, Texture2D pixel, Vector2 at, Vector2 size, Color tint)
    {
        if (size.X <= 0 || size.Y <= 0) return;
        batch.Draw(pixel, at, Pixel, tint, 0, Vector2.Zero, size, SpriteEffects.None, 0);
    }

    private static void Line(SpriteBatch batch, Texture2D pixel, Vector2 from, Vector2 to, float width, Color tint)
    {
        Vector2 delta = to - from;
        if (delta.LengthSquared() < .0001f) return;
        batch.Draw(pixel, from, Pixel, tint, MathF.Atan2(delta.Y, delta.X), new Vector2(0, .5f),
            new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
}

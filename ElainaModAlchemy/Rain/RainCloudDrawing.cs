using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

/// <summary>实际云层、雨丝、水花和开瓶光尘；离线 FNA 直接链接此源文件。</summary>
internal static class RainCloudDrawing
{
    public const int Duration = 60 * 60;
    public const int Columns = 20;
    public const int Width = Columns * 16;
    public const int MaxFall = 320;
    private static readonly Rectangle Pixel = new(0, 0, 1, 1);

    public static void Draw(SpriteBatch batch, Texture2D pixel, Texture2D cloud, Vector2 center,
        float age, float[] lengths, int seed = 0, float releaseHeight = 96)
    {
        float appear = MathHelper.Clamp(age / 30, 0, 1);
        float fade = appear * MathHelper.Clamp((Duration - age) / 60, 0, 1);
        float rainFade = (cloud == null
            ? MathHelper.SmoothStep(0, 1, MathHelper.Clamp((age - 54) / 72, 0, 1))
            : MathHelper.Clamp((age - 30) / 20, 0, 1)) * fade;
        // 固定种子保证暂停、不同绘制帧率与多人画面不产生随机跳闪。
        for (int i = 0; i < 110; i++)
        {
            float random = Fraction(MathF.Sin(i * 127.1f + seed * .17f) * 43758.547f);
            float x = -Width * .5f + 3 + random * (Width - 6);
            int column = Math.Clamp((int)((x + Width * .5f) / 16), 0, Columns - 1);
            float fall = lengths[column];
            if (fall < 6) continue;
            float phase = Fraction((age * (5 + random * 3) + i * 47) / (fall + 26));
            float y = phase * (fall + 26);
            float streak = 5 + random * 6;
            Vector2 start = center + new Vector2(x, 16 + Math.Min(y, fall));
            if (y < fall)
            {
                float lineLength = Math.Min(streak, fall - y);
                Segment(batch, pixel, start, start + new Vector2(-lineLength * .12f, lineLength),
                    1, new Color(130, 183, 225) * ((.34f + random * .28f) * rainFade));
                if (i % 4 == 0)
                    Segment(batch, pixel, start, start + new Vector2(-.5f, 3), 1,
                        new Color(160, 191, 255, 0) * (.32f * rainFade));
            }
            else if (fall < MaxFall && i % 2 == 0)
            {
                float t = (y - fall) / 26;
                float spread = 1 + t * 5;
                Vector2 splash = start - new Vector2(0, MathF.Sin(t * MathHelper.Pi) * 3);
                Color color = new Color(170, 214, 244) * ((1 - t) * .65f * rainFade);
                Segment(batch, pixel, splash + new Vector2(-spread - 2, 0), splash + new Vector2(-spread, -1), 1, color);
                Segment(batch, pixel, splash + new Vector2(spread, -1), splash + new Vector2(spread + 2, 0), 1, color);
            }
        }

        // 雨云使用原版云贴图，多团错层组成覆盖区；紫灰阴影与冰蓝高光保持小云的体积感。
        // 原版雨云贴图是六帧竖向图集（54×168），每次只取一帧。
        // Shader 启用时传 null，仅跳过旧云；原绘制保留用于回退与对照。
        if (cloud != null)
        {
            int frameHeight = cloud.Height / 6;
            Rectangle source = new(0, ((int)age / 9 % 6) * frameHeight, cloud.Width, frameHeight);
            Vector2 origin = new(cloud.Width * .5f, frameHeight * .5f);
            for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < 5; i++)
            {
                float wave = MathF.Sin(age * .025f + i * 1.7f);
                Vector2 position = center + new Vector2((i - 2) * 52 * (.30f + .70f * appear),
                    -8 - MathF.Sin(i * 1.9f) * 4 + wave * 1.5f + (1 - appear) * releaseHeight);
                Vector2 scale = new(1.65f + (i == 2 ? .15f : 0), 1.25f + (i % 2) * .20f);
                scale *= .65f + .35f * appear;
                Color color = layer == 0 ? new Color(125, 111, 174) * (.70f * fade) : new Color(209, 225, 244) * (.94f * fade);
                batch.Draw(cloud, position + new Vector2(0, layer == 0 ? 4 : -3), source, color, 0, origin,
                    scale * (layer == 0 ? 1.04f : 1), SpriteEffects.None, 0);
                if (layer == 1)
                    batch.Draw(cloud, position - new Vector2(0, 3), source, new Color(70, 116, 162, 0) * (.42f * fade),
                        0, origin, scale, SpriteEffects.None, 0);
            }

        }

        // 淡紫星点在云缘短暂闪烁，释放时先向外散开，随后只留下少量微光。
        for (int i = 0; i < 12; i++)
        {
            float phase = Fraction(age / 80 + i * .618f);
            float twinkle = MathF.Pow(MathF.Sin(phase * MathHelper.Pi), 6);
            Vector2 at = center + new Vector2((i - 5.5f) * 25, 20 + MathF.Sin(i * 3.1f) * 10);
            if (age < 30) at = center + new Vector2(0, releaseHeight)
                + new Vector2(MathF.Cos(i * 2.4f), MathF.Sin(i * 2.4f)) * age * 1.6f;
            Color tint = new Color(188, 180, 255, 0) * (.5f * twinkle * fade);
            Segment(batch, pixel, at - new Vector2(2, 0), at + new Vector2(2, 0), 1, tint);
            Segment(batch, pixel, at - new Vector2(0, 3), at + new Vector2(0, 3), 1, tint);
        }
        if (age < 24)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = i * 2.399963f;
                Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 shard = center + new Vector2(0, releaseHeight) + direction * age * 1.7f
                    + new Vector2(0, age * age * .025f);
                Segment(batch, pixel, shard, shard + direction * 3, 1.5f,
                    new Color(187, 219, 242) * ((1 - age / 24) * .75f));
            }
        }
    }

    private static float Fraction(float value) => value - MathF.Floor(value);
    private static void Segment(SpriteBatch batch, Texture2D pixel, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 delta = b - a;
        batch.Draw(pixel, a, Pixel, color, MathF.Atan2(delta.Y, delta.X), new Vector2(0, .5f),
            new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

internal readonly record struct IceShardPart(Vector2 Direction, float Speed, float Life, float Length,
    float Width, float Rotation, float Spin, float Phase, int Variant);

internal static class IceShardVisuals
{
    internal const int Duration = 51;
    internal const string AtlasPath = "伊蕾娜/ElainaModSkills/Skills/Ice/Assets/ShardAtlas";

    internal static IceShardPart[] Create(int seed, float direction)
    {
        var random = new Random(seed);
        // 保持已确认预览的随机序列；原型冷雾占用了前 15 次采样。
        for (int i = 0; i < 15; i++) random.NextDouble();
        var parts = new IceShardPart[27];
        ReadOnlySpan<float> mainAngles = [-2.6f, -1.6f, -.5f, .65f, 2f];
        for (int i = 0; i < parts.Length; i++)
        {
            bool main = i < 5, medium = i >= 5 && i < 9;
            float angle = (main ? mainAngles[i] + Next(random, -.3f, .3f) : Next(random, -MathF.PI, MathF.PI)) + direction;
            float speed = Next(random, main ? 110 : 70, main ? 240 : 275);
            float life = Next(random, main ? .55f : .28f, main ? .82f : .58f);
            float length = main ? Next(random, 7, 11) : medium ? Next(random, 3, 5) : Next(random, .5f, 1.1f);
            float width = length * Next(random, .65f, .90f);
            parts[i] = new IceShardPart(new Vector2(MathF.Cos(angle), MathF.Sin(angle)), speed, life,
                length, width, Next(random, -3, 3) + direction, Next(random, -5, 5), Next(random, -2, 2), i);
        }
        return parts;
    }

    internal static Vector2 Position(in IceShardPart part, float seconds)
    {
        bool main = part.Variant < 5;
        float drag = main ? .85f : 1.9f;
        float travel = (1 - MathF.Exp(-drag * seconds)) / drag;
        return part.Direction * part.Speed * travel + new Vector2(0, (main ? 85 : 22) * seconds * seconds);
    }

    internal static float Fade(in IceShardPart part, float seconds)
    {
        float t = Math.Clamp((seconds / part.Life - .48f) / .52f, 0, 1);
        return 1 - t * t * (3 - 2 * t);
    }

    private static float Next(Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

    private static readonly Rectangle GlowFrame = new(0, 64, 96, 96);
    private static readonly Rectangle LineFrame = new(104, 72, 16, 4);

    // 仅提交预制图集；调用者为整个请求组开启一次 SpriteBatch。
    internal static void Draw(SpriteBatch batch, Texture2D atlas, Vector2 center, IceShardPart[] parts, float ageTicks)
    {
        float age = ageTicks / 60f;
        foreach (ref readonly IceShardPart part in parts.AsSpan())
        {
            if (age < 0 || age >= part.Life) continue;
            Vector2 position = center + Position(part, age);
            float fade = Fade(part, age);
            if (part.Variant < 9)
            {
                int frame = part.Variant % 6;
                Rectangle source = new(frame % 3 * 32, frame / 3 * 32, 32, 32);
                batch.Draw(atlas, position, source, Color.White * fade, part.Rotation + part.Spin * age * .65f,
                    new Vector2(16), new Vector2(part.Length, part.Width) / 24f, SpriteEffects.None, 0);
                batch.Draw(atlas, position, GlowFrame, new Color(222, 240, 250) * (fade * .16f), 0,
                    new Vector2(48), part.Length * .18f / 6, SpriteEffects.None, 0);
            }
            else
            {
                float twinkle = .65f + .35f * MathF.Pow(MathF.Sin(part.Phase + age * 12), 2);
                batch.Draw(atlas, position, GlowFrame, new Color(218, 238, 250) * (fade * twinkle * .36f), 0,
                    new Vector2(48), part.Length * .085f, SpriteEffects.None, 0);
                batch.Draw(atlas, position, GlowFrame, new Color(248, 253, 255) * (fade * twinkle), 0,
                    new Vector2(48), part.Length * .024f, SpriteEffects.None, 0);
            }
            if (part.Variant >= 9 && part.Variant < 12 && age < .07f)
            {
                float length = 7 * (1 - age / .07f);
                batch.Draw(atlas, position - part.Direction * length * .5f, LineFrame,
                    new Color(239, 250, 255) * (.65f * (1 - age / .07f)), MathF.Atan2(part.Direction.Y, part.Direction.X),
                    new Vector2(8, 2), new Vector2(length / 16f, .65f / 2f), SpriteEffects.None, 0);
            }
        }
    }
}

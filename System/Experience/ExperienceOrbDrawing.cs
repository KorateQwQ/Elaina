using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.System.Experience;

/// <summary>客户端世界坐标尾迹；游戏与预览在移动前采样，不依赖相机或绘制帧率。</summary>
internal sealed class ExperienceOrbTrail
{
    private readonly Vector2[] positions = new Vector2[20];
    public int Count { get; private set; }
    public Vector2 this[int index] => positions[index];

    public void Record(Vector2 center)
    {
        // 网络纠正或瞬移时丢弃旧轨迹，避免跨屏连线。
        if (Count > 0 && Vector2.DistanceSquared(center, positions[0]) > 160 * 160)
            Count = 0;
        for (int i = Math.Min(Count, positions.Length - 1); i > 0; i--)
            positions[i] = positions[i - 1];
        positions[0] = center;
        Count = Math.Min(Count + 1, positions.Length);
    }
}

/// <summary>游戏和 FNA 离线预览共享绘制，不依赖额外贴图或 SpriteBatch 状态切换。</summary>
internal static class ExperienceOrbDrawing
{
    // Terraria 的 MagicPixel 是 1×1000，所有绘制只采样一个白色像素。
    private static readonly Rectangle WhitePixel = new(0, 0, 1, 1);

    public static void Draw(SpriteBatch batch, Texture2D pixel, Vector2 center,
        float age, ExperienceOrbTrail trail = null, Vector2 screenPosition = default)
    {
        float fade = MathHelper.Clamp(age / 8f, 0, 1);
        float pulse = 1f + MathF.Sin(age * .12f) * .09f;
        Vector2 head = center - screenPosition;
        if (trail != null)
        {
            Vector2 previous = center;
            float distance = 0;
            for (int i = 0; i < trail.Count; i++)
            {
                Vector2 point = trail[i];
                Vector2 delta = point - previous;
                float length = delta.Length();
                float remaining = 110f - distance;
                if (remaining <= 0) break;
                if (length > remaining) { point = previous + delta * (remaining / length); length = remaining; }
                if (length > .05f)
                {
                    float rotation = MathF.Atan2(delta.Y, delta.X);
                    int steps = Math.Max(1, (int)MathF.Ceiling(length / 3f));
                    for (int step = 0; step < steps; step++)
                    {
                        float t = (step + .5f) / steps;
                        float strength = MathF.Pow(Math.Max(0, 1 - (distance + length * t) / 110f), 1.3f)
                            * (1 - (i + t) / trail.Count);
                        Vector2 midpoint = Vector2.Lerp(previous, point, t) - screenPosition;
                        float segmentLength = length / steps + .15f;
                        // 有色底层让日间背景仍能看见紫色，外层使用既有 AlphaBlend 的加色柔光。
                        Line(batch, pixel, midpoint, segmentLength, 4 * strength + .3f, rotation,
                            new Color(100, 36, 190) * (.36f * strength * fade));
                        Line(batch, pixel, midpoint, segmentLength, 5.5f * strength + .3f, rotation,
                            new Color(108, 44, 226, 0) * (.22f * strength * fade));
                        Line(batch, pixel, midpoint, segmentLength, 1.8f * strength + .25f, rotation,
                            new Color(166, 91, 248, 0) * (.50f * strength * fade));
                    }
                }
                distance += length;

                // 出生 tick 固定碎星的方向，尾迹老化时向两侧散开，避免每帧随机跳动。
                int birth = (int)age - i;
                if (birth % 3 == 0 && i > 1)
                {
                    float seed = birth * 2.399963f;
                    Vector2 normal = length > .05f ? new Vector2(-delta.Y, delta.X) / delta.Length() : Vector2.UnitX;
                    Vector2 mote = point - screenPosition + normal * MathF.Sin(seed) * (3 + i * .65f);
                    float sparkle = (.65f + .35f * MathF.Sin(age * .23f + seed)) * (1 - i / (float)trail.Count) * fade;
                    Cross(batch, pixel, mote, 1.3f + (birth % 2) * .7f, 1.8f + (birth % 2), .7f,
                        new Color(191, 123, 255, 0) * sparkle, 0);
                }
                previous = point;
            }
        }

        // 四尖十字保留清晰轮廓；光晕也沿星芒收尖，避免叠成菱形光球。
        float tilt = MathF.Sin(age * .035f) * .10f;
        Cross(batch, pixel, head, 11 * pulse, 15 * pulse, 3.4f,
            new Color(112, 44, 236, 0) * (.25f * fade), tilt);
        Cross(batch, pixel, head, 8 * pulse, 11 * pulse, 1.7f,
            new Color(182, 107, 255, 0) * (.85f * fade), tilt);
        Cross(batch, pixel, head, 5.5f * pulse, 8 * pulse, .8f,
            new Color(235, 205, 255) * (.92f * fade), tilt);
        Line(batch, pixel, head, 2, 2, tilt, new Color(255, 240, 255) * fade);
    }

    private static void Cross(SpriteBatch batch, Texture2D pixel, Vector2 center,
        float horizontal, float vertical, float thickness, Color color, float rotation)
    {
        // 分层短条构成凹边尖芒；外层低亮、中心高亮，小尺寸依然是十字。
        const int bands = 5;
        for (int i = bands - 1; i >= 0; i--)
        {
            float t = i / (float)bands;
            float width = MathHelper.Lerp(thickness, .35f, t);
            float intensity = i == 0 ? .42f : .18f;
            Line(batch, pixel, center, horizontal * 2 * (t + .2f), width, rotation, color * intensity);
            Line(batch, pixel, center, vertical * 2 * (t + .2f), width, rotation + MathHelper.PiOver2, color * intensity);
        }
    }

    private static void Line(SpriteBatch batch, Texture2D pixel, Vector2 center,
        float length, float width, float rotation, Color color)
        => batch.Draw(pixel, center, WhitePixel, color, rotation, new Vector2(.5f),
            new Vector2(length, width), SpriteEffects.None, 0);
}

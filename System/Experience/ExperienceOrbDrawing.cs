using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.System.Experience;

/// <summary>游戏和 FNA 离线预览共享绘制，不依赖额外贴图或 SpriteBatch 状态切换。</summary>
internal static class ExperienceOrbDrawing
{
    // Terraria 的 MagicPixel 是 1×1000；只采样一个白色像素，缩放值才代表屏幕尺寸。
    private static readonly Rectangle WhitePixel = new(0, 0, 1, 1);

    public static void Draw(SpriteBatch batch, Texture2D pixel, Vector2 center, Vector2 velocity, float age)
    {
        float fade = MathHelper.Clamp(age / 8f, 0, 1);
        float pulse = 1f + MathF.Sin(age * .12f) * .12f;
        Vector2 direction = velocity.LengthSquared() > .01f ? Vector2.Normalize(velocity) : -Vector2.UnitY;
        if (age >= ExperienceRules.FloatTicks)
        {
            for (int i = 5; i >= 1; i--)
                Diamond(batch, pixel, center - direction * i * 3f, (6 - i) * .65f,
                    new Color(158, 112, 240, 0) * ((6 - i) * .045f * fade));
        }

        // A=0 的 tint 在游戏既有 AlphaBlend 中叠加柔光，实心核心保留正常 alpha。
        Diamond(batch, pixel, center, 10 * pulse, new Color(116, 65, 220, 0) * (.10f * fade));
        Diamond(batch, pixel, center, 7 * pulse, new Color(178, 119, 255, 0) * (.23f * fade));
        Diamond(batch, pixel, center, 4.5f, new Color(193, 155, 255) * fade);
        Diamond(batch, pixel, center, 2.2f, new Color(255, 244, 255) * fade);
        batch.Draw(pixel, center, WhitePixel, new Color(225, 202, 255, 0) * (.65f * fade), 0,
            new Vector2(.5f), new Vector2(1, 13 * pulse), SpriteEffects.None, 0);
    }

    private static void Diamond(SpriteBatch batch, Texture2D pixel, Vector2 center, float size, Color color)
        => batch.Draw(pixel, center, WhitePixel, color, MathHelper.PiOver4, new Vector2(.5f),
            new Vector2(size), SpriteEffects.None, 0);
}

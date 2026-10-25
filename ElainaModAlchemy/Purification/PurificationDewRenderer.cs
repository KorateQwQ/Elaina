using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

/// <summary>共享 Bloom 回调中的普通绘制。Effect 和贴图均由资源系统持有。</summary>
internal sealed class PurificationDewRenderer(Effect effect, Texture2D trail)
{
    // 调用者传入当前世界变换，绘制完成后由弹幕回调调用 KL.EndBeginDraw()。
    public void Draw(SpriteBatch batch, Vector2 center, float age, Matrix transform)
    {
        if (age <= 0) return;
        var settings = PurificationDewVisualSettings.Read();
        float height = Math.Min(Math.Max(1, settings.Height), 32 + age * 8);
        float width = Math.Max(1, settings.Width);
        float head = MathHelper.Clamp(settings.HeadPosition, .1f, .95f);
        Vector2 size = new(width, height);
        Vector2 origin = center - new Vector2(width * .5f, height * head);
        if (!Visible(origin, size, transform, batch.GraphicsDevice.Viewport)) return;

        batch.End();
        effect.CurrentTechnique = effect.Techniques["DewLight"];
        effect.Parameters["Time"].SetValue((int)Main.timeForVisualEffects / 60f);
        effect.Parameters["Opacity"].SetValue(MathHelper.Clamp(age / Math.Max(1, settings.FadeInTicks), 0, 1));
        effect.Parameters["Motion"].SetValue(new Vector4(settings.SwaySpeed, settings.SwayAmplitude, settings.SwayFrequency, 0));
        effect.Parameters["Hue"].SetValue(new Vector4(settings.ColorSpeed, settings.HueOffset, settings.Saturation, settings.ColorSpan));
        effect.Parameters["Shape"].SetValue(new Vector4(settings.TipWidth, settings.BodyWidth, settings.TaperExponent, settings.CrossColorSpan));
        effect.Parameters["Intensity"].SetValue(new Vector4(settings.TrailBrightness, settings.CoreBrightness, settings.HeadBrightness, settings.RingBrightness));
        effect.Parameters["HeadShape"].SetValue(new Vector4(head, settings.HeadWidth, settings.HeadLength, settings.CoreSharpness));
        effect.Parameters["RingShape"].SetValue(new Vector4(settings.RingWidth, settings.RingHeight, settings.RingSharpness, settings.RingColorSpan));
        effect.Parameters["RingHueOffset"].SetValue(settings.RingHueOffset);
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, effect, transform);
        batch.Draw(trail, origin, null, Color.White, 0, Vector2.Zero,
            new Vector2(size.X / trail.Width, size.Y / trail.Height), SpriteEffects.None, 0);
    }

    internal static bool OnScreen(Vector2 center, Matrix matrix, Viewport viewport)
    {
        var settings = PurificationDewVisualSettings.Read();
        float width = Math.Max(1, settings.Width), height = Math.Max(1, settings.Height);
        return Visible(center - new Vector2(width / 2 + 48, height + 48), new Vector2(width + 96, height + 112), matrix, viewport);
    }

    private static bool Visible(Vector2 origin, Vector2 size, Matrix matrix, Viewport viewport)
    {
        Vector2 a = Vector2.Transform(origin, matrix), b = Vector2.Transform(origin + new Vector2(size.X, 0), matrix);
        Vector2 c = Vector2.Transform(origin + new Vector2(0, size.Y), matrix), d = Vector2.Transform(origin + size, matrix);
        Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        return max.X > 0 && max.Y > 0 && min.X < viewport.Width && min.Y < viewport.Height;
    }
}

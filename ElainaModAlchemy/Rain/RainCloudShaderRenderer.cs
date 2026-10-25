using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

/// <summary>体积云使用预制噪声和消融表；资源由 ModContent 拥有，绘制后由调用者恢复批次。</summary>
internal sealed class RainCloudShaderRenderer(Effect effect, Texture2D white, Texture2D noise)
{
    public void Draw(SpriteBatch batch, Vector2 center, float age, Matrix transform)
    {
        float appear = MathHelper.Clamp(age / 132, 0, 1);
        float fade = MathHelper.SmoothStep(0, 1, MathHelper.Clamp(age / 42, 0, 1))
            * MathHelper.Clamp((RainCloudDrawing.Duration - age) / 60, 0, 1);
        if (fade <= 0 || appear <= 0) return;
        Vector2 size = new(360, 140);
        Vector2 origin = center + new Vector2(0, -12) - size * .5f;
        GraphicsDevice device = batch.GraphicsDevice;
        if (!Visible(origin, size, transform, device.Viewport)) return;

        batch.End();
        effect.CurrentTechnique = effect.Techniques["CloudVolume"];
        effect.Parameters["Time"].SetValue(age / 60f);
        effect.Parameters["Opacity"].SetValue(fade);
        effect.Parameters["Density"].SetValue(1.2f);
        effect.Parameters["FlowSpeed"].SetValue(.28f);
        float[] thresholds = RainCloudBakedData.RevealThresholds;
        float revealProgress = appear * appear * (3 - 2 * appear);
        float thresholdIndex = revealProgress * (thresholds.Length - 1);
        int lower = Math.Min((int)thresholdIndex, thresholds.Length - 2);
        effect.Parameters["Reveal"].SetValue(MathHelper.Lerp(thresholds[lower], thresholds[lower + 1], thresholdIndex - lower));
        effect.Parameters["RevealGlow"].SetValue(.75f * MathHelper.SmoothStep(0, 1, MathHelper.Clamp(age / 42, 0, 1))
            * (1 - MathHelper.SmoothStep(0, 1, MathHelper.Clamp((revealProgress - .78f) / .22f, 0, 1))));
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, effect, transform);
        device.Textures[1] = noise;
        device.SamplerStates[1] = SamplerState.LinearWrap;
        batch.Draw(white, origin, null, Color.White, 0, Vector2.Zero, size, SpriteEffects.None, 0);
    }

    private static bool Visible(Vector2 origin, Vector2 size, Matrix transform, Viewport viewport)
    {
        Vector2 a = Vector2.Transform(origin, transform);
        Vector2 b = Vector2.Transform(origin + new Vector2(size.X, 0), transform);
        Vector2 c = Vector2.Transform(origin + new Vector2(0, size.Y), transform);
        Vector2 d = Vector2.Transform(origin + size, transform);
        Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        return max.X > 0 && max.Y > 0 && min.X < viewport.Width && min.Y < viewport.Height;
    }

}

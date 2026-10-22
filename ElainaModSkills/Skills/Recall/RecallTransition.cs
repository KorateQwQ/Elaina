using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>成功传送时遮住场景跳变，再平滑揭开落点；不延迟实际传送或锁定操作。</summary>
internal sealed class RecallTransition
{
    internal const float HoldSeconds = .045f;
    internal const float FadeSeconds = .38f;
    private float elapsed, strength;

    internal float Opacity => strength * (1 - Smooth((elapsed - HoldSeconds) / FadeSeconds));

    internal void Begin(float distance)
    {
        if (!float.IsFinite(distance)) return;
        // 20 格内几乎没有镜头跳变；120 格外完整遮住切换，中间距离逐渐增强。
        float next = Smooth((distance - 320f) / 1600f);
        if (next <= 0) return;
        strength = Math.Max(Opacity, next);
        elapsed = 0;
    }

    internal void Advance(float seconds)
    {
        if (strength <= 0 || !float.IsFinite(seconds) || seconds <= 0) return;
        elapsed += seconds;
        if (elapsed >= HoldSeconds + FadeSeconds) Clear();
    }

    internal void Clear() { elapsed = strength = 0; }

    internal void Draw(SpriteBatch batch, Texture2D pixel, Rectangle viewport)
    {
        float alpha = Opacity;
        if (alpha > 0) batch.Draw(pixel, viewport, Color.Black * alpha);
    }

    private static float Smooth(float value)
    {
        float t = Math.Clamp(value, 0, 1);
        return t * t * (3 - 2 * t);
    }
}

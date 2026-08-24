using System;

namespace 伊蕾娜.ElainaModSkills.UICore;

/// <summary>
/// 缓动函数集,输入输出均为 0..1。
/// </summary>
public static class Easing
{
    public static float Clamp01(float t) => t < 0f ? 0f : t > 1f ? 1f : t;

    public static float OutCubic(float t)
    {
        t = Clamp01(t);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    public static float InCubic(float t)
    {
        t = Clamp01(t);
        return t * t * t;
    }

    public static float InOutCubic(float t)
    {
        t = Clamp01(t);
        return t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
    }

    public static float OutQuint(float t)
    {
        t = Clamp01(t);
        return 1f - MathF.Pow(1f - t, 5f);
    }

    public static float OutExpo(float t)
    {
        t = Clamp01(t);
        return t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);
    }

    /// <summary>带轻微过冲的回弹,适合弹出/悬停放大。</summary>
    public static float OutBack(float t, float overshoot = 1.70158f)
    {
        t = Clamp01(t);
        float c3 = overshoot + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + overshoot * u * u;
    }

    public static float OutElastic(float t)
    {
        t = Clamp01(t);
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = MathF.Tau / 3f;
        return MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    /// <summary>0→1→0 的钟形脉冲,用于闪光。</summary>
    public static float Pulse(float t)
    {
        t = Clamp01(t);
        return MathF.Sin(t * MathF.PI);
    }
}

/// <summary>
/// 临界阻尼弹簧浮点,用于自然的追踪动画(悬停放大、选中环滑动等)。
/// </summary>
public struct SpringFloat
{
    public float Value;
    public float Velocity;
    public float Stiffness;
    public float Damping;

    public SpringFloat(float value, float stiffness = 320f, float damping = 24f)
    {
        Value = value;
        Velocity = 0f;
        Stiffness = stiffness;
        Damping = damping;
    }

    public void Update(float target, float dt)
    {
        Velocity += (target - Value) * Stiffness * dt;
        Velocity *= 1f / (1f + Damping * dt);
        Value += Velocity * dt;
    }

    public void Snap(float target)
    {
        Value = target;
        Velocity = 0f;
    }
}

/// <summary>
/// 弹簧二维向量。
/// </summary>
public struct SpringVector2
{
    public Vector2 Value;
    public Vector2 Velocity;
    public float Stiffness;
    public float Damping;

    public SpringVector2(Vector2 value, float stiffness = 320f, float damping = 24f)
    {
        Value = value;
        Velocity = Vector2.Zero;
        Stiffness = stiffness;
        Damping = damping;
    }

    public void Update(Vector2 target, float dt)
    {
        Velocity += (target - Value) * Stiffness * dt;
        Velocity *= 1f / (1f + Damping * dt);
        Value += Velocity * dt;
    }

    public void Snap(Vector2 target)
    {
        Value = target;
        Velocity = Vector2.Zero;
    }
}

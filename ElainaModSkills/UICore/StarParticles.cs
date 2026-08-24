using System;

namespace 伊蕾娜.ElainaModSkills.UICore;

public enum StarParticleType
{
    /// <summary>漂浮星屑(柔光点 + 微星)。</summary>
    Dust,
    /// <summary>四芒星闪光。</summary>
    Sparkle,
    /// <summary>扩散冲击环。</summary>
    Ring,
    /// <summary>沿速度方向的流光短线。</summary>
    Streak,
}

public struct StarParticle
{
    public bool Active;
    public StarParticleType Type;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Drag;
    public float Gravity;
    public float Life;
    public float MaxLife;
    public float Scale;
    public float EndScale;
    public float Rotation;
    public float RotationSpeed;
    public Color Color;
    public float Seed;

    public float Life01 => MaxLife <= 0f ? 0f : Math.Clamp(Life / MaxLife, 0f, 1f);
}

/// <summary>
/// 轻量对象池粒子系统,坐标系由调用方决定(面板局部或屏幕 UI 坐标)。
/// Draw 需在 Additive 混合下调用。
/// </summary>
public class StarParticleSystem
{
    private readonly StarParticle[] _pool;
    private int _cursor;
    private static readonly Random Rng = new();

    public StarParticleSystem(int capacity = 512)
    {
        _pool = new StarParticle[capacity];
    }

    private static float RandF(float min, float max) => min + (float)Rng.NextDouble() * (max - min);

    public ref StarParticle Spawn(StarParticleType type, Vector2 pos, Vector2 vel, float life, float scale, Color color)
    {
        _cursor = (_cursor + 1) % _pool.Length;
        ref StarParticle p = ref _pool[_cursor];
        p.Active = true;
        p.Type = type;
        p.Position = pos;
        p.Velocity = vel;
        p.Drag = 0f;
        p.Gravity = 0f;
        p.Life = life;
        p.MaxLife = life;
        p.Scale = scale;
        p.EndScale = scale;
        p.Rotation = RandF(0f, MathF.Tau);
        p.RotationSpeed = 0f;
        p.Color = color;
        p.Seed = RandF(0f, 1f);
        return ref p;
    }

    /// <summary>星图环境星屑:缓慢上飘、闪烁。</summary>
    public void SpawnAmbient(Vector2 areaMin, Vector2 areaMax, Color color)
    {
        Vector2 pos = new(RandF(areaMin.X, areaMax.X), RandF(areaMin.Y, areaMax.Y));
        ref var p = ref Spawn(StarParticleType.Dust, pos,
            new Vector2(RandF(-4f, 4f), RandF(-11f, -4f)),
            RandF(3.5f, 7f), RandF(1.6f, 4.2f), color * RandF(0.25f, 0.6f));
        p.RotationSpeed = RandF(-0.6f, 0.6f);
    }

    /// <summary>解锁星光爆发:放射星屑 + 闪光 + 双冲击环。</summary>
    public void SpawnUnlockBurst(Vector2 pos, Color primary, Color secondary)
    {
        for (int i = 0; i < 26; i++)
        {
            float ang = RandF(0f, MathF.Tau);
            float spd = RandF(40f, 240f);
            ref var p = ref Spawn(StarParticleType.Dust, pos,
                new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * spd,
                RandF(0.5f, 1.3f), RandF(1.8f, 4.6f),
                (i % 3 == 0 ? secondary : primary) * RandF(0.7f, 1f));
            p.Drag = 3.2f;
            p.Gravity = 26f;
        }

        for (int i = 0; i < 7; i++)
        {
            float ang = RandF(0f, MathF.Tau);
            float spd = RandF(15f, 90f);
            ref var p = ref Spawn(StarParticleType.Sparkle, pos,
                new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * spd,
                RandF(0.45f, 0.95f), RandF(14f, 30f), Color.White * 0.95f);
            p.Drag = 2.4f;
            p.RotationSpeed = RandF(-2f, 2f);
        }

        SpawnRing(pos, primary, 130f, 0.55f);
        SpawnRing(pos, Color.White, 78f, 0.4f);
    }

    /// <summary>小型确认反馈(装备/升级)。</summary>
    public void SpawnConfirmBurst(Vector2 pos, Color color)
    {
        for (int i = 0; i < 10; i++)
        {
            float ang = RandF(0f, MathF.Tau);
            ref var p = ref Spawn(StarParticleType.Dust, pos,
                new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * RandF(30f, 120f),
                RandF(0.35f, 0.8f), RandF(1.5f, 3.4f), color * RandF(0.7f, 1f));
            p.Drag = 3.5f;
        }
        SpawnRing(pos, color, 60f, 0.35f);
    }

    public void SpawnRing(Vector2 pos, Color color, float endRadius, float life)
    {
        ref var p = ref Spawn(StarParticleType.Ring, pos, Vector2.Zero, life, 6f, color);
        p.EndScale = endRadius;
    }

    public void Update(float dt)
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            ref StarParticle p = ref _pool[i];
            if (!p.Active) continue;
            p.Life -= dt;
            if (p.Life <= 0f) { p.Active = false; continue; }
            p.Velocity *= 1f / (1f + p.Drag * dt);
            p.Velocity.Y += p.Gravity * dt;
            p.Position += p.Velocity * dt;
            p.Rotation += p.RotationSpeed * dt;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _pool.Length; i++) _pool[i].Active = false;
    }

    /// <summary>需在 Additive 混合状态下调用,alpha 为整体透明度。</summary>
    public void Draw(SpriteBatch sb, float alpha = 1f)
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            ref StarParticle p = ref _pool[i];
            if (!p.Active) continue;
            float t = p.Life01;

            switch (p.Type)
            {
                case StarParticleType.Dust:
                {
                    float fade = MathF.Min(t * 3f, 1f) * MathF.Min((1f - t) * 5f + 0.15f, 1f);
                    float twinkle = 0.75f + 0.25f * MathF.Sin((1f - t) * 20f + p.Seed * 31f);
                    Color c = p.Color * (fade * twinkle * alpha);
                    UIDrawKit.DrawGlow(sb, p.Position, p.Scale * 2.6f, c * 0.55f);
                    UIDrawKit.DrawSparkle(sb, p.Position, p.Scale * 4.4f, p.Rotation, c);
                    break;
                }
                case StarParticleType.Sparkle:
                {
                    float pulse = Easing.Pulse(1f - t);
                    Color c = p.Color * (pulse * alpha);
                    UIDrawKit.DrawSparkle(sb, p.Position, p.Scale * pulse, p.Rotation, c);
                    UIDrawKit.DrawGlow(sb, p.Position, p.Scale * pulse * 0.6f, c * 0.5f);
                    break;
                }
                case StarParticleType.Ring:
                {
                    float grow = Easing.OutCubic(1f - t);
                    float radius = MathHelper.Lerp(p.Scale, p.EndScale, grow);
                    UIDrawKit.DrawRing(sb, p.Position, radius, p.Color * (t * alpha));
                    break;
                }
                case StarParticleType.Streak:
                {
                    Vector2 dir = p.Velocity.SafeNormalize(Vector2.UnitX);
                    float fade = Easing.Pulse(1f - t);
                    UIDrawKit.DrawGlowLine(sb, p.Position - dir * p.Scale, p.Position + dir * p.Scale,
                        1.2f, p.Color * (fade * alpha));
                    break;
                }
            }
        }
    }
}

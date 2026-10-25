using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaAttribute;

internal sealed class ResourceBarState
{
    public float Value { get; private set; }
    public float Ghost { get; private set; }
    public readonly List<Particle> Particles = new(64);
    private readonly bool mana;
    private readonly Random random;
    private float target, maximum, delay, ambient;
    private bool initialized;

    public sealed class Particle
    {
        public bool Star;
        public Vector2 Position, Velocity;
        public float Gravity, Rotation, Spin, Size, Age, Duration, Phase;
    }

    public ResourceBarState(bool mana, int seed = 41) { this.mana = mana; random = new Random(seed); }
    private float R() => random.NextSingle();
    public static float Fraction(float value, float max) => float.IsFinite(value) && float.IsFinite(max) && max > 0 ? Math.Clamp(value / max, 0, 1) : 0;
    public void Reset(float value, float max)
    {
        initialized = true; maximum = max; target = Value = Ghost = Fraction(value, max);
        delay = ambient = 0; Particles.Clear();
    }
    public void Update(float value, float max, float dt, float time, bool particles = true)
    {
        if (!initialized || max != maximum) { Reset(value, max); return; }
        dt = Math.Clamp(dt, 0, .1f);
        float next = Fraction(value, max), h = mana ? 7 : 8;
        if (next < target)
        {
            float before = Value; Ghost = Math.Max(Ghost, Value); Value = next; delay = .4f;
            if (particles) Lose(next * 262, before * 262, h);
        }
        // Passive regeneration only makes ambient stars. A larger gain sparkles at the edge; the bar itself never flashes.
        else if (next > target && (next - target) * max >= 2)
        {
            if (particles) for (int i = 0; i < 6; i++)
                Spawn(true, next * 262 - R() * 20, R() * h, -6 + R() * 12, -14 - R() * 10, 0, .8f + R(), .6f + R() * .4f);
        }
        target = next;
        if (target > Value) Value = Math.Min(target, Value + (target - Value) * Math.Min(1, dt * 7) + dt * .02f);
        else Value = target;
        if (delay > 0) delay -= dt; else Ghost = Math.Max(Value, Ghost - dt * .45f);
        Ghost = Math.Max(Ghost, Value);
        if (particles && Value * 262 > 2 && (ambient -= dt) <= 0)
        {
            if (mana)
            {
                ambient = next < 1 ? .18f : .6f;
                Spawn(true, R() * Value * 262, R() * h, 0, -6 - R() * 6, 0, .5f + R() * .8f, .9f + R() * .6f);
            }
            else
            {
                ambient = 1.4f + R();
                Spawn(false, Value * 262 - 2, R() * h, 10 + R() * 10, -4, 9, 1.6f + R(), 1.8f);
            }
        }
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var p = Particles[i]; p.Age += dt;
            if (p.Age >= p.Duration) { Particles.RemoveAt(i); continue; }
            p.Velocity.Y += p.Gravity * dt;
            p.Position += p.Velocity * dt + new Vector2(MathF.Sin(time * 3 + p.Phase) * dt * 6, 0);
            p.Rotation += p.Spin * dt;
        }
    }
    private void Lose(float from, float to, float h)
    {
        int count = Math.Clamp((int)MathF.Round((to - from) / 5), 3, 26);
        for (int i = 0; i < count; i++)
            if (mana) Spawn(true, MathHelper.Lerp(from, to, R()), R() * h, -8 + R() * 24, -30 + R() * 18, 6, .8f + R() * 1.4f, .6f + R() * .6f);
            else Spawn(false, MathHelper.Lerp(from, to, R()), R() * h, 8 + R() * 26, -22 + R() * 16, 34, 1.6f + R() * 1.8f, .9f + R() * .8f);
    }
    private void Spawn(bool star, float x, float y, float vx, float vy, float gravity, float size, float duration)
    {
        if (Particles.Count >= 96) Particles.RemoveAt(0);
        Particles.Add(new Particle { Star = star, Position = new(x, y), Velocity = new(vx, vy), Gravity = gravity,
            Size = size, Duration = duration, Rotation = R() * MathF.Tau, Spin = (R() - .5f) * 6, Phase = R() * MathF.Tau });
    }
}

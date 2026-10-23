using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaAttribute;

// Shared verbatim by the game and the FNA preview; mirrors updateHud/levelUp in Tools/ResourceBarPreview/bars.html.
internal sealed class PortraitHudState
{
    public int Level { get; private set; } = 1;
    public int Cap { get; private set; } = 1;
    public int Shown { get; private set; } = 1;
    // Displayed progress in levels: (level - 1) + experience / need, eased toward the real value.
    public float Display { get; private set; }
    public float Flash { get; private set; }
    public float Pulse { get; private set; }
    public bool Capped => Level >= Cap;
    public float Fraction => Math.Clamp(Display - (Shown - 1), 0, 1);
    public readonly List<Spark> Sparks = new(66);
    private readonly Random random;
    private bool initialized;

    public sealed class Spark
    {
        public Vector2 Position, Velocity;
        public float Size, Age, Duration;
    }

    public PortraitHudState(int seed = 67) { random = new Random(seed); }
    private float R() => random.NextSingle();

    private static float Target(int level, int experience, int need) =>
        level - 1 + (need > 0 ? Math.Clamp(experience / (float)need, 0, 1) : 0);

    public void Reset(int level, int experience, int need, int cap)
    {
        initialized = true;
        Level = Math.Max(1, level); Cap = Math.Max(1, cap); Shown = Level;
        Display = Target(Level, experience, need);
        Flash = Pulse = 0; Sparks.Clear();
    }

    public void Update(int level, int experience, int need, int cap, float dt, bool particles = true)
    {
        if (!initialized) { Reset(level, experience, need, cap); return; }
        dt = Math.Clamp(dt, 0, .1f);
        Level = Math.Max(1, level); Cap = Math.Max(1, cap);
        float target = Target(Level, experience, need);
        // Losing progress (debug resets, character swaps) snaps; gains ease and may cross several levels.
        if (target < Display) Display = target;
        else Display = Math.Min(target, Display + Math.Max(dt * .3f, (target - Display) * dt * 4));
        int shown = Math.Min(Level, (int)MathF.Floor(Display) + 1);
        if (shown > Shown) LevelUp(particles);
        Shown = shown;
        Flash = Math.Max(0, Flash - dt * 2.4f);
        Pulse = Math.Max(0, Pulse - dt * 2);
        for (int i = Sparks.Count - 1; i >= 0; i--)
        {
            var s = Sparks[i]; s.Age += dt;
            if (s.Age >= s.Duration) { Sparks.RemoveAt(i); continue; }
            s.Position += s.Velocity * dt;
            s.Velocity *= 1 - dt * 2;
        }
    }

    // Offline previews pin the level-up flash and pulse to compare a single frame with the HTML reference.
    internal void ShowBurst(float strength) { Flash = Pulse = Math.Clamp(strength, 0, 1); }

    public static Vector2 ArmPoint(float radius, float fraction, int side)
    {
        float u = fraction * 2;
        return u <= 1 ? new Vector2(side * radius * u, radius * (1 - u)) : new Vector2(side * radius * (2 - u), -radius * (u - 1));
    }

    private void LevelUp(bool particles)
    {
        Flash = Pulse = 1;
        if (!particles) return;
        for (int i = 0; i < 22; i++)
        {
            var at = ArmPoint(PortraitHudDrawing.Band, i / 22f, i % 2 == 1 ? 1 : -1);
            var direction = at.LengthSquared() > 0 ? Vector2.Normalize(at) : Vector2.Zero;
            if (Sparks.Count >= 66) Sparks.RemoveAt(0);
            Sparks.Add(new Spark
            {
                Position = at,
                Velocity = new Vector2(direction.X * (14 + R() * 16), direction.Y * (14 + R() * 16) - 6),
                Size = .8f + R() * 1.1f,
                Duration = .7f + R() * .5f,
            });
        }
    }
}

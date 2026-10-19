using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Background motion of the star map, timing from Tools/SkillPanelRedesign/index.html:
// · every star breathes between 35% and 100% over its own 2.6–6.4s period;
// · about one star in ten (all large ones) glints a small four-point star every 7–14s;
// · a meteor crosses the upper left of the sky every 6–14s;
// · motes rise from the book in the portrait's hands.
// Everything is in page (design) coordinates: the sky and portrait stay put while the camera moves.
internal sealed class ConstellationSky
{
    internal readonly record struct Star(Vector2 Center, float Size, float Opacity, float Period, float Phase,
        bool Glints, float GlintPeriod, float GlintPhase, float GlintSize);

    internal struct Meteor
    {
        internal Vector2 Start, Direction;
        internal float Speed, Life, Duration, Length;
    }

    internal struct Mote
    {
        internal Vector2 Position, Velocity;
        internal float Life, Duration, Radius, Spark, Phase;
    }

    // Page-space book in the portrait's hands: (428, 188) + (424.21, 620) × (.5, .455).
    internal static readonly Vector2 Book = new(640.1f, 470.1f);
    // The same seeded field as the preview; a second sequence draws the motion so positions match.
    internal static readonly Star[] Stars = BuildStars();
    private readonly List<Mote> _motes = [];
    private readonly Random _random = new();
    private Meteor? _meteor;
    private float _nextMeteor;
    internal float Time { get; private set; }
    internal Meteor? CurrentMeteor => _meteor;
    internal IReadOnlyList<Mote> Motes => _motes;

    internal ConstellationSky() => Reset();

    private static Star[] BuildStars()
    {
        long seed = 173, motion = 911;
        float Next(ref long s) { s = (s * 9301 + 49297) % 233280; return s / 233280f; }
        var stars = new Star[150];
        for (int i = 0; i < stars.Length; i++)
        {
            float size = Next(ref seed) < .9f ? 1 : 2, x = 26 + Next(ref seed) * 727, y = 150 + Next(ref seed) * 558;
            float opacity = .15f + Next(ref seed) * .5f;
            bool glints = size == 2 || Next(ref motion) < .08f;
            float period = 2.6f + Next(ref motion) * 3.8f, phase = Next(ref motion) * 10;
            float glintPeriod = 7 + Next(ref motion) * 7, glintPhase = Next(ref motion) * 14, glintSize = 5 + Next(ref motion) * 4;
            stars[i] = new Star(new Vector2(x + size / 2, y + size / 2), size, opacity, period, phase, glints, glintPeriod, glintPhase, glintSize);
        }
        return stars;
    }

    // CSS `ease-in-out infinite alternate` from .35 to 1.
    internal float Twinkle(in Star star)
    {
        float u = (Time + star.Phase) / star.Period, k = u - MathF.Floor(u);
        if ((int)MathF.Floor(u) % 2 == 1) k = 1 - k;
        return .35f + .65f * k * k * (3 - 2 * k);
    }

    // Linear keyframes: hidden until 86%, opens to full size at 92% while turning 20°, closes by 100% at 45°.
    internal (float Opacity, float Scale, float Rotation) Glint(in Star star)
    {
        float u = (Time + star.GlintPhase) / star.GlintPeriod, k = u - MathF.Floor(u);
        if (k < .86f) return (0, .3f, 0);
        if (k < .92f) { float t = (k - .86f) / .06f; return (t, MathHelper.Lerp(.3f, 1, t), MathHelper.ToRadians(20 * t)); }
        float e = (k - .92f) / .08f;
        return (1 - e, MathHelper.Lerp(1, .3f, e), MathHelper.ToRadians(MathHelper.Lerp(20, 45, e)));
    }

    // Faded in and out over its flight; the tail grows during the first third.
    internal static (Vector2 Head, Vector2 Tail, float Opacity) MeteorPose(in Meteor m)
    {
        float k = m.Life / m.Duration;
        Vector2 head = m.Start + m.Direction * (m.Speed * m.Life);
        return (head, head - m.Direction * (m.Length * Math.Min(1, k * 3)), MathF.Pow(MathF.Sin(MathF.PI * k), 1.5f) * .75f);
    }

    internal static float MoteOpacity(in Mote mote)
    {
        float k = mote.Life / mote.Duration;
        return (k < .2f ? k / .2f : 1 - (k - .2f) / .8f) * .8f;
    }

    internal void Advance(float seconds)
    {
        if (!float.IsFinite(seconds) || seconds < 0) seconds = 0;
        Time += seconds;
        float R() => (float)_random.NextDouble();

        if (_meteor is { } meteor)
        {
            meteor.Life += seconds;
            _meteor = meteor.Life < meteor.Duration ? meteor : null;
        }
        else if ((_nextMeteor -= seconds) <= 0)
        {
            float angle = MathHelper.ToRadians(148 + R() * 14);
            _meteor = new Meteor
            {
                Start = new Vector2(260 + R() * 360, 175 + R() * 120), Direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle)),
                Speed = 330 + R() * 120, Duration = .7f + R() * .3f, Length = 60 + R() * 40,
            };
            _nextMeteor = 6 + R() * 8;
        }

        if (R() < 3.2f * seconds)
            _motes.Add(new Mote
            {
                Position = Book + new Vector2((R() - .5f) * 120, -4 - R() * 10),
                Velocity = new Vector2((R() - .5f) * 6, -(9 + R() * 14)),
                Duration = 2.2f + R() * 1.8f, Spark = R() < .25f ? 4 + R() * 2.5f : 0, Radius = .6f + R() * .8f, Phase = R() * 6,
            });
        float drag = MathF.Pow(.985f, seconds * 60);
        for (int i = _motes.Count - 1; i >= 0; i--)
        {
            var mote = _motes[i];
            mote.Life += seconds;
            if (mote.Life >= mote.Duration) { _motes.RemoveAt(i); continue; }
            // A slow sideways sway as they rise.
            mote.Position += new Vector2(mote.Velocity.X + MathF.Sin(mote.Life * 1.7f + mote.Phase) * 4, mote.Velocity.Y) * seconds;
            mote.Velocity.Y *= drag;
            _motes[i] = mote;
        }
    }

    internal void Reset()
    {
        _motes.Clear();
        _meteor = null;
        _nextMeteor = 3 + (float)_random.NextDouble() * 4;
    }
}

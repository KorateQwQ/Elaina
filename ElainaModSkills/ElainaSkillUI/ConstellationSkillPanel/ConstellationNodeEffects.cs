using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Selection timing from Tools/SkillFramePreview/perch.html. The butterfly landing lights the ring
// from the perch and traces the node's stars; each later wingbeat at rest traces them again.
// Positions are world (node) coordinates, so camera pan/zoom carries the dust with the page.
internal sealed class ConstellationNodeEffects
{
    internal struct Mote
    {
        internal Vector2 Position, Velocity;
        internal float Life, Duration, Radius;
    }

    private const float FadeSeconds = .35f;
    private readonly Dictionary<string, float> _amount = [];
    private readonly List<Mote> _dust = [];
    private readonly Random _random = new();
    private string _landed;
    private bool _wasFlapping;
    private float _landedAt, _tracedAt, _spawn;
    internal float Time { get; private set; }
    internal IReadOnlyList<Mote> Dust => _dust;
    internal bool Flutter { get; private set; }

    internal bool Landed(string id) => id != null && id == _landed;
    internal float SinceLanding => Time - _landedAt;
    // Seconds since the stars of the landed node were last traced; other nodes never trace.
    internal float SinceTrace(string id) => Landed(id) ? Time - _tracedAt : float.MaxValue;
    // Selection strength: grows with the gold arcs, then fades out after the butterfly leaves.
    internal float Amount(string id) => id != null && _amount.TryGetValue(id, out float value) ? value : 0;

    internal void Advance(float seconds, ConstellationButterflyMotion butterfly, string selected)
    {
        if (!float.IsFinite(seconds) || seconds < 0) seconds = 0;
        Time += seconds;
        if (selected == null || !butterfly.Visible || butterfly.Moving) _landed = null;
        else if (_landed != selected) { _landed = selected; _landedAt = _tracedAt = Time; }
        if (_landed != null && butterfly.Flapping && !_wasFlapping) _tracedAt = Time;
        _wasFlapping = butterfly.Flapping;
        Flutter = _landed != null && butterfly.Flapping;

        foreach (string id in _amount.Keys.ToArray())
        {
            if (id == _landed) continue;
            float value = _amount[id] - seconds / FadeSeconds;
            if (value > 0) _amount[id] = value;
            else _amount.Remove(id);
        }
        if (_landed != null) _amount[_landed] = EaseOut(SinceLanding / .55f);

        // Scales drift down from the wings: a trail in flight, a few while fluttering at rest.
        float rate = !butterfly.Visible ? 0 : butterfly.Moving ? 42 : butterfly.Flapping ? 9 : 0;
        _spawn = rate > 0 ? _spawn + rate * seconds : 0;
        Vector2 wings = butterfly.Position + ConstellationDrawing.PerchOffset;
        for (; _spawn >= 1; _spawn--)
            _dust.Add(new Mote
            {
                Position = wings + new Vector2(((float)_random.NextDouble() - .5f) * 12, ((float)_random.NextDouble() - .3f) * 8),
                Velocity = new Vector2(((float)_random.NextDouble() - .5f) * 10, 6 + (float)_random.NextDouble() * 10),
                Duration = .8f + (float)_random.NextDouble() * .7f,
                Radius = .5f + (float)_random.NextDouble() * .9f,
            });
        float drag = MathF.Pow(.98f, seconds * 60);
        for (int i = _dust.Count - 1; i >= 0; i--)
        {
            var mote = _dust[i];
            mote.Life += seconds;
            if (mote.Life >= mote.Duration) { _dust.RemoveAt(i); continue; }
            mote.Position += mote.Velocity * seconds;
            mote.Velocity.X *= drag;
            _dust[i] = mote;
        }
    }

    internal static float MoteOpacity(Mote mote)
    {
        float k = mote.Life / mote.Duration;
        return (k < .15f ? k / .15f : 1 - (k - .15f) / .85f) * .85f;
    }

    internal void Reset()
    {
        _amount.Clear();
        _dust.Clear();
        _landed = null;
        _wasFlapping = Flutter = false;
        Time = _landedAt = _tracedAt = _spawn = 0;
    }

    internal static float EaseOut(float t)
    {
        t = Math.Clamp(t, 0, 1);
        return 1 - (1 - t) * (1 - t) * (1 - t);
    }
}

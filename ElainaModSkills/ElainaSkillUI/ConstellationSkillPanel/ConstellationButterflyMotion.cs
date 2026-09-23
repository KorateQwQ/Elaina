using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// World-space flight: camera pan/zoom carries the marker with the ink on the page.
internal sealed class ConstellationButterflyMotion
{
    private const double FlightDuration = .42, WingDuration = .8, SettleDuration = .65;
    private Vector2 _from, _target;
    private double _time, _moveStart, _wingStart, _stopAt, _idleUntil;
    private int _restCount;
    internal Vector2 Position { get; private set; }
    internal bool Visible { get; private set; }
    internal bool Moving { get; private set; }
    internal bool Flapping { get; private set; }
    internal int Frame => Flapping ? (int)((_time - _wingStart) / WingDuration * 16) % 16 : 0;

    internal void SetTarget(Vector2? target)
    {
        if (target is not { } point) { Reset(); return; }
        if (!Visible)
        {
            Visible = true;
            Position = _from = _target = point;
            Flapping = true;
            _wingStart = _time;
            _stopAt = _time + WingDuration;
            return;
        }
        if (_target == point) return;
        _from = Position;
        _target = point;
        _moveStart = _time;
        Moving = true;
        if (!Flapping) { Flapping = true; _wingStart = _time; }
        // Complete the last wingbeat before resting on the open-wing frame.
        _stopAt = _wingStart + Math.Ceiling((_time + FlightDuration + SettleDuration - _wingStart) / WingDuration) * WingDuration;
    }

    internal void Advance(float seconds)
    {
        if (!Visible || !float.IsFinite(seconds) || seconds <= 0) return;
        _time += seconds;
        if (Moving)
        {
            float t = (float)Math.Min(1, (_time - _moveStart) / FlightDuration);
            float remaining = 1 - t;
            var position = Vector2.Lerp(_from, _target, 1 - remaining * remaining * remaining);
            position.Y -= MathF.Sin(t * MathHelper.Pi) * Math.Min(12, Vector2.Distance(_from, _target) * .06f);
            Position = position;
            if (t >= 1) { Position = _target; Moving = false; }
        }
        // Event times keep motion independent of frame rate, including a long frame.
        while (true)
        {
            if (Flapping)
            {
                if (_time < _stopAt) break;
                Flapping = false;
                _idleUntil = _stopAt + 4.5 + _restCount++ % 3 * .85;
            }
            if (_time < _idleUntil) break;
            Flapping = true;
            _wingStart = _idleUntil;
            _stopAt = _wingStart + WingDuration;
        }
    }

    internal void Reset()
    {
        Visible = Moving = Flapping = false;
        Position = _from = _target = Vector2.Zero;
        _time = _moveStart = _wingStart = _stopAt = _idleUntil = 0;
        _restCount = 0;
    }
}

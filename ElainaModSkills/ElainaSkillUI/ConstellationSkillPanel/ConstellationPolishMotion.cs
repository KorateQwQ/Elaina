using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

internal sealed class ConstellationTween(float value, float duration)
{
    internal float Value { get; private set; } = value;
    private float _from = value, _target = value, _elapsed = duration;
    private readonly float _duration = duration;
    internal void SetTarget(float target)
    {
        if (target == _target) return;
        _from = Value; _target = target; _elapsed = 0;
    }
    internal void Advance(float seconds)
    {
        if (!float.IsFinite(seconds) || seconds <= 0) return;
        _elapsed = Math.Min(_duration, _elapsed + seconds);
        float remaining = 1 - _elapsed / _duration;
        Value = _target + (_from - _target) * remaining * remaining * remaining;
    }
}

internal sealed class ConstellationTooltipDelay
{
    private string _key;
    private float _since;
    internal bool Ready(string key, float time)
    {
        if (key == null) { Reset(); return false; }
        if (_key != key) { _key = key; _since = time; }
        return time - _since >= .25f;
    }
    internal void Reset() => _key = null;
}

internal static class ConstellationTooltipPlacement
{
    internal static Vector2 Place(Vector2 size, Vector2 pointer, Vector2 bounds, Rectangle? anchor)
    {
        var p = pointer + new Vector2(12, 18);
        if (anchor is { } area)
        {
            // Detail buttons prefer the map side, keeping their material list unobscured.
            if (area.Left > bounds.X * .6f && area.Left - size.X - 12 >= 12)
                p = new Vector2(area.Left - size.X - 12, area.Top);
            else if (area.Top - size.Y - 10 >= 12)
                p = new Vector2(area.Center.X - size.X / 2, area.Top - size.Y - 10);
            else p = new Vector2(area.Center.X - size.X / 2, area.Bottom + 10);
        }
        return Vector2.Clamp(p, new Vector2(12), Vector2.Max(new Vector2(12), bounds - size - new Vector2(12)));
    }
}

using System;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

internal sealed class ConstellationToggleAnimation(bool on)
{
    internal float Position { get; private set; } = on ? 1 : 0;
    internal float Hover { get; private set; }
    private float _target = on ? 1 : 0, _hoverTarget;

    internal void SetTarget(bool on, bool hover)
    {
        _target = on ? 1 : 0;
        _hoverTarget = hover ? 1 : 0;
    }

    internal void Advance(float seconds)
    {
        if (!float.IsFinite(seconds) || seconds <= 0) return;
        Position = Approach(Position, _target, seconds, .055f);
        Hover = Approach(Hover, _hoverTarget, seconds, .07f);
        // Hover must be renewed by a visible control each frame; scrolling it away cannot leave it lit.
        _hoverTarget = 0;
    }

    private static float Approach(float current, float target, float seconds, float timeConstant)
    {
        float value = target + (current - target) * MathF.Exp(-seconds / timeConstant);
        return Math.Abs(value - target) < .001f ? target : value;
    }
}

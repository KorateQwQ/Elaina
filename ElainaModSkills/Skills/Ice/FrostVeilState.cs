using System;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>One non-stacking veil per recipient. Strength is snapshotted from the caster's maximum magic.</summary>
public sealed class FrostVeilState
{
    public const int MaximumShields = 3;
    public const int DurationTicks = 60 * 60;
    public int Shields { get; private set; }
    public int RemainingTicks { get; private set; }
    public float Absorption { get; private set; }

    public void Apply(float absorption) => Restore(MaximumShields, DurationTicks, absorption);

    internal void Restore(int shields, int ticks, float absorption)
    {
        Shields = Math.Clamp(shields, 0, MaximumShields);
        RemainingTicks = Shields > 0 ? Math.Clamp(ticks, 0, DurationTicks) : 0;
        Absorption = Shields > 0 && float.IsFinite(absorption) ? Math.Max(0f, absorption) : 0f;
    }

    public void Clear() => Restore(0, 0, 0);

    public bool Tick(bool authority)
    {
        if (Shields == 0) return false;
        if (RemainingTicks > 0) RemainingTicks--;
        if (!authority || RemainingTicks > 0) return false;
        Shields = MaximumShields;
        RemainingTicks = DurationTicks;
        return true;
    }

    public int ReduceDamage(int damage) => Shields > 0 && Absorption > 0f && damage > 0
        ? (int)Math.Max(0d, Math.Floor(damage - (double)Absorption)) : damage;

    public bool Consume()
    {
        if (Shields == 0) return false;
        if (--Shields == 0) Clear();
        return true;
    }
}

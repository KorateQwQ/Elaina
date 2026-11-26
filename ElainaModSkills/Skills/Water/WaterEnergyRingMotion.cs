using System;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

internal sealed class WaterEnergyRingDrain(float initialFill)
{
    private static float BarsPerSecond => 1f;
    internal float Remaining { get; private set; } = initialFill;

    internal void Advance(float elapsedSeconds) =>
        Remaining = Math.Max(0f, Remaining - BarsPerSecond * elapsedSeconds);
}

internal sealed class WaterEnergyRingMotion
{
    private readonly ConstellationUIClock clock = new();
    private float progress;
    private int lastEnergy;
    private bool showing;
    private bool spending;
    private WaterEnergyRingDrain activeDrain;
    internal float MaterialPulse => spending ? -1f : Surge;
    private static float FlowSpeed => .85f;
    // At 1.4 repeats per phase both foam layers tile exactly seven times in five phases.
    internal const int FlowPeriod = 5;
    internal bool Active => showing || progress > 0;
    internal float Opacity => progress * progress * (3f - 2f * progress);
    internal float Fill { get; private set; }
    internal float Phase { get; private set; }
    internal float Surge { get; private set; }
    internal bool Full { get; private set; }
    internal float FullAge { get; private set; }

    internal void Update(int energy, int capacity, bool animating, WaterEnergyRingDrain castDrain = null)
    {
        float elapsed = clock.Sample(animating);
        bool wasFull = Full;
        if (energy > 0) activeDrain = null;
        if (castDrain != null && castDrain != activeDrain)
            activeDrain = castDrain; // 起手首帧显示本次扣除的能量。
        else
            activeDrain?.Advance(elapsed);
        bool opening = energy > 0 || activeDrain?.Remaining > 0;
        // 切换方向的首帧保留当前透明度。
        if (opening == showing)
            progress = Math.Clamp(progress + elapsed * (opening ? 1f / .16f : -1f / .22f), 0f, 1f);
        if (activeDrain != null)
        {
            spending = true;
            Fill = activeDrain.Remaining;
            Full = false;
            Surge = 0f;
            lastEnergy = 0;
        }
        else if (opening)
        {
            spending = false;
            float target = energy / (float)capacity;
            if (lastEnergy == 0) Fill = target;
            if (showing && energy > lastEnergy) Surge = 1f;
            Fill += (target - Fill) * (1f - MathF.Exp(-elapsed * 16f));
            Full = energy == capacity && Fill > .995f;
            lastEnergy = energy;
        }
        FullAge = wasFull && Full ? FullAge + elapsed : 0f;
        showing = opening;
        Phase = (Phase + elapsed * FlowSpeed * (spending ? -1f : 1f)) % FlowPeriod;
        if (Phase < 0) Phase += FlowPeriod;
        Surge = Math.Max(0f, Surge - elapsed * 3.5f);
        if (!Active) Reset();
    }

    internal void Reset()
    {
        clock.Stop();
        activeDrain = null;
        showing = spending = Full = false;
        progress = Fill = Surge = Phase = FullAge = 0f;
        lastEnergy = 0;
    }
}

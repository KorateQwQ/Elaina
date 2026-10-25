using System;
using 伊蕾娜.ElainaModSkills;

namespace 伊蕾娜.Mounts;

/// <summary>每次按下最多产生一次冲刺。冷却使用游戏时钟，不依赖绘制、等级或坐骑开关。</summary>
public sealed class BroomFlightState
{
    private bool trackingPress;
    private bool boostExhausted;
    private int heldTicks;
    private bool waitForRelease;
    private ulong dashUntil;
    private ulong recoveryUntil;
    private ulong dashReadyAt;
    private ulong lastTick = ulong.MaxValue;
    public bool Boosting { get; private set; }
    public bool Dashing { get; private set; }
    public bool DashStarted { get; private set; }
    public float RecoveryProgress { get; private set; } = 1f;

    public void Cancel()
    {
        waitForRelease |= trackingPress;
        trackingPress = boostExhausted = Boosting = Dashing = DashStarted = false;
        heldTicks = 0;
        dashUntil = 0;
        recoveryUntil = 0;
        RecoveryProgress = 1f;
        // 下坐骑、失焦、死亡都不重置 dashReadyAt。
    }

    public void Tick(ulong tick, bool enabled, bool held, bool pressed, bool released,
        FlightBalance balance, Func<float, bool> spendMana)
    {
        if (lastTick == tick) return;
        lastTick = tick;
        Boosting = DashStarted = false;
        // 禁用期间也记录实际松键，避免学习技能后第一次全新的按键被吞掉。
        if (!enabled) { Cancel(); waitForRelease = held; return; }
        if (waitForRelease)
        {
            if (!held) waitForRelease = false;
            return;
        }
        Dashing = tick < dashUntil;
        RecoveryProgress = !Dashing && tick < recoveryUntil
            ? (tick - dashUntil) / (float)(recoveryUntil - dashUntil) : 1f;
        if (pressed && held && !trackingPress)
        {
            trackingPress = true;
            boostExhausted = false;
            heldTicks = 0;
        }
        if (trackingPress && held)
        {
            heldTicks = Math.Min(heldTicks + 1, FlightBalance.Ticks(balance.HoldSeconds));
            if (heldTicks >= FlightBalance.Ticks(balance.HoldSeconds) && !Dashing && !boostExhausted)
            {
                Boosting = spendMana(balance.BoostManaPerSecond / 60f);
                boostExhausted = !Boosting;
            }
        }
        else if (trackingPress)
        {
            bool tap = released && heldTicks > 0 && heldTicks < FlightBalance.Ticks(balance.HoldSeconds);
            trackingPress = false;
            heldTicks = 0;
            if (tap && !Dashing && tick >= dashReadyAt && spendMana(balance.DashManaCost))
            {
                Dashing = DashStarted = true;
                dashUntil = tick + (ulong)FlightBalance.Ticks(balance.DashSeconds);
                recoveryUntil = dashUntil + (ulong)FlightBalance.Ticks(balance.DashRecoverySeconds);
                dashReadyAt = tick + (ulong)FlightBalance.Ticks(balance.DashCooldownSeconds);
            }
        }
    }
}

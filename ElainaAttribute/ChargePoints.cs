using System;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>角色拥有端的充能点。TryCast 只在技能成功出手后结算。</summary>
public sealed class ChargePoints
{
    public static int Maximum => 8;
    public int Current { get; private set; }
    private int reserved;
    // 一点等于 60 单位。每游戏 tick 直接累加每秒速率，避免反复除以 60 造成整点延迟。
    internal const int UnitsPerPoint = 60;
    internal double RecoveryUnits { get; private set; }
    /// <summary>下一点的恢复进度（0–1），满点时为 0。</summary>
    internal float RecoveryFraction => Current == Maximum ? 0 : (float)(RecoveryUnits / UnitsPerPoint);

    internal void Load(int current, double units)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(units);
        if (!double.IsFinite(units) || units >= UnitsPerPoint)
            throw new ArgumentOutOfRangeException(nameof(units));
        Current = Math.Clamp(current, 0, Maximum);
        RecoveryUnits = Current == Maximum ? 0 : units;
    }

    internal void RecoverTick(float pointsPerSecond)
    {
        if (Current == Maximum || pointsPerSecond == 0) return;
        if (!float.IsFinite(pointsPerSecond) || pointsPerSecond < 0)
            throw new ArgumentOutOfRangeException(nameof(pointsPerSecond));
        RecoveryUnits += pointsPerSecond;
        int gained = (int)Math.Min(Maximum - Current, RecoveryUnits / UnitsPerPoint);
        RecoveryUnits -= gained * UnitsPerPoint;
        Restore(gained);
    }

    public bool Has(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return Current >= amount;
    }

    public int Restore(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        int added = Math.Min(amount, Maximum - Current);
        Current += added;
        if (Current == Maximum) RecoveryUnits = 0;
        return added;
    }

    public bool TrySpend(int amount)
    {
        if (!Has(amount) || Current - reserved < amount) return false;
        Current -= amount;
        return true;
    }

    // 预留防止嵌套调用花掉同一笔点数。技能在回调内复查 CanUseSkill 时仍能看到本次额度。
    internal bool TryCast<T>(int cost, T source, Func<T, bool> cast)
    {
        if (!Has(cost) || Current - reserved < cost) return false;
        reserved += cost;
        try
        {
            if (!cast(source)) return false;
            Current -= cost;
            return true;
        }
        finally { reserved -= cost; }
    }
}

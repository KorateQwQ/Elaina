using System;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

public static class AlchemyEfficiency
{
    public const int MaximumLevel = 8;
    public const int SlowestDurationTicks = 240, FastestDurationTicks = 60;

    public static int BonusYieldChancePercent(int level) => 5 * (Math.Clamp(level, 1, MaximumLevel) - 1);

    public static int BrewingDurationTicks(int level) => (int)Math.Round(SlowestDurationTicks
        - (SlowestDurationTicks - FastestDurationTicks) * (Math.Clamp(level, 1, MaximumLevel) - 1d) / (MaximumLevel - 1));

    public static double BrewingSpeedBonusPercent(int level) => (SlowestDurationTicks / (double)BrewingDurationTicks(level) - 1) * 100;
}

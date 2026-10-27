using System;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>初始四只玩偶，进修仅增加动作速度；等级门槛由 SkillBalance.json 提供。</summary>
public static class DollControlProgression
{
    public const int MaximumLevel = 8;
    public const int TeamSize = 4;
    public const int UpgradeCost = 5;
    public static int ClampLevel(int level) => Math.Clamp(level, 1, MaximumLevel);
    public static int DollCount(int level) => TeamSize;
    public static int ActionSpeedBonusPercent(int level) => ClampLevel(level) switch
    {
        MaximumLevel => 200,
        var current => (current - 1) * 25
    };
}

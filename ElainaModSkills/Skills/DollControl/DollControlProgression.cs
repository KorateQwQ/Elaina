using System;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>人偶数量与累计动作速度；等级门槛由 SkillBalance.json 提供。</summary>
public static class DollControlProgression
{
    public const int MaximumLevel = 8;
    public static int ClampLevel(int level) => Math.Clamp(level, 1, MaximumLevel);
    public static int DollCount(int level) => (ClampLevel(level) + 1) / 2;
    public static int ActionSpeedBonusPercent(int level) => ClampLevel(level) switch
    {
        8 => 50,
        >= 6 => 30,
        >= 4 => 20,
        >= 2 => 10,
        _ => 0
    };
}

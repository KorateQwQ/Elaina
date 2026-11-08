namespace 伊蕾娜.ElainaModSkills;

/// <summary>输出技能的研习预算。解封费用仍由各技能的条件定义。</summary>
internal static class CombatSkillProgression
{
    internal const int BasicUpgradeCost = 10;
    internal const int ActiveUpgradeCost = 30;

    internal static (int Previous, int Current) UpgradeCosts(string id) => id switch
    {
        "MagicMissileSkill" or "WindSlashSkill" or "WaterLaserSKill" => (5, BasicUpgradeCost),
        "IceConeSkill" or "FireLaserSkill" => (20, BasicUpgradeCost),
        "WaterBallSkill" or "MultiMissileSkill" => (15, ActiveUpgradeCost),
        "ShardbindSkill" or "FireBurstSkill" or "WindTornadoSkill" or "FireTornadoSkill" => (20, ActiveUpgradeCost),
        _ => (20, 20)
    };
}

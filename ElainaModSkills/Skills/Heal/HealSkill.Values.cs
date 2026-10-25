using System;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.Heal;

public partial class HealSkill
{
    internal const int HealingSeconds = 10;

    // Clamp old overleveled saves and the max-level preview to the final healing tier.
    internal static int GetTotalHealing(int level) => 100 + 20 * (Math.Clamp(level, 1, 8) - 1);

    public override bool AllowsLifeConversion => false;

    protected override object[] SkillDescriptionArgs =>
        [GetTotalHealing(Level), GetTotalHealing(Level) / HealingSeconds, HealingSeconds];

    public override (string Label, string Value, string Unit)[] GetConstellationStats(int level) =>
        [(LocalizeUI("HealingTotal"), GetTotalHealing(level).ToString(), "HP"),
         (LocalizeUI("HealingPerSecond"), (GetTotalHealing(level) / HealingSeconds).ToString(), "HP/s")];

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel) =>
        [(LocalizeUI("HealingTotal"), GetTotalHealing(nextLevel - 1).ToString(), GetTotalHealing(nextLevel).ToString(), "HP"),
         (LocalizeUI("HealingPerSecond"), (GetTotalHealing(nextLevel - 1) / HealingSeconds).ToString(),
             (GetTotalHealing(nextLevel) / HealingSeconds).ToString(), "HP/s")];
}

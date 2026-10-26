using System;
using System.Globalization;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.Alchemy;

public partial class AlchemySkill
{
    private static string CapLabel => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.AlchemyLevelCap");
    private static string BrewSpeed(int level) => AlchemyEfficiency.BrewingSpeedBonusPercent(level).ToString("+0", CultureInfo.InvariantCulture);

    public override (string Label, string Value, string Unit)[] GetConstellationStats(int level)
    {
        var player = Player ?? (Main.gameMenu ? null : Main.LocalPlayer);
        var progress = player?.GetModPlayer<AlchemyProgressPlayer>();
        int actualLevel = progress?.Level ?? 1;
        bool full = progress == null || actualLevel >= progress.MaximumLevel;
        return [(LocalizeUI("AlchemyLevelTitle"), actualLevel.ToString(), $"/ {progress?.MaximumLevel ?? Math.Clamp(level, 1, NotebookLevelLimit)}"),
            (LocalizeUI("AlchemyExpLabel"), full ? "MAX" : progress.Experience.ToString(), full ? "" : $"/ {progress.ExperienceToNextLevel}"),
            (LocalizeUI("AlchemyBrewSpeed"), BrewSpeed(actualLevel), "%"),
            (LocalizeUI("AlchemyBonusYield"), AlchemyEfficiency.BonusYieldChancePercent(actualLevel).ToString(), "%")];
    }

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        int current = Math.Clamp(nextLevel - 1, 1, NotebookLevelLimit);
        int next = Math.Clamp(nextLevel, 1, NotebookLevelLimit);
        return [(CapLabel, current.ToString(), next.ToString(), ""),
            (LocalizeUI("AlchemyCapBrewSpeed"), BrewSpeed(current), BrewSpeed(next), "%"),
            (LocalizeUI("AlchemyCapBonusYield"), AlchemyEfficiency.BonusYieldChancePercent(current).ToString(),
                AlchemyEfficiency.BonusYieldChancePercent(next).ToString(), "%")];
    }
}

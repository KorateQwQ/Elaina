using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.Alchemy;

[SkillUIInfo(State = 4, Pixels = 100)]
public class AlchemySkill : ElainaSkill
{
    public override bool IsPassiveSkill => true;
    // JSON supplies growth only; a passive has no casting cost, cooldown or charges.
    protected override bool UsesLiveBalance => false;

    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost,
            RequireConfiguredCharacterLevelCap(nameof(AlchemySkill), 10),
            SkillUnlockCondition.Custom(
                (player, _) => player?.Player.GetModPlayer<ElainaStatePlayer>().GetLevel() >= UnlockLevel,
                description: (_, _) => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.UnlockConditions.CharacterLevel", UnlockLevel)));

    private static int UnlockLevel => SkillBalanceSystem.TryGetSkill(nameof(AlchemySkill), out var entry) ? entry.UnlockLevel : 6;

    public override int MaxLevel => Player != null && !Main.gameMenu
        && SkillBalanceSystem.TryGetSkill(nameof(AlchemySkill), out var entry)
        ? Math.Max(1, SkillBalanceSystem.Current.GetSkillLevelCap(entry, Player.GetModPlayer<ElainaStatePlayer>().GetLevel())) : 1;

    public override void Initialize()
    {
        base.Initialize();
        MaxCD = MagicPointCost = -1;
    }

    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        MaxCD = MagicPointCost = -1;
        CurrentCD = CooldownDuration = 0;
    }

    public override bool CanUseSkill() => false;
    public override bool PreUpdateCD() => false;

    // Apply the fixed configured cap even to skills learned above it in older saves.
    private static int NotebookLevelLimit => SkillBalanceSystem.TryGetSkill(nameof(AlchemySkill), out var entry)
        && entry.MaxLevel > 0 ? Math.Min(entry.MaxLevel, AlchemyProgressionRules.MaximumLevel) : AlchemyProgressionRules.MaximumLevel;

    public static int GetNotebookMaximumLevel(Player player) => player != null
        && player.GetModPlayer<ElainaSkillModPlayer>().TryGetUnlockedModSkill(out AlchemySkill skill)
        ? Math.Clamp(skill.Level, 1, NotebookLevelLimit) : 1;

    private static string CapLabel => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.AlchemyLevelCap");
    // Mirrors the notebook header: current alchemy level against the cap this skill grants, then EXP.
    public override (string Label, string Value, string Unit)[] GetConstellationStats(int level)
    {
        var player = Player ?? (Main.gameMenu ? null : Main.LocalPlayer);
        if (player == null) return [(CapLabel, Math.Clamp(level, 1, NotebookLevelLimit).ToString(), "")];
        var progress = player.GetModPlayer<AlchemyProgressPlayer>();
        bool full = progress.Level >= progress.MaximumLevel;
        return [(LocalizeUI("AlchemyLevelTitle"), progress.Level.ToString(), $"/ {progress.MaximumLevel}"),
            (LocalizeUI("AlchemyExpLabel"), full ? "MAX" : progress.Experience.ToString(), full ? "" : $"/ {progress.ExperienceToNextLevel}")];
    }
    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel) =>
        [(CapLabel, Math.Clamp(Level, 1, NotebookLevelLimit).ToString(),
            Math.Clamp(nextLevel, 1, NotebookLevelLimit).ToString(), "")];
}

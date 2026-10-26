using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.Alchemy;

[SkillUIInfo(State = 4, Pixels = 100)]
public partial class AlchemySkill : ElainaSkill
{
    public override bool IsPassiveSkill => true;
    // JSON supplies growth only; a passive has no casting cost, cooldown or charges.
    protected override bool UsesLiveBalance => false;

    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(SkillUnlockCondition.BySkillPoint(10),
            RequireConfiguredCharacterLevelCap(nameof(AlchemySkill), 10),
            RequireConfiguredCharacterLevel(nameof(AlchemySkill), 6));

    public override SkillUnlockCondition GetConstellationUpgradeCondition(int nextLevel) => SkillUnlockCondition.BySkillPoint(15);

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

    /// <summary>The cauldron and notebook research belong to Elaina after she learns this skill.</summary>
    public static bool IsLearned(Player player) => player != null && player.GetModPlayer<ElainaModplayer>().Elaina
        && player.GetModPlayer<ElainaSkillModPlayer>().HasUnlockedSkill<AlchemySkill>();

    public static int GetNotebookMaximumLevel(Player player) => player != null
        && player.GetModPlayer<ElainaSkillModPlayer>().TryGetUnlockedModSkill(out AlchemySkill skill)
        ? Math.Clamp(skill.Level, 1, NotebookLevelLimit) : 1;

}

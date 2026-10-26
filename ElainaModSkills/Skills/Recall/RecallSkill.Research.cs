using KL.SkillSystem;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

public partial class RecallSkill
{
    public override bool IsConstellationRevealed(Player player) =>
        player?.GetModPlayer<RecallDiscoveryPlayer>().HasObtainedReturnPotion == true;

    public override string ConstellationRevealHint => Language.GetTextValue(
        "Mods.伊蕾娜.SkillInfo.UnlockConditions.ObtainedReturnPotion");

    public override SkillUnlockCondition UnlockCondition { get; set; } =
        SkillUnlockCondition.ByItemsAndSkillPoint(20, new SkillUnlockItem(ItemID.PotionOfReturn, 1));

    public override bool TryUnlockSkill(KLSkillModPlayer skillPlayer) =>
        IsConstellationRevealed(skillPlayer?.Player) && base.TryUnlockSkill(skillPlayer);
}

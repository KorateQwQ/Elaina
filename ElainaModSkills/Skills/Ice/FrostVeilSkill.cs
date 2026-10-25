using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using Terraria.ID;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

[SkillUIInfo(State = 7, Pixels = 650)]
public sealed class FrostVeilSkill : ElainaSkill
{
    public override Type[] PrerequisiteSkills => [typeof(IceConeSkill)];
    public override Vector2? ConstellationPosition => new Vector2(700, 590);
    // 习得冰锥术即可研习，只需通用消耗。
    public override SkillUnlockCondition UnlockCondition { get; set; } = DefaultUnlockCost;

    protected override object[] SkillDescriptionArgs =>
        [FrostVeilSystem.CastRadius, Player?.GetModPlayer<ElainaAttributeModPlayer>().MaxMagicPoint / 20f ?? 0f];

    public override void Initialize()
    {
        MaxCD = 60;
        MagicPointCost = 50;
        // Only cost, cooldown and progression are used. No attack damage or freeze buildup is calculated.
        base.Initialize();
    }

    public override bool CanUseSkill() => Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer
        && Player.active && !Player.dead && Stack > 0 && base.CanUseSkill();

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!CanUseSkill()) return false;
        var attributes = Player.GetModPlayer<ElainaAttributeModPlayer>();
        float absorption = attributes.MaxMagicPoint / 20f;
        if (absorption <= 0f || !attributes.ConsumeMagicPoint(MagicPointCost)) return false;
        FrostVeilSystem.Cast(Player, absorption);
        return base.PreUseSkill(source);
    }
}

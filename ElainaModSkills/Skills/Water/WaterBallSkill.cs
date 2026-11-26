using 伊蕾娜.Localization;
using System;
using static 伊蕾娜.Localization.UIText;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

[SkillUIInfo(State = 12, Pixels = 400)]
public class WaterBallSkill: ElainaSkill
{
    internal static int ChargeTicks => 60;

    private bool quickCasting;

    internal static bool TryGetEquipped(Player player, out WaterBallSkill skill)
    {
        var skills = player.GetModPlayer<ElainaSkillModPlayer>();
        return skills.TryGetUnlockedModSkill(out skill) && skill.Skill != null
            && skills.ActiveSkill.Contains(skill.Skill);
    }

    internal bool TryQuickCast()
    {
        var actions = Player.GetModPlayer<ActionModPlayer>();
        if (actions.CurrentAnimAction is not Action_WaterLaser { CanQuickCast: true }
            || !TryGetEquipped(Player, out var equipped) || !ReferenceEquals(equipped, this) || Stack <= 0
            || !Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled || !CanUseSkill())
            return false;

        int previousToken = actions.CurrentActionToken;
        quickCasting = true;
        // 使用玩家已学习的同一个技能实例，沿用层数、急速冷却和正式耗魔入口。
        Skill.UseSkill(Player.GetSource_Misc(nameof(WaterBallSkill)),
            Player.GetModPlayer<ElainaSkillModPlayer>().Attributes);
        quickCasting = false;
        return actions.CurrentActionToken != previousToken;
    }

    public override Type[] PrerequisiteSkills => [typeof(WaterLaserSKill)];

    // 学习门槛读取配置中的角色等级上限，默认至少达到 30；水流术需已投入到 3 级。
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(SkillUnlockCondition.BySkillPoint(25), RequireConfiguredCharacterLevelCap(nameof(WaterBallSkill), 30),
            RequireSkillLevel<WaterLaserSKill>(3));


    protected override object[] SkillDescriptionArgs =>
        [SkillValueText.Damage(GetConfiguredDamage(), SkillValueKind.Water), (SkillHitBalance.WaterBallBurningDamageMultiplier - 1f) * 100f, SkillHitBalance.WaterBallBuildup, ChargeTicks / 60f];
    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单次伤害
        int current = GetConfiguredDamage(skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(skillLevel: nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override bool OnCast(IEntitySource source)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        Player localPlayer = Main.LocalPlayer;
        Vector2 directionToMouse = Main.MouseWorld - localPlayer.MountedCenter;

        float startRotation = directionToMouse.ToRotation()*Main.LocalPlayer.gravDir;

        var action = new Action_WaterBall(GetConfiguredDamage(), SkillHitBalance.WaterBallBurningDamageMultiplier, quickCasting);
        if (!localPlayer.GetModPlayer<ActionModPlayer>().StartAction(action, rotation: startRotation)) return false;

        return base.OnCast(source);
    }

}

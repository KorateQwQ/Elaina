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

    /// <summary>对燃烧目标额外增加的周期 DPS 倍率，与基础 ratio 相加。</summary>
    private static double BurningBonusRatio => 0.1;

    /// <summary>按当前技能等级的 ratio 折算燃烧加成的伤害乘数：(ratio + 0.1) / ratio。</summary>
    private float GetBurningDamageMultiplier()
    {
        if (!SkillBalanceSystem.TryGetSkill(BalanceId, out SkillBalanceEntry entry))
            throw new InvalidOperationException($"缺少技能平衡配置：{BalanceId}");
        double ratio = SkillBalanceConfig.GetDpsRatio(entry,
            SkillBalanceSystem.Current.GetSkillCharacterLevel(entry, Math.Max(1, Level)));
        if (ratio <= 0) throw new InvalidOperationException($"{BalanceId} 的 ratio 必须大于 0，当前为 {ratio}");
        return (float)((ratio + BurningBonusRatio) / ratio);
    }

    protected override object[] SkillDescriptionArgs => [GetConfiguredDamage(), (GetBurningDamageMultiplier() - 1f) * 100f];
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

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        Player localPlayer = Main.LocalPlayer;
        Vector2 directionToMouse = Main.MouseWorld - localPlayer.MountedCenter;

        float startRotation = directionToMouse.ToRotation()*Main.LocalPlayer.gravDir;

        localPlayer.GetModPlayer<ActionModPlayer>()
            .StartAction(new Action_WaterBall(GetConfiguredDamage(), GetBurningDamageMultiplier(), quickCasting), rotation: startRotation);

        return base.PreUseSkill(source);
    }

    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale,  Color color, Effect effect = null)
    {
        //if (CurrentCD > 1) CurrentCD = 1;
        return base.PreDrawSkillIcon(position, scale, color, effect);
    }
}

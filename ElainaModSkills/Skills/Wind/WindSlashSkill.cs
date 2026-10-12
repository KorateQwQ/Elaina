using System;
using KL.ActionsSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

[SkillUIInfo(State = 1, Pixels = 300)]

public class WindSlashSkill : ElainaSkill
{
    /// <summary>风刃击退力度；原版按 knockBackResist 衰减，Boss 等免疫击退的敌人不受影响。</summary>
    private const float SlashKnockback = 6f;

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单次伤害
        int current = GetConfiguredDamage(skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(skillLevel: nextLevel);
        return [("伤害", current.ToString(), next.ToString(), "")];
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        AnimAction animAction = new Action_SimpleShoot(24);
        animAction.AddNode(new ShootActionNode(
            1,
            ModContent.ProjectileType<WindSlash>(),
            _ => WandCenter,
            damage: GetConfiguredDamage(),
            SlashKnockback,
            player => new Vector2(1, 0).RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation()) * 20f)
        );

        float startRotation = (Main.MouseWorld - Player.MountedCenter).ToRotation();
        Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
        return base.PreUseSkill(source);
    }

    protected override object[] SkillDescriptionArgs => new object[]
    {
        GetConfiguredDamage(),
    };
}

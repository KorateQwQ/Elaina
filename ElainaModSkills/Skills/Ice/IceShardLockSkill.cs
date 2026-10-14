using static 伊蕾娜.Localization.UIText;
using System;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

[SkillUIInfo(State = 6, Pixels = 400)]
public class IceShardLockSkill : ElainaSkill
{
    public override void Initialize()
    {
        base.Initialize();
    }
    public override void ResetEffects(Player player)
    {
        //测试用。
        if (CurrentCD > 1) CurrentCD = 1;
        //MagicPointCost = 5;//(int)(player.statMana*0.2)+200;
        //MaxStack = 1;
        base.ResetEffects(player);
    }
    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单次伤害
        int current = GetConfiguredDamage(skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(skillLevel: nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }

    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireSkillLevel<IceConeSkill>(7));
    
    public override bool CanUseSkill()
    {
        MagicPointCost = 3;

        return base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        ShootIceShardLock3D(Player, Main.MouseWorld);
        return base.PreUseSkill(source);
    }
    
    public void ShootIceShardLock3D(Player player, Vector2 targetWorldPosition)
    {
        Vector2 aimDirection = (targetWorldPosition - player.MountedCenter).SafeNormalize(Vector2.UnitX * (player.direction == 0 ? 1 : player.direction));

        AnimAction animAction = new Action_Cast()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<IceShardLockProj>(),
                _ => targetWorldPosition,
                GetConfiguredDamage(),
                2f,
                _ => Vector2.Zero));

        float startRotation = aimDirection.ToRotation() * player.gravDir;
        player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
    }
}
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
    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单次伤害
        int current = GetConfiguredDamage(hitCount: 1, skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(hitCount: 1, skillLevel: nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }

    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireSkillLevel<IceConeSkill>(7));
    
    public override bool CanUseSkill()
    {
        return Player.whoAmI == Main.myPlayer
            && !Player.GetModPlayer<ActionModPlayer>().HasAction
            && IceConeTargetSystem.ResolveLockTarget() != null
            && base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        NPC target = IceConeTargetSystem.ResolveLockTarget();
        if (target == null || !CanUseSkill() || !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        ShootIceShardLock3D(Player, target);
        return base.PreUseSkill(source);
    }
    
    public void ShootIceShardLock3D(Player player, NPC target)
    {
        Vector2 targetWorldPosition = target.Center;
        uint selectedSpawnSerial = target.GetGlobalNPC<IceboundNPC>().SpawnSerial;
        float freezeBuildup = GetConfiguredFreezeBuildup();
        Vector2 aimDirection = (targetWorldPosition - player.MountedCenter).SafeNormalize(Vector2.UnitX * (player.direction == 0 ? 1 : player.direction));

        AnimAction animAction = new Action_SimpleSlash()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<IceShardLockProj>(),
                _ => target.active ? target.Center : targetWorldPosition,
                GetConfiguredDamage(hitCount: 1),
                2f,
                _ => Vector2.Zero,
                projectile =>
                {
                    ConfigureFreezeBuildup(projectile, freezeBuildup);
                    ((IceShardLockProj)projectile.ModProjectile).BindTarget(target, selectedSpawnSerial);
                }));

        float startRotation = aimDirection.ToRotation() * player.gravDir;
        player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
    }

    protected override object[] SkillDescriptionArgs => [GetConfiguredDamage(hitCount: 1), GetConfiguredFreezeBuildup()];
}

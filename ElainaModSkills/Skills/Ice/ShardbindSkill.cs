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
public class ShardbindSkill : ElainaSealedSkill
{
    public override Type[] PrerequisiteSkills => [typeof(IceConeSkill)];

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单次伤害
        int current = GetConfiguredDamage(hitCount: 1, skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(hitCount: 1, skillLevel: nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }

    // 当前成长配置下，花后角色等级上限 Lv.65 对应冰锥术满级 Lv.19。
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireSkillLevel<IceConeSkill>(19),
            SkillUnlockCondition.Custom((_, _) => NPC.downedChristmasIceQueen,
                description: (_, _) => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.UnlockConditions.IceQueen")));
    
    public override bool CanUseSkill()
    {
        return Player.whoAmI == Main.myPlayer
            && !Player.GetModPlayer<ActionModPlayer>().HasAction
            && IceConeTargetSystem.ResolveTarget() != null
            && base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        NPC target = IceConeTargetSystem.ResolveTarget();
        var attributes = Player.GetModPlayer<ElainaAttributeModPlayer>();
        if (target == null || !CanUseSkill() || !attributes.ConsumeMagicPoint(MagicPointCost, false))
            return false;

        if (!ShootShardbind3D(Player, target)) return false;
        if (!attributes.ConsumeMagicPoint(MagicPointCost))
        {
            Player.GetModPlayer<ActionModPlayer>().InterruptCurrentAction();
            return false;
        }
        Seal();
        return base.PreUseSkill(source);
    }
    
    public bool ShootShardbind3D(Player player, NPC target)
    {
        Vector2 targetWorldPosition = target.Center;
        uint selectedSpawnSerial = target.GetGlobalNPC<IceboundNPC>().SpawnSerial;
        float freezeBuildup = GetConfiguredFreezeBuildup();
        Vector2 aimDirection = (targetWorldPosition - player.MountedCenter).SafeNormalize(Vector2.UnitX * (player.direction == 0 ? 1 : player.direction));

        AnimAction animAction = new Action_SimpleSlash()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<ShardbindProj>(),
                _ => target.active ? target.Center : targetWorldPosition,
                GetConfiguredDamage(hitCount: 1),
                2f,
                _ => Vector2.Zero,
                projectile =>
                {
                    ConfigureFreezeBuildup(projectile, freezeBuildup);
                    ((ShardbindProj)projectile.ModProjectile).BindTarget(target, selectedSpawnSerial);
                }));

        float startRotation = aimDirection.ToRotation() * player.gravDir;
        return player.GetModPlayer<ActionModPlayer>().StartAction(animAction, interruptCurrentAction: false, rotation: startRotation);
    }

    protected override object[] SkillDescriptionArgs => [GetConfiguredDamage(hitCount: 1), GetConfiguredFreezeBuildup()];
}

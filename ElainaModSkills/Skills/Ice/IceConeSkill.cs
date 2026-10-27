using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using Terraria.ID;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaDamageClass;
using 伊蕾娜.ElainaModSkills.Skills.MagicMissile;
using 伊蕾娜.ElainaModSkills.Skills.Wind;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

[SkillUIInfo(State = 7, Pixels = 500)]
public class IceConeSkill : ElainaSkill
{
    protected override object[] SkillDescriptionArgs => [GetConfiguredDamage(hitCount: 3), GetConfiguredFreezeBuildup(), ShardbindSkill.NameFor(Player)];
    public override bool IsPassiveSkill => false;

    // 当前 JSON 配置为血肉墙后开放（角色等级上限 40）；门槛由 unlockStageCap 决定，不要求实际等级达到上限。
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireConfiguredCharacterLevelCap(nameof(IceConeSkill), 25),
            SkillUnlockCondition.ByItems(new SkillUnlockItem(ItemID.FrostCore, 1)));


    public override void Initialize()
    {
        base.Initialize();
    }

    public override void ResetEffects(Player player)
    {
        base.ResetEffects(player);
    }

    public override void OnRightClickInSkillPanel()
    {
        base.OnRightClickInSkillPanel();
    }

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单次伤害
        int current = GetConfiguredDamage(skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(skillLevel: nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }
    
    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        // 锁定碰撞箱距离鼠标最近的敌人，没有目标时照常朝鼠标发射
        NPC target = IceConeTargetSystem.ResolveTarget();
        ShootIceCone(Player, target?.Center ?? Main.MouseWorld, baseSpeed: 35, lockedTarget: target);
        return base.PreUseSkill(source);
    }


    public void ShootIceCone(Player player, Vector2 aimTarget, int? damage = null, float knockback = 2f,
        float baseSpeed = 25f, NPC lockedTarget = null)
    {
        Vector2 directionToMouse = aimTarget - player.MountedCenter;
        Vector2 aimDirection = directionToMouse.SafeNormalize(Vector2.UnitX * player.direction);
        Vector2 backwardDirection = -aimDirection;

        Vector2[][] positionSets =
        [
            [
                backwardDirection * 10f,
                backwardDirection * -20f + aimDirection.RotatedBy(1.5) * 55f,
                backwardDirection * -55f - aimDirection.RotatedBy(1.5) * 25f,
            ],
            [
                backwardDirection * -20f,
                backwardDirection * -70f + aimDirection.RotatedBy(1.5) * 55f,
                backwardDirection * -55f - aimDirection.RotatedBy(1.5) * 75f,
            ],
            [
                backwardDirection * 30f,
                backwardDirection * -70f + aimDirection.RotatedBy(1.5) * 55f,
                backwardDirection * -15f - aimDirection.RotatedBy(2.5) * 55f,
            ],
        ];

        Vector2[] offsets = positionSets[Main.rand.Next(positionSets.Length)];
        Vector2[] spawnPositions = new Vector2[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            spawnPositions[i] = player.MountedCenter + offsets[i];
        }

        // 按本动作实际生成的发数分摊，不再手写技能名字，也不让每发领取整次预算。
        int damagePerShot = damage ?? GetConfiguredDamage(hitCount: spawnPositions.Length);
        float freezeBuildup = GetConfiguredFreezeBuildup();
        AnimAction animAction = new Action_SimpleSlash();
        int count = 0;
        Vector2 GetAimPoint() => lockedTarget is { active: true } ? lockedTarget.Center : aimTarget;
        foreach (Vector2 spawnPosition in spawnPositions)
        {
            Vector2 cachedSpawnPosition = spawnPosition;
            animAction.AddNode(new ShootActionNode(
                1 + count,
                ModContent.ProjectileType<IceConeProj>(),
                _ => cachedSpawnPosition,
                damagePerShot,
                knockback,
                // 各发冰锥在动作的不同帧生成，锁定时取目标生成那一刻的位置
                _ => (GetAimPoint() - cachedSpawnPosition).SafeNormalize(aimDirection) * baseSpeed,
                // ai[0] = 目标 whoAmI + 1，冰锥后撤蓄力期间持续转向该目标
                projectile =>
                {
                    ConfigureFreezeBuildup(projectile, freezeBuildup);
                    if (lockedTarget != null) projectile.ai[0] = lockedTarget.whoAmI + 1;
                }));
            count += 3;
        }

        float startRotation = aimDirection.ToRotation() * player.gravDir;
        player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
    }
    

    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale, Color color, Effect effect = null)
    {
        return base.PreDrawSkillIcon(position, scale, color, effect);
    }

    public override void OnUnlockSkillAdded()
    {
        base.OnUnlockSkillAdded();
    }

    public override void OnLockSkill()
    {
        base.OnLockSkill();
    }
}


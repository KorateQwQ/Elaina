using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using System;
using Terraria.ModLoader.IO;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

[SkillUIInfo(State = 8, Pixels = 600)]

public class FireBurstSkill : ElainaSkill
{
    private SkillUnlockCondition unlockCondition;
    public override SkillUnlockCondition UnlockCondition
    {
        get => unlockCondition ??= new CompositeSkillUnlockCondition(DefaultUnlockCost,
            SkillUnlockCondition.ByItems(new SkillUnlockItem(ModContent.ItemType<item.FlameSpellbook>(), 1)),
            RequireConfiguredCharacterLevelCap(nameof(FireBurstSkill), 50),
            new StageGateUnlockCondition((_, _) => NPC.downedMechBossAny, (_, _) => NPC.downedMechBossAny,
                (_, _) => Language.GetTextValue("Conditions.DownedMechBossAny")));
        set => unlockCondition = value;
    }

    protected override object[] SkillDescriptionArgs => [GetConfiguredDamage(), MagicPointCost, MaxStack, MaxCD];

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel) =>
        [(LocalizeUI("Damage"), GetConfiguredDamage(skillLevel: Math.Max(1, nextLevel - 1)).ToString(),
            GetConfiguredDamage(skillLevel: nextLevel).ToString(), "")];

    public override bool CanUseSkill() => Player.whoAmI == Main.myPlayer && Stack > 0
        && !Player.GetModPlayer<ActionModPlayer>().HasAction && base.CanUseSkill();

    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        if (Stack >= MaxStack) CurrentCD = 0;
    }

    public override bool PreUpdateCD()
    {
        bool update = base.PreUpdateCD();
        if (Stack < MaxStack) return update;
        CurrentCD = 0;
        return false;
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!CanUseSkill())
            return false;

        Vector2 position = Main.MouseWorld;
        AnimAction animAction = new Action_SimpleShoot()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<FireBurstProj>(),
                _ => position,
                damage: GetConfiguredDamage(),
                knockback: 2f,
                getVelocity: _ => Vector2.Zero));

        float startRotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
        if (!Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, interruptCurrentAction: false, rotation: startRotation))
            return false;
        Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost);
        return base.PreUseSkill(source);
    }

}

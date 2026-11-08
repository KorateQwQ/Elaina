using static 伊蕾娜.Localization.UIText;
using System;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using Terraria.ID;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

[SkillUIInfo(State = 7, Pixels = 500)]
public class IceConeSkill : ElainaSkill
{
    protected override object[] SkillDescriptionArgs => [GetConfiguredDamage(hitCount: 1), GetConfiguredFreezeBuildup() * .5f, ShardbindSkill.NameFor(Player)];
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
    
    public override bool CanUseSkill() => Player.whoAmI == Main.myPlayer
        && !Player.GetModPlayer<ActionModPlayer>().HasAction
        && base.CanUseSkill();

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!CanUseSkill() || !TryGetConfiguredBalance(out SkillBalanceResult balance))
            return false;

        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        int intervalFrames = Math.Max(1, (int)Math.Round(balance.Entry.BaseCooldown * 60));
        float rotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
        var action = new Action_IceCone(this, balance.DamagePerHit, (float)balance.Entry.ManaCost,
            intervalFrames, GetConfiguredFreezeBuildup());
        if (!Player.GetModPlayer<ActionModPlayer>().StartAction(action, interruptCurrentAction: false, rotation: rotation))
            return false;

        return base.PreUseSkill(source);
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


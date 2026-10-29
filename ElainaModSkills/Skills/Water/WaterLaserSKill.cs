using static 伊蕾娜.Localization.UIText;
using System;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaModSkills.item;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

[SkillUIInfo(State = 4, Pixels = 400)]
public class WaterLaserSKill : ElainaSkill
{
    private SkillUnlockCondition unlockCondition;

    // 默认骷髅王后开放（角色等级上限 30）；具体门槛读取 JSON，不要求实际等级达到上限。
    // 研习消耗一本水魔法书。技能模板在加载期创建，物品类型此时可能尚未注册，因此首次读取时再构建。
    public override SkillUnlockCondition UnlockCondition
    {
        get => unlockCondition ??= new CompositeSkillUnlockCondition(SkillUnlockCondition.BySkillPoint(25),
            SkillUnlockCondition.ByItems(new SkillUnlockItem(ModContent.ItemType<WaterSpellbook>(), 1)),
            RequireConfiguredCharacterLevelCap(nameof(WaterLaserSKill), 30));
        set => unlockCondition = value;
    }

    public override SkillUnlockCondition GetConstellationUpgradeCondition(int nextLevel) => SkillUnlockCondition.BySkillPoint(5);

    public override bool CanUseSkill()
    {
        return Player.whoAmI == Main.myPlayer
            && !Player.GetModPlayer<ActionModPlayer>().HasAction
            && Player.ownedProjectileCounts[ModContent.ProjectileType<WaterLaser>()] == 0
            && base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!CanUseSkill() || !TryGetConfiguredBalance(out SkillBalanceResult balance))
            return false;

        // 整次引导使用同一份配置；每个伤害周期开始时由射线支付一次费用。
        int intervalFrames = Math.Max(1, (int)Math.Round(balance.Entry.BaseCooldown * 60));
        float rotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
        return Player.GetModPlayer<ActionModPlayer>().StartAction(
            new Action_WaterLaser(balance.DamagePerHit, (float)balance.Entry.ManaCost, intervalFrames),
            interruptCurrentAction: false, rotation: rotation);
    }

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单次伤害
        int current = GetDescriptionDamage(Math.Max(1, nextLevel - 1));
        int next = GetDescriptionDamage(nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }

    // 弹幕仍按冻结 CD 期间的伤害生成；说明与升级预览改用通常伤害作为基数。
    private int GetDescriptionDamage(int? skillLevel = null) =>
        SkillBalanceConfig.ToHitDamage(GetConfiguredDamage(skillLevel: skillLevel)
            * WaterLaser.OutsideFreezeCooldownDamageMultiplier);

    protected override object[] SkillDescriptionArgs =>
    [
        MaxCD,
        GetDescriptionDamage(),
        WaterLaser.SplashDamageMultiplier * 100f,
        MagicPointCost,
        WaterLaser.FreezeCooldownDamageBonus * 100f,
        Ice.ShardbindSkill.NameFor(Player)
    ];
}

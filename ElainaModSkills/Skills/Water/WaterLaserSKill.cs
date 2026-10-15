using System;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

[SkillUIInfo(State = 4, Pixels = 400)]
public class WaterLaserSKill : ElainaSkill
{
    // 默认骷髅王后开放（角色等级上限 30）；具体门槛读取 JSON，不要求实际等级达到上限。
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireConfiguredCharacterLevelCap(nameof(WaterLaserSKill), 30));

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

    protected override object[] SkillDescriptionArgs =>
    [
        MaxCD,
        GetConfiguredDamage(),
        WaterLaser.SplashDamageMultiplier * 100f,
        MagicPointCost,
        WaterLaser.OutsideFreezeCooldownDamageMultiplier * 100f
    ];
}

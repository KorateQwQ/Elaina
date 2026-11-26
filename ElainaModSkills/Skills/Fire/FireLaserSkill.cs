using 伊蕾娜.Localization;
using System;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaActions;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

[SkillUIInfo(State = 6, Pixels = 600)]
public class FireLaserSkill : ElainaSkill
{
    public const float SkillCooldown = 0.3f;
    private bool channelActive;

    internal float GetDomainDamageMultiplier()
    {
        if (!SkillBalanceSystem.TryGetSkill(BalanceId, out var entry))
            throw new InvalidOperationException($"缺少技能平衡配置：{BalanceId}");
        double ratio = SkillBalanceConfig.GetDpsRatio(entry,
            SkillBalanceSystem.Current.GetSkillCharacterLevel(entry, Math.Max(1, Level)));
        if (ratio <= 0) throw new InvalidOperationException($"{BalanceId} 的 ratio 必须大于 0，当前为 {ratio}");
        return (float)((ratio + SkillHitBalance.FireLaserDomainBonusRatio) / ratio);
    }

    private SkillUnlockCondition unlockCondition;
    public override SkillUnlockCondition UnlockCondition
    {
        get => unlockCondition ??= new CompositeSkillUnlockCondition(DefaultUnlockCost,
            SkillUnlockCondition.ByItems(new SkillUnlockItem(ModContent.ItemType<item.FlameSpellbook>(), 1)),
            RequireConfiguredCharacterLevelCap(nameof(FireLaserSkill), 50),
            new StageGateUnlockCondition((_, _) => NPC.downedMechBossAny, (_, _) => NPC.downedMechBossAny,
                (_, _) => Language.GetTextValue("Conditions.DownedMechBossAny")));
        set => unlockCondition = value;
    }

    protected override object[] SkillDescriptionArgs
    {
        get
        {
            int intervalFrames = SkillBalanceSystem.TryGetSkill(BalanceId, out var entry)
                ? Math.Max(1, (int)Math.Round(entry.BaseCooldown * 60)) : 10;
            return
            [
                FireLaser.ChargeTicks / 60f,
                intervalFrames / 60f,
                SkillValueText.Damage(GetConfiguredDamage(), SkillValueKind.Fire),
                MagicPointCost,

                SkillHitBalance.FireLaserBuildup,
                (GetDomainDamageMultiplier() - 1f) * 100f,
                (SkillHitBalance.FireLaserFrostDamageMultiplier - 1f) * 100f
            ];
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        MaxCD = SkillCooldown;
    }

    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        MaxCD = SkillCooldown;
    }

    public override bool CanUseSkill() =>
        Player.whoAmI == Main.myPlayer && !channelActive && CurrentCD <= 0f
        && !Player.GetModPlayer<ActionModPlayer>().HasAction
        && Player.ownedProjectileCounts[ModContent.ProjectileType<FireLaser>()] == 0
        && base.CanUseSkill();

    protected override bool OnCast(IEntitySource source)
    {
        if (!CanUseSkill() || !TryGetConfiguredBalance(out SkillBalanceResult balance))
            return false;

        // 配置的攻击周期用于伤害与耗魔；技能冷却由通用施放流程独立计时。
        int intervalFrames = Math.Max(1, (int)Math.Round(balance.Entry.BaseCooldown * 60));
        float rotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
        if (!Player.GetModPlayer<ActionModPlayer>().StartAction(
                new Action_FireLaser(balance.DamagePerHit, (float)balance.Entry.ManaCost, intervalFrames, this, GetDomainDamageMultiplier()),
                interruptCurrentAction: false, rotation: rotation))
            return false;

        channelActive = true;
        MaxCD = SkillCooldown;
        return true;
    }

    internal void EndChannel()
    {
        channelActive = false;
    }

    public override bool PreUpdateCD()
    {
        bool update = base.PreUpdateCD();
        MaxCD = SkillCooldown;
        return update;
    }
}

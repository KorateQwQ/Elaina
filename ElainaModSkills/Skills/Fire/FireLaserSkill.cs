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
    public const float ReleaseCooldown = 1f;
    private bool channelActive;

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

    protected override object[] SkillDescriptionArgs =>
    [
        FireLaser.ChargeTicks / 60f,
        SkillBalanceSystem.TryGetSkill(BalanceId, out var entry)
            ? Math.Max(1, (int)Math.Round(entry.BaseCooldown * 60)) / 60f : 0.3f,
        GetConfiguredDamage(),
        MagicPointCost,
        KL.DamageSystem.FireBurnNPC.ExposureTicks / 60f,
        KL.DamageSystem.FireBurnNPC.DamageIgnitionFraction * 100
    ];

    public override void Initialize()
    {
        base.Initialize();
        MaxCD = ReleaseCooldown;
    }

    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        MaxCD = ReleaseCooldown;
    }

    public override bool CanUseSkill() =>
        Player.whoAmI == Main.myPlayer && !channelActive && CurrentCD <= 0f
        && !Player.GetModPlayer<ActionModPlayer>().HasAction
        && Player.ownedProjectileCounts[ModContent.ProjectileType<FireLaser>()] == 0
        && base.CanUseSkill();

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!CanUseSkill() || !TryGetConfiguredBalance(out SkillBalanceResult balance))
            return false;

        // 配置的攻击周期仍用于伤害与耗魔；松手后的防重触冷却单独计时。
        int intervalFrames = Math.Max(1, (int)Math.Round(balance.Entry.BaseCooldown * 60));
        float rotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
        if (!Player.GetModPlayer<ActionModPlayer>().StartAction(
                new Action_FireLaser(balance.DamagePerHit, (float)balance.Entry.ManaCost, intervalFrames, this),
                interruptCurrentAction: false, rotation: rotation))
            return false;

        channelActive = true;
        MaxCD = CurrentCD = CooldownDuration = ReleaseCooldown;
        return true;
    }

    internal void EndChannel()
    {
        channelActive = false;
        // 固定一秒的防重触间隔从结束引导开始，收势计入其中。
        MaxCD = CurrentCD = CooldownDuration = ReleaseCooldown;
    }

    public override bool PreUpdateCD()
    {
        bool update = base.PreUpdateCD();
        MaxCD = ReleaseCooldown;
        return update && !channelActive;
    }
}

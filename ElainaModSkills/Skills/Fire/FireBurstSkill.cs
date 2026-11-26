using 伊蕾娜.Localization;
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
    private bool quickCasting;

    internal static bool TryGetEquipped(Player player, out FireBurstSkill skill)
    {
        var skills = player.GetModPlayer<ElainaSkillModPlayer>();
        return skills.TryGetUnlockedModSkill(out skill) && skill.Skill != null
            && skills.ActiveSkill.Contains(skill.Skill);
    }

    /// <summary>引导火焰射线时右键快速施放：打断射线，播放完整的炎爆施法动作。返回是否已开始施法动作。</summary>
    internal bool TryQuickCast()
    {
        var actions = Player.GetModPlayer<ActionModPlayer>();
        if (actions.CurrentAnimAction is not Action_FireLaser { CanQuickCast: true }
            || !TryGetEquipped(Player, out var equipped) || !ReferenceEquals(equipped, this)
            || !Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled)
            return false;

        int previousToken = actions.CurrentActionToken;
        quickCasting = true;
        Skill.UseSkill(Player.GetSource_Misc(nameof(FireBurstSkill)),
            Player.GetModPlayer<ElainaSkillModPlayer>().Attributes);
        quickCasting = false;
        return actions.CurrentActionToken != previousToken;
    }

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

    // 领域内只有一名敌人时，炎爆（含单体加成）的目标 DPS 相对角色基准 DPS 的倍率；领域据此降低落下频率。
    public static double SingleTargetDpsRatio => .3;

    // 旧领域单体约每 1.92 秒落一道。能量定价后保留节奏，让调整系数直接改变伤害。
    private static double SoloStrikesPerSecond => .521;

    private static int StrikeDamage(SkillBalanceResult balance) => balance.Entry.EnergyCost == 0
        ? balance.DamagePerHit
        : SkillBalanceConfig.ToHitDamage(balance.CastDamage / (FireBurstDomain.BaseTicks / 60d
            * SoloStrikesPerSecond * (1 + SkillHitBalance.FireBurstSingleTargetBonus)));

    private int GetStrikeDamage(int? level = null) => TryGetConfiguredBalance(out var balance, skillLevel: level)
        ? StrikeDamage(balance) : 0;

    protected override object[] SkillDescriptionArgs
    {
        get
        {
            int damage = GetStrikeDamage();
            // 描述只写高于基础伤害的部分：单体为基础 × SkillHitBalance.FireBurstSingleTargetBonus。
            int single = (int)Math.Round(damage * SkillHitBalance.FireBurstSingleTargetBonus);
            return [SkillValueText.Damage(damage, SkillValueKind.Fire), MagicPointCost, MaxStack, MaxCD, FireBurstDomain.BaseTicks / 60f, FireBurstDomain.ExtendTicks / 60f,
                SkillValueText.Number(single, SkillValueKind.Fire),
                FireBurstDomain.MaxRemainingTicks / 60f, SkillHitBalance.FireBurstBuildup,
                (SkillHitBalance.FireBurstFrostDamageMultiplier - 1f) * 100f];
        }
    }

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel) =>
        [(LocalizeUI("Damage"), GetStrikeDamage(Math.Max(1, nextLevel - 1)).ToString(),
            GetStrikeDamage(nextLevel).ToString(), "")];

    public override bool CanUseSkill() => Player.whoAmI == Main.myPlayer && Stack > 0
        && (!Player.GetModPlayer<ActionModPlayer>().HasAction
            || quickCasting && Player.GetModPlayer<ActionModPlayer>().CurrentAnimAction is Action_FireLaser { CanQuickCast: true })
        && base.CanUseSkill();

    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        if (Stack >= MaxStack) CurrentCD = 0;
    }

    public override bool PreUpdateCD()
    {
        if(CurrentCD>1) CurrentCD = 1;

        bool update = base.PreUpdateCD();
        if (Stack < MaxStack) return update;
        return false;
    }

    protected override bool OnCast(IEntitySource source)
    {
        if (!CanUseSkill())
            return false;

        if (!TryGetConfiguredBalance(out SkillBalanceResult balance))
            return false;
        float singleMultiplier = (float)(1 + SkillHitBalance.FireBurstSingleTargetBonus);
        float singleTargetStrikesPerSecond = balance.Entry.EnergyCost > 0 ? (float)SoloStrikesPerSecond
            : (float)(SingleTargetDpsRatio * SkillBalanceSystem.GetCharacterBaselineDps(balance.CharacterLevel)
                / (balance.BaseCastDamage * singleMultiplier));

        void ConfigureDomain(Projectile projectile)
        {
            var domain = (FireBurstDomain)projectile.ModProjectile;
            domain.SetSingleTargetMultiplier(singleMultiplier);
            domain.SetSingleTargetStrikeRate(singleTargetStrikesPerSecond);
        }

        // 领域在施法动作的爆燃帧生成，中心为那一刻玩家所在的位置。快速施放同样播放完整动作，并打断正在引导的射线。
        AnimAction animAction = new Action_FireBurst(StrikeDamage(balance), ConfigureDomain, quickCasting);

        float startRotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
        if (!Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, interruptCurrentAction: quickCasting, rotation: startRotation))
            return false;
        Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost);
        return base.OnCast(source);
    }

}

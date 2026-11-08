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

    internal bool TryQuickCast()
    {
        if (Player.GetModPlayer<ActionModPlayer>().CurrentAnimAction is not Action_FireLaser { CanQuickCast: true }
            || !TryGetEquipped(Player, out var equipped) || !ReferenceEquals(equipped, this)
            || !Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled)
            return false;

        int previousStack = Stack;
        quickCasting = true;
        Skill.UseSkill(Player.GetSource_Misc(nameof(FireBurstSkill)),
            Player.GetModPlayer<ElainaSkillModPlayer>().Attributes);
        quickCasting = false;
        return Stack < previousStack;
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

    // 基础伤害由 SkillBalance.json 的 ratio 定价；以下为相对基础伤害的额外倍率，随 ratio 与等级曲线同比变化。
    // 只命中一个敌人时伤害为基础 ×(1 + SingleTargetBonus)；命中冻结敌人再额外加基础 × FrozenBonus。
    // 表达式属性便于热重载（下次施放生效）。
    public static double SingleTargetBonus => .43;
    public static double FrozenBonus => 1.43;
    // 领域内只有一名敌人时，炎爆（含单体加成）的目标 DPS 相对角色基准 DPS 的倍率；领域据此降低落下频率。
    public static double SingleTargetDpsRatio => .3;

    protected override object[] SkillDescriptionArgs
    {
        get
        {
            int damage = GetConfiguredDamage();
            int single = (int)Math.Round(damage * (1 + SingleTargetBonus));
            int frozen = (int)Math.Round(damage * FrozenBonus);
            return [damage, MagicPointCost, MaxStack, MaxCD, FireBurstDomain.BaseTicks / 60f, FireBurstDomain.ExtendTicks / 60f, single, frozen,
                FireBurstDomain.MaxRemainingTicks / 60f];
        }
    }

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel) =>
        [(LocalizeUI("Damage"), GetConfiguredDamage(skillLevel: Math.Max(1, nextLevel - 1)).ToString(),
            GetConfiguredDamage(skillLevel: nextLevel).ToString(), "")];

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
        /*
        if(CurrentCD>1) CurrentCD = 1;
        */

        bool update = base.PreUpdateCD();
        if (Stack < MaxStack) return update;
        return false;
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!CanUseSkill())
            return false;

        if (!TryGetConfiguredBalance(out SkillBalanceResult balance))
            return false;
        // 弹幕伤害按 json 的 ratio 定价；单体与冻结的额外倍率在命中时乘到基础伤害上。
        float singleMultiplier = (float)(1 + SingleTargetBonus);
        float frozenBonus = (float)FrozenBonus;
        // 单体每道伤害相对基准 DPS 的倍率（与装备增伤无关），换算出单体时每秒应落下的炎爆数。
        double singleHitRatio = balance.BaseCastDamage * (1 + SingleTargetBonus)
            / SkillBalanceSystem.GetCharacterBaselineDps(balance.CharacterLevel);
        float singleTargetStrikesPerSecond = (float)(SingleTargetDpsRatio / singleHitRatio);

        void ConfigureDomain(Projectile projectile)
        {
            var domain = (FireBurstDomain)projectile.ModProjectile;
            domain.SetDamageMultipliers(singleMultiplier, frozenBonus);
            domain.SetSingleTargetStrikeRate(singleTargetStrikesPerSecond);
        }

        if (quickCasting)
        {
            if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
                return false;
            int index = Projectile.NewProjectile(source ?? Player.GetSource_Misc(nameof(FireBurstSkill)),
                Player.Center, Vector2.Zero, ModContent.ProjectileType<FireBurstDomain>(),
                balance.DamagePerHit, 2f, Player.whoAmI);
            ConfigureDomain(Main.projectile[index]);
            return base.PreUseSkill(source);
        }

        // 领域中心固定在施放时玩家所在的位置。
        Vector2 position = Player.Center;
        AnimAction animAction = new Action_FireBurst()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<FireBurstDomain>(),
                _ => position,
                damage: balance.DamagePerHit,
                knockback: 2f,
                getVelocity: _ => Vector2.Zero,
                configureProjectile: ConfigureDomain));

        float startRotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
        if (!Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, interruptCurrentAction: false, rotation: startRotation))
            return false;
        Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost);
        return base.PreUseSkill(source);
    }

}

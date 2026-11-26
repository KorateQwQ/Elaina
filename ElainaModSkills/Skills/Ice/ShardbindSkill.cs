using 伊蕾娜.Localization;
using static 伊蕾娜.Localization.UIText;
using System;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.Utils;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

[SkillUIInfo(State = 6, Pixels = 400)]
public class ShardbindSkill : ElainaSealedSkill
{
    // 临时特效测试开关；测试结束后改为 false，恢复冷却、研习和战斗解封要求。
    internal static bool VisualTestMode => true;
    public override bool IsSealed => !VisualTestMode && base.IsSealed;

    public override void Initialize()
    {
        base.Initialize();
        ApplyVisualTestCooldown();
    }

    public override bool PreUpdateCD()
    {
        bool update = base.PreUpdateCD();
        ApplyVisualTestCooldown();
        return update;
    }

    private void ApplyVisualTestCooldown()
    {
        if (!VisualTestMode) return;
        MaxCD = 1f;
        CurrentCD = Math.Min(CurrentCD, 1f);
        CooldownDuration = Math.Min(CooldownDuration, 1f);
    }

    private NPC quickCastTarget;

    internal static bool TryGetEquipped(Player player, out ShardbindSkill skill)
    {
        var skills = player.GetModPlayer<ElainaSkillModPlayer>();
        return skills.TryGetUnlockedModSkill(out skill) && skill.Skill != null
            && skills.ActiveSkill.Contains(skill.Skill);
    }

    internal bool CanPrepareQuickCast() => base.CanUseSkill();

    internal bool TryQuickCast(NPC target)
    {
        var actions = Player.GetModPlayer<ActionModPlayer>();
        if (actions.CurrentAnimAction is not Action_IceCone { CanReleaseQuickCast: true }
            || !TryGetEquipped(Player, out var equipped) || !ReferenceEquals(equipped, this)
            || target == null || !IceConeTargetSystem.IsValidTarget(target) || !CanPrepareQuickCast())
            return false;

        int previousToken = actions.CurrentActionToken;
        quickCastTarget = target;
        Skill.UseSkill(Player.GetSource_Misc(nameof(ShardbindSkill)),
            Player.GetModPlayer<ElainaSkillModPlayer>().Attributes);
        quickCastTarget = null;
        return actions.CurrentActionToken != previousToken;
    }

    private NPC CastTarget => quickCastTarget ?? IceConeTargetSystem.ResolveTarget();
    public override Type[] PrerequisiteSkills => [typeof(IceConeSkill)];

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单次伤害
        int current = GetConfiguredDamage(hitCount: 1, skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(hitCount: 1, skillLevel: nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }

    // 冰锥需投入到 Lv.13；花后霜月及女王的探索门槛独立判断。
    private SkillUnlockCondition unlockCondition =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireSkillLevel<IceConeSkill>(13),
            // 霜月事件在花后开放，按角色等级上限判断阶段。
            RequireEvent(player => player.GetModPlayer<ShardbindDiscoveryPlayer>().DefeatedIceQueen,
                KLGameStateManager.GetLevelCap(KLGameStateManager.Plantera),
                "Mods.伊蕾娜.SkillInfo.UnlockConditions.IceQueen"));

    public override SkillUnlockCondition UnlockCondition
    {
        get => VisualTestMode ? SkillUnlockCondition.None : unlockCondition;
        set => unlockCondition = value;
    }

    // 花后以问号显现，角色经历过霜月后启封；击败霜月女王作为研习需求列出。
    public override bool IsConstellationRevealed(Player player) =>
        VisualTestMode || player?.GetModPlayer<ShardbindDiscoveryPlayer>().ExperiencedFrostMoon == true;

    public override string ConstellationRevealHint => Language.GetTextValue(
        "Mods.伊蕾娜.SkillInfo.UnlockConditions.ShardbindReveal");

    /// <summary>其他技能描述提到冰凌锁时使用：对该角色仍未显现或待启封时显示「技能???」，避免剧透。</summary>
    public static string NameFor(Player player)
    {
        player ??= Main.LocalPlayer;
        var skillPlayer = player?.GetModPlayer<ElainaSkillModPlayer>();
        var template = ModContent.GetInstance<ShardbindSkill>();
        bool known = skillPlayer != null && template != null
            && (skillPlayer.UnlockedSkill?.ContainsKey(nameof(ShardbindSkill)) == true
                || (template.IsConstellationRevealed(player)
                    && StageGateUnlockCondition.AllReachable(template.UnlockCondition, skillPlayer, template)));
        return known ? Language.GetTextValue($"Mods.伊蕾娜.SkillInfo.SkillName.{nameof(ShardbindSkill)}") : Language.GetTextValue("Mods.伊蕾娜.SkillInfo.Snippets.HiddenSkill");
    }

    public override bool CanUseSkill()
    {
        return Player.whoAmI == Main.myPlayer
            && (!Player.GetModPlayer<ActionModPlayer>().HasAction
                || quickCastTarget != null && Player.GetModPlayer<ActionModPlayer>().CurrentAnimAction
                    is Action_IceCone { CanReleaseQuickCast: true })
            && CastTarget is { } target && IceConeTargetSystem.IsValidTarget(target)
            && base.CanUseSkill();
    }

    protected override bool OnCast(IEntitySource source)
    {
        NPC target = CastTarget;
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
        return base.OnCast(source);
    }
    
    public bool ShootShardbind3D(Player player, NPC target)
    {
        Vector2 targetWorldPosition = target.Center;
        uint selectedSpawnSerial = target.GetGlobalNPC<IceboundNPC>().SpawnSerial;
        float buildup = SkillHitBalance.ShardbindFinalBuildup;
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
                    ConfigureBuildup(projectile, KL.DamageSystem.ElementType.Ice, buildup);
                    ((ShardbindProj)projectile.ModProjectile).BindTarget(target, selectedSpawnSerial);
                }));

        float startRotation = aimDirection.ToRotation() * player.gravDir;
        return player.GetModPlayer<ActionModPlayer>().StartAction(animAction,
            interruptCurrentAction: quickCastTarget != null, rotation: startRotation);
    }

    protected override object[] SkillDescriptionArgs => [SkillValueText.Damage(GetConfiguredDamage(hitCount: 1), SkillValueKind.Ice), SkillHitBalance.ShardbindFinalBuildup, (SkillHitBalance.ShardbindWetDamageMultiplier - 1f) * 100f];
}

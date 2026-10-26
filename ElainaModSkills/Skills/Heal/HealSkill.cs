using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;

namespace 伊蕾娜.ElainaModSkills.Skills.Heal;

[SkillUIInfo(State = 3, Pixels = 0)]
public partial class HealSkill : ElainaSkill
{
    public override SkillUnlockCondition GetConstellationUpgradeCondition(int nextLevel) => SkillUnlockCondition.BySkillPoint(15);

    private const float TargetSearchRadius = 3000f;
    private const int InvalidTargetIndex = -1;

    private int pendingTargetIndex = InvalidTargetIndex;
    private int pendingSkillSlot = -1;

    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireConfiguredCharacterLevelCap(nameof(HealSkill), 25));

    /// <summary>
    /// 有队友时只打开选择盘，即使队友全在范围外也需确认自疗，避免误触。
    /// 按住期间没有挥杖与使用音效，松开确认后才由 <see cref="CastAt"/> 出手；无队友时直接自疗。
    /// </summary>
    public override bool BeginWandUse()
    {
        // 选择盘打开或还在等这次松开时，长按自动重用不能重开选择盘，也不能挥杖。
        if (ElainaHealingWheel.OwnsPointer) return false;
        if (!HasAlly()) return true;
        OpenPicker();
        return false;
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (pendingTargetIndex != InvalidTargetIndex)
        {
            int targetIndex = pendingTargetIndex;
            ClearPendingSelection();
            return CastAt(targetIndex, source);
        }

        if (ElainaHealingWheel.IsShowing)
        {
            // Auto-reuse while holding the wand must preserve the pending release selection.
            return false;
        }

        // 是否打开选择盘不受距离影响；盘内目标和实际施法仍需通过范围校验。
        // 有队友但全在范围外时只显示自己，必须手动确认，不能直接自疗。
        // 魔杖平时已在 BeginWandUse 打开选择盘；这里保留给不经魔杖的调用方。
        if (!HasAlly()) return CastAt(Player.whoAmI, source);
        OpenPicker();
        return false;
    }

    private bool HasAlly()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != Player.whoAmI && TryGetValidTarget(i, out _, requireInRange: false)) return true;
        }
        return false;
    }

    private void OpenPicker()
    {
        pendingSkillSlot = Skill.SkillSlot;
        bool opened = ElainaHealingWheel.TryOpen(
            index => TryGetValidTarget(index, out _),
            PendingSkillIsValid,
            OnTargetConfirmed,
            ClearPendingSelection);

        if (!opened)
        {
            ClearPendingSelection();
        }
    }

    private bool CastAt(int targetIndex, IEntitySource source)
    {
        if (!TryGetValidTarget(targetIndex, out Player target) ||
            !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost, allowLifeConversion: AllowsLifeConversion)) return false;

        int totalHealing = GetTotalHealing(Level);
        AnimAction action = new Action_SimpleShoot().AddNode(new ShootActionNode(
            1, ModContent.ProjectileType<HealProj>(), _ => target.MountedCenter,
            damage: 0, 0, _ => Vector2.Zero, projectile =>
            {
                projectile.ai[0] = totalHealing;
                projectile.ai[1] = targetIndex;
            }));
        float rotation = (target.MountedCenter - Player.MountedCenter).ToRotation();
        Player.GetModPlayer<ActionModPlayer>().StartAction(action, rotation: rotation);
        return base.PreUseSkill(source);
    }

    private bool PendingSkillIsValid()
    {
        ElainaSkillModPlayer skills = Player.GetModPlayer<ElainaSkillModPlayer>();
        return pendingSkillSlot >= 0 && pendingSkillSlot < skills.ActiveSkill.Count
            && ReferenceEquals(skills.ActiveSkill[pendingSkillSlot], Skill)
            && Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled;
    }
    private void OnTargetConfirmed(int targetIndex)
    {
        int skillSlot = pendingSkillSlot;
        ElainaSkillModPlayer skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();
        if (!PendingSkillIsValid())
        {
            ClearPendingSelection();
            return;
        }

        pendingTargetIndex = targetIndex;
        try
        {
            skillPlayer.UseSkill(skillSlot);
        }
        finally
        {
            ClearPendingSelection();
        }
    }

    private bool TryGetValidTarget(int targetIndex, out Player target, bool requireInRange = true)
    {
        target = null;
        if (targetIndex < 0 || targetIndex >= Main.maxPlayers ||
            Main.player[targetIndex] is not { active: true, dead: false } candidate)
        {
            return false;
        }

        if (targetIndex != Player.whoAmI &&
            (Player.hostile || candidate.hostile || Player.team != candidate.team))
        {
            return false;
        }

        if (requireInRange && Vector2.DistanceSquared(Player.Center, candidate.Center) >
            TargetSearchRadius * TargetSearchRadius)
        {
            return false;
        }

        target = candidate;
        return true;
    }

    private void ClearPendingSelection()
    {
        pendingTargetIndex = InvalidTargetIndex;
        pendingSkillSlot = -1;
    }

    public override void UpdateEquips(Player player)
    {
        if (Main.mouseRight && Main.mouseRightRelease && ElainaHealingWheel.IsShowing)
        {
            ElainaHealingWheel.CancelOpen();
            ClearPendingSelection();
        }
        base.UpdateEquips(player);
    }

}

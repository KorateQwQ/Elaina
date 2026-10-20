using KL.SkillSystem.SilkyUI;
using KL.ActionsSystem;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

[SkillUIInfo(State = 5, Pixels = 200)]
public class RecallSkill : ElainaSkill
{
    private int pendingSlot = -1;
    private int pendingTarget = -1;
    private int approvedTarget = -1;
    private bool castPaid;

    public override void Initialize()
    {
        MaxCD = 10;
        MagicPointCost = 200;
        base.Initialize();
    }

    // 与治疗一致：无其他友军时直接以自己施法；有友军时必须选择，距离只影响盘内候选。
    public override bool BeginWandUse()
    {
        if (RecallPortalSystem.HasPendingCast(Player) || ElainaHealingWheel.OwnsPointer) return false;
        if (!HasAlly()) return true;
        OpenPicker();
        return false;
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        // 只有建立法阵的请求已获确认，才进入技能框架的扣费/扣层数路径。
        if (approvedTarget >= 0)
        {
            int approved = approvedTarget;
            approvedTarget = -1;
            if (!RecallPortalSystem.IsSelectable(Player, approved) || Stack <= 0 ||
                !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost)) return false;
            castPaid = true;
            Player.GetModPlayer<ActionModPlayer>().StartAction(new Action_SimpleShoot(),
                rotation: (Main.player[approved].Center - Player.Center).ToRotation());
            return base.PreUseSkill(source);
        }
        if (RecallPortalSystem.HasPendingCast(Player)) return false;
        if (pendingTarget < 0)
        {
            if (ElainaHealingWheel.OwnsPointer) return false;
            if (HasAlly())
            {
                OpenPicker();
                return false;
            }
            pendingTarget = Player.whoAmI;
        }

        int target = pendingTarget;
        ClearSelection();
        if (!RecallPortalSystem.IsSelectable(Player, target) || Stack <= 0 ||
            !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost, consume: false)) return false;
        int slot = Skill.SkillSlot;
        RecallPortalSystem.RequestCast(Player, target, () => CommitApprovedCast(slot, target));
        return false;
    }

    private bool HasAlly()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
            if (i != Player.whoAmI && RecallPortalSystem.IsAlly(Player, Main.player[i])) return true;
        return false;
    }

    private bool CommitApprovedCast(int slot, int target)
    {
        var skills = Player.GetModPlayer<ElainaSkillModPlayer>();
        if (slot < 0 || slot >= skills.ActiveSkill.Count || !ReferenceEquals(skills.ActiveSkill[slot], Skill) ||
            Player.HeldItem.ModItem is not Items.ElainaWand ||
            !Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled) return false;
        approvedTarget = target;
        castPaid = false;
        try { skills.UseSkill(slot); return castPaid; }
        finally { approvedTarget = -1; }
    }

    private void OpenPicker()
    {
        pendingSlot = Skill.SkillSlot;
        if (!ElainaHealingWheel.TryOpen(index => RecallPortalSystem.IsSelectable(Player, index),
            SelectionIsValid, Confirm, ClearSelection, recall: true)) ClearSelection();
    }

    private bool SelectionIsValid()
    {
        var skills = Player.GetModPlayer<ElainaSkillModPlayer>();
        return pendingSlot >= 0 && pendingSlot < skills.ActiveSkill.Count &&
            ReferenceEquals(skills.ActiveSkill[pendingSlot], Skill) &&
            Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled;
    }

    private void Confirm(int target)
    {
        if (!SelectionIsValid()) { ClearSelection(); return; }
        int slot = pendingSlot;
        pendingTarget = target;
        try { Player.GetModPlayer<ElainaSkillModPlayer>().UseSkill(slot); }
        finally { ClearSelection(); }
    }

    private void ClearSelection() { pendingSlot = pendingTarget = -1; }
}

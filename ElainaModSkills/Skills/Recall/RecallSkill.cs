using KL.SkillSystem.SilkyUI;
using KL.ActionsSystem;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

[SkillUIInfo(State = 5, Pixels = 200)]
public partial class RecallSkill : ElainaSkill
{
    private int pendingSlot = -1;
    private int pendingTarget = -1;

    public override void Initialize()
    {
        MaxCD = 10;
        MagicPointCost = 200;
        base.Initialize();
    }

    // 与治疗一致：无其他友军时直接以自己施法；有友军时必须选择，距离只影响盘内候选。
    public override bool BeginWandUse()
    {
        if (ElainaHealingWheel.OwnsPointer) return false;
        if (!HasAlly()) return true;
        OpenPicker();
        return false;
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
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
        var attribute = Player.GetModPlayer<ElainaAttributeModPlayer>();
        // 先确认能付得起，法阵成功打开后才真正扣魔力并进入技能框架的扣层数路径。
        if (!RecallPortalSystem.IsSelectable(Player, target) || Stack <= 0 ||
            !attribute.ConsumeMagicPoint(MagicPointCost, consume: false) ||
            !RecallPortalSystem.Cast(Player, target)) return false;
        attribute.ConsumeMagicPoint(MagicPointCost);
        Player.GetModPlayer<ActionModPlayer>().StartAction(new Action_SimpleShoot(),
            rotation: (Main.player[target].Center - Player.Center).ToRotation());
        return base.PreUseSkill(source);
    }

    private bool HasAlly()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
            if (i != Player.whoAmI && RecallPortalSystem.IsAlly(Player, Main.player[i])) return true;
        return false;
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

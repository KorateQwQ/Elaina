using KL.ActionsSystem;
using Terraria.GameInput;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;
using 伊蕾娜.ElainaModSkills.Skills.DollControl;
using 伊蕾娜.ElainaModSkills.Skills.MagicMissile;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaActions;

public sealed class Action_MagicMissile : Action_SimpleShoot
{
    internal const int FireFrame = 1;
    private bool quickCastRequested;

    private static bool MissileSelected(Player player)
    {
        var skills = player.GetModPlayer<ElainaSkillModPlayer>().ActiveSkill;
        int index = ElainaSkillModPlayer.CurrentSkillIndex;
        return index >= 0 && index < skills.Count && skills[index]?.ModSkill is MagicMissileSkill;
    }

    internal bool CanQuickCast => owner.active && !owner.dead && !owner.CCed && !owner.noItems
        && owner.HeldItem.ModItem is ElainaWand && MissileSelected(owner)
        && Main.hasFocus && !Main.gameMenu && !Main.gamePaused && !Main.drawingPlayerChat
        && !Main.ingameOptionsWindow && !Main.inFancyUI && !Main.editSign && !Main.editChest
        && !owner.mouseInterface && !ElainaSkillWheel.IsShowing && !ElainaHealingWheel.OwnsPointer
        && !DollCommandWheel.OwnsPointer;

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        base.Update(actionPlayer, actionFrame, actionProgress);
        if (owner.whoAmI != Main.myPlayer) return;
        if (PlayerInput.Triggers.JustPressed.MouseRight && Main.mouseRight && CanQuickCast)
            quickCastRequested = true;

        // 起手当帧的右键等普通飞弹出手后再衔接，避免吞掉已经付费的普攻。
        if (!quickCastRequested || actionFrame < FireFrame) return;
        quickCastRequested = false;
        if (CanQuickCast && MultiMissileSkill.TryGetEquipped(owner, out var skill))
            skill.QuickCast();
    }

    internal static bool OwnsQuickCastInput(Player player)
    {
        if (!Main.mouseRight || !MultiMissileSkill.TryGetEquipped(player, out _)) return false;
        return player.GetModPlayer<ActionModPlayer>().CurrentAnimAction switch
        {
            Action_MagicMissile missile => missile.CanQuickCast,
            Action_MultiMissile burst => burst.UsesQuickCastInput,
            null => Main.mouseLeft && MissileSelected(player),
            _ => false
        };
    }
}

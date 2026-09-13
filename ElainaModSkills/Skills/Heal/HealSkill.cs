using KL.ActionsSystem;
using KL.SkillSystem.SilkyUI;
using KL.SkillSystem.UI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Heal;

[SkillUIInfo(State = 3, Pixels = 0)]
public class HealSkill : ElainaSkill
{
    private const float TargetSearchRadius = 1200f;
    private const int InvalidTargetIndex = -1;

    private int pendingTargetIndex = InvalidTargetIndex;
    private int pendingSkillSlot = -1;

    public override void Initialize()
    {
        MaxCD = 1;
        MagicPointCost = 100;
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (pendingTargetIndex != InvalidTargetIndex)
        {
            int targetIndex = pendingTargetIndex;
            ClearPendingSelection();

            if (!TryGetValidTarget(targetIndex, out Player target) ||
                !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            {
                return false;
            }

            AnimAction animAction = new Action_SimpleShoot()
                .AddNode(new ShootActionNode(
                    1,
                    ModContent.ProjectileType<HealProj>(),
                    _ => target.MountedCenter,
                    damage: 0,
                    0,
                    _ => Vector2.Zero,
                    projectile => projectile.ai[0] = targetIndex));

            float startRotation = (target.MountedCenter - Player.MountedCenter).ToRotation();
            Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);

            return base.PreUseSkill(source);
        }

        if (PlayerTargetSelectorUI.TryGet(out var selector) && selector.IsOpen)
        {
            selector.Cancel();
            ClearPendingSelection();
            return false;
        }

        pendingSkillSlot = Skill.SkillSlot;
        bool opened = PlayerTargetSelectorUI.TryOpen(
            new PlayerTargetSelectorOptions
            {
                Filter = PlayerTargetFilter.FriendlyOnly,
                IncludeSelf = true,
                MaxTargets = 8,
                SearchRadius = TargetSearchRadius
            },
            OnTargetConfirmed,
            ClearPendingSelection);

        if (!opened)
        {
            ClearPendingSelection();
        }

        return false;
    }
    private void OnTargetConfirmed(int targetIndex)
    {
        int skillSlot = pendingSkillSlot;
        ElainaSkillModPlayer skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();
        if (skillSlot < 0 || skillSlot >= skillPlayer.ActiveSkill.Count ||
            !ReferenceEquals(skillPlayer.ActiveSkill[skillSlot], Skill))
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

    private bool TryGetValidTarget(int targetIndex, out Player target)
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

        if (Vector2.DistanceSquared(Player.Center, candidate.Center) >
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
        if (Main.mouseRight && Main.mouseRightRelease)
        {
            if (PlayerTargetSelectorUI.TryGet(out var selector) && selector.IsOpen)
            {
                selector.Cancel();
                ClearPendingSelection();
            }
        }
        base.UpdateEquips(player);
    }

}

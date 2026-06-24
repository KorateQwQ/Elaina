using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;

namespace 伊蕾娜.ElainaModSkills.Skills.AshenWitch;

[SkillUIInfo(State = 0, Pixels = 0)]
public class AshenWitchSkill: ElainaSkill
{
    //public const int BonusMana = 40;

    public override bool IsPassiveSkill => true;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void ResetEffects(Player player)
    {
        base.ResetEffects(player);
    }
    public override void UpdateEquips(Player player)
    {
        player.statManaMax2 += 200;
        //PrintText("更新装备");
    }
    
    public override void OnRightClickInSkillPanel()
    {
        /*if (ElainaSkillModPlayer.SkillModPlayer.UnlockedSkill.ContainsKey(this.GetType().Name))
        {
            ElainaSkillModPlayer.SkillModPlayer.LockSkill(Skill);
        }
        else ElainaSkillModPlayer.SkillModPlayer.UnlockSkill(Skill);*/
        base.OnRightClickInSkillPanel();
    }

    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale, Color color, Effect effect = null)
    {
        return base.PreDrawSkillIcon(position, scale, color, effect);
    }

    public override void OnUnlockSkillAdded()
    {
        base.OnUnlockSkillAdded();
    }

    public override void OnLockSkill()
    {
        base.OnLockSkill();
    }
}
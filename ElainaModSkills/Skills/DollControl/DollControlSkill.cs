using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

[SkillUIInfo(State = 2, Pixels = 400)]
public class DollControlSkill : ElainaSkill
{
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
        base.UpdateEquips(player);
    }

    public override bool CanUseSkill()
    {
        return base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        return base.PreUseSkill(source);
    }
}

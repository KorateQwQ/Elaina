using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModSkills.Skills.Alchemy;

[SkillUIInfo(State = 4, Pixels = 100)]
public class AlchemySkill : ElainaSkill
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

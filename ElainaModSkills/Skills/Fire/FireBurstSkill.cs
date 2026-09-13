using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

[SkillUIInfo(State = 8, Pixels = 600)]

public class FireBurstSkill : ElainaSkill
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        return base.PreUseSkill(source);
    }
}
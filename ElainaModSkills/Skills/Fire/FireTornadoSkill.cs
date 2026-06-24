using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

[SkillUIInfo(State = 5, Pixels = 300)]
public class FireTornadoSkill : ElainaSkill
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
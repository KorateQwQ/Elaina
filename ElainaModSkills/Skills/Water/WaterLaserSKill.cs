using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

[SkillUIInfo(State = 4, Pixels = 400)]
public class WaterLaserSKill : ElainaSkill
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
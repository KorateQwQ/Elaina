using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

[SkillUIInfo(State = 1, Pixels = 200)]
public class MultiMissileSkill : ElainaSkill
{
    public override void Initialize()
    {
        MaxCD = 5;
        MaxStack = 1;
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        return base.PreUseSkill(source);
    }

    public override bool PreUpdateCD()
    {
        return base.PreUpdateCD();
    }
    
    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale,Color color, Effect effect = null)
    {
        base.PostDrawSkillIcon(position, scale,color, effect);

    }
}
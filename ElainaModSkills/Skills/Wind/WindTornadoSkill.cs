using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

[SkillUIInfo(State = 8, Pixels = 500)]
public class WindTornadoSkill : ElainaSkill
{
    public override void Initialize()
    {
        MaxCD = 5;
        MaxStack = 1;
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        CurrentCD = 0.1f;
        Projectile projectile = Projectile.NewProjectileDirect(source,WandCenter , WandDirection*0, ModContent.ProjectileType<WindTornado>(), 
            10, 2);
        
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
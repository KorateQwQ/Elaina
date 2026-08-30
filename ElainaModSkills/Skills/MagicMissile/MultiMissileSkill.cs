using KL.ActionsSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

[SkillUIInfo(State = 1, Pixels = 200)]
public class MultiMissileSkill : ElainaSkill
{
    public override void Initialize()
    {
        MaxCD = 2;
        MaxStack = 1;
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        MaxCD = 0.5f;

        AnimAction animAction = new Action_SimpleSlash()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<MagicMissleSpawner>(),
                _ => Main.MouseWorld+new Vector2(0,0),
                damage:1,//DpsHelper.GetSkillDamage(GetType().Name,1)
                2,
                _ => Vector2.Zero));
        
        Vector2 aimDirection = (Main.MouseWorld - Player.MountedCenter).SafeNormalize(Vector2.UnitX * (Player.direction == 0 ? 1 : Player.direction));

        float startRotation = aimDirection.ToRotation() * Player.gravDir;
        Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
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
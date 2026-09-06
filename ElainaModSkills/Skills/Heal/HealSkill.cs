using KL.ActionsSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;

namespace 伊蕾娜.ElainaModSkills.Skills.Heal;

[SkillUIInfo(State = 3, Pixels = 0)]
public class HealSkill : ElainaSkill
{
    public override void Initialize()
    {
        MaxCD = 10;
        base.Initialize();
    }

    public override bool CanUseSkill()
    {
        MaxCD = 1;
        return base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        AnimAction animAction = new Action_SimpleShoot()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<HealProj>(),
                _ => WandCenter+new Vector2(0,0),
                damage:0,//DpsHelper.GetSkillDamage(GetType().Name,1)
                0,
                _ => new Vector2(0)));
        
        float startRotation = (Main.MouseWorld - Player.MountedCenter).ToRotation();
        Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
        
        return base.PreUseSkill(source);
    }
    public override void UpdateEquips(Player player)
    {
        base.UpdateEquips(player);
    }

}
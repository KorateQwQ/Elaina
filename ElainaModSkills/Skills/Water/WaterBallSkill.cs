using KL.ActionsSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

[SkillUIInfo(State = 12, Pixels = 400)]
public class WaterBallSkill: ElainaSkill
{
    public override void Initialize()
    {
        MaxCD = 0.5f;
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        Player localPlayer = Main.LocalPlayer;
        Vector2 directionToMouse = Main.MouseWorld - localPlayer.MountedCenter;

        float startRotation = directionToMouse.ToRotation()*Main.LocalPlayer.gravDir;

        AnimAction animAction = new Action_SimpleShoot()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<WaterBall>(),
                _ => WandCenter + new Vector2(0, 0),
                damage: 10, //DpsHelper.GetSkillDamage(GetType().Name,1)
                2,
                player => new Vector2(1, 0).RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation()) * 15f));
        
        localPlayer.GetModPlayer<ActionModPlayer>().StartAction(animAction,rotation: startRotation);

        return base.PreUseSkill(source);
    }

    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale,  Color color, Effect effect = null)
    {

        return base.PreDrawSkillIcon(position, scale, color, effect);
    }
}
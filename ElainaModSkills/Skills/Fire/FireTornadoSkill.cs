using KL.ActionsSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

[SkillUIInfo(State = 8, Pixels = 300)]
public class FireTornadoSkill : ElainaSkill
{
    public override void Initialize()
    {
        MaxCD = 1;
        base.Initialize();
    }

    public override void ResetEffects(Player player)
    {
        base.ResetEffects(player);
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        AnimAction animAction = new Action_SimpleShoot()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<FireTornado>(),
                _ => Main.MouseWorld,
                damage:10,//DpsHelper.GetSkillDamage(GetType().Name,1)
                2,
                player => new Vector2(1, 0).RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation()) * 15f));
        
        Player localPlayer = Main.LocalPlayer;
        localPlayer.GetModPlayer<ActionModPlayer>().StartAction(animAction);

        return base.PreUseSkill(source);
    }
}
using KL.ActionsSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

[SkillUIInfo(State = 2, Pixels = 300)]

public class WindBladeSkill : ElainaSkill
{
    public override void Initialize()
    {

        // 冷却时间：1秒
        MaxCD = 0.5f;

        // 魔力消耗
        MagicPointCost = 10;
        base.Initialize();

    }

    public override bool CanUseSkill()
    {
        if (!Player.CheckMana(MagicPointCost, false))
            return false;
        return base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {

        if (!Player.CheckMana(MagicPointCost, true))
            return false;
        
        AnimAction animAction = new Action_SimpleShoot(10);
        animAction.AddNode(new ShootActionNode(
            1,
            ModContent.ProjectileType<WindSlash>(),
            _ => WandCenter,
            damage: 100,
            2,
            player => new Vector2(1, 0).RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation()) * 20f)
        );

        float startRotation = (Main.MouseWorld - Player.MountedCenter).ToRotation();
        Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
        return base.PreUseSkill(source);
    }

    protected override object[] SkillDescriptionArgs => new object[] { 100 }; // 伤害值参数
}
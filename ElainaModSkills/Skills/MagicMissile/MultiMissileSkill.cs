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

        Vector2 aimDirection = Vector2.UnitX * (Player.direction == 0 ? 1 : Player.direction);
        float baseRotation = aimDirection.ToRotation();

        // 半圆参数：以玩家为圆心，从前方到后方均匀分布 5 个位置
        const int missileCount = 5;
        const float radius = 120f;

        AnimAction animAction = new Action_SimpleSlash();

        for (int i = 0; i < missileCount; i++)
        {
            // 从 0（前方）到 π（后方）等分角度
            float angleOffset = MathHelper.Lerp(0f, -MathHelper.Pi, i / (float)(missileCount - 1));
            float finalRotation = baseRotation + angleOffset * Player.direction;

            animAction.AddNode(new ShootActionNode(
                1 + i * 5,
                ModContent.ProjectileType<MagicMissleSpawner>(),
                _ => Player.MountedCenter + finalRotation.ToRotationVector2() * radius,
                damage: 1,//DpsHelper.GetSkillDamage(GetType().Name,1)
                2,
                _ => Vector2.Zero));
        }

        float startRotation = baseRotation * Player.gravDir;
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
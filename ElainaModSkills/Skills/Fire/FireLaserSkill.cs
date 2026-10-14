using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills.Skills.Lightning;
using 伊蕾娜.ElainaModSkills.Skills.Wind;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;
[SkillUIInfo(State = 6, Pixels = 600)]
public class FireLaserSkill : ElainaSkill
{
    // 默认血肉墙后开放（角色等级上限 40）；具体门槛读取 JSON，不要求实际等级达到上限。
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireConfiguredCharacterLevelCap(nameof(FireLaserSkill), 40));

    public override void Initialize()
    {
        MaxCD = 10;
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        CurrentCD = 0.1f;
        Projectile projectile = Projectile.NewProjectileDirect(source,WandCenter, WandDirection*10, ModContent.ProjectileType<FireBall>(), 
            10, 2);
        return base.PreUseSkill(source);
    }

    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale,Color color, Effect effect = null)
    {
        return base.PreDrawSkillIcon(position, scale,color, effect);
    }

    public override bool PreUpdateCD()
    {
        return base.PreUpdateCD();
    }

}
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

[SkillUIInfo(State = 12, Pixels = 400)]
public class WaterBallSkill: ElainaSkill
{
    // 学习门槛读取配置中的角色等级上限，默认至少达到 30。
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireConfiguredCharacterLevelCap(nameof(WaterBallSkill), 30));

    protected override object[] SkillDescriptionArgs => [GetConfiguredDamage()];
    public override void Initialize()
    {
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        Player localPlayer = Main.LocalPlayer;
        Vector2 directionToMouse = Main.MouseWorld - localPlayer.MountedCenter;

        float startRotation = directionToMouse.ToRotation()*Main.LocalPlayer.gravDir;

        localPlayer.GetModPlayer<ActionModPlayer>()
            .StartAction(new Action_WaterBall(GetConfiguredDamage()), rotation: startRotation);

        return base.PreUseSkill(source);
    }

    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale,  Color color, Effect effect = null)
    {
        if (CurrentCD > 1) CurrentCD = 1;
        return base.PreDrawSkillIcon(position, scale, color, effect);
    }
}

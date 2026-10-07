using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

[SkillUIInfo(State = 6, Pixels = 400)]
public class IceShardLockSkill : ElainaSkill
{
    public override void Initialize()
    {
        MagicPointCost = 3;
        MaxCD = 1;
        base.Initialize();
    }
    public override void ResetEffects(Player player)
    {
        //MagicPointCost = 5;//(int)(player.statMana*0.2)+200;
        //MaxStack = 1;
        base.ResetEffects(player);
    }

    public override bool CanUseSkill()
    {
        MagicPointCost = 3;

        return base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        return false;
    }
}
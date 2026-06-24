using KL.DamageSystem;

namespace 伊蕾娜.ElainaModSkills.ElainaDamageClass;

//伊蕾娜默认伤害类，当魔法为无元素魔法时为粉色。
public class ElainaBasicDamage : DamageClass
{
    public override void Load()
    {
        DamageManager.RegisterDamageColor(this,伊蕾娜.SkillDamageColor(伊蕾娜.HitType.HitByMagicMissile));
        base.Load();
    }

    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
    {
        // 伊蕾娜伤害类从通用伤害加成中完全受益
        if (damageClass == DamageClass.Generic)
            return StatInheritanceData.Full;
        
        // 伊蕾娜伤害类从魔法伤害加成中完全受益
        if (damageClass == DamageClass.Magic)
            return StatInheritanceData.Full;

        // 对于其他伤害类，不继承任何加成
        return StatInheritanceData.None;
    }

    public override bool GetEffectInheritance(DamageClass damageClass)
    {
        // 伊蕾娜伤害类可以激活魔法伤害的特效,此处不需要考虑Generic伤害类
        return damageClass == DamageClass.Magic;
    }

    public override void SetDefaultStats(Player player)
    {
        // 可以在这里设置伊蕾娜伤害类的默认属性加成
        // 例如：增加暴击几率或魔法伤害加成
        //player.GetCritChance<ElainaDamageClass>() += 0;
        base.SetDefaultStats(player);
    }

    // 使用标准的暴击计算
    public override bool UseStandardCritCalcs => true;

    public override bool ShowStatTooltipLine(Player player, string lineName)
    {
        // 你可以使用的四个行名称是 "Damage"、"CritChance"、"Speed" 和 "Knockback"。所有四种情况默认为 true，因此将显示。
        // 显示所有标准的工具提示行
        
        return true;
    }
}
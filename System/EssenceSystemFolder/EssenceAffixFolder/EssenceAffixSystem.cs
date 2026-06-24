using Terraria.Utilities;
using 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.GoldRarityAffix;
using 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.PurpleRarityAffix;

namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder;

public class EssenceAffixSystem : ModSystem
{
    public override void Load()
    {
        //——————白色稀有度分界线——————//
        /*EssenceAffixRegistry.Register(new EssenceAffixDef("melee_attackSpeed", EssenceAffixRarity.White) { Effect = Effect_MeleeSpeed.Instance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("manaCostReduce", EssenceAffixRarity.White) { Effect = Effect_ManaCostReduce.Instance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("lifeRegen", EssenceAffixRarity.White) { Effect = Effect_LifeRegen.Instance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("armor", EssenceAffixRarity.White) { Effect = Effect_Armor.Instance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("armorPenetration", EssenceAffixRarity.White) { Effect = Effect_ArmorPenetration.Instance});*/
        
        //——————蓝色稀有度分界线——————//
        /*
        EssenceAffixRegistry.Register(new EssenceAffixDef("melee_damage", EssenceAffixRarity.Blue) { Effect = Effect_DamageAddition.MeleeInstance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("magic_damage", EssenceAffixRarity.Blue) { Effect = Effect_DamageAddition.MagicInstance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("ranged_damage", EssenceAffixRarity.Blue) { Effect = Effect_DamageAddition.RangedInstance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("summon_damage", EssenceAffixRarity.Blue) { Effect = Effect_DamageAddition.SummonInstance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("throwing_damage", EssenceAffixRarity.Blue) { Effect = Effect_DamageAddition.ThrowingInstance});
        
        //——————紫色稀有度分界线——————//
        EssenceAffixRegistry.Register(new EssenceAffixDef("crit", EssenceAffixRarity.Purple) { Effect = Effect_CritChance.Instance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("critDamage", EssenceAffixRarity.Purple) { Effect = Effect_CritDamage.Instance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("minionAmount", EssenceAffixRarity.Purple) { Effect = Effect_MinionAmount.Instance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("damageReduce", EssenceAffixRarity.Purple) { Effect = Effect_DamageReduce.Instance});
        */

        
        //——————金色稀有度分界线——————//
        //生命偷取
        EssenceAffixRegistry.Register(new EssenceAffixDef("lifeSteal", EssenceAffixRarity.Gold) { Effect = Effect_LifeSteal.Instance});
        EssenceAffixRegistry.Register(new EssenceAffixDef("ghostStep", EssenceAffixRarity.Gold) { Effect = Effect_GhostStep.Instance});

        base.Load();
    }

    public override void Unload()
    {
        EssenceAffixRegistry.Clear();
        base.Unload();
    }

    public override void PostUpdateProjectiles()
    {
        base.PostUpdateProjectiles();
    }
}
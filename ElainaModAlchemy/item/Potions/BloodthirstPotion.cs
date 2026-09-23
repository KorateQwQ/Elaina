using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class BloodthirstPotion : AlchemyPotion
{
    public override string EntryId => "bloodlust";
    protected override int PotionBuffType => ModContent.BuffType<BloodthirstBuff>();
}

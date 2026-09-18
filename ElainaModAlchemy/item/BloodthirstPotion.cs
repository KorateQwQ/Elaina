using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaModAlchemy.item;

public sealed class BloodthirstPotion : AlchemyPotion
{
    protected override int PotionBuffType => ModContent.BuffType<BloodthirstBuff>();
}

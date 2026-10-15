using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class FeatherlightPotion : AlchemyPotion
{
    public override string EntryId => "featherlight";
    protected override int PotionBuffType => ModContent.BuffType<FeatherlightBuff>();
}

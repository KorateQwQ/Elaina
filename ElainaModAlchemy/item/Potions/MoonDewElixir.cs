using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

/// <summary>伊蕾娜专属的持续魔力恢复药剂。</summary>
public sealed class MoonDewElixir : AlchemyPotion
{
    public override string EntryId => "mana";
    protected override int PotionBuffType => ModContent.BuffType<MoonDewElixirBuff>();

    public override bool CanUseItem(Player player) => !player.dead && player.GetModPlayer<ElainaModplayer>().Elaina;
}

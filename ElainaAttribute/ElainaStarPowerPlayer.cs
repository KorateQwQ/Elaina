using System;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>将星力药水接入原版和灰之魔女两种魔力体系。</summary>
public sealed class ElainaStarPowerPlayer : ModPlayer
{
    public override void PostUpdateMiscEffects()
    {
        if (!Player.GetModPlayer<AlchemyBuffPlayer>().StarPowerActive) return;

        // 装备与灰之魔女的生命转换已完成；先计入药水自身的上限加成。
        Player.statManaMax2 += 40;
        var attributes = Player.GetModPlayer<ElainaAttributeModPlayer>();
        float maximumMana = attributes.UniqueMagicEnabled ? attributes.MaxMagicPoint : Player.statManaMax2;
        float difference = Math.Max(0f, maximumMana - Player.statLife);
        Player.GetDamage(DamageClass.Magic) += Math.Min(1f, difference / 300f) * .30f;
    }
}

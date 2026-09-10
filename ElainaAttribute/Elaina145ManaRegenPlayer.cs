using System;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 将原版魔力消耗入口桥接到伊蕾娜独特魔力。
/// 灰之魔女关闭时不拦截原版魔力流程。
/// </summary>
public class Elaina145ManaRegenPlayer : ModPlayer
{
    public int NaturalManaGainThisTick { get; private set; }

    public float NaturalManaRegenPerSecond
    {
        get
        {
            ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
            return attributePlayer.UniqueMagicEnabled
                ? attributePlayer.GetMagicPointRecovery()
                : Math.Max(0f, Player.manaRegen / 120f);
        }
    }

    public override void Load()
    {
        On_Player.CheckMana_Item_int_bool_bool += On_Player_CheckMana_Item_int_bool_bool;
    }

    public override void Unload()
    {
        On_Player.CheckMana_Item_int_bool_bool -= On_Player_CheckMana_Item_int_bool_bool;
    }

    private static bool IsUsingUniqueMagic(Player player)
    {
        return player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled;
    }

    private bool On_Player_CheckMana_Item_int_bool_bool(
        On_Player.orig_CheckMana_Item_int_bool_bool orig,
        Player self,
        Item item,
        int amount,
        bool pay,
        bool blockQuickMana)
    {
        if (!IsUsingUniqueMagic(self) || item.ModItem != null)
        {
            return orig(self, item, amount, pay, blockQuickMana);
        }

        int requiredMana = amount >= 0 ? amount : self.GetManaCost(item);
        return self.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(requiredMana, pay);
    }

    public static void ApplyManaRegenerationDelay145(Player player)
    {
        // 保留空实现，以兼容已有动作代码的调用。
    }
}

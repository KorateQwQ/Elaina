using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.item.Materials;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Pre-Hardmode ingredient sources matching the alchemy notebook.</summary>
public sealed class AlchemyIngredientSources : GlobalNPC
{
    public override void ModifyShop(NPCShop shop)
    {
        if (shop.NpcType != NPCID.Merchant) return;
        shop.Add<Flour>();
        shop.Add<Sugar>();
        shop.Add<Egg>();
    }

    public override void SetupTravelShop(int[] shop, ref int nextSlot)
    {
        AddTravelIngredient(shop, ref nextSlot, ModContent.ItemType<Beef>());
        AddTravelIngredient(shop, ref nextSlot, ModContent.ItemType<Potato>());
    }

    private static void AddTravelIngredient(int[] shop, ref int nextSlot, int type)
    {
        if (Array.IndexOf(shop, type) >= 0 || nextSlot < 0 || nextSlot >= shop.Length) return;
        shop[nextSlot++] = type;
    }

    public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
    {
        if (npc.type != NPCID.TravellingMerchant) return;
        // A saved visit may have been generated before these ingredients existed.
        // Fill only unused slots; preserve reserved entries and the buyback slot.
        EnsureTravelIngredient(items, ModContent.ItemType<Beef>());
        EnsureTravelIngredient(items, ModContent.ItemType<Potato>());
    }

    private static void EnsureTravelIngredient(Item[] items, int type)
    {
        if (Array.Exists(items, item => item?.type == type)) return;
        for (int i = 0; i < items.Length - 1; i++)
        {
            if (items[i] != null) continue;
            items[i] = new Item(type);
            return;
        }
    }
}

using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Research-scroll inventory and vanilla item delivery; crafting material collection stays in Recipe.</summary>
public static class AlchemyInventory
{
    public const int SlotCount = 58;
    public static IReadOnlyList<Item> Items(Player player) => player?.inventory == null
        ? Array.Empty<Item>() : new ArraySegment<Item>(player.inventory, 0, Math.Min(SlotCount, player.inventory.Length));
    public static int Count(Player player, int type)
    {
        long count = 0;
        foreach (var item in Items(player)) if (item != null && !item.IsAir && item.type == type) count += item.stack;
        return (int)Math.Min(int.MaxValue, count);
    }
    internal static void Give(Player player, Item item, IEntitySource source)
    {
        Item remaining = player.GetItem(player.whoAmI, item,
            new GetItemSettings(LongText: false, NoText: false, CanGoIntoVoidVault: false));
        if (remaining.IsAir) return;
        int slot = Item.NewItem(source, player.getRect(), remaining);
        if (slot < Main.maxItems) Main.item[slot].noGrabDelay = 0;
    }
}
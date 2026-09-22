using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

/// <summary>只在点击制作时收集真实槽位；与原版扣料顺序及材料来源一致。</summary>
internal sealed class FacsimileCraftingInventory
{
    internal sealed class Slot
    {
        internal readonly Item Item;
        internal readonly int Type;
        internal readonly int Stack;
        internal readonly int Index;
        internal readonly Item[] Container;
        internal readonly int Chest;
        internal readonly ModPlayer.ItemConsumedCallback Callback;
        internal int Used;

        internal Slot(Item item, int index, Item[] container, int chest, ModPlayer.ItemConsumedCallback callback)
        {
            Item = item;
            Type = item.type;
            Stack = item.stack;
            Index = index;
            Container = container;
            Chest = chest;
            Callback = callback;
        }
    }

    internal readonly Dictionary<int, int> Counts = new();
    private readonly List<Slot> slots = new();
    private readonly HashSet<Item> seen = new(ReferenceEqualityComparer.Instance);

    internal FacsimileCraftingInventory(Player player)
    {
        AddContainer(player.inventory, 58);
        if (player.chest >= 0)
            AddContainer(Main.chest[player.chest].item, 40, player.chest);
        else
        {
            Item[] bank = player.chest switch
            {
                -2 => player.bank.item,
                -3 => player.bank2.item,
                -4 => player.bank3.item,
                -5 => player.bank4.item,
                _ => null
            };
            if (bank != null)
                AddContainer(bank, 40);
        }
        if (player.useVoidBag() && player.chest != -5)
            AddContainer(player.bank4.item, 40);
        foreach (var (items, callback) in PlayerLoader.GetModdedCraftingMaterials(player))
        {
            int index = 0;
            foreach (Item item in items)
                Add(item, index++, null, -1, callback);
        }
    }

    private void AddContainer(Item[] items, int length, int chest = -1)
    {
        for (int i = 0; i < Math.Min(length, items.Length); i++)
            Add(items[i], i, items, chest, null);
    }

    private void Add(Item item, int index, Item[] container, int chest, ModPlayer.ItemConsumedCallback callback)
    {
        if (item == null || item.IsAir || !seen.Add(item))
            return;
        slots.Add(new Slot(item, index, container, chest, callback));
        Counts.TryGetValue(item.type, out int previous);
        Counts[item.type] = (int)Math.Min(int.MaxValue, (long)previous + item.stack);
    }

    internal bool TryPlan(Recipe recipe, FacsimileRecipe cached, int dustType, out int dustCost)
    {
        long missing = 0;
        for (int i = 0; i < recipe.requiredItem.Count; i++)
        {
            Item ingredient = recipe.requiredItem[i];
            int needed = ingredient.stack;
            // 保留炼药桌及其他模组的省料回调，每种配方条目仅调用一次。
            RecipeLoader.ConsumeIngredient(recipe, ingredient.type, ref needed, false);
            if (needed > 0)
                missing += Reserve(cached.TypesByIngredient[i], needed);
        }
        dustCost = 0;
        if (missing > int.MaxValue)
            return false;
        dustCost = (int)missing;
        return Reserve(new[] { dustType }, dustCost) == 0;
    }

    private int Reserve(int[] accepted, int needed)
    {
        foreach (Slot slot in slots)
        {
            if (needed <= 0)
                break;
            if (Array.BinarySearch(accepted, slot.Type) < 0)
                continue;
            int take = Math.Min(needed, slot.Stack - slot.Used);
            slot.Used += take;
            needed -= take;
        }
        return needed;
    }

    internal bool IsCurrent()
    {
        // 同时验证作为门槛样本但因省料而未扣除的槽位。
        foreach (Slot slot in slots)
        {
            if (slot.Item.type != slot.Type || slot.Item.stack != slot.Stack ||
                slot.Container != null && !ReferenceEquals(slot.Container[slot.Index], slot.Item))
                return false;
        }
        return true;
    }

    internal void Commit(List<Item> consumed)
    {
        consumed.Clear();
        // 先完整扣料，再通知外部容器，避免回调影响尚未扣除的槽位。
        foreach (Slot slot in slots)
        {
            if (slot.Used == 0)
                continue;
            Item taken = slot.Item.Clone();
            taken.stack = slot.Used;
            consumed.Add(taken);
            slot.Item.stack -= slot.Used;
            if (slot.Item.stack == 0)
                slot.Item.TurnToAir();
        }
        foreach (Slot slot in slots)
        {
            if (slot.Used == 0)
                continue;
            if (slot.Chest >= 0 && Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncChestItem, number: slot.Chest, number2: slot.Index);
            slot.Callback?.Invoke(slot.Item, slot.Index);
        }
    }
}

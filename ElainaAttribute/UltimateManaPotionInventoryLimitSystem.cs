using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 究极魔力药水可以在储存空间中大量堆叠，但玩家主背包中合计最多持有五个。
/// </summary>
public sealed class UltimateManaPotionInventoryLimitSystem : ModSystem
{
    public const int InventoryCarryLimit = 5;

    public override void Load()
    {
        On_Player.GetItem_FillIntoOccupiedSlot += LimitOccupiedInventorySlotTransfer;
        On_Player.GetItem_FillEmptyInventorySlot += LimitEmptyInventorySlotTransfer;

        if (!Main.dedServ)
            On_ItemSlot.OverrideLeftClick += LimitMouseInventoryTransfer;
    }

    public override void Unload()
    {
        On_Player.GetItem_FillIntoOccupiedSlot -= LimitOccupiedInventorySlotTransfer;
        On_Player.GetItem_FillEmptyInventorySlot -= LimitEmptyInventorySlotTransfer;

        if (!Main.dedServ)
            On_ItemSlot.OverrideLeftClick -= LimitMouseInventoryTransfer;
    }

    public static bool CanAddToInventory(Player player, int stack)
    {
        return stack <= GetRemainingCapacity(player);
    }

    private static bool LimitOccupiedInventorySlotTransfer(
        On_Player.orig_GetItem_FillIntoOccupiedSlot orig,
        Player self,
        int playerIndex,
        Item newItem,
        GetItemSettings settings,
        Item source,
        int slot)
    {
        if (source.type != ModContent.ItemType<究极魔力药水>())
            return orig(self, playerIndex, newItem, settings, source, slot);

        int remainingCapacity = GetRemainingCapacity(self);
        if (remainingCapacity <= 0)
            return false;

        if (source.stack <= remainingCapacity)
            return orig(self, playerIndex, newItem, settings, source, slot);

        // 只让原版向当前背包堆叠转移剩余可持有数量
        Item limitedSource = source.Clone();
        limitedSource.stack = remainingCapacity;
        int limitedStack = limitedSource.stack;
        orig(self, playerIndex, limitedSource, settings, limitedSource, slot);
        int transferred = limitedStack - limitedSource.stack;
        source.stack -= transferred;

        // 即使限定数量已全部转移，也要让 GetItem 继续处理原物品的剩余部分
        return false;
    }

    private static bool LimitEmptyInventorySlotTransfer(
        On_Player.orig_GetItem_FillEmptyInventorySlot orig,
        Player self,
        int playerIndex,
        Item newItem,
        GetItemSettings settings,
        Item source,
        int slot)
    {
        if (source.type != ModContent.ItemType<究极魔力药水>())
            return orig(self, playerIndex, newItem, settings, source, slot);

        int remainingCapacity = GetRemainingCapacity(self);
        if (remainingCapacity <= 0)
            return false;

        if (source.stack <= remainingCapacity)
            return orig(self, playerIndex, newItem, settings, source, slot);

        // 空槽会直接持有传入的物品实例，因此使用克隆保留原物品中的溢出数量
        Item limitedSource = source.Clone();
        limitedSource.stack = remainingCapacity;
        if (!orig(self, playerIndex, limitedSource, settings, limitedSource, slot))
            return false;

        source.stack -= remainingCapacity;

        // 主背包已达到上限，让 GetItem 把剩余部分继续交给虚空袋
        return false;
    }

    private static bool LimitMouseInventoryTransfer(
        On_ItemSlot.orig_OverrideLeftClick orig,
        Item[] inventory,
        int context,
        int slot)
    {
        bool isMainInventorySlot = context is ItemSlot.Context.InventoryItem or ItemSlot.Context.HotbarItem;

        if (isMainInventorySlot &&
            Main.mouseItem.type == ModContent.ItemType<究极魔力药水>() &&
            !CanAddToInventory(Main.LocalPlayer, Main.mouseItem.stack))
        {
            return true;
        }

        return orig(inventory, context, slot);
    }

    private static int GetRemainingCapacity(Player player)
    {
        int currentStack = player.CountItem(
            ModContent.ItemType<究极魔力药水>(),
            InventoryCarryLimit + 1);

        return global::System.Math.Max(0, InventoryCarryLimit - currentStack);
    }
}

public sealed class UltimateManaPotionPickupLimitGlobalItem : GlobalItem
{
    public override bool CanPickup(Item item, Player player)
    {
        return item.type != ModContent.ItemType<究极魔力药水>() ||
               UltimateManaPotionInventoryLimitSystem.CanAddToInventory(player, 1) ||
               player.ItemSpaceForCofveve(item);
    }
}

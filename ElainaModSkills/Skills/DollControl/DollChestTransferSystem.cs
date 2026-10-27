using Terraria.DataStructures;
using Terraria.UI;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 玩偶远程打开的箱子：取出（点击箱内物品、一键夺取、快速补货）交给开箱玩偶携带；
/// 放入（鼠标物品点箱子格、Shift 点击背包物品、存入全部、快速堆叠）交给另一只空闲玩偶送过去，没有空闲玩偶时拦下并提示。
/// 只在 <see cref="DollCommandPlayer.RemoteChestTransferActive"/> 时生效，其余情况完全走原版。
/// </summary>
public sealed class DollChestTransferSystem : ModSystem
{
    public override void Load()
    {
        On_ItemSlot.LeftClick_ItemArray_int_int += LeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += RightClick;
        On_ChestUI.LootAll += LootAll;
        On_ChestUI.DepositAll += DepositAll;
        On_ChestUI.QuickStack += QuickStack;
        On_ChestUI.Restock += Restock;
        On_ChestUI.TryPlacingInChest += TryPlacingInChest;
    }

    public override void Unload()
    {
        On_ItemSlot.LeftClick_ItemArray_int_int -= LeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int -= RightClick;
        On_ChestUI.LootAll -= LootAll;
        On_ChestUI.DepositAll -= DepositAll;
        On_ChestUI.QuickStack -= QuickStack;
        On_ChestUI.Restock -= Restock;
        On_ChestUI.TryPlacingInChest -= TryPlacingInChest;
    }

    private static DollCommandPlayer RemoteTransfer()
    {
        if (Main.gameMenu || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers) return null;
        var commands = Main.LocalPlayer.GetModPlayer<DollCommandPlayer>();
        return commands.RemoteChestTransferActive ? commands : null;
    }

    private static void LeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot)
    {
        // Shift 点击插入聊天物品链接不涉及拿取，保留原版。
        if (context != ItemSlot.Context.ChestItem || Main.cursorOverride == 2 || RemoteTransfer() is not { } commands)
        {
            orig(inv, context, slot);
            return;
        }
        if (!Main.mouseLeft || !Main.mouseLeftRelease || Main.LocalPlayer.itemAnimation != 0) return;
        // 空手时普通左键、Shift 与 Ctrl 点击都由开箱玩偶取走整组；鼠标上有物品时交给送货玩偶，优先放进点击的格子。
        if (!Main.mouseItem.IsAir) commands.DepositToRemoteChest(Main.mouseItem, slot);
        else commands.TakeFromRemoteChest(slot, inv[slot].stack);
    }

    private static void RightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot)
    {
        if (context != ItemSlot.Context.ChestItem || RemoteTransfer() is not { } commands)
        {
            orig(inv, context, slot);
            return;
        }
        if (!Main.mouseRight || !Main.mouseRightRelease || Main.LocalPlayer.itemAnimation != 0) return;
        // 右键只取不放：物品由玩偶带回，不经过鼠标，所以鼠标上拿着什么都不影响。
        commands.TakeFromRemoteChest(slot, 1);
    }

    private static void LootAll(On_ChestUI.orig_LootAll orig)
    {
        if (RemoteTransfer() is { } commands) commands.LootAllIntoDoll();
        else orig();
    }

    private static void DepositAll(On_ChestUI.orig_DepositAll orig, ContainerTransferContext context)
    {
        if (RemoteTransfer() is { } commands) commands.DepositAllIntoDoll();
        else orig(context);
    }

    private static void QuickStack(On_ChestUI.orig_QuickStack orig, ContainerTransferContext context, bool voidStack)
    {
        // 按钮只发起背包的快速堆叠；虚空袋那一遍只来自“堆叠到附近箱子”，那时打开的不是远程箱子。
        if (RemoteTransfer() is not { } commands) orig(context, voidStack);
        else if (!voidStack) commands.QuickStackIntoDoll();
    }

    private static void Restock(On_ChestUI.orig_Restock orig)
    {
        if (RemoteTransfer() is { } commands) commands.RestockIntoDoll();
        else orig();
    }

    /// <summary>Shift 点击背包物品：悬停判断返回是否有送货玩偶且放得下，点击时交给送货玩偶。</summary>
    private static bool TryPlacingInChest(On_ChestUI.orig_TryPlacingInChest orig, Item item, bool justCheck, int itemSlotContext)
    {
        if (RemoteTransfer() is not { } commands) return orig(item, justCheck, itemSlotContext);
        return justCheck ? commands.CanQueueDeposit(item) : commands.DepositToRemoteChest(item);
    }
}

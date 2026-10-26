using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Terraria.UI;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 玩偶远程打开的箱子只出不进：点击箱内物品或一键夺取都交给开箱玩偶携带，存入、快速堆叠、补货与放入均被拦下。
/// 只在 <see cref="DollCommandPlayer.RemoteChestTransferActive"/> 时生效，其余情况完全走原版。
/// </summary>
public sealed class DollChestTransferSystem : ModSystem
{
    /// <summary>存入全部、快速堆叠、补货三个按钮隐藏后，下方按钮上移的距离（原版按钮行距 26）。</summary>
    private const int HiddenButtonsHeight = 3 * 26;

    public override void Load()
    {
        On_ItemSlot.LeftClick_ItemArray_int_int += LeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += RightClick;
        On_ChestUI.LootAll += LootAll;
        On_ChestUI.DepositAll += DepositAll;
        On_ChestUI.QuickStack += QuickStack;
        On_ChestUI.Restock += Restock;
        On_ChestUI.TryPlacingInChest += TryPlacingInChest;
        On_ChestUI.DrawButton += DrawButton;
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
        On_ChestUI.DrawButton -= DrawButton;
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
        // 普通左键、Shift 与 Ctrl 点击都由玩偶取走整组；鼠标上有物品时不能放入或交换。
        if (!Main.mouseItem.IsAir) commands.NotifyNoDeposit();
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
        if (!Main.mouseItem.IsAir) commands.NotifyNoDeposit();
        else commands.TakeFromRemoteChest(slot, 1);
    }

    private static void LootAll(On_ChestUI.orig_LootAll orig)
    {
        if (RemoteTransfer() is { } commands) commands.LootAllIntoDoll();
        else orig();
    }

    private static void DepositAll(On_ChestUI.orig_DepositAll orig, ContainerTransferContext context)
    {
        if (RemoteTransfer() == null) orig(context);
    }

    private static void QuickStack(On_ChestUI.orig_QuickStack orig, ContainerTransferContext context, bool voidStack)
    {
        if (RemoteTransfer() == null) orig(context, voidStack);
    }

    private static void Restock(On_ChestUI.orig_Restock orig)
    {
        if (RemoteTransfer() == null) orig();
    }

    private static bool TryPlacingInChest(On_ChestUI.orig_TryPlacingInChest orig, Item item, bool justCheck, int itemSlotContext)
        => RemoteTransfer() == null && orig(item, justCheck, itemSlotContext);

    private static void DrawButton(On_ChestUI.orig_DrawButton orig, SpriteBatch spriteBatch, int id, int x, int y)
    {
        if (RemoteTransfer() == null)
        {
            orig(spriteBatch, id, x, y);
            return;
        }
        switch (id)
        {
            case ChestUI.ButtonID.DepositAll:
            case ChestUI.ButtonID.QuickStack:
            case ChestUI.ButtonID.Restock:
                ChestUI.UpdateHover(id, hovering: false);
                return;
            case >= ChestUI.ButtonID.Sort:
                orig(spriteBatch, id, x, y - HiddenButtonsHeight);
                return;
            default:
                orig(spriteBatch, id, x, y);
                return;
        }
    }
}

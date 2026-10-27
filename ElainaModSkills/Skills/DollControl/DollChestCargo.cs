using System;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 远程开箱时取出的物品先放在开箱玩偶身上，等它回到主人身边再抛还；放入见 DollChestDeposit。
/// 携带物保存完整物品实例（前缀、Mod 数据），记在主人身上并随角色存档，玩偶消失也不会丢；
/// 携带者失效后由任意回到主人身边的玩偶交还。携带只在主人端，其他客户端只看到箱子同步变化。
/// </summary>
public sealed partial class DollCommandPlayer
{
    private readonly List<(Item Item, DollPetProjectile Carrier)> chestCargo = [];
    private uint depositHintAt;

    /// <summary>当前箱子由玩偶远程打开且主人本人够不着：拿取交给开箱玩偶，放入交给另一只送货玩偶。主人走近后恢复原版。</summary>
    internal bool RemoteChestTransferActive => Player.whoAmI == Main.myPlayer && !remoteChestTravelling
        && remoteChestIndex >= 0 && remoteChestOrigin is { } origin && RemoteHelperAlive() && ObserveOpenedChest()
        && !Player.IsInInteractionRangeToMultiTileHitbox(origin.X, origin.Y);

    private bool CarrierAlive(DollPetProjectile carrier) => carrier?.Projectile is { active: true } projectile
        && projectile.owner == Player.whoAmI && projectile.ModProjectile == carrier && !carrier.IsDismissing;

    internal bool IsCarryingCargo(DollPetProjectile worker)
    {
        foreach (var (_, carrier) in chestCargo)
            if (carrier == worker) return true;
        return false;
    }

    /// <summary>玩偶头顶显示的携带物：该玩偶要送进箱子或带回的第一件物品，没有时为 0。</summary>
    internal int CarryType(DollPetProjectile worker)
    {
        foreach (var (item, courier, _) in chestDeposits)
            if (courier == worker) return item.type;
        foreach (var (item, carrier) in chestCargo)
            if (carrier == worker) return item.type;
        return 0;
    }

    private bool Deliverable(DollPetProjectile carrier, DollPetProjectile worker) => carrier == worker || !CarrierAlive(carrier);

    /// <summary>不在开箱、且身上有货或有无人携带的货时，回到主人身边即可交还。</summary>
    internal bool CanDeliverCargo(DollPetProjectile worker)
    {
        if (chestCargo.Count == 0 || IsRemoteChestHelper(worker)) return false;
        foreach (var (_, carrier) in chestCargo)
            if (Deliverable(carrier, worker)) return true;
        return false;
    }

    /// <summary>远程箱子第 <paramref name="slot"/> 格交给开箱玩偶携带 <paramref name="amount"/> 个。</summary>
    internal void TakeFromRemoteChest(int slot, int amount)
    {
        if (!RemoteChestTransferActive || Main.chest[Player.chest] is not { } chest
            || slot < 0 || slot >= chest.item.Length || chest.item[slot] is not { IsAir: false } source) return;
        amount = Math.Min(amount, source.stack);
        if (amount <= 0) return;
        Item taken = source.Clone();
        ItemLoader.SplitStack(taken, source, amount);
        if (source.stack <= 0) chest.item[slot] = new Item();
        AddCargo(taken, remoteChestHelper);
        // 与原版拿取一致，箱子格变化由原版消息同步给服务器。
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, Player.chest, slot);
        SoundEngine.PlaySound(SoundID.Grab, remoteChestHelper.Projectile.Center);
    }

    internal void LootAllIntoDoll()
    {
        if (!RemoteChestTransferActive || Main.chest[Player.chest] is not { } chest) return;
        for (int i = 0; i < chest.item.Length; i++)
            if (chest.item[i] is { IsAir: false } item) TakeFromRemoteChest(i, item.stack);
    }

    private void AddCargo(Item item, DollPetProjectile carrier)
    {
        foreach (var (held, heldCarrier) in chestCargo)
        {
            if (heldCarrier != carrier || held.type != item.type || held.stack >= held.maxStack) continue;
            ItemLoader.TryStackItems(held, item, out _);
            if (item.stack <= 0) return;
        }
        chestCargo.Add((item, carrier));
    }

    /// <summary>没有能送货的空闲玩偶、放入被拦下时给一次提示，按住或连点不刷屏。</summary>
    internal void NotifyNoDeposit()
    {
        if (Main.GameUpdateCount - depositHintAt < 60) return;
        depositHintAt = Main.GameUpdateCount;
        SoundEngine.PlaySound(SoundID.MenuTick);
        CombatText.NewText(Player.getRect(), new Color(240, 165, 176), LocalizeUI("DollChestNoDeposit"), dot: true);
    }

    /// <summary>把 <paramref name="worker"/> 携带的（以及无人携带的）箱子物品从 <paramref name="from"/> 抛向主人；只在主人端调用。</summary>
    internal void DeliverChestCargo(DollPetProjectile worker, Vector2 from, IEntitySource source)
    {
        if (Player.whoAmI != Main.myPlayer || chestCargo.Count == 0) return;
        from = ClearDropPoint(from);
        bool delivered = false;
        for (int i = 0; i < chestCargo.Count;)
        {
            var (item, carrier) = chestCargo[i];
            if (!Deliverable(carrier, worker))
            {
                i++;
                continue;
            }
            chestCargo.RemoveAt(i);
            if (item.IsAir) continue;
            delivered = true;
            ThrowToOwner(Item.NewItem(source, (int)from.X - 8, (int)from.Y - 8, 16, 16, item, noGrabDelay: true), from);
        }
        if (delivered) SoundEngine.PlaySound(SoundID.Grab, from);
    }

    private void SaveChestCargo(TagCompound tag)
    {
        List<TagCompound> list = [];
        foreach (var (item, _) in chestCargo)
            if (!item.IsAir) list.Add(ItemIO.Save(item));
        // 还没送进箱子的物品同样存为携带物。
        foreach (var (item, _, _) in chestDeposits)
            if (!item.IsAir) list.Add(ItemIO.Save(item));
        // 远程放置带走但尚未放下的物品同样是玩家的真实物品，存为携带物，下次进入时由玩偶交还。
        foreach (Item item in CarriedPlacementItems()) list.Add(ItemIO.Save(item));
        if (list.Count > 0) tag["DollChestCargo"] = list;
    }

    private void LoadChestCargo(TagCompound tag)
    {
        chestCargo.Clear();
        chestDeposits.Clear();
        // 未加载 Mod 的物品也原样保留（UnloadedItem 带着原数据），这些是玩家的真实物品。
        foreach (TagCompound entry in tag.GetList<TagCompound>("DollChestCargo"))
        {
            Item item = ItemIO.Load(entry);
            if (!item.IsAir) chestCargo.Add((item, null));
        }
    }
}

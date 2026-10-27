using System;
using Terraria.Audio;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 远程开箱时的放入：开箱玩偶守着箱子，另一只空闲玩偶充当送货员。放入的瞬间物品只交到送货玩偶手上，
/// 它飞到箱旁播完空手动作后才真正放进箱子；放不下的部分、或途中箱子已关闭，就转为携带物带回主人身边。
/// 放入量按原版 TryPlacingInChest 的顺序（指定格 → 同类未满格 → 第一个空格）对“箱子现状 + 已在途的放入”预先计算，
/// 只带走放得下的部分。送货单与携带物一样只在主人端，存档时并入携带物，不会丢。
/// </summary>
public sealed partial class DollCommandPlayer
{
    private readonly List<(Item Item, DollPetProjectile Courier, int Slot)> chestDeposits = [];

    /// <summary>送货玩偶：已有在途送货的优先继续接单，否则选开箱玩偶以外空闲（或只在照料种植盆）的玩偶，没带货、离箱子近的优先。</summary>
    private DollPetProjectile FindDepositCourier()
    {
        foreach (var (_, courier, _) in chestDeposits)
            if (CarrierAlive(courier)) return courier;
        if (remoteChestOrigin is not { } origin) return null;
        Vector2 chestCenter = origin.ToWorldCoordinates(16, 16);
        DollPetProjectile best = null;
        int bestRank = int.MaxValue;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (!projectile.active || projectile.owner != Player.whoAmI || projectile.ModProjectile is not DollPetProjectile doll
                || doll.IsDismissing || doll == remoteChestHelper || !doll.IsInterruptible) continue;
            int rank = IsCarryingCargo(doll) ? 1 : 0;
            float distance = Vector2.DistanceSquared(projectile.Center, chestCenter);
            if (rank > bestRank || rank == bestRank && distance >= bestDistance) continue;
            best = doll;
            bestRank = rank;
            bestDistance = distance;
        }
        return best;
    }

    internal bool TrySelectChestDeposit(DollPetProjectile worker, out Point target)
    {
        target = new Point(-1, -1);
        if (remoteChestOrigin is not { } origin) return false;
        foreach (var (_, courier, _) in chestDeposits)
        {
            if (courier != worker) continue;
            target = origin;
            return true;
        }
        return false;
    }

    /// <summary>箱子现状叠加在途放入后的样子；没有在途放入且不需要改写时直接返回箱子本身。</summary>
    private Item[] PlannedChest(Chest chest, bool copy)
    {
        if (!copy && chestDeposits.Count == 0) return chest.item;
        var slots = new Item[chest.item.Length];
        for (int i = 0; i < slots.Length; i++) slots[i] = chest.item[i].Clone();
        foreach (var (item, _, slot) in chestDeposits) Fit(slots, item, slot, apply: true);
        return slots;
    }

    /// <summary><paramref name="item"/> 能放进 <paramref name="slots"/> 多少；apply 时写入（只用于虚拟副本，不触发堆叠钩子）。</summary>
    private static int Fit(Item[] slots, Item item, int preferred, bool apply)
    {
        int left = item.stack;
        if (preferred >= 0 && preferred < slots.Length) left -= FitSlot(slots, preferred, item, left, apply);
        if (item.maxStack > 1)
            for (int i = 0; i < slots.Length && left > 0; i++)
                if (i != preferred && !slots[i].IsAir) left -= FitSlot(slots, i, item, left, apply);
        for (int i = 0; i < slots.Length && left > 0; i++)
            if (i != preferred && slots[i].IsAir) left -= FitSlot(slots, i, item, left, apply);
        return item.stack - left;
    }

    /// <summary>原版 Item.IsTheSameAs 不对模组公开，判定相同：type 与 netID 都一致。</summary>
    private static bool SameItem(Item a, Item b) => a.type == b.type && a.netID == b.netID;

    private static int FitSlot(Item[] slots, int index, Item item, int amount, bool apply)
    {
        Item slot = slots[index];
        if (amount <= 0) return 0;
        // 与原版一致，空格直接放下整组剩余。
        if (slot.IsAir)
        {
            if (apply) { slots[index] = item.Clone(); slots[index].stack = amount; }
            return amount;
        }
        if (slot.stack >= slot.maxStack || !SameItem(slot, item) || !ItemLoader.CanStack(slot, item)) return 0;
        int moved = Math.Min(amount, slot.maxStack - slot.stack);
        if (apply) slot.stack += moved;
        return moved;
    }

    /// <summary>把 <paramref name="source"/> 中放得下的部分交给送货玩偶，source 原地扣减；返回交出的数量。</summary>
    private int QueueDeposit(Item source, int slot, Item[] planned, DollPetProjectile courier)
    {
        int amount = Fit(planned, source, slot, apply: true);
        if (amount <= 0) return 0;
        Item taken;
        if (amount >= source.stack)
        {
            taken = source.Clone();
            source.TurnToAir();
        }
        else
        {
            taken = source.Clone();
            ItemLoader.SplitStack(taken, source, amount);
        }
        chestDeposits.Add((taken, courier, slot));
        return amount;
    }

    private void AfterDepositQueued(DollPetProjectile courier)
    {
        SoundEngine.PlaySound(SoundID.Grab, courier.Projectile.Center);
        Recipe.FindRecipes();
    }

    /// <summary>Shift 悬停判断：有送货玩偶且放得下才显示放入光标。</summary>
    internal bool CanQueueDeposit(Item item) => RemoteChestTransferActive && item is { IsAir: false }
        && Main.chest[Player.chest] is { } chest && FindDepositCourier() != null
        && Fit(PlannedChest(chest, copy: false), item, -1, apply: false) > 0;

    /// <summary>鼠标物品点箱子格、Shift 点击背包物品：交给送货玩偶，<paramref name="slot"/> 为优先放入的箱子格。</summary>
    internal bool DepositToRemoteChest(Item source, int slot = -1)
    {
        if (!RemoteChestTransferActive || Main.chest[Player.chest] is not { } chest || source is not { IsAir: false }) return false;
        if (FindDepositCourier() is not { } courier)
        {
            NotifyNoDeposit();
            return false;
        }
        if (QueueDeposit(source, slot, PlannedChest(chest, copy: true), courier) <= 0) return false;
        AfterDepositQueued(courier);
        return true;
    }

    /// <summary>存入全部：与原版相同，背包第 10–49 格未收藏的物品，放得下多少带走多少。</summary>
    internal void DepositAllIntoDoll()
    {
        if (!RemoteChestTransferActive || Main.chest[Player.chest] is not { } chest) return;
        if (FindDepositCourier() is not { } courier)
        {
            NotifyNoDeposit();
            return;
        }
        Item[] planned = PlannedChest(chest, copy: true);
        bool queued = false;
        for (int i = 49; i >= 10; i--)
            if (Player.inventory[i] is { IsAir: false, favorited: false } item)
                queued |= QueueDeposit(item, -1, planned, courier) > 0;
        if (queued) AfterDepositQueued(courier);
    }

    /// <summary>快速堆叠：与原版相同，只放箱子里（含在途放入）已有的非钱币种类，背包第 10–49 格未收藏的物品。</summary>
    internal void QuickStackIntoDoll()
    {
        if (!RemoteChestTransferActive || Main.chest[Player.chest] is not { } chest) return;
        if (FindDepositCourier() is not { } courier)
        {
            NotifyNoDeposit();
            return;
        }
        Item[] planned = PlannedChest(chest, copy: true);
        HashSet<int> types = [];
        foreach (Item item in planned)
            if (!item.IsAir && !item.IsACoin) types.Add(item.netID);
        bool queued = false;
        for (int i = 10; i < 50; i++)
            if (Player.inventory[i] is { IsAir: false, favorited: false } item && types.Contains(item.netID))
                queued |= QueueDeposit(item, -1, planned, courier) > 0;
        if (queued) AfterDepositQueued(courier);
    }

    /// <summary>
    /// 快速补货属于取出，由开箱玩偶带回：背包里已有的可堆叠种类补到满（扣掉玩偶已带着的同类），
    /// 弹药在背包有空格时整组带走，与原版补货的结果一致。
    /// </summary>
    internal void RestockIntoDoll()
    {
        if (!RemoteChestTransferActive || Main.chest[Player.chest] is not { } chest) return;
        Dictionary<int, int> room = [];
        int empty = 0;
        for (int i = 57; i >= 0; i--)
        {
            if (i >= 50 && i < 54) continue;
            Item item = Player.inventory[i];
            if (item.IsACoin) continue;
            if (item.stack > 0 && item.maxStack > 1) room[item.netID] = room.GetValueOrDefault(item.netID) + item.maxStack - item.stack;
            else if (item.IsAir) empty++;
        }
        foreach (var (item, _) in chestCargo)
            if (room.TryGetValue(item.netID, out int left)) room[item.netID] = left - item.stack;
        for (int i = 0; i < chest.item.Length; i++)
        {
            Item item = chest.item[i];
            if (item.IsAir || !room.TryGetValue(item.netID, out int left)) continue;
            int amount = Math.Min(Math.Max(left, 0), item.stack);
            if (amount < item.stack && item.ammo != 0 && empty > 0)
            {
                amount = item.stack;
                empty--;
            }
            if (amount <= 0) continue;
            room[item.netID] = left - amount;
            TakeFromRemoteChest(i, amount);
        }
    }

    /// <summary>送货玩偶在箱旁播完动作：真正放入；箱子已不再开着或放不下的部分转为它的携带物。</summary>
    internal void CompleteChestDeposit(DollPetProjectile courier)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        Chest chest = remoteChestOrigin is { } origin && !remoteChestTravelling && ObserveOpenedChest()
            && Vector2.DistanceSquared(courier.Projectile.Center, origin.ToWorldCoordinates(16, 16)) <= 80 * 80
            ? Main.chest[Player.chest] : null;
        bool placed = false;
        for (int i = 0; i < chestDeposits.Count;)
        {
            var (item, carrier, slot) = chestDeposits[i];
            if (carrier != courier)
            {
                i++;
                continue;
            }
            chestDeposits.RemoveAt(i);
            if (chest != null) placed |= PlaceInChest(chest, item, slot);
            if (!item.IsAir) AddCargo(item, courier);
        }
        if (placed) SoundEngine.PlaySound(SoundID.Grab, courier.Projectile.Center);
    }

    private bool PlaceInChest(Chest chest, Item item, int preferred)
    {
        Item[] slots = chest.item;
        bool placed = false;
        if (preferred >= 0 && preferred < slots.Length) placed |= PlaceSlot(slots, preferred, item);
        if (item.maxStack > 1)
            for (int i = 0; i < slots.Length && !item.IsAir; i++)
                if (i != preferred && !slots[i].IsAir) placed |= PlaceSlot(slots, i, item);
        for (int i = 0; i < slots.Length && !item.IsAir; i++)
            if (i != preferred && slots[i].IsAir) placed |= PlaceSlot(slots, i, item);
        return placed;
    }

    private bool PlaceSlot(Item[] slots, int index, Item item)
    {
        if (item.IsAir) return false;
        if (slots[index].IsAir)
        {
            slots[index] = item.Clone();
            item.TurnToAir();
        }
        else
        {
            if (slots[index].stack >= slots[index].maxStack || !SameItem(slots[index], item)
                || !ItemLoader.TryStackItems(slots[index], item, out _)) return false;
            if (item.stack <= 0) item.TurnToAir();
        }
        // 与原版放入一致，箱子格变化由原版消息同步给服务器。
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, Player.chest, index);
        return true;
    }

    /// <summary>
    /// 尚未放下的送货转为携带物，由原送货玩偶带回（它已失效则任意玩偶交还）。
    /// <paramref name="courier"/> 为空时处理全部；<paramref name="lostOnly"/> 时只处理送货玩偶已失效的。
    /// </summary>
    internal void ReturnDeposits(DollPetProjectile courier = null, bool lostOnly = false)
    {
        for (int i = 0; i < chestDeposits.Count;)
        {
            var (item, carrier, _) = chestDeposits[i];
            if (courier != null && carrier != courier || lostOnly && CarrierAlive(carrier))
            {
                i++;
                continue;
            }
            chestDeposits.RemoveAt(i);
            if (!item.IsAir) AddCargo(item, carrier);
        }
    }
}

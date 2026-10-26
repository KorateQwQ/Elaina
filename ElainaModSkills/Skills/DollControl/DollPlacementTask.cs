using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 远程放置的待放置队列：每项记下目标格、占用区域、所放物品与登记时的朝向，只在主人端存在。
/// 登记的瞬间玩偶就带走一份物品（魔杖带走一份材料），背包数量立刻减少；放下后这份物品即被用掉，
/// 取消任务、收回玩偶、目标放不下或主人走远时原样还给主人。存档时尚未放下的物品记入玩偶携带物，下次交还。
/// </summary>
public sealed partial class DollCommandPlayer
{
    /// <summary>同时排队的待放置上限。</summary>
    internal const int PlacementLimit = 1000;

    /// <param name="Target">回放原版放置时的目标格（登记时的光标格）。</param>
    /// <param name="Area">即将被占用的格子，用于拒绝重叠的登记。</param>
    /// <param name="Source">放置时拿在手上的物品（单个副本，保留模组数据）。</param>
    /// <param name="Carried">玩偶从背包带走的那一份（消耗品为自身，魔杖为材料）；不消耗的物品为 null。</param>
    /// <param name="TileType">即将放下的物块类型与样式，供 <see cref="DollPlacementGhost"/> 绘制虚影。</param>
    /// <param name="IsObject">按物件数据放置（家具等）；否则为单格物块，虚影按相邻格挑选接缝帧。</param>
    internal readonly record struct PendingPlacement(Point Target, Rectangle Area, Item Source, Item Carried, int Direction,
        int TileType = -1, int Style = 0, int Alternate = 0, int Random = -1, bool IsObject = false)
    {
        internal int ItemType => Source?.type ?? 0;
    }

    internal readonly List<PendingPlacement> Placements = [];
    private uint placementsCheckedAt;

    /// <summary>登记一次远程放置并带走一份物品；与已有待放置重叠、背包里已无可带走的物品或超过上限时拒绝。</summary>
    internal bool QueuePlacement(Item item, Rectangle area, Terraria.TileObject data, bool isObject, int tileType)
    {
        if (Player.whoAmI != Main.myPlayer || Placements.Count >= PlacementLimit) return false;
        foreach (var placement in Placements)
            if (placement.Area.Intersects(area)) return false;
        Item source = item.Clone();
        source.stack = 1;
        if (!TryCarry(item, out Item carried)) return false;
        Placements.Add(new(new(Player.tileTargetX, Player.tileTargetY), area, source, carried, Player.direction,
            isObject ? data.type : tileType, isObject ? data.style : item.placeStyle, data.alternate, data.random, isObject));
        return true;
    }

    /// <summary>
    /// 从背包带走一份：魔杖取一份材料，消耗品取手上的这一个，均经过物品消耗钩子；钩子不允许消耗或物品本不消耗时什么也不带。
    /// </summary>
    private bool TryCarry(Item item, out Item carried)
    {
        carried = null;
        Item supply = item;
        if (item.tileWand > 0)
        {
            supply = null;
            for (int i = 0; i < 58 && supply == null; i++)
                if (Player.inventory[i].type == item.tileWand && Player.inventory[i].stack > 0) supply = Player.inventory[i];
            if (supply == null) return false;
        }
        else if (!item.consumable || ItemID.Sets.ForceConsumption[item.type] == false) return true;
        if (supply.IsAir || supply.stack <= 0) return false;
        if (!ItemLoader.ConsumeItem(supply, Player)) return true;
        carried = supply.Clone();
        carried.stack = 1;
        supply.stack--;
        if (supply.stack <= 0) supply.TurnToAir();
        // 抓在鼠标上的物品放在第 58 格，那一格每帧都会被 Main.mouseItem 覆盖，必须同步回鼠标。
        if (Player.selectedItem == 58 && ReferenceEquals(supply, Player.inventory[58])) Main.mouseItem = supply.Clone();
        return true;
    }

    /// <summary>把待放置带走的物品还给主人：先放回背包，装不下的掉在主人脚下。</summary>
    private void ReleasePlacement(PendingPlacement placement)
    {
        if (Player.whoAmI != Main.myPlayer || placement.Carried is not { IsAir: false } carried) return;
        Item rest = Player.GetItem(Player.whoAmI, carried, GetItemSettings.InventoryEntityToPlayerInventorySettings);
        if (!rest.IsAir) Player.QuickSpawnItem(Player.GetSource_Misc("DollPlacement"), rest, rest.stack);
    }

    /// <summary>取消全部待放置并交还带走的物品。</summary>
    private void ReleasePlacements()
    {
        foreach (var placement in Placements) ReleasePlacement(placement);
        Placements.Clear();
    }

    /// <summary>格子是否已被某个待放置占用。</summary>
    internal bool PlacementCovers(Point point)
    {
        foreach (var placement in Placements)
            if (placement.Area.Contains(point)) return true;
        return false;
    }

    private int PlacementAt(Point target) => Placements.FindIndex(p => p.Target == target);

    /// <summary>每半秒清理：主人走远后放弃远处的待放置，带走的物品还给主人。</summary>
    private void ValidatePlacements()
    {
        if (Placements.Count == 0 || Main.GameUpdateCount - placementsCheckedAt < 30) return;
        placementsCheckedAt = Main.GameUpdateCount;
        for (int i = Placements.Count - 1; i >= 0; i--)
        {
            if (InRange(Placements[i].Target)) continue;
            ReleasePlacement(Placements[i]);
            Placements.RemoveAt(i);
        }
    }

    internal bool TrySelectPlacementTarget(Vector2 from, Point current, out Point target, DollPetProjectile worker = null)
    {
        ValidatePlacements();
        target = new(-1, -1);
        if (PlacementAt(current) >= 0 && !IsClaimed(current, false, true, worker)) target = current;
        else
        {
            float best = float.MaxValue;
            foreach (var placement in Placements)
            {
                if (IsClaimed(placement.Target, false, true, worker)) continue;
                float distance = Vector2.DistanceSquared(from, placement.Target.ToWorldCoordinates());
                if (distance < best) { best = distance; target = placement.Target; }
            }
        }
        if (target.X < 0) return false;
        ClaimTarget(target, false, true, worker);
        return true;
    }

    /// <summary>玩偶到位后放下物块：在目标格回放原版放置，用掉带来的那一份；放不下的目标放弃，物品还给主人。</summary>
    internal void CompletePlacement(Point target)
    {
        int index = PlacementAt(target);
        if (Player.whoAmI != Main.myPlayer || index < 0) return;
        var placement = Placements[index];
        Placements.RemoveAt(index);
        if (!Player.active || Player.dead || Player.noBuilding
            || !DollPlacementSystem.Replay(Player, placement.Source, placement.Target, placement.Direction))
            ReleasePlacement(placement);
    }

    /// <summary>尚未放下的物品随角色保存为玩偶携带物，避免退出时丢失；下次召唤后由玩偶交还。</summary>
    private IEnumerable<Item> CarriedPlacementItems()
    {
        foreach (var placement in Placements)
            if (placement.Carried is { IsAir: false } carried) yield return carried;
    }
}

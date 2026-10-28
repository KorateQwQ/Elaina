using System;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader.IO;
using 伊蕾娜.Config;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;
using Microsoft.Xna.Framework.Input;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 玩偶的指令、标记与收集物都记在主人身上：弹幕被顶掉或重新召唤时任务与物品不会丢失。
/// 标记只在本地客户端存在、仅自己可见；主人端推进挖掘，联机收集的拆除和掉落由服务器确认。
/// </summary>
public sealed partial class DollCommandPlayer : ModPlayer
{
    /// <summary>连锁标记单次最多覆盖的物块数，避免点一下地下石头就标记整片地层。</summary>
    public const int ChainLimit = 200;
    /// <summary>离主人超过此距离的标记暂不处理，玩偶回到主人身边，主人走近后继续。</summary>
    public const float WorkRange = 2400f;

    /// <summary>通过原版镐力判定后，每次敲击获得三倍挖掘进度，不提高工具镐力。</summary>
    private const int MiningDamageMultiplier = 3;

    public readonly HashSet<Point> TileMarks = [];
    public readonly HashSet<Point> WallMarks = [];
    /// <summary>仅在标记键按住期间存在，松开即提交；从不保留为草稿。</summary>
    public readonly HashSet<Point> SelectionMarks = [];
    private readonly Dictionary<int, int> loot = [];
    private readonly List<(int Type, int Stack)> captured = [];

    public DollCommand MarkMode { get; private set; } = DollCommand.Mine;
    public bool SettingsOpen { get; private set; }
    public DollCommand? Tool => DragStart.HasValue ? MarkMode : null;
    public Point? DragStart { get; private set; }
    public Point HoverTile { get; private set; }
    private bool dragged, lastLeft;
    private Point selectionEnd;
    private int validateTimer;

    /// <summary>挖掘、敲墙与远程放置任务；照料种植盆是自发行为，不算在内。</summary>
    public bool HasTask => TileMarks.Count > 0 || WallMarks.Count > 0 || Placements.Count > 0;
    public bool HasLoot => loot.Count > 0;
    /// <summary>挖掘与敲墙是否收集掉落；随角色保存，后续敲击立即使用当前选择。</summary>
    public bool BringBackItems { get; private set; } = true;
    public string SelectionBlockReason { get; private set; }

    public override void OnEnterWorld()
    {
        miningCarriers.Clear();
        // 标记属于这个世界；同一角色换世界时不能带过去。
        ClearTasks();
        DollGardenSystem.Join(Player);
        lastLeft = true;
    }

    #region 输入

    /// <summary>玩偶在场即可标记，与当前术式和手持物品无关。</summary>
    public bool CanCommand => Player.whoAmI == Main.myPlayer && Player.active && !Player.dead
        && DollPetProjectile.FindOwned(Player) != null;

    private bool InputAvailable => Main.hasFocus && !Main.gameMenu && !Main.blockMouse && !Main.mapFullscreen
        && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest && !Main.inFancyUI
        && !Main.ingameOptionsWindow && !Main.hideUI && Player.chest < 0 && Player.talkNPC < 0;
    private bool PointerInWorld => InputAvailable && !Player.mouseInterface;

    /// <summary>标记键当前绑定的按键名；未绑定时为 null。</summary>
    public static string MarkKeyName
    {
        get
        {
            string keys = KeyBind.DollMark == null ? "" : string.Join(" / ", KeyBind.DollMark.GetAssignedKeys());
            return keys.Length == 0 ? null : keys;
        }
    }

    internal void OpenCommandWheel()
    {
        CancelSelection();
        SettingsOpen = false;
        if (CanCommand) DollCommandWheel.TryOpen(() => CanCommand, MarkMode, OnCommandChosen);
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        bool leftPressed = Main.mouseLeft && !lastLeft;
        lastLeft = Main.mouseLeft;
        HoverTile = Main.MouseWorld.ToTileCoordinates();
        if (!CanCommand || !InputAvailable)
        {
            CancelSelection();
            SettingsOpen = false;
            return;
        }
        if (Main.keyState.IsKeyDown(Keys.Escape))
        {
            CancelSelection();
            SettingsOpen = false;
            return;
        }
        if (DollCommandWheel.OwnsPointer || ElainaSkillWheel.IsShowing || ElainaHealingWheel.IsShowing)
        {
            CancelSelection();
            return;
        }
        if (SettingsOpen)
        {
            var layout = DollSelectionBarDrawing.GetLayout(PlayerInput.OriginalScreenSize / Main.UIScale);
            Point pointer = (new Vector2(PlayerInput.MouseX, PlayerInput.MouseY) / Main.UIScale).ToPoint();
            if (layout.Panel.Contains(pointer))
            {
                Player.mouseInterface = true;
                Player.controlUseItem = Player.controlUseTile = false;
                if (leftPressed)
                {
                    if (layout.Start.Contains(pointer)) SettingsOpen = false;
                    else if (layout.BringBack.Contains(pointer)) BringBackItems = !BringBackItems;
                    else if (layout.Chain.Contains(pointer))
                        MarkMode = DollCommands.IsChain(MarkMode) ? DollCommands.BaseCommand(MarkMode)
                            : DollCommands.TargetsWalls(MarkMode) ? DollCommand.BreakWallChain : DollCommand.MineChain;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                return;
            }
        }
        if (!PointerInWorld)
        {
            CancelSelection();
            return;
        }
        ModKeybind key = KeyBind.DollMark;
        if (key == null) { CancelSelection(); return; }
        if (key.JustPressed)
        {
            SettingsOpen = false;
            DragStart = HoverTile;
            dragged = false;
            RefreshSelection();
        }
        if (DragStart is not { } start) return;
        // 离开起点后即为矩形手势，即使又移回起点也不会突然变成连锁。
        dragged |= start != HoverTile;
        if (selectionEnd != HoverTile || Main.GameUpdateCount % 15 == 0 || !key.Current) RefreshSelection();
        if (key.Current) return;
        if (SelectionBlockReason != null) Notify(SelectionBlockReason, new Color(240, 165, 176));
        else if (SelectionMarks.Count > 0)
        {
            (DollCommands.TargetsWalls(MarkMode) ? WallMarks : TileMarks).UnionWith(SelectionMarks);
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }
        CancelSelection();
    }

    private void OnCommandChosen(DollCommand command)
    {
        CancelSelection();
        switch (command)
        {
            case DollCommand.Cancel:
                ClearTasks();
                Notify("DollCommandCancelled", new Color(200, 187, 218));
                break;
            case DollCommand.Dismiss:
                ClearTasks();
                DollPetProjectile.DismissAll(Player);
                break;
            default:
                // 切换工作类型时保留连锁偏好，设置本身不产生任何任务。
                MarkMode = DollCommands.IsChain(MarkMode)
                    ? DollCommands.TargetsWalls(command) ? DollCommand.BreakWallChain : DollCommand.MineChain
                    : DollCommands.BaseCommand(command);
                SettingsOpen = true;
                break;
        }
    }

    private void CancelSelection()
    {
        DragStart = null;
        dragged = false;
        SelectionMarks.Clear();
        SelectionBlockReason = null;
    }

    private void RefreshSelection()
    {
        SelectionMarks.Clear();
        SelectionBlockReason = null;
        if (DragStart is not { } start) return;
        selectionEnd = HoverTile;
        bool walls = DollCommands.TargetsWalls(MarkMode);
        if (!dragged && DollCommands.IsChain(MarkMode)) SelectionMarks.UnionWith(FindChain(start, walls));
        else SelectionMarks.UnionWith(DollTileSelection.RectangleCells(start, HoverTile, walls));
        Item item = walls ? BestHammer() : Player.GetBestPickaxe();
        foreach (Point point in SelectionMarks)
        {
            SelectionBlockReason = DollMiningRules.BlockReason(Player, point, walls, item);
            if (SelectionBlockReason != null) break;
        }
    }

    private void Notify(string key, Color color) =>
        CombatText.NewText(Player.getRect(), color, LocalizeUI(key), dot: true);

    #endregion

    #region 标记

    public static bool IsMarkable(int x, int y, bool walls)
    {
        if (!WorldGen.InWorld(x, y, 1)) return false;
        Tile tile = Main.tile[x, y];
        // 家具、瓶子等可用镐拆除的物件不一定实心；碰撞属性不能作为选区条件。
        // 树木与祭坛等需要斧/锤的目标不属于当前镐子任务。
        return walls ? tile.WallType > 0
            : tile.HasTile && !Main.tileAxe[tile.TileType] && !Main.tileHammer[tile.TileType];
    }

    /// <summary>从起点按四邻接找相连同类，最多搜索 <see cref="ChainLimit"/> 格，再补齐边界处家具。</summary>
    public static List<Point> FindChain(Point start, bool walls)
    {
        List<Point> group = [];
        if (!IsMarkable(start.X, start.Y, walls)) return group;
        Tile origin = Main.tile[start.X, start.Y];
        int type = walls ? origin.WallType : origin.TileType;
        HashSet<Point> seen = [start];
        Queue<Point> queue = new();
        queue.Enqueue(start);
        while (queue.Count > 0 && group.Count < ChainLimit)
        {
            Point point = queue.Dequeue();
            group.Add(point);
            Span<Point> next = [new(point.X + 1, point.Y), new(point.X - 1, point.Y), new(point.X, point.Y + 1), new(point.X, point.Y - 1)];
            foreach (Point neighbor in next)
            {
                if (!IsMarkable(neighbor.X, neighbor.Y, walls) || !seen.Add(neighbor)) continue;
                Tile tile = Main.tile[neighbor.X, neighbor.Y];
                if ((walls ? tile.WallType : tile.TileType) == type) queue.Enqueue(neighbor);
            }
        }
        // 连锁上限不能把最后一件家具切成半件；最多补齐边界处的整件家具。
        HashSet<Point> complete = new(group);
        foreach (Point point in group) DollTileSelection.AddObject(complete, point, walls);
        return new List<Point>(complete);
    }

    /// <summary>取消任务：清空全部标记与待放置（远程放置带走的物品还给主人）并退出标记工具，玩偶随之回到跟随状态；空闲后仍会自发照料种植盆。</summary>
    public void ClearTasks()
    {
        TileMarks.Clear();
        WallMarks.Clear();
        ReleasePlacements();
        workClaims.Clear();
        ResetGarden();
        ReleaseRemoteChest(true);
        CancelSelection();
        SettingsOpen = false;
    }

    #endregion

    #region 挖掘

    /// <summary>
    /// 主人端玩偶每帧调用：沿用仍有效的当前目标，否则在范围内按距离挑选下一个可挖的标记。
    /// 标记是一次性的：物块已不在（被挖掉或被别人拆掉）即移除，之后填回也不会再挖。
    /// 镐力或锤子不足、地牢墙等永远挖不动的标记直接移除；暂时挖不动的（如上方有树、墙未露出边缘）
    /// 在没有其他可挖目标时一并清除，任务结束，避免玩偶一直卡住。
    /// </summary>
    internal bool TrySelectTarget(Vector2 from, Point current, bool currentWall, out Point target, out bool wall,
        DollPetProjectile worker = null)
    {
        if (++validateTimer >= 30)
        {
            validateTimer = 0;
            TileMarks.RemoveWhere(p => !IsMarkable(p.X, p.Y, false));
            WallMarks.RemoveWhere(p => !IsMarkable(p.X, p.Y, true));
        }

        target = current;
        wall = currentWall;
        if (current.X >= 0 && (currentWall ? WallMarks : TileMarks).Contains(current)
            && InRange(current) && Check(current, currentWall) == Work.Ready && !IsClaimed(current, currentWall, false, worker))
        {
            ClaimTarget(current, currentWall, false, worker);
            return true;
        }

        bool deferred = false;
        float best = float.MaxValue;
        Point found = new(-1, -1);
        bool foundWall = false;
        Scan(TileMarks, false);
        Scan(WallMarks, true);
        target = found;
        wall = foundWall;
        if (found.X >= 0)
        {
            ClaimTarget(found, foundWall, false, worker);
            return true;
        }
        if (!deferred && HasTask)
        {
            // 没有其他玩偶正在处理、也没有远处待办时，剩下的都挖不动，任务到此结束。
            TileMarks.Clear();
            WallMarks.Clear();
        }
        return false;

        void Scan(HashSet<Point> marks, bool walls)
        {
            List<Point> invalid = null;
            foreach (Point point in marks)
            {
                if (!InRange(point))
                {
                    deferred = true;
                    continue;
                }
                Work work = Check(point, walls);
                if (work == Work.Never)
                {
                    (invalid ??= []).Add(point);
                    continue;
                }
                if (work != Work.Ready) continue;
                if (IsClaimed(point, walls, false, worker))
                {
                    // 这只玩偶暂时空闲；其他玩偶完成后可能露出新的墙边缘/解除物块阻挡。
                    deferred = true;
                    continue;
                }
                float distance = Vector2.DistanceSquared(from, point.ToWorldCoordinates());
                if (distance >= best) continue;
                best = distance;
                found = point;
                foundWall = walls;
            }
            if (invalid != null) foreach (Point point in invalid) marks.Remove(point);
        }
    }

    private bool InRange(Point point) =>
        Vector2.DistanceSquared(Player.Center, point.ToWorldCoordinates()) <= WorkRange * WorkRange;

    private enum Work { Ready, Later, Never }

    private Work Check(Point point, bool walls)
    {
        int x = point.X, y = point.Y;
        if (!IsMarkable(x, y, walls)) return Work.Never;
        Item tool = walls ? BestHammer() : Player.GetBestPickaxe();
        if (DollMiningRules.BlockReason(Player, point, walls, tool) != null) return Work.Never;
        if (walls)
        {
            return Player.CanPlayerSmashWall(x, y) ? Work.Ready : Work.Later;
        }
        return WorldGen.CanKillTile(x, y) ? Work.Ready : Work.Later;
    }

    public Item BestHammer()
    {
        Item best = null;
        for (int i = 0; i < 50; i++)
        {
            Item item = Player.inventory[i];
            if (item.stack > 0 && item.hammer > 0 && (best == null || item.hammer > best.hammer)) best = item;
        }
        return best;
    }

    /// <summary>两次敲击的间隔：与玩家用同一把工具的挥动节奏一致，敲墙按原版取一半。</summary>
    internal int HitInterval(bool walls)
    {
        Item tool = walls ? BestHammer() : Player.GetBestPickaxe();
        if (tool == null) return 30;
        return walls ? Math.Max(5, tool.useTime / 2) : Math.Max(6, tool.useTime);
    }

    /// <summary>按原版 PickTile / PickWall 的流程敲一下；伤害共用主人的 hitTile，裂纹由原版绘制。</summary>
    internal void Hit(Point point, bool walls, DollPetProjectile worker = null)
    {
        int x = point.X, y = point.Y;
        if (!IsMarkable(x, y, walls)) return;
        HitTile hitTile = Player.hitTile;
        if (walls)
        {
            Item hammer = BestHammer();
            if (hammer == null) return;
            int id = hitTile.HitObject(x, y, 2);
            if (hitTile.AddDamage(id, (int)(hammer.hammer * 1.5f)) >= 100)
            {
                hitTile.Clear(id);
                // 拆除被其他规则拦下时放弃这格，免得玩偶反复敲同一处。
                if (DollTileBreaker.BreakWall(x, y, captured, BringBackItems)) Collect(point, WallMarks, worker);
                else WallMarks.Remove(point);
            }
            else
            {
                WorldGen.KillWall(x, y, fail: true);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 2, x, y, 1f);
            }
            hitTile.Prune();
            return;
        }

        Item pickaxe = Player.GetBestPickaxe();
        if (pickaxe == null) return;
        Tile tile = Main.tile[x, y];
        int index = hitTile.HitObject(x, y, 1);
        int damage = DollTileBreaker.PickaxeDamage(Player, x, y, pickaxe.pick, index, tile);
        if (!WorldGen.CanKillTile(x, y)) damage = 0;
        if (Main.getGoodWorld) damage *= 2;
        // 仅放大判定后的有效进度：镐力不足或禁止拆除时仍为 0，不能越级挖掘。
        damage *= MiningDamageMultiplier;
        // 草、苔藓等先被敲成土或石头，下一次再挖掉。
        if (DollTileBreaker.TransformsOnKill(Player, hitTile, damage, x, y, pickaxe.pick, index, tile)) damage = 0;
        if (hitTile.AddDamage(index, damage) >= 100)
        {
            hitTile.TryClearingAndPruning(x, y, 1);
            if (Main.netMode == NetmodeID.MultiplayerClient && BringBackItems)
            {
                // 收集由服务器执行；收到回执后才入库，不能先在客户端拆家具或预造掉落。
                if (DollMiningNetwork.Request(Player, point, worker)) TileMarks.Remove(point);
            }
            else if (DollTileBreaker.BreakTile(x, y, captured, BringBackItems)) Collect(point, TileMarks, worker);
            else TileMarks.Remove(point);
        }
        else
        {
            WorldGen.KillTile(x, y, fail: true);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y, 1f);
        }
        if (damage != 0) hitTile.Prune();
    }

    private void Collect(Point point, HashSet<Point> marks, DollPetProjectile worker)
    {
        marks.Remove(point);
        ReceiveMiningLoot(captured, worker);
        captured.Clear();
    }

    /// <summary>单机拆除或服务器确认的掉落进入同一份可保存的玩偶背包。</summary>
    internal void ReceiveMiningLoot(IEnumerable<(int Type, int Stack)> drops, DollPetProjectile worker = null)
    {
        foreach (var (type, stack) in drops)
        {
            if (type <= ItemID.None || type >= ItemLoader.ItemCount || stack <= 0) continue;
            loot.TryGetValue(type, out int total);
            loot[type] = (int)Math.Min(int.MaxValue, (long)total + stack);
            AssignMiningCargo(worker, type, loot[type] - total);
        }
    }

    #endregion

    #region 收集物

    /// <summary>把玩偶收集的物品从 <paramref name="from"/> 抛向主人；只在主人端调用。</summary>
    public void DeliverLoot(Vector2 from, IEntitySource source, DollPetProjectile worker = null)
    {
        if (Player.whoAmI != Main.myPlayer || loot.Count == 0) return;
        from = ClearDropPoint(from);
        bool delivered = false;
        foreach (var (type, total) in new List<KeyValuePair<int, int>>(loot))
        {
            if (type <= ItemID.None || type >= ItemLoader.ItemCount) continue;
            int amount = DeliverableMiningLoot(type, total, worker);
            if (amount <= 0) continue;
            delivered = true;
            int maxStack = Math.Max(1, ContentSamples.ItemsByType[type].maxStack);
            for (int remaining = amount; remaining > 0;)
            {
                int stack = Math.Min(remaining, maxStack);
                remaining -= stack;
                ThrowToOwner(Item.NewItem(source, (int)from.X - 8, (int)from.Y - 8, 16, 16, type, stack, noGrabDelay: true), from);
            }
            if (amount == total) loot.Remove(type);
            else loot[type] = total - amount;
        }
        if (worker == null) miningCarriers.Clear();
        else miningCarriers.Remove(worker);
        foreach (var carrier in new List<DollPetProjectile>(miningCarriers.Keys))
            if (!LiveCarrier(carrier)) miningCarriers.Remove(carrier);
        if (delivered) SoundEngine.PlaySound(SoundID.Grab, from);
    }

    /// <summary>刚生成的掉落物从 <paramref name="from"/> 抛向主人，并同步给其他端。</summary>
    private void ThrowToOwner(int index, Vector2 from)
    {
        if (index < 0 || index > Main.maxItems) return;
        Item item = Main.item[index];
        Vector2 toward = Player.Center - from;
        item.velocity = toward.SafeNormalize(Vector2.UnitY) * Math.Min(7f, toward.Length() / 14f + 2f)
            + new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), -2.5f);
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);
    }

    /// <summary>玩偶能穿墙，可能停在方块里：沿玩偶到主人的连线找第一个不压实心物块的抛出点，都不行就从主人身上抛出。</summary>
    private Vector2 ClearDropPoint(Vector2 from)
    {
        const int Steps = 8;
        for (int i = 0; i <= Steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, Player.Center, i / (float)Steps);
            if (!Collision.SolidCollision(point - new Vector2(8f), 16, 16)) return point;
        }
        return Player.Center;
    }

    // 收集物存进角色存档：玩偶消失或退出世界都不会吞掉物品，下次召唤时交还。
    public override void SaveData(TagCompound tag)
    {
        SaveGardenOwner(tag);
        SaveChestCargo(tag);
        tag["DollBringBackItems"] = BringBackItems;
        tag["DollMarkMode"] = (int)MarkMode;
        if (loot.Count == 0) return;
        List<TagCompound> list = [];
        foreach (var (type, stack) in loot)
            list.Add(new TagCompound { ["item"] = ItemIO.Save(new Item(type)), ["stack"] = stack });
        tag["DollLoot"] = list;
    }

    public override void LoadData(TagCompound tag)
    {
        miningCarriers.Clear();
        LoadGardenOwner(tag);
        LoadChestCargo(tag);
        BringBackItems = !tag.ContainsKey("DollBringBackItems") || tag.GetBool("DollBringBackItems");
        int mode = tag.GetInt("DollMarkMode");
        MarkMode = mode >= 0 && mode <= (int)DollCommand.BreakWallChain ? (DollCommand)mode : DollCommand.Mine;
        loot.Clear();
        foreach (TagCompound entry in tag.GetList<TagCompound>("DollLoot"))
        {
            Item item = ItemIO.Load(entry.GetCompound("item"));
            int stack = entry.GetInt("stack");
            if (item.IsAir || stack <= 0 || item.ModItem is Terraria.ModLoader.Default.UnloadedItem) continue;
            loot.TryGetValue(item.type, out int total);
            loot[item.type] = (int)Math.Min(int.MaxValue, (long)total + stack);
        }
    }

    #endregion
}

using System;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Items;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 玩偶的指令、标记与收集物都记在主人身上：弹幕被顶掉或重新召唤时任务与物品不会丢失。
/// 标记只在本地客户端存在、仅自己可见；挖掘由主人客户端执行，物块变化走原版同步。
/// </summary>
public sealed class DollCommandPlayer : ModPlayer
{
    /// <summary>连锁标记单次最多覆盖的物块数，避免点一下地下石头就标记整片地层。</summary>
    public const int ChainLimit = 100;
    /// <summary>离主人超过此距离的标记暂不处理，玩偶回到主人身边，主人走近后继续。</summary>
    public const float WorkRange = 2400f;

    public readonly HashSet<Point> TileMarks = [];
    public readonly HashSet<Point> WallMarks = [];
    private readonly Dictionary<int, int> loot = [];
    private readonly List<(int Type, int Stack)> captured = [];

    /// <summary>当前标记工具；为空时左键打开指令盘。</summary>
    public DollCommand? Tool { get; private set; }
    /// <summary>矩形框选的起点，松开左键时与 <see cref="HoverTile"/> 围成的区域整体反选。</summary>
    public Point? DragStart { get; private set; }
    public Point HoverTile { get; private set; }

    private bool lastLeft, lastRight;
    private int validateTimer;

    public bool HasTask => TileMarks.Count > 0 || WallMarks.Count > 0;
    public bool HasLoot => loot.Count > 0;

    public override void OnEnterWorld()
    {
        // 标记属于这个世界；同一角色换世界时不能带过去。
        ClearTasks();
        lastLeft = lastRight = true;
    }

    #region 输入

    /// <summary>手持魔杖、当前术式为玩偶操控且玩偶在场时，左键归指令盘与标记使用。</summary>
    public bool InControlMode => Player.whoAmI == Main.myPlayer && Player.active && !Player.dead
        && Player.HeldItem.ModItem is ElainaWand && CurrentSkillIsDoll
        && Player.GetModPlayer<ElainaModplayer>().Elaina
        && Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled
        && DollPetProjectile.FindOwned(Player) != null;

    private static bool CurrentSkillIsDoll
    {
        get
        {
            var skills = ElainaSkillModPlayer.GetActiveSkill;
            int index = ElainaSkillModPlayer.CurrentSkillIndex;
            return skills != null && index >= 0 && index < skills.Count && skills[index]?.ModSkill is DollControlSkill;
        }
    }

    private bool PointerInWorld => !Player.mouseInterface && !Main.blockMouse && !Main.mapFullscreen
        && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest && !Main.inFancyUI;

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        bool left = Main.mouseLeft, right = Main.mouseRight;
        bool leftPressed = left && !lastLeft, rightPressed = right && !lastRight;
        lastLeft = left;
        lastRight = right;
        HoverTile = Main.MouseWorld.ToTileCoordinates();

        if (!InControlMode)
        {
            ExitTool();
            return;
        }
        if (DollCommandWheel.OwnsPointer) return;

        if (Tool is not { } tool)
        {
            if (leftPressed && PointerInWorld)
                DollCommandWheel.TryOpen(() => InControlMode, null, OnCommandChosen);
            return;
        }

        if (rightPressed)
        {
            // 吃掉这次右键，避免同时开门、开箱等物块交互。
            Main.mouseRightRelease = false;
            ExitTool();
            SoundEngine.PlaySound(SoundID.MenuClose);
            return;
        }

        bool walls = DollCommands.TargetsWalls(tool);
        if (DollCommands.IsChain(tool))
        {
            if (leftPressed && PointerInWorld) ToggleChain(HoverTile, walls);
            return;
        }
        if (leftPressed && PointerInWorld) DragStart = HoverTile;
        if (DragStart is { } start && !left)
        {
            DragStart = null;
            ToggleRect(start, HoverTile, walls);
        }
    }

    private void OnCommandChosen(DollCommand command)
    {
        switch (command)
        {
            case DollCommand.Cancel:
                ClearTasks();
                Notify("DollCommandCancelled", new Color(200, 187, 218));
                break;
            case DollCommand.Dismiss:
                ClearTasks();
                DollPetProjectile.FindOwned(Player)?.Dismiss();
                break;
            case DollCommand.Attack:
                Notify("DollCommandUnavailable", new Color(189, 176, 206));
                break;
            default:
                bool walls = DollCommands.TargetsWalls(command);
                if ((walls ? BestHammer() : Player.GetBestPickaxe()) == null)
                {
                    Notify(walls ? "DollNoHammer" : "DollNoPickaxe", new Color(240, 165, 176));
                    break;
                }
                Tool = command;
                DragStart = null;
                break;
        }
    }

    private void ExitTool()
    {
        Tool = null;
        DragStart = null;
    }

    private void Notify(string key, Color color) =>
        CombatText.NewText(Player.getRect(), color, LocalizeUI(key), dot: true);

    #endregion

    #region 标记

    public static bool IsMarkable(int x, int y, bool walls)
    {
        if (!WorldGen.InWorld(x, y, 1)) return false;
        Tile tile = Main.tile[x, y];
        return walls ? tile.WallType > 0
            : tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
    }

    /// <summary>区域内每个实心物块（或墙）的标记状态取反：未标记的标上，已标记的擦除。</summary>
    private void ToggleRect(Point a, Point b, bool walls)
    {
        HashSet<Point> marks = walls ? WallMarks : TileMarks;
        int left = Math.Clamp(Math.Min(a.X, b.X), 1, Main.maxTilesX - 2);
        int right = Math.Clamp(Math.Max(a.X, b.X), 1, Main.maxTilesX - 2);
        int top = Math.Clamp(Math.Min(a.Y, b.Y), 1, Main.maxTilesY - 2);
        int bottom = Math.Clamp(Math.Max(a.Y, b.Y), 1, Main.maxTilesY - 2);
        bool changed = false;
        for (int x = left; x <= right; x++)
            for (int y = top; y <= bottom; y++)
            {
                if (!IsMarkable(x, y, walls)) continue;
                Point point = new(x, y);
                if (!marks.Remove(point)) marks.Add(point);
                changed = true;
            }
        if (changed) SoundEngine.PlaySound(SoundID.MenuTick);
    }

    /// <summary>点中未标记的物块则标记整组相连同类，点中已标记的则清除整组。</summary>
    private void ToggleChain(Point start, bool walls)
    {
        List<Point> group = FindChain(start, walls);
        if (group.Count == 0) return;
        HashSet<Point> marks = walls ? WallMarks : TileMarks;
        bool mark = !marks.Contains(start);
        foreach (Point point in group)
        {
            if (mark) marks.Add(point);
            else marks.Remove(point);
        }
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    /// <summary>从起点按四邻接广度优先找同种类相连的物块（或墙），离起点近的优先，最多 <see cref="ChainLimit"/> 个。</summary>
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
        return group;
    }

    /// <summary>取消任务：清空全部标记并退出标记工具，玩偶随之回到跟随状态。</summary>
    public void ClearTasks()
    {
        TileMarks.Clear();
        WallMarks.Clear();
        ExitTool();
    }

    #endregion

    #region 挖掘

    /// <summary>
    /// 主人端玩偶每帧调用：沿用仍有效的当前目标，否则在范围内按距离挑选下一个可挖的标记。
    /// 标记是一次性的：物块已不在（被挖掉或被别人拆掉）即移除，之后填回也不会再挖。
    /// 镐力或锤子不足、地牢墙等永远挖不动的标记直接移除；暂时挖不动的（如上方有树、墙未露出边缘）
    /// 在没有其他可挖目标时一并清除，任务结束，避免玩偶一直卡住。
    /// </summary>
    internal bool TrySelectTarget(Vector2 from, Point current, bool currentWall, out Point target, out bool wall)
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
            && InRange(current) && Check(current, currentWall) == Work.Ready)
            return true;

        bool deferred = false;
        float best = float.MaxValue;
        Point found = new(-1, -1);
        bool foundWall = false;
        Scan(TileMarks, false);
        Scan(WallMarks, true);
        target = found;
        wall = foundWall;
        if (found.X >= 0) return true;
        if (!deferred && HasTask)
        {
            // 剩下的都暂时挖不动，任务到此结束。
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
        Tile tile = Main.tile[x, y];
        if (walls)
        {
            Item hammer = BestHammer();
            if (hammer == null || Main.wallDungeon[tile.WallType] && !NPC.downedBoss3
                || tile.WallType == WallID.LihzahrdBrickUnsafe && !NPC.downedGolemBoss) return Work.Never;
            return Player.CanPlayerSmashWall(x, y) ? Work.Ready : Work.Later;
        }
        Item pickaxe = Player.GetBestPickaxe();
        if (pickaxe == null || DollTileBreaker.PickaxeDamage(Player, x, y, pickaxe.pick, 0, tile) <= 0) return Work.Never;
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
    internal void Hit(Point point, bool walls)
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
                if (DollTileBreaker.BreakWall(x, y, captured)) Collect(point, WallMarks);
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
        // 草、苔藓等先被敲成土或石头，下一次再挖掉。
        if (DollTileBreaker.TransformsOnKill(Player, hitTile, damage, x, y, pickaxe.pick, index, tile)) damage = 0;
        if (hitTile.AddDamage(index, damage) >= 100)
        {
            hitTile.TryClearingAndPruning(x, y, 1);
            if (DollTileBreaker.BreakTile(x, y, captured)) Collect(point, TileMarks);
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

    private void Collect(Point point, HashSet<Point> marks)
    {
        marks.Remove(point);
        foreach (var (type, stack) in captured)
        {
            loot.TryGetValue(type, out int total);
            loot[type] = (int)Math.Min(int.MaxValue, (long)total + stack);
        }
        captured.Clear();
    }

    #endregion

    #region 收集物

    /// <summary>把玩偶收集的物品从 <paramref name="from"/> 抛向主人；只在主人端调用。</summary>
    public void DeliverLoot(Vector2 from, IEntitySource source)
    {
        if (Player.whoAmI != Main.myPlayer || loot.Count == 0) return;
        from = ClearDropPoint(from);
        foreach (var (type, total) in loot)
        {
            if (type <= ItemID.None || type >= ItemLoader.ItemCount) continue;
            int maxStack = Math.Max(1, ContentSamples.ItemsByType[type].maxStack);
            for (int remaining = total; remaining > 0;)
            {
                int stack = Math.Min(remaining, maxStack);
                remaining -= stack;
                int index = Item.NewItem(source, (int)from.X - 8, (int)from.Y - 8, 16, 16, type, stack, noGrabDelay: true);
                if (index < 0 || index > Main.maxItems) continue;
                Item item = Main.item[index];
                Vector2 toward = Player.Center - from;
                item.velocity = toward.SafeNormalize(Vector2.UnitY) * Math.Min(7f, toward.Length() / 14f + 2f)
                    + new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), -2.5f);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);
            }
        }
        loot.Clear();
        SoundEngine.PlaySound(SoundID.Grab, from);
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
        if (loot.Count == 0) return;
        List<TagCompound> list = [];
        foreach (var (type, stack) in loot)
            list.Add(new TagCompound { ["item"] = ItemIO.Save(new Item(type)), ["stack"] = stack });
        tag["DollLoot"] = list;
    }

    public override void LoadData(TagCompound tag)
    {
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

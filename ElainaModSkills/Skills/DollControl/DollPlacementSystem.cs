using System;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ObjectData;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 远程放置物块：手持可放置物块、目标在原版放置距离外但仍在屏幕内时，把原版放置距离（含智能光标的可达范围）暂时放宽到整个屏幕，
/// 让原版完成全部判定；真正落块的一步改为登记待放置，玩偶当即从背包带走一份物品。按住拖动时每帧补齐光标经过的格子，不受使用间隔节流。
/// 平台、种植盆可以悬空登记；普通实心块贴着其他待放置的格子也算有支撑，拖出的整条线不会因前一格尚未放下而断开。
/// 玩偶飞到位后由 <see cref="DollCommandPlayer.CompletePlacement"/> 在该格回放一次原版放置。距离内的放置完全走原版。
/// </summary>
public sealed class DollPlacementSystem : ModSystem
{
    /// <summary>放宽后的距离只需覆盖屏幕；远程判定另有屏幕内限制。</summary>
    private const int ExtendedRange = 1000;
    private static readonly FieldInfo dontConsumeWand =
        typeof(Player).GetField("dontConsumeWand", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>一帧内沿拖动轨迹补齐的最多格数。</summary>
    private const int MaxDragCells = 256;

    /// <summary>本帧放宽距离执行的原版放置；落块时改为登记。</summary>
    private static bool queueing;
    private static bool queued;
    /// <summary>上一帧按住使用时的光标格，用于补齐快速拖动跳过的格子。</summary>
    private static Point? lastDrag;
    private static uint lastDragTick;
    /// <summary>玩偶回放原版放置期间；放置结果记在 <see cref="ReplayPlaced"/>。</summary>
    internal static bool Replaying;
    internal static bool ReplayPlaced;

    public override void Load()
    {
        On_Player.PlaceThing += PlaceThing;
        On_Player.PlaceThing_Tiles += PlaceTiles;
        On_Player.PlaceThing_Tiles_PlaceIt += PlaceIt;
        On_Player.PlaceThing_Tiles_BlockPlacementForAssortedThings += Support;
        On_Player.PlaceThing_Tiles_CheckWandUsability += WandUsability;
        On_SmartCursorHelper.SmartCursorLookup += SmartCursorLookup;
    }

    public override void Unload()
    {
        On_Player.PlaceThing -= PlaceThing;
        On_Player.PlaceThing_Tiles -= PlaceTiles;
        On_Player.PlaceThing_Tiles_PlaceIt -= PlaceIt;
        On_Player.PlaceThing_Tiles_BlockPlacementForAssortedThings -= Support;
        On_Player.PlaceThing_Tiles_CheckWandUsability -= WandUsability;
        On_SmartCursorHelper.SmartCursorLookup -= SmartCursorLookup;
        queueing = queued = Replaying = ReplayPlaced = false;
        lastDrag = null;
    }

    public override void OnWorldUnload() => lastDrag = null;

    private static bool DontConsumeWand(Player player) => dontConsumeWand?.GetValue(player) is true;
    private static void SetDontConsumeWand(Player player, bool value) => dontConsumeWand?.SetValue(player, value);

    /// <summary>手持物块、玩偶在场且界面不拦截时，本地主人可以请玩偶代放。</summary>
    private static bool CanAssist(Player player)
        => player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server && player.active && !player.dead
            && !player.noBuilding && !Main.gameMenu && !Main.mapFullscreen && !player.mouseInterface
            && !Main.drawingPlayerChat && !Main.editSign && !Main.inFancyUI
            && player.inventory[player.selectedItem] is { IsAir: false, createTile: >= 0 }
            && DollPetProjectile.FindOwned(player) != null;

    /// <summary>与原版 PlaceThing_Tiles 相同的放置距离（含 blockRange）。</summary>
    internal static bool InVanillaRange(Player player, Item item, int x, int y)
    {
        float boost = item.tileBoost + player.blockRange;
        return player.position.X / 16f - Player.tileRangeX - boost <= x
            && (player.position.X + player.width) / 16f + Player.tileRangeX + boost - 1f >= x
            && player.position.Y / 16f - Player.tileRangeY - boost <= y
            && (player.position.Y + player.height) / 16f + Player.tileRangeY + boost - 2f >= y;
    }

    private static void PlaceThing(On_Player.orig_PlaceThing orig, Player self, ref Player.ItemCheckContext context)
    {
        queued = false;
        orig(self, ref context);
        // 登记时玩偶已带走一份物品，原版本帧不再另行消耗。
        if (queued) context.SkipItemConsumption = true;
        queued = false;
    }

    private static void PlaceTiles(On_Player.orig_PlaceThing_Tiles orig, Player self)
    {
        if (Replaying || queueing)
        {
            orig(self);
            return;
        }
        if (!CanAssist(self))
        {
            lastDrag = null;
            orig(self);
            return;
        }
        Item item = self.inventory[self.selectedItem];
        Point current = new(Player.tileTargetX, Player.tileTargetY);
        bool held = self.controlUseItem && self.itemAnimation > 0;
        // 智能光标每帧自己挑落点，前后落点之间不是拖动轨迹，不补格。
        Point? from = held && !Main.SmartCursorIsUsed && lastDrag is { } last && last != current
            && unchecked(Main.GameUpdateCount - lastDragTick) <= 2 ? last : null;
        lastDrag = held ? current : null;
        lastDragTick = Main.GameUpdateCount;
        if (InVanillaRange(self, item, current.X, current.Y) || !DollChestReachSystem.TileOnScreen(current.X, current.Y))
        {
            orig(self);
            return;
        }
        // 轨迹格在放宽距离之前筛选：够得着的格子留给原版，屏幕外的不登记；当前格最后处理，光标提示以它为准。
        List<Point> cells = [];
        if (from is { } start)
            foreach (Point cell in DragPath(start, current))
                if (!InVanillaRange(self, item, cell.X, cell.Y) && DollChestReachSystem.TileOnScreen(cell.X, cell.Y)) cells.Add(cell);
        cells.Add(current);
        int blockRange = self.blockRange;
        bool replacement = self.builderAccStatus[10] == 0;
        queueing = true;
        try
        {
            self.blockRange = ExtendedRange;
            // 替换物块会在原地直接挖掉旧块，远程时不启用。
            self.builderAccStatus[10] = 1;
            foreach (Point cell in cells)
            {
                Player.tileTargetX = cell.X;
                Player.tileTargetY = cell.Y;
                orig(self);
            }
        }
        finally
        {
            Player.tileTargetX = current.X;
            Player.tileTargetY = current.Y;
            self.blockRange = blockRange;
            if (replacement) self.builderAccStatus[10] = 0;
            queueing = false;
        }
        if (self.cursorItemIconEnabled) DollChestCursorSystem.CapturePlacementHover(self);
    }

    /// <summary>
    /// 上一帧光标格与当前格之间（两端都不含）的格子，按四连通逐格前进：斜向拖动得到阶梯而不是对角跳格，
    /// 相邻的待放置可以互相支撑。
    /// </summary>
    internal static IEnumerable<Point> DragPath(Point from, Point to)
    {
        int dx = Math.Abs(to.X - from.X), dy = Math.Abs(to.Y - from.Y);
        int sx = Math.Sign(to.X - from.X), sy = Math.Sign(to.Y - from.Y);
        int x = from.X, y = from.Y, error = dx - dy;
        for (int step = 0; step < MaxDragCells; step++)
        {
            if (y == to.Y || x != to.X && error * 2 > -dy) { error -= dy; x += sx; }
            else { error += dx; y += sy; }
            if (x == to.X && y == to.Y) yield break;
            yield return new(x, y);
        }
    }

    /// <summary>
    /// 远程登记与回放时补充原版的贴邻判定：平台、种植盆本身可以悬空；普通实心块贴着其他待放置的格子也算有支撑。
    /// 只在原版判定失败、目标格为空时放行；绳索、火把、种子等有专门规则的物块不受影响。
    /// </summary>
    private static bool Support(On_Player.orig_PlaceThing_Tiles_BlockPlacementForAssortedThings orig, Player self, bool canPlace)
    {
        bool result = orig(self, canPlace);
        if (result || !(queueing || Replaying) || self.whoAmI != Main.myPlayer) return result;
        int x = Player.tileTargetX, y = Player.tileTargetY;
        if (!WorldGen.InWorld(x, y, 2) || Main.tile[x, y].HasTile) return result;
        Item item = self.inventory[self.selectedItem];
        if (FreeStanding(item)) return true;
        if (!PlainBlock(item)) return result;
        var commands = self.GetModPlayer<DollCommandPlayer>();
        return commands.PlacementCovers(new(x - 1, y)) || commands.PlacementCovers(new(x + 1, y))
            || commands.PlacementCovers(new(x, y - 1)) || commands.PlacementCovers(new(x, y + 1));
    }

    /// <summary>回放魔杖放置时材料已由玩偶带来，不再要求背包里还有材料。</summary>
    private static bool WandUsability(On_Player.orig_PlaceThing_Tiles_CheckWandUsability orig, Player self, bool canUse)
        => Replaying && self.inventory[self.selectedItem].tileWand > 0 ? canUse : orig(self, canUse);

    /// <summary>平台与种植盆（含识别为种植盆的模组物块）不依赖任何支撑。</summary>
    private static bool FreeStanding(Item item) => item.createTile >= 0 && item.createTile < TileLoader.TileCount
        && (TileID.Sets.Platforms[item.createTile] || item.createTile == TileID.PlanterBox || DollPlanterBuilding.IsMaterial(item));

    /// <summary>走原版通用贴邻判定的普通实心块；草种、苔藓与再生工具需要特定的目标格，不在此列。</summary>
    private static bool PlainBlock(Item item)
    {
        int tile = item.createTile;
        return tile >= 0 && tile < TileLoader.TileCount && Main.tileSolid[tile] && !Main.tileSolidTop[tile]
            && !Main.tileFrameImportant[tile] && !Main.tileMoss[tile] && !ItemID.Sets.GrassSeeds[item.type]
            && item.type is not (ItemID.StaffofRegrowth or ItemID.AcornAxe);
    }

    private static TileObject PlaceIt(On_Player.orig_PlaceThing_Tiles_PlaceIt orig, Player self, bool newObjectType, TileObject data,
        int tileToCreate)
    {
        if (queueing)
        {
            Item item = self.inventory[self.selectedItem];
            if (self.GetModPlayer<DollCommandPlayer>().QueuePlacement(item, Footprint(self, newObjectType, data), data, newObjectType, tileToCreate))
            {
                // 登记不开始使用间隔，拖动经过的每一格都能登记；带走的那一份已在登记时扣除，原版本帧不再消耗。
                SetDontConsumeWand(self, true);
                queued = true;
            }
            return data;
        }
        Point target = new(Player.tileTargetX, Player.tileTargetY);
        Tile tile = Main.tile[target.X, target.Y];
        bool had = tile.HasTile;
        ushort type = tile.TileType;
        short frameX = tile.TileFrameX, frameY = tile.TileFrameY;
        int itemType = self.inventory[self.selectedItem].type;
        TileObject result = orig(self, newObjectType, data, tileToCreate);
        tile = Main.tile[target.X, target.Y];
        bool placed = tile.HasTile && (!had || tile.TileType != type || tile.TileFrameX != frameX || tile.TileFrameY != frameY);
        if (Replaying) ReplayPlaced = placed;
        // 玩偶在场时放下的种植盆（亲手或由玩偶代放）交给玩偶照料。
        if (placed && self.whoAmI == Main.myPlayer && ContentSamples.ItemsByType.TryGetValue(itemType, out Item sample)
            && DollPlanterBuilding.IsMaterial(sample) && DollPetProjectile.FindOwned(self) != null)
            DollGardenSystem.Register(self, target, itemType);
        return result;
    }

    /// <summary>即将放置的物件占用的格子：物件按其放置数据的尺寸，普通物块为目标格。</summary>
    private static Rectangle Footprint(Player player, bool newObjectType, TileObject data)
    {
        var tileData = newObjectType ? TileObjectData.GetTileData(data.type, data.style, data.alternate) : null;
        return tileData == null ? new(Player.tileTargetX, Player.tileTargetY, 1, 1)
            : new(data.xCoord, data.yCoord, Math.Max(1, tileData.Width), Math.Max(1, tileData.Height));
    }

    /// <summary>智能光标的可达范围同样放宽到屏幕边缘，沿线铺块、自动寻找落点等原版行为在远处照常可用。</summary>
    private static void SmartCursorLookup(On_SmartCursorHelper.orig_SmartCursorLookup orig, Player player)
    {
        if (!Main.SmartCursorIsUsed || !CanAssist(player))
        {
            orig(player);
            return;
        }
        int rangeX = Player.tileRangeX, rangeY = Player.tileRangeY;
        Vector2 size = new Vector2(Main.screenWidth, Main.screenHeight) / Main.GameViewMatrix.Zoom;
        Vector2 topLeft = Main.screenPosition + (new Vector2(Main.screenWidth, Main.screenHeight) - size) / 2f;
        // 只放宽到恰好覆盖屏幕，避免智能光标逐格扫描的区域过大。
        int needX = (int)MathF.Ceiling(Math.Max(player.Center.X - topLeft.X, topLeft.X + size.X - player.Center.X) / 16f) + 1;
        int needY = (int)MathF.Ceiling(Math.Max(player.Center.Y - topLeft.Y, topLeft.Y + size.Y - player.Center.Y) / 16f) + 1;
        try
        {
            Player.tileRangeX = Math.Max(rangeX, needX);
            Player.tileRangeY = Math.Max(rangeY, needY);
            orig(player);
        }
        finally
        {
            Player.tileRangeX = rangeX;
            Player.tileRangeY = rangeY;
        }
    }

    /// <summary>
    /// 以主人身份在 <paramref name="target"/> 回放一次原版放置：手持临时换成玩偶带来的物品、朝向取登记时的朝向，
    /// 其余输入与使用状态在结束后全部还原。返回是否真的放下。
    /// </summary>
    internal static bool Replay(Player player, Item source, Point target, int direction)
    {
        Item held = player.inventory[player.selectedItem];
        int selected = player.selectedItem, targetX = Player.tileTargetX, targetY = Player.tileTargetY;
        int facing = player.direction, itemTime = player.itemTime, itemTimeMax = player.itemTimeMax;
        int animation = player.itemAnimation, animationMax = player.itemAnimationMax, blockRange = player.blockRange;
        bool use = player.controlUseItem, icon = player.cursorItemIconEnabled, wand = DontConsumeWand(player);
        bool replacement = player.builderAccStatus[10] == 0, blockMouse = Main.blockMouse;
        var preview = new TileObjectPreviewData();
        preview.CopyFrom(TileObject.objectPreview);
        Replaying = true;
        ReplayPlaced = false;
        try
        {
            // 手上临时换成玩偶带来的那件物品；物品已在登记时带走，放置本身不再扣背包。
            player.inventory[selected] = source.Clone();
            Player.tileTargetX = target.X;
            Player.tileTargetY = target.Y;
            player.direction = direction;
            player.itemTime = 0;
            player.itemAnimation = Math.Max(1, animation);
            player.controlUseItem = true;
            player.blockRange = ExtendedRange;
            player.builderAccStatus[10] = 1;
            var context = new Player.ItemCheckContext();
            player.PlaceThing(ref context);
            return ReplayPlaced;
        }
        finally
        {
            Replaying = false;
            player.selectedItem = selected;
            player.inventory[selected] = held;
            Player.tileTargetX = targetX;
            Player.tileTargetY = targetY;
            player.direction = facing;
            player.itemTime = itemTime;
            player.itemTimeMax = itemTimeMax;
            player.itemAnimation = animation;
            player.itemAnimationMax = animationMax;
            player.controlUseItem = use;
            player.blockRange = blockRange;
            if (replacement) player.builderAccStatus[10] = 0;
            player.cursorItemIconEnabled = icon;
            Main.blockMouse = blockMouse;
            SetDontConsumeWand(player, wand);
            TileObject.objectPreview.CopyFrom(preview);
        }
    }
}

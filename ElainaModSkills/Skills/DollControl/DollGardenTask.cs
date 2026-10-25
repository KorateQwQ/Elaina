using System;
using System.Linq;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

public sealed partial class DollCommandPlayer
{
    internal Guid GardenOwner = Guid.NewGuid();
    private bool gardenPaused;
    private readonly HashSet<Point> gardenReady = [];
    private readonly Dictionary<Point, ulong> gardenRetry = [];
    private Point[] gardenSweep = [];
    private int gardenCursor, gardenRevision = -1;
    private ulong gardenTick = ulong.MaxValue;
    internal bool HasGardenTask => !gardenPaused && gardenReady.Count > 0;

    internal void PauseGarden()
    {
        gardenPaused = true;
        gardenReady.Clear(); gardenRetry.Clear();
        gardenSweep = []; gardenRevision = -1; gardenCursor = 0;
    }
    internal void ResumeGarden() { gardenPaused = false; gardenTick = ulong.MaxValue; }

    // 每个主人每帧最多检查 64 个花盆，四只玩偶共享结果；不扫描整个世界。
    private void RefreshGarden()
    {
        if (gardenPaused || gardenTick == Main.GameUpdateCount) return;
        gardenTick = Main.GameUpdateCount;
        if (gardenRevision != DollGardenSystem.Revision)
        {
            gardenSweep = DollGardenSystem.Beds.Where(p => p.Value.Owner == GardenOwner).Select(p => p.Key).ToArray();
            gardenRevision = DollGardenSystem.Revision;
        }
        foreach (Point p in gardenReady.ToArray())
            if (!GardenAction(p, out _, out _)) gardenReady.Remove(p);
        for (int i = 0; i < Math.Min(64, gardenSweep.Length) && gardenReady.Count < 64; i++)
        {
            gardenCursor %= gardenSweep.Length;
            Point p = gardenSweep[gardenCursor++];
            if (GardenAction(p, out _, out _)) gardenReady.Add(p);
        }
    }

    private enum GardenWork { None, Plant, Harvest, ClearWeed }

    private bool GardenAction(Point p, out Item seed, out GardenWork action)
    {
        seed = null; action = GardenWork.None;
        if (gardenPaused || !InRange(p) || !DollGardenSystem.Beds.TryGetValue(p, out var bed)
            || bed.Owner != GardenOwner || !DollGardenSystem.Exists(p, bed)) return false;
        if (gardenRetry.TryGetValue(p, out ulong retry) && Main.GameUpdateCount < retry) return false;
        // 手动拆除任务优先，尤其不能在正在挖走的种植盆上重新种植。
        for (int dy = -1; dy <= 0; dy++)
            if (TileMarks.Contains(new(p.X, p.Y + dy)) || PlanterMarks.ContainsKey(new(p.X, p.Y + dy))) return false;
        Tile pot = Main.tile[p.X, p.Y];
        if (pot.IsActuated || pot.IsHalfBlock || pot.Slope != SlopeType.Solid) return false;
        if (DollHerbPlanting.CanClearWeed(p.X, p.Y - 1))
        {
            action = GardenWork.ClearWeed;
            return true;
        }
        int seedType = bed.Seed.Type;
        if (seedType < 0) return false; // 对应模组被卸载时保留记录，不偷偷换种。
        if (Main.tile[p.X, p.Y - 1].HasTile)
        {
            // 主人手工换种也继续照料；只识别原版或适配器明确声明的植物。
            if (seedType == 0 || !DollHerbPlanting.Matches(seedType, p.X, p.Y - 1))
            {
                seedType = DollHerbPlanting.IdentifySeed(p.X, p.Y - 1);
                if (seedType > 0) DollGardenSystem.SetSeed(Player, p, seedType);
            }
            if (seedType == 0) return false;
            if (!DollHerbPlanting.CanHarvest(seedType, p.X, p.Y - 1)) return false;
            action = GardenWork.Harvest;
            return true;
        }
        seed = DollHerbPlanting.FindSeed(Player, p, seedType);
        if (seed != null) action = GardenWork.Plant;
        return seed != null;
    }

    internal bool TrySelectGardenTarget(Vector2 from, Point current, out Point target, DollPetProjectile worker = null)
    {
        RefreshGarden();
        target = new(-1, -1);
        if (GardenAction(current, out _, out _) && !IsClaimed(current, true, true, worker)) target = current;
        else
        {
            float best = float.MaxValue;
            foreach (Point p in gardenReady)
            {
                if (IsClaimed(p, true, true, worker) || !GardenAction(p, out _, out _)) continue;
                float distance = Vector2.DistanceSquared(from, p.ToWorldCoordinates());
                if (distance < best) { best = distance; target = p; }
            }
        }
        if (target.X < 0) return false;
        ClaimTarget(target, true, true, worker);
        return true;
    }

    internal void TendGarden(Point p)
    {
        if (Player.whoAmI != Main.myPlayer || !GardenAction(p, out Item seed, out GardenWork action)) return;
        bool success;
        if (action == GardenWork.ClearWeed)
        {
            // 本次只割草，下一次完整动作再种；缺种时保持空盆，记住的药草品种不变。
            success = DollTileBreaker.BreakTile(p.X, p.Y - 1, captured, bringBackItems: false);
        }
        else if (action == GardenWork.Harvest)
        {
            int seedType = DollGardenSystem.Beds[p].Seed.Type;
            success = DollTileBreaker.BreakTile(p.X, p.Y - 1, captured);
            if (success)
            {
                // 收获的同种种子先交进主人主背包，便于下一次动作续种；装不下的仍由玩偶携带。
                for (int i = 0; i < captured.Count; i++)
                {
                    var (type, stack) = captured[i];
                    if (type != seedType) continue;
                    Item remainder = Player.GetItem(Player.whoAmI, new Item(type, stack), GetItemSettings.InventoryEntityToPlayerInventorySettings);
                    captured[i] = (type, remainder.IsAir ? 0 : remainder.stack);
                }
                foreach (var (type, stack) in captured)
                    if (stack > 0) loot[type] = (int)Math.Min(int.MaxValue, (long)loot.GetValueOrDefault(type) + stack);
                captured.Clear();
            }
        }
        else
        {
            int seedType = seed.type; // 最后一个种子消耗后原物品会 TurnToAir。
            success = DollHerbPlanting.Plant(Player, p, seed);
            if (success) DollGardenSystem.SetSeed(Player, p, seedType);
        }
        gardenReady.Remove(p);
        if (success) { gardenRetry.Remove(p); SoundEngine.PlaySound(SoundID.Grass, p.ToWorldCoordinates()); }
        else gardenRetry[p] = Main.GameUpdateCount + 300; // 模组钩子拒绝时退让，不长期占用玩偶。
    }

    private void SaveGardenOwner(TagCompound tag) => tag["DollGardenOwner"] = GardenOwner.ToString();
    private void LoadGardenOwner(TagCompound tag)
    {
        GardenOwner = Guid.TryParse(tag.GetString("DollGardenOwner"), out Guid owner) && owner != Guid.Empty ? owner : Guid.NewGuid();
        PauseGarden();
    }
}

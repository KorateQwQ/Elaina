using System;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameInput;
using static 伊蕾娜.ElainaModSkills.Skills.DollControl.DollPlanterBuilding;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

public sealed partial class DollCommandPlayer
{
    internal readonly Dictionary<Point, int> PlanterMarks = [];
    internal int SelectedPlanter { get; private set; }
    internal bool MaterialMenu { get; private set; }
    internal int MaterialMenuOffset { get; private set; }
    internal List<(int Type, int Count)> MaterialMenuOptions { get; private set; } = [];

    private void CloseMaterialMenu()
    {
        MaterialMenu = false;
        MaterialMenuOffset = 0;
        MaterialMenuOptions.Clear();
    }

    private void RefreshMaterialMenu()
    {
        if (!MaterialMenu) return;
        MaterialMenuOptions = Options(Player);
        // 库存增减不会让菜单按数量重新排序，鼠标下的条目保持稳定。
        MaterialMenuOptions.Sort((a, b) => a.Type.CompareTo(b.Type));
    }

    private void ToggleMaterialMenu()
    {
        if (MaterialMenu) { CloseMaterialMenu(); return; }
        MaterialMenu = true;
        MaterialMenuOffset = 0;
        RefreshMaterialMenu();
        DragStart = null;
        SoundEngine.PlaySound(SoundID.MenuOpen);
    }

    private void SelectDefaultMaterials()
    {
        CloseMaterialMenu();
        var planters = Options(Player);
        SelectedPlanter = planters.Count > 0 ? planters[0].Type : 0;
    }

    private void ChooseMaterial(int selected)
    {
        if (!Options(Player).Exists(o => o.Type == selected)) return;
        SelectedPlanter = selected;
        CloseMaterialMenu();
        DraftMarks.RemoveWhere(p => !CanDraftPlanter(p));
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private bool HandleMaterialMenu(DollSelectionBarDrawing.Layout bar, Vector2 pointer, bool leftPressed)
    {
        if (!MaterialMenu) return false;
        RefreshMaterialMenu();
        var menu = DollSelectionBarDrawing.GetMaterialMenuLayout(bar, MaterialMenuOptions.Count, MaterialMenuOffset);
        MaterialMenuOffset = menu.Offset;
        Point mouse = pointer.ToPoint();
        DragStart = null;
        if (menu.Panel.Contains(mouse))
        {
            Capture();
            PlayerInput.LockVanillaMouseScroll("Elaina:DollMaterials");
            int wheel = PlayerInput.ScrollWheelDeltaForUI;
            if (wheel != 0)
                MaterialMenuOffset = Math.Clamp(MaterialMenuOffset - Math.Sign(wheel), 0,
                    Math.Max(0, MaterialMenuOptions.Count - menu.VisibleRows));
            if (leftPressed)
            {
                if (menu.Previous.Contains(mouse)) MaterialMenuOffset = Math.Max(0, menu.Offset - menu.VisibleRows);
                else if (menu.Next.Contains(mouse)) MaterialMenuOffset = Math.Min(
                    Math.Max(0, MaterialMenuOptions.Count - menu.VisibleRows), menu.Offset + menu.VisibleRows);
                else
                    for (int row = 0; row < menu.VisibleRows; row++)
                    {
                        int index = menu.Offset + row;
                        if (index < MaterialMenuOptions.Count && menu.Row(row).Contains(mouse))
                        {
                            ChooseMaterial(MaterialMenuOptions[index].Type);
                            break;
                        }
                    }
            }
            return true;
        }
        if (bar.Panel.Contains(mouse))
        {
            // 让操作条继续处理切换列表、开始和退出。
            return false;
        }
        if (leftPressed || Main.mouseRight)
        {
            CloseMaterialMenu();
            Capture(); // 收起菜单的这次点击不能穿透到世界框选。
        }
        return true;

        void Capture()
        {
            Player.mouseInterface = true;
            Player.controlUseItem = Player.controlUseTile = false;
        }
    }

    internal bool CanDraftPlanter(Point p)
    {
        if (!CanBuild(p)) return false;
        for (int dy = -1; dy <= 1; dy++)
            if (PlanterMarks.ContainsKey(new Point(p.X, p.Y + dy))) return false;
        for (int dy = -1; dy <= 0; dy++)
            if (TileMarks.Contains(new Point(p.X, p.Y + dy))) return false;
        return true;
    }

    internal bool DraftOverlaps(Point p)
    {
        for (int dy = -1; dy <= 1; dy++)
            if (dy != 0 && DraftMarks.Contains(new Point(p.X, p.Y + dy))) return true;
        return false;
    }

    private void TogglePlanterRect(Point a, Point b)
    {
        Rectangle area = Area(a, b);
        bool limited = false;
        for (int y = area.Top + 1; y < area.Bottom; y += 2)
            for (int x = area.Left; x < area.Right; x++)
            {
                Point p = new(x, y);
                if (DraftMarks.Remove(p)) continue;
                if (DraftMarks.Count + PlanterMarks.Count >= Limit) { limited = true; continue; }
                if (CanDraftPlanter(p) && !DraftOverlaps(p)) DraftMarks.Add(p);
            }
        if (limited) Notify("DollPlanterLimit", new Color(240, 165, 176));
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    internal bool CanStartPlanting => DraftMarks.Count > 0 && Count(Player, SelectedPlanter) > 0;

    private void CommitPlanters()
    {
        DraftMarks.RemoveWhere(p => !CanDraftPlanter(p));
        if (DraftMarks.Count == 0) return;
        if (!CanStartPlanting)
        {
            Notify("DollNoPlanter", new Color(240, 165, 176));
            return;
        }
        foreach (Point p in DraftMarks) PlanterMarks[p] = SelectedPlanter;
        ExitTool();
        SoundEngine.PlaySound(SoundID.MenuOpen);
    }

    internal bool TrySelectPlanterTarget(Vector2 from, Point current, out Point target, DollPetProjectile worker = null)
    {
        target = new(-1, -1);
        if (PlanterMarks.ContainsKey(current) && InRange(current)
            && CanBuild(current) && !IsClaimed(current, false, true, worker))
        {
            target = current;
            ClaimTarget(target, false, true, worker);
            return true;
        }
        float best = float.MaxValue;
        List<Point> invalid = null;
        foreach (Point p in PlanterMarks.Keys)
        {
            if (!CanBuild(p)) { (invalid ??= []).Add(p); continue; }
            if (!InRange(p) || IsClaimed(p, false, true, worker)) continue;
            float distance = Vector2.DistanceSquared(from, p.ToWorldCoordinates());
            if (distance < best) { best = distance; target = p; }
        }
        if (invalid != null) foreach (Point p in invalid) PlanterMarks.Remove(p);
        if (target.X < 0) return false;
        ClaimTarget(target, false, true, worker);
        return true;
    }

    internal void PlacePlanter(Point p)
    {
        if (!PlanterMarks.TryGetValue(p, out int planter)) return;
        Result result = Place(Player, p, planter);
        if (result == Result.MissingPlanter)
        {
            PlanterMarks.Clear();
            Notify("DollPlanterExhausted", new Color(240, 165, 176));
            return;
        }
        PlanterMarks.Remove(p);
        if (result == Result.Placed)
        {
            DollGardenSystem.Register(Player, p, planter);
            SoundEngine.PlaySound(SoundID.Dig, p.ToWorldCoordinates());
        }
    }
}

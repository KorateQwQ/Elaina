using System;
using KL.Drawing;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using 伊蕾娜.ElainaModSkills.UICore;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 本地绘制玩偶的待挖标记与标记工具预览：物块标记为琥珀色，墙标记为天蓝色，连成片的区域只描外轮廓。
/// 标记不随术式切换消失，挖完或取消前一直提示这片区域即将被挖掘。
/// </summary>
public sealed class DollMarkSystem : ModSystem
{
    private static readonly Color TileColor = new(255, 196, 92), WallColor = new(110, 196, 255),
        AddColor = new(150, 235, 160), RemoveColor = new(240, 120, 130);
    // 操作条主按钮两端的四芒星与炼金手记共用同一图集。
    private const string StarsPath = "伊蕾娜/ElainaModAlchemy/UI/Assets/NotebookStars";

    private List<Point> _chain = [];
    private Point _chainAt = new(-1, -1);
    private bool _chainWalls;
    private uint _chainTick;

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || Main.myPlayer < 0) return;
        DollCommandPlayer commands = Main.LocalPlayer.GetModPlayer<DollCommandPlayer>();
        if (!commands.HasTask && commands.Tool == null) return;

        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            Rectangle view = new((int)(Main.screenPosition.X / 16) - 2, (int)(Main.screenPosition.Y / 16) - 2,
                Main.screenWidth / 16 + 5, Main.screenHeight / 16 + 5);
            float pulse = .5f + .5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4f);
            DrawMarks(batch, commands.WallMarks, WallColor, view, pulse);
            DrawMarks(batch, commands.TileMarks, TileColor, view, pulse);
            foreach (Point p in commands.PlanterMarks.Keys)
                if (view.Contains(p)) DrawPlanter(batch, p, false, .6f + .3f * pulse);
            if (commands.Tool is { } tool)
            {
                bool planting = tool == DollCommand.PlacePlanters;
                if (planting)
                {
                    foreach (Point p in commands.DraftMarks)
                        if (view.Contains(p)) DrawPlanter(batch, p, false, .9f);
                }
                else DrawMarks(batch, commands.DraftMarks, AddColor, view, pulse);
                var layout = DollSelectionBarDrawing.GetLayout(PlayerInput.OriginalScreenSize / Main.UIScale, planting);
                Vector2 pointer = new Vector2(PlayerInput.MouseX, PlayerInput.MouseY) / Main.UIScale;
                if (!Main.LocalPlayer.mouseInterface && !Main.mapFullscreen && !Main.blockMouse
                    && !commands.MaterialMenu && !layout.Panel.Contains(pointer.ToPoint())) DrawPreview(batch, commands, tool, view);
            }
        }
        finally
        {
            batch.End();
        }
    }

    private static void DrawMarks(SpriteBatch batch, HashSet<Point> marks, Color color, Rectangle view, float pulse)
    {
        Color fill = color * (.16f + .08f * pulse), edge = color * .9f;
        foreach (Point point in marks)
        {
            if (!view.Contains(point)) continue;
            Rectangle cell = Cell(point);
            batch.Draw(TextureAssets.MagicPixel.Value, cell, fill);
            // 只在与未标记格相邻的一侧描边，整片区域呈现为一圈轮廓。
            if (!marks.Contains(new Point(point.X, point.Y - 1))) Edge(batch, cell.X, cell.Y, 16, 2, edge);
            if (!marks.Contains(new Point(point.X, point.Y + 1))) Edge(batch, cell.X, cell.Bottom - 2, 16, 2, edge);
            if (!marks.Contains(new Point(point.X - 1, point.Y))) Edge(batch, cell.X, cell.Y, 2, 16, edge);
            if (!marks.Contains(new Point(point.X + 1, point.Y))) Edge(batch, cell.Right - 2, cell.Y, 2, 16, edge);
        }
    }

    private void DrawPreview(SpriteBatch batch, DollCommandPlayer commands, DollCommand tool, Rectangle view)
    {
        if (tool == DollCommand.PlacePlanters)
        {
            Rectangle planterArea = DollPlanterBuilding.Area(commands.DragStart ?? commands.HoverTile, commands.HoverTile);
            int firstY = planterArea.Top + 1 + Math.Max(0, (view.Top - planterArea.Top - 1) / 2) * 2;
            for (int y = firstY; y < Math.Min(planterArea.Bottom, view.Bottom); y += 2)
                for (int x = Math.Max(planterArea.Left, view.Left); x < Math.Min(planterArea.Right, view.Right); x++)
                {
                    Point p = new(x, y);
                    bool invalid = commands.DraftMarks.Contains(p) || !commands.CanDraftPlanter(p) || commands.DraftOverlaps(p);
                    DrawPlanter(batch, p, invalid, .8f);
                }
            Rectangle bounds = Cell(new Point(planterArea.X, planterArea.Y));
            bounds.Width = planterArea.Width * 16; bounds.Height = planterArea.Height * 16;
            Outline(batch, bounds, Color.White * .8f);
            return;
        }
        bool walls = DollCommands.TargetsWalls(tool);
        HashSet<Point> marks = commands.DraftMarks;
        Point hover = commands.HoverTile;
        Color toolColor = walls ? WallColor : TileColor;

        if (DollCommands.IsChain(tool))
        {
            // 预览这次点击会标记（绿）或清除（红）的整组；指针移到新格子或每秒刷新一次。
            if (hover != _chainAt || walls != _chainWalls || Main.GameUpdateCount - _chainTick > 60)
            {
                _chain = DollCommandPlayer.FindChain(hover, walls);
                _chainAt = hover;
                _chainWalls = walls;
                _chainTick = Main.GameUpdateCount;
            }
            Color color = (marks.Contains(hover) ? RemoveColor : AddColor) * .35f;
            foreach (Point point in _chain)
                if (view.Contains(point)) batch.Draw(TextureAssets.MagicPixel.Value, Cell(point), color);
            Outline(batch, Cell(hover), toolColor);
            return;
        }

        if (commands.DragStart is not { } start)
        {
            Outline(batch, Cell(hover), toolColor);
            return;
        }
        int left = Math.Min(start.X, hover.X), right = Math.Max(start.X, hover.X);
        int top = Math.Min(start.Y, hover.Y), bottom = Math.Max(start.Y, hover.Y);
        int visibleLeft = Math.Max(left, view.Left), visibleRight = Math.Min(right, view.Right);
        int visibleTop = Math.Max(top, view.Top), visibleBottom = Math.Min(bottom, view.Bottom);
        // 框内每格松开后会被反选：未标记的显示为绿色（将标记），已标记的显示为红色（将擦除）。
        for (int x = visibleLeft; x <= visibleRight; x++)
            for (int y = visibleTop; y <= visibleBottom; y++)
            {
                if (!DollCommandPlayer.IsMarkable(x, y, walls)) continue;
                Point point = new(x, y);
                batch.Draw(TextureAssets.MagicPixel.Value, Cell(point),
                    (marks.Contains(point) ? RemoveColor : AddColor) * .35f);
            }
        Rectangle area = Cell(new Point(left, top));
        area.Width = (right - left + 1) * 16;
        area.Height = (bottom - top + 1) * 16;
        Outline(batch, area, Color.White * .9f);
    }

    private static Rectangle Cell(Point point)
    {
        Vector2 position = new Vector2(point.X * 16, point.Y * 16) - Main.screenPosition;
        return new Rectangle((int)position.X, (int)position.Y, 16, 16);
    }

    private static void DrawPlanter(SpriteBatch batch, Point p, bool invalid, float opacity) =>
        DollPlanterDrawing.Draw(batch, TextureAssets.MagicPixel.Value,
            new Vector2(p.X * 16, p.Y * 16) - Main.screenPosition, invalid, opacity);

    private static void Edge(SpriteBatch batch, int x, int y, int width, int height, Color color) =>
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, width, height), color);

    private static void Outline(SpriteBatch batch, Rectangle area, Color color)
    {
        Edge(batch, area.X, area.Y, area.Width, 2, color);
        Edge(batch, area.X, area.Bottom - 2, area.Width, 2, color);
        Edge(batch, area.X, area.Y, 2, area.Height, color);
        Edge(batch, area.Right - 2, area.Y, 2, area.Height, color);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index >= 0)
            layers.Insert(index, new LegacyGameInterfaceLayer("伊蕾娜: Doll Selection", DrawSelectionBar, InterfaceScaleType.UI));
    }

    /// <summary>固定在屏幕下方的选区操作条；鼠标旁不再跟随整段说明。</summary>
    private static bool DrawSelectionBar()
    {
        if (Main.gameMenu || Main.myPlayer < 0 || Main.mapFullscreen || Main.inFancyUI
            || Main.drawingPlayerChat || Main.editSign || Main.editChest) return true;
        DollCommandPlayer commands = Main.LocalPlayer.GetModPlayer<DollCommandPlayer>();
        if (commands.Tool is not { } tool) return true;
        bool walls = DollCommands.TargetsWalls(tool);
        bool planting = tool == DollCommand.PlacePlanters;
        var layout = DollSelectionBarDrawing.GetLayout(PlayerInput.OriginalScreenSize / Main.UIScale, planting);
        Vector2 pointer = new Vector2(PlayerInput.MouseX, PlayerInput.MouseY) / Main.UIScale;
        if (layout.Panel.Contains(pointer.ToPoint())) Main.LocalPlayer.mouseInterface = true;
        Texture2D icon = ModContent.Request<Texture2D>(DollCommands.IconPath(tool)).Value;
        var art = new DollSelectionBarDrawing.Art(TextureAssets.MagicPixel.Value,
            ModContent.Request<Texture2D>(StarsPath).Value, UIDrawKit.TitleFont,
            FontManager.HarmonyOS_Sans_SC?.Value ?? FontAssets.MouseText.Value);
        DollSelectionBarDrawing.MaterialMenuView? menu = null;
        if (planting && commands.MaterialMenu)
        {
            var options = commands.MaterialMenuOptions;
            var menuLayout = DollSelectionBarDrawing.GetMaterialMenuLayout(layout, options.Count, commands.MaterialMenuOffset);
            if (menuLayout.Panel.Contains(pointer.ToPoint()))
            {
                Main.LocalPlayer.mouseInterface = true;
                PlayerInput.LockVanillaMouseScroll("Elaina:DollMaterials");
            }
            int selected = commands.SelectedPlanter;
            List<DollSelectionBarDrawing.MaterialMenuEntry> entries = [];
            foreach (var option in options)
                entries.Add(new(Material(option.Type, option.Count), option.Type == selected));
            menu = new(menuLayout, entries, LocalizeUI("DollChoosePlanter"),
                LocalizeUI("DollMaterialNone"));
        }
        // 数量文案保留 {0}，由绘制拆开后单独画数字。
        string countFormat = Language.GetTextValue("Mods.伊蕾娜.UI." + (planting ? "DollSelectionPlanters" : walls ? "DollSelectionWalls" : "DollSelectionTiles"));
        DollSelectionBarDrawing.Draw(Main.spriteBatch, art, icon, layout, pointer, Main.UIScale,
            LocalizeUI("DollCommand" + DollCommands.BaseCommand(tool)), countFormat, commands.DraftMarks.Count,
            LocalizeUI(planting ? "DollSelectionEmptyPlanters" : walls ? "DollSelectionEmptyWalls" : "DollSelectionEmptyTiles"),
            LocalizeUI(planting ? "DollPlanterHint" : DollCommands.IsChain(tool) ? "DollMarkHintChain" : "DollMarkHintRect"),
            LocalizeUI(planting ? "DollSelectionStartPlanters" : walls ? "DollSelectionStartWalls" : "DollSelectionStart"), LocalizeUI("DollSelectionExit"),
            planting ? Material() : null,
            planting ? commands.CanStartPlanting : commands.DraftBlockReason == null,
            menu, commands.MaterialMenu, planting ? null : LocalizeUI("DollBringBackItems"), commands.BringBackItems,
            planting ? null : LocalizeUI(walls ? "DollChainWalls" : "DollChainMining"), DollCommands.IsChain(tool),
            LocalizeUI("DollToggleOn"), LocalizeUI("DollToggleOff"),
            !planting && commands.DraftBlockReason != null ? LocalizeUI(commands.DraftBlockReason) : null);
        return true;

        DollSelectionBarDrawing.MaterialOption Material(int? choice = null, int? stock = null)
        {
            int type = choice ?? commands.SelectedPlanter;
            int owned = stock ?? DollPlanterBuilding.Count(commands.Player, type);
            int required = choice.HasValue ? 0 : commands.DraftMarks.Count;
            Texture2D texture = null;
            Rectangle source = default;
            if (type > 0)
            {
                Main.instance.LoadItem(type);
                texture = TextureAssets.Item[type].Value;
                source = Main.itemAnimations[type]?.GetFrame(texture) ?? texture.Bounds;
            }
            string label = choice.HasValue
                ? Language.GetTextValue("Mods.伊蕾娜.UI.DollMaterialListOption", Lang.GetItemNameValue(type), owned)
                : Language.GetTextValue("Mods.伊蕾娜.UI.DollMaterialOption",
                LocalizeUI("DollPlanterLabel"),
                type > 0 ? Lang.GetItemNameValue(type) : LocalizeUI("DollMaterialNone"), owned, required);
            if (!choice.HasValue && !commands.MaterialMenu
                && layout.Planter.Contains(pointer.ToPoint()))
                Main.instance.MouseText(LocalizeUI("DollMaterialHint"));
            return new(texture, source, label, owned >= Math.Max(1, required));
        }
    }
}

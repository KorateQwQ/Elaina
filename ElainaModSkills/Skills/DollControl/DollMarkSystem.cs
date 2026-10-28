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
/// 远程放置的待放置位置显示半透明物品图标与占用范围。
/// 标记不随术式切换消失，挖完或取消前一直提示这片区域即将被挖掘或放置。
/// </summary>
public sealed class DollMarkSystem : ModSystem
{
    private static readonly Color TileColor = new(255, 196, 92), WallColor = new(110, 196, 255),
        AddColor = new(150, 235, 160), RemoveColor = new(240, 120, 130), PlaceColor = new(202, 178, 237);
    // 操作条主按钮两端的四芒星与炼金手记共用同一图集。
    private const string StarsPath = "伊蕾娜/ElainaModAlchemy/UI/Assets/NotebookStars";

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
            DrawPlacements(batch, commands, view, pulse);
            if (commands.Tool is { })
            {
                DrawMarks(batch, commands.SelectionMarks, commands.SelectionBlockReason == null ? AddColor : RemoveColor, view, pulse);
                if (commands.DragStart is { } start)
                {
                    Rectangle area = Cell(new Point(Math.Min(start.X, commands.HoverTile.X), Math.Min(start.Y, commands.HoverTile.Y)));
                    area.Width = (Math.Abs(start.X - commands.HoverTile.X) + 1) * 16;
                    area.Height = (Math.Abs(start.Y - commands.HoverTile.Y) + 1) * 16;
                    Outline(batch, area, Color.White * .9f);
                }
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

    private readonly Dictionary<Point, int> _pendingTiles = [];

    /// <summary>
    /// 待放置显示为即将放下的物块的半透明虚影（<see cref="DollPlacementGhost"/>），相邻的待放置格互相接缝；
    /// 无法按原版规则绘制的物块退回为淡紫色占位框加半透明物品图标。
    /// </summary>
    private void DrawPlacements(SpriteBatch batch, DollCommandPlayer commands, Rectangle view, float pulse)
    {
        if (commands.Placements.Count == 0) return;
        _pendingTiles.Clear();
        foreach (var placement in commands.Placements)
            if (!placement.IsObject && placement.TileType >= 0) _pendingTiles[placement.Target] = placement.TileType;
        Func<Point, int> pending = p => _pendingTiles.TryGetValue(p, out int type) ? type : -1;
        foreach (var placement in commands.Placements)
            if (view.Intersects(placement.Area) && !DollPlacementGhost.Draw(batch, placement, pending))
                DrawPlacementIcon(batch, placement, pulse);
    }

    private static void DrawPlacementIcon(SpriteBatch batch, DollCommandPlayer.PendingPlacement placement, float pulse)
    {
        Rectangle area = Cell(placement.Area.Location);
        area.Width = placement.Area.Width * 16;
        area.Height = placement.Area.Height * 16;
        batch.Draw(TextureAssets.MagicPixel.Value, area, PlaceColor * (.1f + .06f * pulse));
        Color edge = PlaceColor * .7f;
        Edge(batch, area.X, area.Y, area.Width, 1, edge);
        Edge(batch, area.X, area.Bottom - 1, area.Width, 1, edge);
        Edge(batch, area.X, area.Y, 1, area.Height, edge);
        Edge(batch, area.Right - 1, area.Y, 1, area.Height, edge);
        int type = placement.ItemType;
        if (type <= 0 || type >= TextureAssets.Item.Length) return;
        Main.instance.LoadItem(type);
        Texture2D icon = TextureAssets.Item[type].Value;
        Rectangle frame = Main.itemAnimations[type]?.GetFrame(icon) ?? icon.Frame();
        float scale = Math.Min(1f, Math.Min(area.Width, area.Height) / (float)Math.Max(frame.Width, frame.Height));
        batch.Draw(icon, area.Center.ToVector2(), frame, Color.White * (.45f + .2f * pulse), 0f, frame.Size() / 2f,
            scale, SpriteEffects.None, 0f);
    }

    private static Rectangle Cell(Point point)
    {
        Vector2 position = new Vector2(point.X * 16, point.Y * 16) - Main.screenPosition;
        return new Rectangle((int)position.X, (int)position.Y, 16, 16);
    }

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

    /// <summary>固定在屏幕下方的工作模式设置；临时标记不打开操作条。</summary>
    private static bool DrawSelectionBar()
    {
        if (Main.gameMenu || Main.myPlayer < 0 || Main.mapFullscreen || Main.inFancyUI
            || Main.drawingPlayerChat || Main.editSign || Main.editChest) return true;
        DollCommandPlayer commands = Main.LocalPlayer.GetModPlayer<DollCommandPlayer>();
        if (!commands.SettingsOpen) return true;
        DollCommand tool = commands.MarkMode;
        bool walls = DollCommands.TargetsWalls(tool);
        var layout = DollSelectionBarDrawing.GetLayout(PlayerInput.OriginalScreenSize / Main.UIScale);
        Vector2 pointer = new Vector2(PlayerInput.MouseX, PlayerInput.MouseY) / Main.UIScale;
        if (layout.Panel.Contains(pointer.ToPoint())) Main.LocalPlayer.mouseInterface = true;
        Texture2D icon = ModContent.Request<Texture2D>(DollCommands.IconPath(tool)).Value;
        var art = new DollSelectionBarDrawing.Art(TextureAssets.MagicPixel.Value,
            ModContent.Request<Texture2D>(StarsPath).Value, UIDrawKit.TitleFont,
            FontManager.HarmonyOS_Sans_SC?.Value ?? FontAssets.MouseText.Value);
        string keyName = DollCommandPlayer.MarkKeyName ?? LocalizeUI("DollMarkUnbound");
        DollSelectionBarDrawing.Draw(Main.spriteBatch, art, icon, layout, pointer, Main.UIScale,
            LocalizeUI("DollCommand" + DollCommands.BaseCommand(tool)), LocalizeUI("DollModeSettings"),
            Language.GetTextValue("Mods.伊蕾娜.UI.DollMarkKeyHint", keyName), LocalizeUI("DollSettingsDone"),
            LocalizeUI("DollBringBackItems"), commands.BringBackItems,
            LocalizeUI(walls ? "DollChainWalls" : "DollChainMining"), DollCommands.IsChain(tool),
            LocalizeUI("DollToggleOn"), LocalizeUI("DollToggleOff"));
        return true;
    }
}

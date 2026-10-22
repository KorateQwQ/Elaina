using System;
using Terraria.GameContent;
using Terraria.UI;
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
            if (commands.Tool is { } tool) DrawPreview(batch, commands, tool, view);
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
        bool walls = DollCommands.TargetsWalls(tool);
        HashSet<Point> marks = walls ? commands.WallMarks : commands.TileMarks;
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
            layers.Insert(index, new LegacyGameInterfaceLayer("伊蕾娜: Doll Command Hint", DrawHint, InterfaceScaleType.UI));
    }

    /// <summary>标记工具生效时在指针旁显示当前指令与操作提示，提醒右键可退出框选。</summary>
    private static bool DrawHint()
    {
        if (Main.myPlayer < 0 || Main.LocalPlayer.GetModPlayer<DollCommandPlayer>().Tool is not { } tool) return true;
        SpriteBatch batch = Main.spriteBatch;
        Vector2 at = Main.MouseScreen + new Vector2(20, 18);
        Texture2D icon = ModContent.Request<Texture2D>(DollCommands.IconPath(tool)).Value;
        batch.Draw(icon, at, null, Color.White, 0, Vector2.Zero, 28f / Math.Max(icon.Width, icon.Height), SpriteEffects.None, 0);
        Vector2 text = at + new Vector2(32, 0);
        Utils.DrawBorderString(batch, LocalizeUI("DollCommand" + tool), text, DollCommands.TargetsWalls(tool) ? WallColor : TileColor, .85f);
        string hint = LocalizeUI(DollCommands.IsChain(tool) ? "DollMarkHintChain" : "DollMarkHintRect");
        Utils.DrawBorderString(batch, hint, text + new Vector2(0, 20), new Color(236, 225, 247), .75f);
        return true;
    }
}

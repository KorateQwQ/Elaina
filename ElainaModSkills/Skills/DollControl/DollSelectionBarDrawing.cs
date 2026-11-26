using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using 伊蕾娜.ElainaModSkills.UICore;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 工作模式设置条的共享布局与绘制：细线框、四角折线、金色完成按钮和选项开关。
/// 输入和绘制均使用 UI 逻辑坐标；只依赖传入的贴图和字体，离线预览直接链接本文件。
/// </summary>
internal static class DollSelectionBarDrawing
{
    // 与 AlchemyDrawing 同一组颜色。
    private static readonly Color Ink = new(238, 233, 245), Muted = new(178, 168, 195), Lavender = new(202, 178, 237),
        Gold = new(230, 206, 165), LineColor = new(76, 62, 88), GoldFill = new(185, 155, 87),
        Top = new(31, 26, 43), Bottom = new(22, 19, 31);
    private const int RowHeight = 50, Height = 72;
    // 游戏的 MagicPixel 是 1×1000 贴图，按比例缩放时必须只取一个像素。
    private static readonly Rectangle Unit = new(0, 0, 1, 1);

    internal readonly record struct Layout(Rectangle Panel, Rectangle Start,
        Rectangle BringBack = default, Rectangle Chain = default);

    /// <summary>Stars 复用技能面板的多尺寸预制四芒星。</summary>
    internal readonly record struct Art(Texture2D Pixel, UIRasterStar Stars, UIRasterFont Serif, UIRasterFont Sans, UIRasterShapes Shapes);

    internal static Layout GetLayout(Vector2 viewport)
    {
        int width = Math.Clamp((int)viewport.X - 24, 320, 432);
        int height = Height + 30;
        int x = (int)(viewport.X - width) / 2;
        int y = Math.Max(12, (int)viewport.Y - 50 - height);
        int startWidth = 112;
        var start = new Rectangle(x + width - 12 - startWidth, y + 11, startWidth, 28);
        return new(new(x, y, width, height), start,
            new(x + 16 + (width - 32) / 2, y + 49, (width - 32) / 2, 24),
            new(x + 12, y + 49, (width - 32) / 2, 24));
    }

    /// <summary>工作模式设置，选项立即保存；完成按钮仅关闭面板。</summary>
    internal static void Draw(SpriteBatch batch, Art art, Texture2D icon, Layout layout, Vector2 pointer, float uiScale,
        string title, string subtitle, string hint, string done, string bringBackLabel, bool bringBackItems,
        string chainLabel, bool chain, string onLabel, string offLabel)
    {
        Rectangle panel = layout.Panel;
        Texture2D pixel = art.Pixel;
        bool enabled = true;

        // 底板：纵向渐变 + 1px 线框 + 四角折线。
        for (int i = 0; i < panel.Height; i++)
            Box(panel.X, panel.Y + i, panel.Width, 1, Color.Lerp(Top, Bottom, i / (float)(panel.Height - 1)) * .95f);
        Frame(panel.X, panel.Y, panel.Width, panel.Height, LineColor);
        Corners(panel.X + 3, panel.Y + 3, panel.Width - 6, panel.Height - 6, Lavender * .45f);

        // 工具图标外的细圆圈，同炼金手记标题图标。
        float rowCenter = panel.Y + RowHeight / 2f;
        Vector2 medal = new(panel.X + 28, rowCenter);
        Disc(medal, 16, Lavender * .05f);
        Arc(medal, 16, 0, 360, Lavender * .32f);
        float iconScale = 22f / Math.Max(icon.Width, icon.Height);
        batch.Draw(icon, medal, null, Color.White, 0, new Vector2(icon.Width, icon.Height) / 2, iconScale, SpriteEffects.None, 0);

        float textX = panel.X + 54, textWidth = layout.Start.X - 10 - textX;
        Fitted(art.Serif, title, textX, rowCenter - 8, 15, textWidth, Ink, 0);
        Fitted(art.Sans, subtitle, textX, rowCenter + 9, 10, textWidth, Muted, 0);

        // 完成按钮：金色胶囊，两端四芒星。
        Rectangle startRect = layout.Start;
        bool startHover = enabled && Contains(startRect);
        float alpha = enabled ? 1 : .4f;
        RoundRect(startRect, GoldFill * ((startHover ? .2f : .1f) * alpha), Gold * ((startHover ? .95f : .6f) * alpha));
        float sparkY = startRect.Y + startRect.Height / 2f;
        Spark(new(startRect.X + 13.5f, sparkY), 9, Gold * (.55f * alpha));
        Spark(new(startRect.Right - 13.5f, sparkY), 9, Gold * (.55f * alpha));
        Fitted(art.Serif, done, startRect.Center.X, sparkY, 13, startRect.Width - 52, Gold * alpha, .5f, 1);

        // 虚线分隔与一行操作说明。
        if (bringBackLabel != null && layout.BringBack.Width > 0) Toggle(layout.BringBack, bringBackLabel, bringBackItems);
        if (chainLabel != null && layout.Chain.Width > 0) Toggle(layout.Chain, chainLabel, chain);
        float ruleY = panel.Bottom - (Height - RowHeight);
        for (int x = panel.X + 12; x < panel.Right - 12; x += 4) Box(x, ruleY, Math.Min(2, panel.Right - 12 - x), 1, Ink * .14f);
        Fitted(art.Sans, hint, panel.Center.X, ruleY + (Height - RowHeight) / 2f + .5f,
            10, panel.Width - 24, Muted * .85f, .5f);

        void Toggle(Rectangle rect, string label, bool active)
        {
            bool hover = Contains(rect);
            Color accent = active ? Gold : Lavender;
            RoundRect(rect, accent * (active ? hover ? .20f : .12f : hover ? .09f : .025f),
                accent * (active ? .7f : hover ? .45f : .25f));
            string state = active ? onLabel : offLabel;
            float stateWidth = Measure(art.Sans, state, 9);
            Fitted(art.Sans, label, rect.X + 12, rect.Center.Y, 11, rect.Width - stateWidth - 34,
                active ? Gold : hover ? Ink : Muted, 0);
            Text(art.Sans, state, rect.Right - 12, rect.Center.Y, 9, active ? Gold : Muted * .7f, 1);
        }

        bool Contains(Rectangle rect) => rect.Contains(new Point((int)pointer.X, (int)pointer.Y));
        void Box(float x, float y, float width, float height, Color color)
            => batch.Draw(pixel, new Vector2(x, y), Unit, color, 0, Vector2.Zero, new Vector2(width, height), SpriteEffects.None, 0);
        void Frame(float x, float y, float width, float height, Color color)
        {
            Box(x, y, width, 1, color); Box(x, y + height - 1, width, 1, color);
            Box(x, y + 1, 1, height - 2, color); Box(x + width - 1, y + 1, 1, height - 2, color);
        }
        void Corners(float x, float y, float width, float height, Color color)
        {
            const float arm = 9;
            Box(x, y, arm, 1, color); Box(x, y + 1, 1, arm - 1, color);
            Box(x + width - arm, y, arm, 1, color); Box(x + width - 1, y + 1, 1, arm - 1, color);
            Box(x, y + height - 1, arm, 1, color); Box(x, y + height - arm, 1, arm - 1, color);
            Box(x + width - arm, y + height - 1, arm, 1, color); Box(x + width - 1, y + height - arm, 1, arm - 1, color);
        }
        void Arc(Vector2 center, float radius, float from, float sweep, Color color)
            => art.Shapes.Arc(batch, center, radius, from, sweep, 1, color);
        void Disc(Vector2 center, float radius, Color color) => art.Shapes.Disc(batch, center, radius, color);
        void RoundRect(Rectangle rect, Color fill, Color border)
            => art.Shapes.RoundRect(batch, new Vector2(rect.X, rect.Y), new Vector2(rect.Width, rect.Height), rect.Height / 2f, fill, border);
        void Spark(Vector2 center, float size, Color color)
            => art.Stars.Draw(batch, center, size, color);
        float Measure(UIRasterFont font, string text, float size)
            => string.IsNullOrEmpty(text) ? 0 : font.Measure(text, UIRasterText.Pixels(batch, size)) / uiScale;
        void Text(UIRasterFont font, string text, float x, float centerY, float size, Color color, float align, float spacing = 0)
            => UIRasterText.Draw(batch, font, text, new Vector2(x, centerY), color,
                UIRasterText.Pixels(batch, size), new Vector2(align, .5f), spacing: spacing);
        void Fitted(UIRasterFont font, string text, float x, float centerY, float size, float maxWidth, Color color,
            float align, float spacing = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            float pixels = UIRasterText.Pixels(batch, size);
            text = UIRasterText.Fit(font, text, (maxWidth - Math.Max(0, text.Length - 1) * spacing) * uiScale, ref pixels, Math.Min(pixels, 10));
            UIRasterText.Draw(batch, font, text, new Vector2(x, centerY), color, pixels, new Vector2(align, .5f), spacing: spacing);
        }
    }
}

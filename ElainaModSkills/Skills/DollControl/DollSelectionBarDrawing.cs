using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 选区操作条的共享布局与绘制，沿用炼金手记的视觉语言：细线框 + 四角折线、金色「炼制」式主按钮、淡描边次按钮。
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

    internal readonly record struct Layout(Rectangle Panel, Rectangle Start, Rectangle Exit,
        Rectangle BringBack = default, Rectangle Chain = default);

    /// <summary>Stars 为炼金手记的 NotebookStars 图集（预乘 alpha）。</summary>
    internal readonly record struct Art(Texture2D Pixel, Texture2D Stars, DynamicSpriteFont Serif, DynamicSpriteFont Sans);

    internal static Layout GetLayout(Vector2 viewport)
    {
        int width = Math.Clamp((int)viewport.X - 24, 320, 432);
        int height = Height + 30;
        int x = (int)(viewport.X - width) / 2;
        int y = Math.Max(12, (int)viewport.Y - 50 - height);
        int startWidth = width >= 400 ? 148 : 124, exitWidth = 64;
        var start = new Rectangle(x + width - 12 - startWidth, y + 11, startWidth, 28);
        return new(new(x, y, width, height), start, new(start.X - 8 - exitWidth, y + 12, exitWidth, 26),
            new(x + 16 + (width - 32) / 2, y + 49, (width - 32) / 2, 24),
            new(x + 12, y + 49, (width - 32) / 2, 24));
    }

    /// <param name="countFormat">含 {0} 的数量文案；数字单独以衬线大字绘制。</param>
    /// <param name="uiScale">UI 缩放，用于挑选四芒星图集的预过滤层级。</param>
    internal static void Draw(SpriteBatch batch, Art art, Texture2D icon, Layout layout, Vector2 pointer, float uiScale,
        string title, string countFormat, int count, string empty, string hint, string start, string exit,
        bool canStart = true, string bringBackLabel = null, bool bringBackItems = true,
        string chainLabel = null, bool chain = false, string onLabel = null, string offLabel = null, string warning = null)
    {
        Rectangle panel = layout.Panel;
        Texture2D pixel = art.Pixel;
        bool enabled = count > 0 && canStart && warning == null;

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

        // 标题与数量：标签用无衬线 Muted 小字，数字用衬线 Ink。
        float textX = panel.X + 54, textWidth = layout.Exit.X - 10 - textX;
        Fitted(art.Serif, title, textX, rowCenter - 8, 15, textWidth, Ink, 0);
        if (count > 0)
        {
            int split = countFormat.IndexOf("{0}", StringComparison.Ordinal);
            string pre = split < 0 ? countFormat.Trim() : countFormat[..split].Trim();
            string post = split < 0 ? "" : countFormat[(split + 3)..].Trim();
            string number = count.ToString();
            const float label = 10, digits = 14, gap = 3;
            float preWidth = Measure(art.Sans, pre, label), numberWidth = Measure(art.Serif, number, digits);
            float postWidth = Measure(art.Sans, post, label);
            float total = preWidth + numberWidth + postWidth + (pre.Length > 0 ? gap : 0) + (post.Length > 0 ? gap : 0);
            float fit = Math.Min(1, textWidth / Math.Max(1, total)), x = textX, y = rowCenter + 9;
            Text(art.Sans, pre, x, y + .5f, label * fit, Muted, 0);
            if (pre.Length > 0) x += (preWidth + gap) * fit;
            Text(art.Serif, number, x, y, digits * fit, Ink, 0);
            Text(art.Sans, post, x + (numberWidth + gap) * fit, y + .5f, label * fit, Muted, 0);
        }
        else Fitted(art.Sans, empty, textX, rowCenter + 9, 10, textWidth, Muted * .6f, 0);

        // 次按钮「退出」：淡描边胶囊。
        Rectangle exitRect = layout.Exit;
        bool exitHover = Contains(exitRect);
        RoundRect(exitRect, exitHover ? Ink * .05f : Color.Transparent, Ink * (exitHover ? .3f : .18f));
        Fitted(art.Sans, exit, exitRect.Center.X, exitRect.Y + exitRect.Height / 2f, 11, exitRect.Width - 16,
            exitHover ? Ink : Muted, .5f);

        // 主按钮：与「炼制」相同的金色胶囊，两端四芒星；禁用时整体 40%。
        Rectangle startRect = layout.Start;
        bool startHover = enabled && Contains(startRect);
        float alpha = enabled ? 1 : .4f;
        RoundRect(startRect, GoldFill * ((startHover ? .2f : .1f) * alpha), Gold * ((startHover ? .95f : .6f) * alpha));
        float sparkY = startRect.Y + startRect.Height / 2f;
        Spark(new(startRect.X + 13.5f, sparkY), 9, Gold * (.55f * alpha));
        Spark(new(startRect.Right - 13.5f, sparkY), 9, Gold * (.55f * alpha));
        Fitted(art.Serif, start, startRect.Center.X, sparkY, 13, startRect.Width - 52, Gold * alpha, .5f, 1);

        // 虚线分隔与一行操作说明。
        if (bringBackLabel != null && layout.BringBack.Width > 0) Toggle(layout.BringBack, bringBackLabel, bringBackItems);
        if (chainLabel != null && layout.Chain.Width > 0) Toggle(layout.Chain, chainLabel, chain);
        float ruleY = panel.Bottom - (Height - RowHeight);
        for (int x = panel.X + 12; x < panel.Right - 12; x += 4) Box(x, ruleY, Math.Min(2, panel.Right - 12 - x), 1, Ink * .14f);
        Fitted(art.Sans, warning ?? hint, panel.Center.X, ruleY + (Height - RowHeight) / 2f + .5f,
            10, panel.Width - 24, warning != null ? new Color(240, 165, 176) : Muted * .85f, .5f);

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
        void Line(Vector2 a, Vector2 b, Color color)
        {
            Vector2 delta = b - a;
            if (delta.LengthSquared() < .001f) return;
            batch.Draw(pixel, a, Unit, color, MathF.Atan2(delta.Y, delta.X), new Vector2(0, .5f),
                new Vector2(delta.Length(), 1), SpriteEffects.None, 0);
        }
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
        {
            int segments = Math.Max(2, (int)MathF.Ceiling(sweep / 360 * 96));
            Vector2 Point(int i)
            {
                float angle = MathHelper.ToRadians(from + sweep * i / segments);
                return center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            }
            for (int i = 0; i < segments; i++) Line(Point(i), Point(i + 1), color);
        }
        // 以 1px 竖条拼出圆角，与 AlchemyDrawing.RoundRect 的填充方式一致。
        void Strips(float x, float y, float width, float height, float radius, Color color)
        {
            int columns = Math.Max(1, (int)MathF.Ceiling(width));
            float step = width / columns;
            for (int i = 0; i < columns; i++)
            {
                float mid = x + (i + .5f) * step, edge = Math.Min(mid - x, x + width - mid), inset = 0;
                if (edge < radius) inset = radius - MathF.Sqrt(Math.Max(0, radius * radius - (radius - edge) * (radius - edge)));
                Box(x + i * step, y + inset, step, height - inset * 2, color);
            }
        }
        void Disc(Vector2 center, float radius, Color color) => Strips(center.X - radius, center.Y - radius, radius * 2, radius * 2, radius, color);
        void RoundRect(Rectangle rect, Color fill, Color border)
        {
            float x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height, r = h / 2f;
            if (fill.A > 0) Strips(x, y, w, h, r, fill);
            Line(new(x + r, y), new(x + w - r, y), border);
            Line(new(x + r, y + h), new(x + w - r, y + h), border);
            Arc(new(x + r, y + r), r, 90, 180, border);
            Arc(new(x + w - r, y + r), r, 270, 180, border);
        }
        // NotebookStars 第一行为正方四芒星，按 64/32/16/8/4 预过滤；长轴占格子 112/128。
        void Spark(Vector2 center, float size, Color color)
        {
            int side = 64, x = 0;
            float pixels = size * 128 / 112 * uiScale;
            while (side > 4 && pixels < side * .7071f) { x += side; side /= 2; }
            batch.Draw(art.Stars, center, new Rectangle(x, 0, side, side), color, 0, new Vector2(side / 2f),
                size * 128f / 112 / side, SpriteEffects.None, 0);
        }
        // 字号按「国」字宽归一，与 AlchemyDrawing 相同；文字以行中线定位。
        float Measure(DynamicSpriteFont font, string text, float size)
            => string.IsNullOrEmpty(text) ? 0 : font.MeasureString(text).X * Factor(font, size);
        float Factor(DynamicSpriteFont font, float size) => size / Math.Max(1f, font.MeasureString("国").X);
        void Text(DynamicSpriteFont font, string text, float x, float centerY, float size, Color color, float align, float spacing = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            float factor = Factor(font, size), lineHeight = font.MeasureString("国").Y * factor;
            float width = Measure(font, text, size) + Math.Max(0, text.Length - 1) * spacing;
            var at = new Vector2(x - width * align, centerY - lineHeight / 2);
            if (spacing == 0)
            {
                batch.DrawString(font, text, at, color, 0, Vector2.Zero, factor, SpriteEffects.None, 0);
                return;
            }
            foreach (char c in text)
            {
                string glyph = c.ToString();
                batch.DrawString(font, glyph, at, color, 0, Vector2.Zero, factor, SpriteEffects.None, 0);
                at.X += font.MeasureString(glyph).X * factor + spacing;
            }
        }
        void Fitted(DynamicSpriteFont font, string text, float x, float centerY, float size, float maxWidth, Color color,
            float align, float spacing = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            float width = Measure(font, text, size) + Math.Max(0, text.Length - 1) * spacing;
            float fit = width > maxWidth ? maxWidth / width : 1;
            Text(font, text, x, centerY, size * fit, color, align, spacing * fit);
        }
    }
}

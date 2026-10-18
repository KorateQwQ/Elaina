using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModAlchemy.UI;

// Page geometry follows the installed skill notebook. No runtime texture generation.
internal sealed partial class AlchemyDrawing
{
    internal static readonly Color DotColor = new Color(236, 229, 247) * .32f;
    private static Vector2 Polar(float radius, float degrees)
        => new(radius * MathF.Cos(MathHelper.ToRadians(degrees)), radius * MathF.Sin(MathHelper.ToRadians(degrees)));
    internal void Dot(Vector2 center, float radius, Color color)
        => Stretch(AlchemyNotebookPresentation.AssetRoot + "NotebookDot", center.X - radius, center.Y - radius, radius * 2, radius * 2, color);
    internal void Dots(float x1, float y1, float x2, float y2, Color color)
    {
        Vector2 a = new(x1, y1), b = new(x2, y2);
        float length = Vector2.Distance(a, b);
        for (float p = 0; p <= length; p += 4.5f)
            Dot(Vector2.Lerp(a, b, length > 0 ? p / length : 0), .7f, color);
    }
    internal void DotSeparator(float x, float top, float bottom)
    {
        Dots(x, top + 6, x, bottom - 6, DotColor);
        Dot(new(x, top + 2), 2, DotColor * 1.5f);
        Dot(new(x, bottom - 2), 2, DotColor * 1.5f);
    }
    // NotebookStars rows (bake_redesign.py): aspect (width / height) and waist of each baked shape,
    // copied from the skill atlas with the HTML crossStar geometry.
    private static readonly (float Aspect, float Waist)[] StarRows = [(1, .2f), (2.833f, .2f), (.778f, .25f), (.976f, .28f)];
    internal void Star(Vector2 center, Vector2 size, Color color, float waist = .2f)
    {
        // Pick the baked shape closest in aspect and waist: stretching one square star to a
        // slim flash blurred it along the squashed axis (the tiles have no mipmaps).
        float aspect = size.X / Math.Max(.001f, size.Y);
        int row = 0;
        float best = float.MaxValue;
        for (int i = 0; i < StarRows.Length; i++)
        {
            float cost = Math.Abs(MathF.Log(aspect / StarRows[i].Aspect)) * 4 + Math.Abs(waist - StarRows[i].Waist) * 10;
            if (cost < best) { best = cost; row = i; }
        }
        // Prefiltered levels 64, 32, 16, 8, 4; premultiplied alpha. The longer axis spans 112/128 of a tile.
        int side = 64, x = 0;
        float extent = Math.Max(size.X, size.Y) * 128 / 112;
        var transform = BatchTransform(Batch);
        float pixels = extent * Scale * MathF.Sqrt(transform.M11 * transform.M11 + transform.M12 * transform.M12);
        while (side > 4 && pixels < side * .7071f) { x += side; side /= 2; }
        float rowAspect = StarRows[row].Aspect;
        Vector2 baked = rowAspect >= 1 ? new(1, 1 / rowAspect) : new(rowAspect, 1);
        Batch.Draw(Texture(AlchemyNotebookPresentation.AssetRoot + "NotebookStars"), At(center.X, center.Y),
            new Rectangle(x, row * 64, side, side), color, 0, new Vector2(side / 2f),
            size / baked * (128f / 112 / side * Scale), SpriteEffects.None, 0);
    }
    internal void Spark(Vector2 center, float size, Color color) => Star(center, new(size), color);
    internal void Trio(Vector2 center)
    {
        Spark(center, 13.33f, Ink);
        Spark(center - new Vector2(14.33f, 0), 4.67f, Ink);
        Spark(center + new Vector2(14.33f, 0), 4.67f, Ink);
    }
    internal void RoundRect(float x, float y, float width, float height, float radius, Color fill,
        Color border = default, float borderWidth = 0)
    {
        float r = Math.Min(radius, Math.Min(width, height) / 2);
        if (fill.A > 0) Strips(x, y, width, height, r, r, _ => fill);
        if (borderWidth <= 0) return;
        Line(new(x+r,y),new(x+width-r,y),border,borderWidth);
        Line(new(x+r,y+height),new(x+width-r,y+height),border,borderWidth);
        Line(new(x,y+r),new(x,y+height-r),border,borderWidth);
        Line(new(x+width,y+r),new(x+width,y+height-r),border,borderWidth);
        Arc(new(x+r,y+r),r,180,90,border,borderWidth);
        Arc(new(x+width-r,y+r),r,270,90,border,borderWidth);
        Arc(new(x+width-r,y+height-r),r,0,90,border,borderWidth);
        Arc(new(x+r,y+height-r),r,90,90,border,borderWidth);
    }
    private void Shadow(float x, float y, float w, float h, float radius, Color color, float blur)
        => Glow(x-blur, y-blur, w+blur*2, h+blur*2, color);

    // Cover the catalog edge with the same page pixels, fading only the entries.
    // This avoids an additional render target and leaves native SUI clipping intact.
    internal void CatalogFade()
    {
        const int x = AlchemyNotebookPresentation.CatalogX, y = AlchemyNotebookPresentation.CatalogY;
        const int w = AlchemyNotebookPresentation.CatalogWidth - 8, h = AlchemyNotebookPresentation.CatalogHeight;
        Stretch(AlchemyNotebookPresentation.AssetRoot + "CatalogFadeTop", x, y, w, 16, Color.White);
        // Native scissor bounds round outwards; extend past the logical edge so a
        // fractional UI scale cannot expose a one-pixel strip of the clipped cards.
        Stretch(AlchemyNotebookPresentation.AssetRoot + "CatalogFadeBottom", x, y+h-26, w, 27, Color.White);
    }
    internal void DetailFade() => Stretch(AlchemyNotebookPresentation.AssetRoot + "DetailFadeBottom", 790, 571, 270, 23, Color.White);
    private void Strips(float x, float y, float width, float height, float leftRadius, float rightRadius, Func<float, Color> color)
    {
        int count = Math.Max(1, (int)MathF.Ceiling(width));
        float step = width / count;
        for (int i = 0; i < count; i++)
        {
            float mid = x + (i + .5f) * step, inset = 0;
            float Round(float radius, float distance)
                => radius - MathF.Sqrt(Math.Max(0, radius * radius - (radius - distance) * (radius - distance)));
            if (mid - x < leftRadius) inset = Round(leftRadius, mid - x);
            else if (x + width - mid < rightRadius) inset = Round(rightRadius, x + width - mid);
            Box(x + i * step, y + inset, step, height - inset * 2, color(mid));
        }
    }

    private static Color Stops(float t, params (float At, Color Color)[] stops)
    {
        t = Math.Clamp(t, 0, 1);
        for (int i = 1; i < stops.Length; i++)
            if (t <= stops[i].At)
                return Color.Lerp(stops[i - 1].Color, stops[i].Color, (t - stops[i - 1].At) / Math.Max(.0001f, stops[i].At - stops[i - 1].At));
        return stops[^1].Color;
    }

    internal void NotebookPage(float width, float height)
    {
        const float block = 10;
        Shadow(-6, 18, width + 22, height + 10, 8, Color.Black * .6f, 16);
        float diagonal = width * width + height * height;
        Strips(-11, -5, width + block + 15, height + block + 9, 5, 5, x => Color.Lerp(new Color(29, 25, 39), new Color(16, 14, 23),
            Math.Clamp((x * width + height / 2 * height) / diagonal, 0, 1)));
        Strips(-11, -5, 11, height + block + 9, 5, 0, x => Stops((x + 11) / 11,
            (0, new Color(14, 12, 20)), (.45f, new Color(50, 42, 66)), (1, new Color(19, 16, 27))));
        RoundRect(-11, -5, width + block + 15, height + block + 9, 5, Color.Transparent, Lavender * .14f, 1);
        // Ribbon, hanging from between the leaves with a swallowtail end.
        for (float x = 962; x < 974; x += .5f)
        {
            float notch = 6 * (1 - Math.Abs(x + .25f - 968) / 6);
            Box(x, height - 8, .5f, 40 - notch, Stops((x + .25f - 962) / 12,
                (0, new Color(79, 63, 102)), (.45f, new Color(110, 90, 142)), (1, new Color(74, 59, 96))));
        }
        Line(new Vector2(965.5f, height - 8), new Vector2(965.5f, height + 28), new Color(236, 220, 255) * .16f, .8f);
        Star(new Vector2(968, height + 20), new Vector2(5.5f * .976f, 5.5f), Gold * .75f, .28f);
        // Page block: lighter right face, darker bottom face.
        for (float k = 0; k < block; k += .5f)
        {
            Box(width + k, k, .5f, height + .5f, Stops((k + .25f) / block,
                (0, new Color(66, 57, 86)), (.6f, new Color(92, 81, 114)), (1, new Color(84, 74, 105))));
            Box(0, height + k, width + k, .5f, Stops((k + .25f) / block,
                (0, new Color(48, 41, 63)), (.6f, new Color(66, 57, 85)), (1, new Color(60, 52, 78))));
        }
        foreach (var (k, a) in new[] { (1.3f, .34f), (2.4f, .16f), (3.2f, .28f), (4.6f, .14f), (5.5f, .3f), (6.9f, .18f), (7.8f, .26f), (9f, .2f) })
        {
            Color seam = new Color(18, 14, 27) * a;
            Line(new Vector2(width + k, k), new Vector2(width + k, height + k), seam, .6f);
            Line(new Vector2(width + k, height + k), new Vector2(10, height + k), seam, .6f);
            Line(new Vector2(10, height + k), new Vector2(0, height + k * 1.08f), seam, .6f);
        }
        Gradient(0, height, 18, block, new Color(10, 9, 17) * .55f, Color.Transparent);
        Color edge = Lavender * .4f;
        Line(new Vector2(width, 0), new Vector2(width + block, block), edge);
        Line(new Vector2(width + block, block), new Vector2(width + block, height + block - 2), edge);
        Line(new Vector2(width + block, height + block - 2), new Vector2(width + block - 2, height + block), edge);
        Line(new Vector2(width + block - 2, height + block), new Vector2(0, height + block), edge);
        Box(0, height, width, 2.5f, new Color(10, 8, 16) * .35f);
        Box(width, 1.25f, 2.5f, height + 1.25f, new Color(10, 8, 16) * .35f);

        Stretch(SurfacePath, 0, 0, width, height, Color.White);
        Frame(.5f, .5f, width - 1, height - 1, Lavender * .34f);
        Gradient(0, 0, 14, height, new Color(10, 9, 17) * .55f, Color.Transparent);
        // Dotted inner frame: corners give way to star ornaments, a small star on each side.
        float l = 12.5f, r = width - 12.5f, t = 12.5f, b = height - 12.5f, mid = height / 2;
        const float g = 16;
        Dots(l + g, t, r - g, t, DotColor); Dots(l + g, b, r - g, b, DotColor);
        Dots(l, t + g, l, mid - 10, DotColor); Dots(l, mid + 10, l, b - g, DotColor);
        Dots(r, t + g, r, mid - 10, DotColor); Dots(r, mid + 10, r, b - g, DotColor);
        Star(new Vector2(l, mid), new Vector2(6.224f, 8), Lavender * .85f, .25f);
        Star(new Vector2(r, mid), new Vector2(6.224f, 8), Lavender * .85f, .25f);
        StarCorner(new Vector2(l, t), 1, 1); StarCorner(new Vector2(r, t), -1, 1);
        StarCorner(new Vector2(r, b), -1, -1); StarCorner(new Vector2(l, b), 1, -1);
    }

    private void StarCorner(Vector2 corner, int sx, int sy)
    {
        Glow( corner.X - 9, corner.Y - 9, 18, 18, new Color(214, 186, 240) * .32f);
        float from = (sx, sy) switch { (1, 1) => 0, (-1, 1) => 90, (-1, -1) => 180, _ => 270 };
        Arc(corner, 14, from, 90, Lavender * .42f);
        Star(corner, new Vector2(11 * .976f, 11), new Color(236, 226, 247), .28f);
        Dot(corner + new Vector2(sx, sy) * 15.5f, 1, Lavender * .55f);
    }

    internal void Arc(Vector2 center, float radius, float from, float sweep, Color color, float width = 1)
    {
        if (sweep <= 0 || radius <= 0) return;
        int segments = Math.Max(2, (int)MathF.Ceiling(sweep / 360 * 160));
        Vector2 previous = center + Polar(radius, from);
        for (int i = 1; i <= segments; i++)
        {
            Vector2 next = center + Polar(radius, from + sweep * i / segments);
            Line(previous, next, color, width);
            previous = next;
        }
    }
}

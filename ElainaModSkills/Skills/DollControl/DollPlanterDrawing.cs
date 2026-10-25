using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>与离线预览共享的两层占位标记：虚线生长空间、绿色种植盆。</summary>
internal static class DollPlanterDrawing
{
    internal static void Draw(SpriteBatch batch, Texture2D pixel, Vector2 pot, bool invalid, float opacity)
    {
        Color grow = invalid ? new(240, 120, 130) : new(160, 210, 240);
        Color plant = invalid ? grow : new(150, 235, 160);
        Box(0, -16, 16, 16, grow * (.06f * opacity));
        for (int x = 0; x < 16; x += 4) Box(x, -16, 2, 1, grow * opacity);
        Box(0, -16, 1, 16, grow * (.5f * opacity));
        Box(15, -16, 1, 16, grow * (.5f * opacity));
        Box(0, 0, 16, 16, plant * (.22f * opacity));
        Box(3, 5, 10, 3, plant * (.8f * opacity));
        Box(5, 8, 6, 6, plant * (.7f * opacity));
        void Box(int x, int y, int w, int h, Color color) => batch.Draw(pixel,
            new Rectangle((int)pot.X + x, (int)pot.Y + y, w, h), new Rectangle(0, 0, 1, 1), color);
    }
}

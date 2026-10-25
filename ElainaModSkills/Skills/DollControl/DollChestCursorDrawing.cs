using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>Positioned relative to the actual vanilla hint frame, including key/animated/mod item icons.</summary>
internal static class DollChestCursorDrawing
{
    internal static void Draw(SpriteBatch batch, Texture2D bear, Vector2 iconPosition, Vector2 frameSize, float cursorScale)
    {
        // Small overlap ties the badge to the icon while keeping the chest/key recognizable.
        Vector2 position = iconPosition + (frameSize - new Vector2(8, 7)) * cursorScale;
        batch.Draw(bear, position, null, Color.White, 0, Vector2.Zero, cursorScale, SpriteEffects.None, 0);
    }
}

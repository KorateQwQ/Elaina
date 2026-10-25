using Microsoft.Xna.Framework;
using 伊蕾娜.ElainaModAlchemy.Gameplay;

namespace 伊蕾娜.ElainaModAlchemy.UI;

/// <summary>Sprite layout shared by the player layer and the FNA frame-strip preview.</summary>
internal static class AlchemyBrewAnimation
{
    internal const int FrameWidth = 40, FrameHeight = 56;
    // The legacy pose used a 7px lift. Its stick tip (row 35) must reach
    // the cauldron rim, 28px above the floor: -56 - 7 + 35 == -28.
    // Compress the 28x28 stool vertically to support that pose.
    internal const int StoolWidth = 28, StoolHeight = 7;
    internal static readonly Vector2 StoolScale = new(1, StoolHeight / 28f);
    internal static string TexturePath(bool stirring) => stirring ? "炼金/搅拌动作" : "炼金/翻书";
    internal static Rectangle Frame(bool stirring, int frame) => new(0,
        frame % (stirring ? AlchemyBrewSession.StirringFrames : AlchemyBrewSession.ReadingFrames) * 56, 40, 56);
    internal static Vector2 Ground(Vector2 drawPosition, int playerWidth, int playerHeight, float heightOffsetVisual)
        => new((int)(drawPosition.X + playerWidth / 2f), (int)(drawPosition.Y + playerHeight + heightOffsetVisual));
    internal static Vector2 BodyPosition(Vector2 ground, bool stirring)
        => ground + new Vector2(-FrameWidth / 2, -FrameHeight - (stirring ? StoolHeight : 0));
    internal static Vector2 StoolPosition(Vector2 ground) => ground + new Vector2(-StoolWidth / 2, -StoolHeight);
    internal static int HeadSway(bool stirring, int frame) => !stirring ? 0 : frame switch
    {
        >= 5 and <= 9 => frame - 4,
        >= 10 and <= 12 => 13 - frame,
        _ => 0
    };
    internal static Vector2 HatPosition(Vector2 ground, bool stirring, int frame)
        => BodyPosition(ground, stirring) + new Vector2(HeadSway(stirring, frame), 0);
    internal static Vector2 PendantPosition(Vector2 ground, bool stirring, int frame)
        => BodyPosition(ground, stirring) + new Vector2(20 + HeadSway(stirring, frame), 31);
}

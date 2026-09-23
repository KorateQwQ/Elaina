using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModAlchemy.UI;

// The alchemy cover uses the skill notebook's shared motion and page curl.
internal static class AlchemyBookArt
{
    internal const string CoverPath = "伊蕾娜/ElainaModAlchemy/UI/Assets/AlchemyCoverFront";
    internal const string BackPath = "伊蕾娜/ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/BookCoverBack";

    internal static void PaintCover(AlchemyDrawing draw)
    {
        draw.Image(CoverPath, 0, 0, 1100, 800);
        draw.Text("炼金手记", 550, 512, 29, new Color(226, 208, 233), .5f, spacing: 7, serif: true);
    }
}

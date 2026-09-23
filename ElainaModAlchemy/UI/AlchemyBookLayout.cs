using System;
using Microsoft.Xna.Framework;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

namespace 伊蕾娜.ElainaModAlchemy.UI;

// A motion origin near the existing skill bar, without a visible or clickable dock.
internal static class AlchemyBookLayout
{
    internal static Rectangle DockBounds()
    {
        var skillDock = ConstellationBookButton.DockBounds();
        return new Rectangle(Math.Max(8, skillDock.X - 104), skillDock.Y, 96, 30);
    }
}

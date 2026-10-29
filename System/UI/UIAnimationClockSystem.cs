using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

namespace 伊蕾娜.System.UI;

// A hidden SUI tree is neither updated nor drawn. Observe the client lifecycle
// outside that tree so its first visible sample excludes the hidden interval.
[Autoload(Side = ModSide.Client)]
public sealed class UIAnimationClockSystem : ModSystem
{
    private (bool Focus, bool Hidden, bool Menu) _previous;

    public override void UpdateUI(GameTime gameTime)
    {
        var current = (Main.hasFocus, Main.hideUI, Main.gameMenu);
        if (current == _previous) return;
        _previous = current;
        ConstellationUIClock.InvalidateSamples();
    }

    public override void OnWorldUnload() => ConstellationUIClock.InvalidateSamples();
}

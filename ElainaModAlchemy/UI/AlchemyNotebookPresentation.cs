using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModAlchemy.UI;

/// <summary>Measured presentation rows shared by native SUI and the FNA preview.</summary>
/// <remarks>Paint receives the pointer in row-local design units, or null when the row is not hovered.
/// A row holding several targets (e.g. wrapped chips) resolves the clicked one through EntryAt.</remarks>
internal sealed record AlchemyNotebookRow(float Height, Action<AlchemyDrawing, Vector2?> Paint, string Entry = null,
    Func<Vector2, string> EntryAt = null, bool BlocksDragging = false)
{
    internal string EntryFor(Vector2 local) => EntryAt != null ? EntryAt(local) : Entry;
}

internal static partial class AlchemyNotebookPresentation
{
    internal static readonly Color Ink = new(238, 233, 245), Muted = new(178, 168, 195),
        Lavender = new(202, 178, 237), Gold = new(230, 206, 165), Rule = new(76, 62, 88),
        Green = new(184, 208, 187), Red = new(226, 128, 150),
        Pearl = new(228, 200, 239); // The skill notebook's warm pink-violet highlight.
    internal const int DesignWidth = 1100, DesignHeight = 800;
    internal const int CardWidth = AlchemyCatalogLayout.CardWidth, CardHeight = AlchemyCatalogLayout.CardHeight;
    internal const int CatalogX = 26, CatalogY = 150, CatalogWidth = 727, CatalogHeight = 550;
    internal const int DetailX = 790, DetailY = 286, DetailWidth = 280, DetailHeight = 307;
    internal const int DetailContentWidth = 270;
    internal const int QuillWidth = 22, QuillHeight = 42;
    internal static readonly Rectangle CloseButtonBounds = new(1022, 22, 52, 46);
    internal static readonly IReadOnlyList<AlchemyCatalogCategory> Navigation =
        new[] { new AlchemyCatalogCategory("all", "Catalog.All", "All", "", "", "") }.Concat(AlchemyCatalog.Categories).ToArray();
#if DEBUG
    internal static readonly Rectangle DebugLevelUpBounds = new(536, 754, 66, 26);
    internal static readonly Rectangle DebugMaxLevelBounds = new(608, 754, 66, 26);
    internal static readonly Rectangle DebugUnlockAllBounds = new(680, 754, 88, 26);
    internal static readonly Rectangle DebugRestockBounds = new(774, 754, 66, 26);
    internal static readonly Rectangle DebugResetBounds = new(846, 754, 66, 26);

    internal static void DebugButton(AlchemyDrawing d, string label, bool hover, bool enabled = true, int width = 66)
    {
        hover &= enabled;
        d.RoundRect(0, 2, width - 2, 22, 11, Ink * (hover ? .05f : 0), Ink * (enabled ? .18f : .08f), 1);
        d.FittedText(label, (width - 2) / 2f, 7, width - 10, 9.5f, enabled ? Muted : Muted * .45f, .5f);
    }

    internal static void DebugLevelUpButton(AlchemyDrawing d, AlchemyNotebookState s, bool hover, bool enabled = true)
        => DebugButton(d, s.Level < s.MaximumLevel ? LocalizeUI("AlchemyDebugLevel") : LocalizeUI("MaxLevelReached"), hover, enabled && s.Level < s.MaximumLevel);

    internal static void DebugMaxLevelButton(AlchemyDrawing d, AlchemyNotebookState s, bool hover, bool enabled = true)
        => DebugButton(d, LocalizeUI("AlchemyDebugMaxLevel"), hover, enabled && s.Level < s.MaximumLevel);

    internal static void DebugUnlockAllButton(AlchemyDrawing d, AlchemyNotebookState s, bool hover, bool enabled = true)
        => DebugButton(d, LocalizeUI(s.AllRecipesUnlocked ? "AlchemyDebugAllUnlocked" : "AlchemyDebugUnlockAll"), hover,
            enabled && !s.AllRecipesUnlocked, DebugUnlockAllBounds.Width);
#endif
    internal const string AssetRoot = "伊蕾娜/ElainaModAlchemy/UI/Assets/";
    internal static int Number(AlchemyCatalogRecipe recipe) => AlchemyCatalog.Recipes.ToList().IndexOf(recipe) + 1;
    internal static Color StateColor(string key) => key switch { "short" or "researchable" => Gold, "acquire" => Green, "locked" => Muted * .65f, _ => Lavender };
    internal static void Label(AlchemyDrawing d, string text, float x, float y, float size, Color color, float align = 0, bool serif = false)
        => d.Text(text, x, y, size, color, align, serif: serif);

    // Local coordinates shared with the native SUI hit area and offline preview.
    internal static void CloseButton(AlchemyDrawing d, bool hover)
    {
        d.Diamond(new(32, 20), 19, hover ? Lavender * .8f : Lavender * .32f);
        d.Line(new(28, 16), new(36, 24), hover ? Ink : Lavender, 1.1f);
        d.Line(new(28, 24), new(36, 16), hover ? Ink : Lavender, 1.1f);
        d.LatinText("ESC", 21, 35, 9, hover ? Ink : Muted, 1, .6f);
    }

    // Moon paper tilts with its text and seal; pixel specimens keep their native orientation.
    private const string PaperPath = AssetRoot + "NotebookCards";
    private const int PaperVariants = 1, PaperMargin = 12, PaperResolution = 2;
    private const float TextWidth = CardWidth - 22; // inside the ink frame
    internal static readonly Color PaperInk = new(52, 44, 64), PaperMuted = new(118, 108, 134),
        StampInk = new(138, 79, 134), ResearchInk = new(147, 103, 28);
    private static readonly Color FrameInk = new(80, 58, 70), ShadowInk = new(8, 5, 18), SealShadow = new(20, 10, 30),
        MoteInk = new(255, 236, 190), MoteGlow = new(240, 200, 120);
    private readonly record struct PaperTone(Color Paper, Color Light, Color Mid, Color Dark);
    // The baked paper already contains its moon-white tint.
    private static readonly PaperTone Tone = new(Color.White, new(200, 180, 232), new(154, 124, 201), new(94, 69, 144));

    // Resting offset of the card on the desk, in design units. The quill follows it.
    internal static Vector2 CardOffset(AlchemyCatalogRecipe r) => AlchemyCardMotion.BaseOffset(Number(r));

    // Without a pose the card is drawn at rest: the selected one upright, lifted and mid-breath.
    internal static void Card(AlchemyDrawing d, AlchemyNotebookState s, AlchemyCatalogRecipe r, bool hover, float flash = 0, AlchemyCardPose? pose = null)
    {
        bool selected = s.SelectedId == r.Id;
        var p = pose ?? AlchemyCardMotion.Settled(Number(r), selected, hover);
        var origin = d.Origin;
        d.Origin += (CardOffset(r) - new Vector2(0, p.Lift)) * d.Scale;
        var frame = d.PushFrame(new(CardWidth / 2f, CardHeight / 2f), MathHelper.ToRadians(p.Tilt), p.Scale);
        try
        {
            if (s.StateOf(r) == AlchemyNotebookState.Locked) BlankCard(d, s, r, hover, selected, flash);
            else PaperCard(d, s, r, p, flash);
        }
        finally
        {
            d.PopFrame(frame);
            // Paper tilts; the specimen stays upright at native size. Rotate only its anchor.
            Vector2 pivot = new(CardWidth / 2f, CardHeight / 2f);
            Vector2 anchor = pivot + Vector2.Transform((new Vector2(CardWidth / 2f, 46) - pivot) * p.Scale,
                Matrix.CreateRotationZ(MathHelper.ToRadians(p.Tilt)));
            d.ItemIcon(s.IsUnlocked(r) ? r.IconPath : r.LockedIconPath, anchor.X, anchor.Y,
                tint: Color.White * (s.StateOf(r) == AlchemyNotebookState.Locked ? .26f : 1));
            d.Origin = origin;
        }
    }

    // Requirements not met yet: only a dashed outline where the paper will go.
    private static void BlankCard(AlchemyDrawing d, AlchemyNotebookState s, AlchemyCatalogRecipe r, bool hover, bool selected, float flash)
    {
        Color faded = Muted * .55f;
        d.Box(0, 0, CardWidth, CardHeight, (selected ? Gold : Lavender) * .07f);
        d.DashedFrame(0, 0, CardWidth, CardHeight, selected ? Gold * .7f : Lavender * (hover ? .5f : .3f));

        if (flash > 0) d.Ring(new(CardWidth / 2, 45), 26 + (1 - flash) * 14, 26 + (1 - flash) * 14, Lavender * flash);
        NumberRule(d, r, faded, Lavender * .25f);
        d.FittedText(MaskedName(r), CardWidth / 2, 89, TextWidth, 13, faded, .5f, true, 2);
        var research = s.Research(r);
        d.FittedText(research.RequiresRecipe ? LocalizeUI("RecipeRequired") : LocalizeUI("AlchemyLevel", research.RequiredLevel),
            CardWidth / 2, 108, TextWidth, 10, faded, .5f);
    }

    private static void PaperCard(AlchemyDrawing d, AlchemyNotebookState s, AlchemyCatalogRecipe r, AlchemyCardPose pose, float flash)
    {
        string state = s.StateOf(r);
        bool researchable = state == AlchemyNotebookState.Researchable;
        int number = Number(r);
        Vector2 center = new(CardWidth / 2f, CardHeight / 2f), pin = new(CardWidth / 2f, -1);

        // Atlas rows: paper, 1 px dilated outline, soft silhouette (shadow and glow).
        int cellWidth = (CardWidth + PaperMargin * 2) * PaperResolution, cellHeight = (CardHeight + PaperMargin * 2) * PaperResolution;
        Rectangle Cell(int row) => new(number % PaperVariants * cellWidth, row * cellHeight, cellWidth, cellHeight);
        const float texel = 1f / PaperResolution;
        // The shadow stays on the desk: the higher the card, the further and wider it falls.
        float lift = pose.Lift;
        d.Sprite(PaperPath, Cell(2), center + new Vector2(0, 3 + lift * 1.2f), texel * (1 + lift * .006f), ShadowInk * (.38f + lift * .02f));
        if (pose.Selected > .005f)
        {
            float breathe = pose.Time >= 0 ? .14f * MathF.Sin(pose.Time * 2.6f) : 0;
            d.Sprite(PaperPath, Cell(2), center, texel * 1.04f, Gold * (pose.Selected * (.4f + breathe)));
            d.Sprite(PaperPath, Cell(1), center, texel, Gold * pose.Selected);
        }
        d.Sprite(PaperPath, Cell(0), center, texel, Tone.Paper);

        Color edge = new Color(120, 100, 145) * .35f, marks = new Color(110, 85, 140) * .6f;
        d.Frame(.5f, .5f, CardWidth-1, CardHeight-1, pose.Selected > .5f ? Gold : edge);
        Color dots = new Color(90,70,115) * .28f;
        d.Dots(6.5f,6.5f,CardWidth-6.5f,6.5f,dots);
        d.Dots(6.5f,CardHeight-6.5f,CardWidth-6.5f,CardHeight-6.5f,dots);
        d.Dots(6.5f,6.5f,6.5f,CardHeight-6.5f,dots);
        d.Dots(CardWidth-6.5f,6.5f,CardWidth-6.5f,CardHeight-6.5f,dots);
        foreach (var corner in new Vector2[] {new(6.5f,6.5f),new(CardWidth-6.5f,6.5f),new(6.5f,CardHeight-6.5f),new(CardWidth-6.5f,CardHeight-6.5f)})
            d.Diamond(corner,2.2f,marks,true);
        Color seal = researchable || pose.Selected > .5f ? Gold : marks;
        d.Line(pin-new Vector2(16,0),pin-new Vector2(7,0),seal);
        d.Line(pin+new Vector2(7,0),pin+new Vector2(16,0),seal);
        d.Diamond(pin,6.5f,researchable || pose.Selected > .5f ? new Color(70,56,40) : new Color(226,220,234),true);
        d.Diamond(pin,6.5f,seal);
        d.Diamond(pin,2.4f,seal,true);
        d.Glow(CardWidth/2-34,16,68,60,CategoryGlow(r.Category)*.20f);

        bool unlocked = s.IsUnlocked(r);
        if (researchable) d.Glow(CardWidth / 2 - 24, 21, 48, 48, new Color(214, 170, 80) * .5f);

        if (flash > 0)
        {
            d.Ring(new(CardWidth / 2, 45), 26 + (1 - flash) * 14, 26 + (1 - flash) * 14, Tone.Mid * flash);
            d.Glow(CardWidth / 2 - 40, 5, 80, 80, Tone.Light * (.5f * flash));
        }
        string name = unlocked ? r.Name : MaskedName(r);
        float nameSize = 13;
        var nameLines = WrapUIParagraph(name, TextWidth, t => d.Measure(t, nameSize, 0, true));
        if (nameLines.Length > 1) nameSize = 12;
        do
        {
            nameLines = WrapUIParagraph(name, TextWidth, t => d.Measure(t, nameSize, 0, true));
            if (nameLines.Length <= 2) break;
            nameSize -= .5f;
        } while (nameSize > 8);
        bool two = nameLines.Length > 1 && unlocked;
        NumberRule(d, r, PaperMuted, FrameInk * .38f, two ? 7 : 0);
        for (int i=0; i<Math.Min(2,nameLines.Length); i++)
            d.FittedText(nameLines[i], CardWidth/2f, (two ? 80 : 89)+i*14, TextWidth, nameSize,
                unlocked ? PaperInk : PaperMuted, .5f, true);
        string caption = researchable ? LocalizeUI("Researchable") : state == AlchemyNotebookState.Ready ? LocalizeUI("CardStamp") : null;
        if (caption != null)
        {
            float size = 10, width = d.Measure(caption,size);
            if (width > TextWidth-10) { size *= (TextWidth-10)/width; width = TextWidth-10; }
            float left = (CardWidth-width-8)/2;
            if (researchable) d.Diamond(new(left+2.5f,118),2.5f,ResearchInk,true);
            else d.Spark(new(left+2.5f,118),6,ResearchInk);
            d.Text(caption,left+8,111,size,ResearchInk);
        }

        // Breath: while held, three small diamonds drift up from the seal on a 1.8 s cycle.
        // Positions follow from the pickup time alone, so no particle list is kept.
        if (pose.Time < 0) return;
        for (int k = 0; k < 3; k++)
        {
            float age = pose.Time - .4f - k * .6f;
            if (age < 0) continue;
            float phase = age / 1.8f;
            int cycle = (int)MathF.Floor(phase);
            float u = phase - cycle, alpha = .85f * MathF.Sin(MathF.PI * u) * pose.Selected;
            Vector2 at = pin + new Vector2((AlchemyCardMotion.Jitter(cycle + 3, k + 41) * 2 - 1) * 26 + MathF.Sin(u * 5 + k) * 2, -3 - 20 * u);
            d.Glow(at.X - 6, at.Y - 6, 12, 12, MoteGlow * (.7f * alpha));
            d.Diamond(at, 2.83f * (.8f - .3f * u), MoteInk * alpha, true);
        }
    }

    // —— No.07 —— between the specimen and its name.
    private static void NumberRule(AlchemyDrawing d, AlchemyCatalogRecipe r, Color text, Color rule, float lift = 0)
    {
        string label = "No." + Number(r).ToString("00");
        float half = d.LatinWidth(label, 9) / 2, x = CardWidth / 2f;
        d.LatinText(label, x, 76 - lift, 9, text, .5f);
        d.Gradient(x - half - 30, 82 - lift, 24, 1, Color.Transparent, rule);
        d.Gradient(x + half + 6, 82 - lift, 24, 1, rule, Color.Transparent);
    }

    private static readonly string[] MysterySuffixes = ["药水", "合剂", "露滴", "滴管", "面包", "布蕾", "树脂", "液滴", "尘", "药", "雨", "菜", "露", "粉", "蛋", "肉", "豆"];
    private static string MaskedName(AlchemyCatalogRecipe recipe)
    {
        if (!recipe.Name.Any(c => c >= '\u4e00' && c <= '\u9fff')) return "???";
        string suffix = MysterySuffixes.FirstOrDefault(value => recipe.Name.Length > value.Length
            && recipe.Name.EndsWith(value, StringComparison.Ordinal)) ?? "";
        return new string('?', recipe.Name.Length - suffix.Length) + suffix;
    }

    private static void MysteryIcon(AlchemyDrawing d, AlchemyCatalogRecipe recipe, float x, float y)
    {
        d.ItemIcon(recipe.LockedIconPath, x, y);
    }

    // Card-local anchor; the quill leans past the lifted paper's lower right corner.
    // It rises with the card and moves out with its pickup scale, but does not turn with it.
    internal static void SelectionQuill(AlchemyDrawing d, AlchemyCardPose? pose = null)
    {
        var p = pose ?? AlchemyCardMotion.Settled(0, true, false);
        Vector2 center = new(CardWidth / 2f, CardHeight / 2f), anchor = new(CardWidth - 12 + QuillWidth / 2f, CardHeight - 43 + QuillHeight / 2f);
        Vector2 at = center + (anchor - center) * p.Scale - new Vector2(QuillWidth, QuillHeight) / 2 - new Vector2(0, p.Lift);
        d.Image(AssetRoot + "SelectionQuill", at.X, at.Y, QuillWidth, QuillHeight, Color.White);
    }

}

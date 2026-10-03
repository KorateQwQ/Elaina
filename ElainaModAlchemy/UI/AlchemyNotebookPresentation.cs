using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModAlchemy.UI;

/// <summary>Measured presentation rows shared by native SUI and the FNA preview.</summary>
internal sealed record AlchemyNotebookRow(float Height, Action<AlchemyDrawing, bool> Paint, string Entry = null);

internal static class AlchemyNotebookPresentation
{
    internal static readonly Color Ink = new(238, 233, 245), Muted = new(178, 168, 195),
        Lavender = new(202, 178, 237), Gold = new(230, 206, 165), Rule = new(76, 62, 88),
        Green = new(184, 208, 187), Red = new(224, 163, 157),
        Pearl = new(228, 200, 239); // The skill notebook's warm pink-violet highlight.
    internal const int DesignWidth = 1100, DesignHeight = 800;
    internal const int CardWidth = AlchemyCatalogLayout.CardWidth, CardHeight = AlchemyCatalogLayout.CardHeight;
    internal const int CatalogX = 48, CatalogY = 166, CatalogWidth = 672, CatalogHeight = 509;
    internal const int DetailX = 780, DetailY = 274, DetailWidth = 272, DetailHeight = 342;
    internal const int DetailContentWidth = DetailWidth - 24;
    internal const int QuillWidth = 22, QuillHeight = 42;
    internal static readonly Rectangle CloseButtonBounds = new(1022, 22, 52, 46);
    internal static readonly IReadOnlyList<AlchemyCatalogCategory> Navigation =
        new[] { new AlchemyCatalogCategory("all", "全部", "All", "", "", "") }.Concat(AlchemyCatalog.Categories).ToArray();
#if DEBUG
    internal static readonly Rectangle DebugLevelUpBounds = new(626, 744, 82, 35);

    internal static void DebugButton(AlchemyDrawing d, string label, bool hover, bool enabled = true)
    {
        hover &= enabled;
        d.Box(0, 3, 82, 29, Gold * (hover ? .13f : .045f));
        d.Frame(0, 3, 82, 29, Gold * (enabled ? hover ? .7f : .32f : .12f));
        Label(d, label, 41, 11, 10, enabled ? hover ? Ink : Gold : Muted * .45f, .5f);
    }

    internal static void DebugLevelUpButton(AlchemyDrawing d, AlchemyNotebookState s, bool hover, bool enabled = true)
        => DebugButton(d, s.Level < s.MaximumLevel ? "等级 +1" : "已满级", hover, enabled && s.Level < s.MaximumLevel);
#endif
    internal const string AssetRoot = "伊蕾娜/ElainaModAlchemy/UI/Assets/";
    internal static int Number(AlchemyCatalogRecipe recipe) => AlchemyCatalog.Recipes.ToList().IndexOf(recipe) + 1;
    internal static Color StateColor(string key) => key switch { "short" or "researchable" => Gold, "acquire" => Green, "locked" => Muted * .65f, _ => Lavender };
    internal static void Label(AlchemyDrawing d, string text, float x, float y, float size, Color color, float align = 0, bool serif = false)
        => d.Text(text, x, y, size, color, align, serif: serif);

    internal static void Chrome(AlchemyDrawing d, AlchemyNotebookState s)
    {
        d.NotebookPage(DesignWidth, DesignHeight);
        d.Line(new(32, 83), new(1068, 83), Lavender * .17f);
        d.Line(new(26, 145), new(1074, 145), Lavender * .10f);
        d.Line(new(26, 725), new(1074, 725), Lavender * .15f);
        // The alchemy surface has continuous shading, without the star chart's viewport bands.
        d.Line(new(753, 146), new(753, 725), Lavender * .24f);
        d.Line(new(780, 260), new(1052, 260), Lavender * .12f);
        d.Glow(21, 2, 80, 80, Pearl * .07f);
        d.Ring(new(61, 42), 30, 30, Lavender * .17f);
        d.Image(AssetRoot + "AlchemyCrest", 38, 17, 46, 46, Lavender);
        Label(d, "炼金手记", 101, 27, 28, Ink, serif: true);
        ProgressHeader(d, s);
        Label(d, "已研究", 952, 115, 10, Muted, 1);
        d.LatinText(s.Visible.Count(s.IsUnlocked).ToString("00"), 1008, 109, 19, Ink);
        d.LatinText("/ " + s.Visible.Count.ToString("00"), 1036, 114, 12, Muted);
        string[] legendStates = [AlchemyNotebookState.Ready, AlchemyNotebookState.Short, AlchemyNotebookState.Locked];
        for (int i = 0; i < legendStates.Length; i++)
        {
            string key = legendStates[i];
            StateMarker(d, key, new(51 + i * 120, 702));
            Label(d, AlchemyNotebookState.StateLabel(key), 62 + i * 120, 697, 10, Muted);
        }
        Label(d, "素材", 33, 748, 13, Ink, serif: true);
        Label(d, "合成可用", 33, 768, 9, Muted);
    }

    private static void ProgressHeader(AlchemyDrawing d, AlchemyNotebookState s)
    {
        d.Image(AssetRoot + "LevelCauldron", 850, 28, 24, 28, Gold * .9f);
        Label(d, "炼金术", 884, 28, 12, new Color(199, 186, 211), serif: true);
        d.LatinText($"Lv.{s.Level}", 1024, 17, 28, Gold, 1);
        d.RoundedBar(884, 48, 140, 3, Lavender * .14f);
        float progress = s.Level >= s.MaximumLevel ? 1 : s.Experience / (float)Math.Max(1, s.ExperienceToNextLevel);
        d.RoundedBar(884, 48, 140 * Math.Clamp(progress, 0, 1), 3, Pearl * .75f);
        Label(d, s.Level >= s.MaximumLevel ? "炼金经验已满" : $"炼金经验 {s.Experience} / {s.ExperienceToNextLevel}",
            884, 56, 10, Muted);
    }

    // Local coordinates shared with the native SUI hit area and offline preview.
    internal static void CloseButton(AlchemyDrawing d, bool hover)
    {
        d.Diamond(new(32, 20), 19, hover ? Lavender * .8f : Lavender * .32f);
        d.Line(new(28, 16), new(36, 24), hover ? Ink : Lavender, 1.1f);
        d.Line(new(28, 24), new(36, 16), hover ? Ink : Lavender, 1.1f);
        d.LatinText("ESC", 21, 35, 9, hover ? Ink : Muted, 1, .6f);
    }

    internal static void Category(AlchemyDrawing d, AlchemyNotebookState s, AlchemyCatalogCategory c, bool hover)
    {
        bool active = s.Category == c.Id;
        d.Diamond(new(12, 20), 5, active || hover ? Lavender : Muted * .7f, active);
        Label(d, c.Name, 26, 13, 14, active || hover ? Ink : Muted, serif: true);
        int count = c.Id == "all" ? AlchemyCatalog.Recipes.Count : AlchemyCatalog.Recipes.Count(r => r.Category == c.Id);
        d.LatinText(count.ToString("00"), 91, 15, 11, active ? Lavender : Muted * .65f);
        if (!active) return;
        // Match ConstellationDrawing.ActiveFilterMarker's two premultiplied halos.
        d.Glow(0, 43, 116, 8, new Color(192, 154, 220) * .12f);
        d.Glow(42, 33, 32, 28, new Color(207, 157, 250) * .34f);
        for (int i = 0; i < 112; i++)
            d.Box(2 + i, 47, 1, 1, new Color(192, 154, 220) * (.4f * (1 - Math.Abs(i - 55.5f) / 55.5f)));
        d.Diamond(new(58, 47), 3, Lavender, true);
    }

    internal static void Card(AlchemyDrawing d, AlchemyNotebookState s, AlchemyCatalogRecipe r, bool hover, float flash = 0)
    {
        bool selected = s.SelectedId == r.Id, unlocked = s.IsUnlocked(r);
        Color edge = selected ? Gold : hover ? Lavender : Lavender * .23f;
        d.Box(0, 0, CardWidth, CardHeight, Pearl * (selected ? .065f : hover ? .045f : .012f));
        d.Glow(CardWidth / 2 - 56, 0, 112, 108, Pearl * (selected ? .14f : hover ? .10f : .035f));
        if (unlocked) d.Frame(0, 0, CardWidth, CardHeight, edge * (selected || hover ? .72f : 1));
        else d.DashedFrame(0, 0, CardWidth, CardHeight, edge * (selected || hover ? .72f : .7f));
        if (selected) d.Frame(4, 4, CardWidth - 8, CardHeight - 8, Gold * .12f);
        d.LatinText("No." + Number(r).ToString("00"), 10, 8, 9, Muted * .8f);
        if (unlocked)
        {
            StateMarker(d, s.StateOf(r), new(CardWidth - 12, 13));
            d.Ring(new(CardWidth / 2, 53), 33, 33, (selected ? Gold : Lavender) * .24f);
            d.ItemIcon(r.IconPath, CardWidth / 2, 53);
        }
        else
        {
            StateMarker(d, s.StateOf(r), new(CardWidth - 12, 13));
            d.CornerBrackets(CardWidth / 2 - 34, 23, 68, 62, Muted * .36f);
            MysteryIcon(d, r, CardWidth / 2, 56);
        }
        if (flash > 0) d.Ring(new(CardWidth / 2, 53), 33 + (1 - flash) * 14, 33 + (1 - flash) * 14, Lavender * flash);
        if (flash > 0) d.Glow(CardWidth / 2 - 56, -3, 112, 112, Pearl * (.24f * flash));
        if (unlocked) Label(d, r.Name, CardWidth / 2, 96, 13, selected ? Gold : Ink, .5f, true);
        else d.Text(MaskedName(r), CardWidth / 2, 97, 14, selected ? Gold : Muted, .5f, 2, true);
        string caption = unlocked
            ? r.Craftable ? $"每批 ×{r.Yield}    可制 {s.MaxBatches(r)}" : $"{r.Stage}    持有 {s.Owned(r)}"
            : s.Research(r).CanResearch ? "可研究" : s.Research(r).RequiresRecipe ? "需要配方" : $"炼金术 Lv.{s.Research(r).RequiredLevel}";
        Label(d, caption, CardWidth / 2, 129, 10, !unlocked && s.Research(r).CanResearch ? Gold : Muted, .5f);
    }

    // Share the exact badge shapes between cards and the three-entry legend.
    // Gathered/bought materials have no brewing badge; their source stays in the details.
    private static void StateMarker(AlchemyDrawing d, string state, Vector2 center)
    {
        if (state is AlchemyNotebookState.Locked or AlchemyNotebookState.Researchable)
            d.DashedRing(center, 4, state == AlchemyNotebookState.Researchable ? Gold * .75f : Muted * .55f);
        else if (state is AlchemyNotebookState.Ready or AlchemyNotebookState.Short)
            d.Diamond(center, 3, StateColor(state), state == AlchemyNotebookState.Ready);
    }

    private static readonly string[] MysterySuffixes = ["药水", "合剂", "露滴", "滴管", "面包", "布蕾", "树脂", "液滴", "尘", "药", "雨", "菜", "露", "粉", "蛋", "肉", "豆"];
    private static string MaskedName(AlchemyCatalogRecipe recipe)
    {
        string suffix = MysterySuffixes.FirstOrDefault(value => recipe.Name.Length > value.Length
            && recipe.Name.EndsWith(value, StringComparison.Ordinal)) ?? "";
        return new string('?', recipe.Name.Length - suffix.Length) + suffix;
    }

    private static void MysteryIcon(AlchemyDrawing d, AlchemyCatalogRecipe recipe, float x, float y)
    {
        d.ItemIcon(recipe.LockedIconPath, x, y);
    }

    internal static void SelectionQuill(AlchemyDrawing d, float x, float y)
        => d.Image(AssetRoot + "SelectionQuill", x, y, QuillWidth, QuillHeight, Color.White);

    internal static void Hero(AlchemyDrawing d, AlchemyNotebookState s, float brewProgress)
    {
        var r = s.Current;
        Label(d, AlchemyNotebookState.StateLabel(s.StateOf(r)), 1052, 164, 11, StateColor(s.StateOf(r)), 1);
        d.Glow(758, 166, 108, 108, Pearl * (.13f + (brewProgress > 0 ? .08f * MathF.Sin(brewProgress * MathF.PI) : 0)));
        float rock = brewProgress > 0 ? MathF.Sin(brewProgress * MathF.PI * 6) * .06f : 0;
        bool unlocked = s.IsUnlocked(r);
        if (unlocked)
        {
            d.Ring(new(812, 220), 32, 32, Lavender * .25f);
            d.Diamond(new(812, 220), 39, Lavender * .13f, false);
            d.ItemIcon(r.IconPath, 812, 220, rock);
        }
        else
        {
            d.CornerBrackets(778, 186, 68, 68, Muted * .45f);
            MysteryIcon(d, r, 812, 220);
        }
        if (brewProgress > 0)
            for (int i = 0; i < 7; i++)
            {
                float a = brewProgress * MathHelper.TwoPi + i * MathHelper.TwoPi / 7;
                d.Diamond(new(812 + MathF.Cos(a) * 34, 220 + MathF.Sin(a) * 34), 2, Lavender * MathF.Sin(brewProgress * MathF.PI), true);
            }
        string name = unlocked ? r.Name : MaskedName(r);
        Label(d, name, 858, 198, Math.Min(22, 180f / Math.Max(1, name.Length)), Ink, serif: true);
        if (unlocked) Label(d, r.Kind, 859, 234, 10, Muted);
    }

    internal static List<AlchemyNotebookRow> DetailRows(AlchemyDrawing measure, AlchemyNotebookState s)
    {
        var rows = new List<AlchemyNotebookRow>();
        var r = s.Current;
        void Paragraph(string value, int size, Color color, int leading, int after = 0)
        {
            var lines = measure.WrapParagraph(value ?? "", DetailContentWidth - 5, size);
            rows.Add(new(lines.Length * leading + after, (d, _) =>
            {
                for (int i = 0; i < lines.Length; i++) Label(d, lines[i], 0, i * leading, size, color);
            }));
        }
        if (!s.IsUnlocked(r))
        {
            var research = s.Research(r);
            Paragraph("这一页的内容尚未揭晓。满足研究条件后，便可记录造物的用途与制作方法。", 12, Muted, 23, 20);
            Paragraph("研究条件", 14, Ink, 24, 8);
            Paragraph($"炼金等级  {s.Level} / {research.RequiredLevel}", 12, s.Level >= research.RequiredLevel ? Green : Red, 23, 12);
            if (research.RequiresRecipe)
            {
                Paragraph(research.RecipeConfigured ? research.RecipeName : "配方物品尚未登记", 12, Ink, 23, 4);
                Paragraph($"背包持有  {research.RecipeOwned} / {research.RecipeRequired}", 11,
                    research.RecipeOwned >= research.RecipeRequired && research.RecipeConfigured ? Green : Red, 21, 8);
                Paragraph(research.ConsumeRecipe ? "研究时消耗所需配方。" : "持有配方即可研究，不消耗配方。", 11, Muted, 21);
            }
            else Paragraph("无需配方物品，达到等级后点击研究即可解锁。", 12, Muted, 23);
            return rows;
        }
        Paragraph(r.Description, 12, new Color(201, 187, 211), 21, 12);
        foreach (var effect in r.Effects)
        {
            var lines = measure.WrapParagraph(effect.Value, DetailContentWidth - 18, 11);
            rows.Add(new(20 + lines.Length * 18 + 8, (d, _) =>
            {
                d.Diamond(new(3, 6), 2, Lavender * .65f, true);
                Label(d, effect.Label, 12, 0, 10, Muted);
                for (int i = 0; i < lines.Length; i++) Label(d, lines[i], 12, 19 + i * 18, 11, effect.Tone == "bad" ? Red : effect.Tone == "live" ? Green : Ink);
            }));
        }
        string condition = r.Acquisition ?? r.Stage ?? $"炼金等级 {s.Research(r).RequiredLevel} · 已研究";
        var stage = measure.WrapParagraph(condition, DetailContentWidth - 24, 12);
        rows.Add(new(39 + stage.Length * 20 + (r.Craftable ? 0 : 20), (d, _) =>
        {
            float h = 29 + stage.Length * 20 + (r.Craftable ? 0 : 20);
            d.Box(0, 0, DetailContentWidth - 3, h, Lavender * .025f);
            d.Frame(0, 0, DetailContentWidth - 3, h, Lavender * .15f);
            Label(d, r.Craftable ? "研究记录" : "获取方式", 11, 9, 10, Muted);
            for (int i = 0; i < stage.Length; i++) Label(d, stage[i], 11, 29 + i * 20, 12, Ink);
            if (!r.Craftable) Label(d, $"{r.Stage} · 持有 {s.Owned(r)}", 11, 30 + stage.Length * 20, 10, Muted);
        }));
        if (!r.Craftable) return rows;
        rows.Add(new(30, (d, _) =>
        {
            Label(d, "所需素材", 0, 7, 12, Ink, serif: true);
            Label(d, $"× {s.Quantity} 批", DetailContentWidth - 3, 9, 10, Muted, 1);
        }));
        foreach (var ingredient in r.Ingredients)
        {
            var mats = ingredient.Choices.Select(AlchemyCatalog.GetMaterial).ToArray();
            string name = string.Join(" / ", mats.Select(m => m.Name));
            string entry = mats.Length == 1 ? mats[0].Entry : null;
            float height = 46;
            rows.Add(new(height, (d, hover) =>
            {
                int have = s.Available(ingredient), need = ingredient.Count * s.Quantity;
                d.Box(0, 0, DetailContentWidth - 3, height - 4, have < need ? Red * .04f : Lavender * .018f);
                if (hover && entry != null) d.Frame(0, 0, DetailContentWidth - 3, height - 4, Lavender * .35f);
                for (int i = 0; i < mats.Length; i++) d.MaterialIcon(mats[i].IconPath, 2 + i * 12, 5 + i * 5, mats.Length == 1 ? 30 : 23, mats.Length == 1 ? 30 : 23);
                Label(d, name, 40, 5, 11, entry != null ? Lavender : Ink);
                if (entry != null) d.Line(new(40, 20), new(40 + measure.Measure(name, 11), 20), Lavender * .35f);
                Label(d, $"{have} / {need}", DetailContentWidth - 8, 13, 10, have < need ? Red : Muted, 1);
            }, entry));
        }
        return rows;
    }

    internal static string CraftLabel(AlchemyNotebookState s, bool brewing)
        => brewing ? "炼制中…" : s.IsBusy ? "处理中…" : !s.IsUnlocked(s.Current) ? "研究"
            : !s.Current.Craftable ? s.Current.Stage == "商人" ? "商人售卖" : s.Current.Stage == "旅商" ? "旅商固定售卖" : "以太滴管取得"
            : s.MaxBatches(s.Current) < 1 ? "素材不足" : $"{(s.Current.Category == "food" ? "制作" : "炼制")}  × {s.Current.Yield * s.Quantity}";

    internal static void CraftButton(AlchemyDrawing d, AlchemyNotebookState s, bool hover, float progress)
    {
        bool enabled = !s.IsBusy && progress <= 0 && (s.IsUnlocked(s.Current) ? s.CanCraft() : s.CanResearch());
        d.Box(0, 0, 272, 38, Lavender * (enabled ? hover ? .25f : .15f : .04f));
        d.Frame(0, 0, 272, 38, Lavender * (enabled ? .55f : .20f));
        d.Diamond(new(9, 19), 3, Lavender * (enabled ? .8f : .3f), false);
        d.Diamond(new(263, 19), 3, Lavender * (enabled ? .8f : .3f), false);
        Label(d, CraftLabel(s, progress > 0), 136, 10, 14, enabled || progress > 0 ? Ink : Muted * .6f, .5f, true);
        if (progress > 0) d.Box(0, 36, 272 * progress, 2, Lavender);
    }

    internal static string Hint(AlchemyNotebookState s, bool brewing)
        => brewing ? "材料正在慢慢交融…" : !s.IsUnlocked(s.Current) ? s.Research(s.Current).Hint
            : !s.Current.Craftable ? "可在素材页查看食材与药液的获取方式"
            : s.MaxBatches(s.Current) < 1 ? "素材不足，请准备所需合成材料"
            : $"持有 {s.Owned(s.Current)} · 每批产出 {s.Current.Yield} · 最多可制 {s.MaxBatches(s.Current)} 批";
}

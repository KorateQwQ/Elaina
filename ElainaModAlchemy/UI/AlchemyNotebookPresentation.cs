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
    internal const int DesignWidth = 1100, DesignHeight = 800, CardWidth = 156, CardHeight = 132;
    internal const int CatalogX = 48, CatalogY = 200, CatalogWidth = 672, CatalogHeight = 475;
    internal const int DetailX = 780, DetailY = 274, DetailWidth = 272, DetailHeight = 342;
    internal const int QuillWidth = 22, QuillHeight = 42;
    private const int NoteTextLeft = 16, NoteTextWidth = 612;
    internal const string AssetRoot = "伊蕾娜/ElainaModAlchemy/UI/Assets/";
    internal static string CategoryName(string id) => AlchemyCatalog.Categories.First(c => c.Id == id).Name;
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
        // NotebookSurface already contains the skill notebook's detail shading.
        // Do not apply a second dark overlay on the alchemy page.
        d.Line(new(753, 146), new(753, 725), Lavender * .24f);
        d.Glow(21, 2, 80, 80, Pearl * .07f);
        d.Ring(new(61, 42), 30, 30, Lavender * .17f);
        d.Image(AssetRoot + "AlchemyCrest", 38, 17, 46, 46, Lavender);
        Label(d, "炼金手记", 101, 20, 28, Ink, serif: true);
        Label(d, "魔药与造物", 257, 35, 10, Muted);
        Label(d, "配方、奇物与旅途中的一餐", 104, 58, 10, Muted);
        Label(d, "炼金等级", 800, 26, 11, Muted);
        d.LatinText(s.Level.ToString("00"), 880, 18, 28, Gold);
        d.LatinText("/ " + s.MaximumLevel, 924, 32, 11, Muted);
        Label(d, s.Level >= s.MaximumLevel ? "已达等级上限" : $"经验  {s.Experience} / {s.ExperienceToNextLevel}", 800, 51, 10, Muted);
        d.Box(800, 69, 213, 3, Lavender * .14f);
        float progress = s.Level >= s.MaximumLevel ? 1 : s.Experience / (float)Math.Max(1, s.ExperienceToNextLevel);
        d.Box(800, 69, 213 * Math.Clamp(progress, 0, 1), 3, Pearl * .75f);
        Label(d, "已研究", 952, 115, 10, Muted, 1);
        d.LatinText(s.Visible.Count(s.IsUnlocked).ToString("00"), 1008, 109, 19, Ink);
        d.LatinText("/ " + AlchemyCatalog.Recipes.Count(r => r.Category == s.Category).ToString("00"), 1036, 114, 12, Muted);
        var cat = AlchemyCatalog.Categories.First(c => c.Id == s.Category);
        Label(d, cat.Title, 47, 165, 17, new Color(221, 201, 230), serif: true);
        Label(d, cat.Description, 173, 170, 10, Muted);
        for (int i = 0; i < 4; i++)
        {
            string key = new[] { "researchable", "ready", "short", "acquire" }[i];
            d.Diamond(new(51 + i * 108, 702), 3, StateColor(key), key == "ready");
            Label(d, AlchemyNotebookState.StateLabel(key), 62 + i * 108, 697, 10, Muted);
        }
        d.LatinText("- " + (AlchemyCatalog.Categories.ToList().FindIndex(c => c.Id == s.Category) + 1).ToString("00") + " -", 685, 698, 10, Muted, .5f);
        Label(d, "素材", 33, 748, 13, Ink, serif: true);
        Label(d, "合成可用", 33, 768, 9, Muted);
    }

    internal static void Category(AlchemyDrawing d, AlchemyNotebookState s, AlchemyCatalogCategory c, bool hover)
    {
        bool active = s.Category == c.Id;
        d.Diamond(new(12, 20), 5, active || hover ? Lavender : Muted * .7f, active);
        Label(d, c.Name, 26, 13, 14, active || hover ? Ink : Muted, serif: true);
        int count = AlchemyCatalog.Recipes.Count(r => r.Category == c.Id);
        d.LatinText(count.ToString("00"), 94, 15, 12, active ? Lavender : Muted * .65f);
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
        bool selected = s.SelectedId == r.Id;
        Color edge = selected ? Gold : hover ? Lavender : Lavender * .23f;
        d.Box(0, 0, CardWidth, CardHeight, Pearl * (selected ? .07f : hover ? .06f : .025f));
        d.Glow(CardWidth / 2 - 50, -5, 100, 100, Pearl * (selected ? .16f : hover ? .12f : .055f));
        d.Frame(0, 0, CardWidth, CardHeight, edge * (selected || hover ? .72f : 1));
        if (selected) d.Frame(4, 4, CardWidth - 8, CardHeight - 8, Gold * .12f);
        d.LatinText("No." + Number(r).ToString("00"), 10, 8, 9, Muted * .8f);
        d.Diamond(new(CardWidth - 12, 13), 3, StateColor(s.StateOf(r)), s.StateOf(r) == "ready");
        d.Ring(new(CardWidth / 2, 45), 28, 28, (selected ? Gold : Lavender) * .24f);
        if (flash > 0) d.Ring(new(CardWidth / 2, 45), 28 + (1 - flash) * 14, 28 + (1 - flash) * 14, Lavender * flash);
        if (flash > 0) d.Glow(CardWidth / 2 - 56, -11, 112, 112, Pearl * (.24f * flash));
        bool unlocked = s.IsUnlocked(r);
        if (unlocked) d.Image(r.IconPath, CardWidth / 2 - 23, 22, 46, 46, Color.White);
        else Label(d, "?", CardWidth / 2, 27, 29, Lavender * .75f, .5f, true);
        Label(d, unlocked ? r.Name : "???", CardWidth / 2, 81, 12, selected ? Gold : Ink, .5f, true);
        string caption = !unlocked ? $"炼金等级 {s.Research(r).RequiredLevel} · {AlchemyNotebookState.StateLabel(s.StateOf(r))}"
            : r.Craftable ? $"每批 ×{r.Yield}    可制 {s.MaxBatches(r)}" : $"{r.Stage}    持有 {s.Owned(r)}";
        Label(d, caption, CardWidth / 2, 108, 9, Muted, .5f);
    }

    internal static float NoteHeight(AlchemyDrawing d, AlchemyNotebookState s)
        => d.WrapParagraph("“" + AlchemyCatalog.Categories.First(c => c.Id == s.Category).Note + "”", NoteTextWidth, 12).Length * 24 + 63;
    internal static void SelectionQuill(AlchemyDrawing d, float x, float y)
        => d.Image(AssetRoot + "SelectionQuill", x, y, QuillWidth, QuillHeight, Color.White);

    internal static void PageNote(AlchemyDrawing d, AlchemyNotebookState s)
    {
        var c = AlchemyCatalog.Categories.First(c => c.Id == s.Category);
        d.Line(new(5, 18), new(651, 18), Gold * .16f);
        float bottom = d.Paragraph("“" + c.Note + "”", NoteTextLeft, 36, NoteTextWidth, 12, new Color(195, 169, 207), 24);
        Label(d, "— 伊蕾娜的手记", 625, bottom + 8, 10, Muted * .75f, 1, true);
    }

    internal static void Hero(AlchemyDrawing d, AlchemyNotebookState s, float brewProgress)
    {
        var r = s.Current;
        Label(d, CategoryName(r.Category) + " · No." + Number(r).ToString("00"), 780, 164, 10, Muted);
        Label(d, AlchemyNotebookState.StateLabel(s.StateOf(r)), 1052, 164, 11, StateColor(s.StateOf(r)), 1);
        d.Glow(758, 166, 108, 108, Pearl * (.13f + (brewProgress > 0 ? .08f * MathF.Sin(brewProgress * MathF.PI) : 0)));
        d.Ring(new(812, 220), 32, 32, Lavender * .25f);
        d.Diamond(new(812, 220), 39, Lavender * .13f, false);
        float rock = brewProgress > 0 ? MathF.Sin(brewProgress * MathF.PI * 6) * .06f : 0;
        bool unlocked = s.IsUnlocked(r);
        if (unlocked) d.Image(r.IconPath, 786, 194, 52, 52, Color.White, rock);
        else Label(d, "?", 812, 199, 31, Lavender * .8f, .5f, true);
        if (brewProgress > 0)
            for (int i = 0; i < 7; i++)
            {
                float a = brewProgress * MathHelper.TwoPi + i * MathHelper.TwoPi / 7;
                d.Diamond(new(812 + MathF.Cos(a) * 34, 220 + MathF.Sin(a) * 34), 2, Lavender * MathF.Sin(brewProgress * MathF.PI), true);
            }
        string name = unlocked ? r.Name : "未解锁的造物";
        Label(d, name, 858, 198, Math.Min(22, 180f / Math.Max(1, name.Length)), Ink, serif: true);
        Label(d, unlocked ? r.Kind : "研究后记入炼金手记", 859, 234, 10, Muted);
    }

    internal static List<AlchemyNotebookRow> DetailRows(AlchemyDrawing measure, AlchemyNotebookState s)
    {
        var rows = new List<AlchemyNotebookRow>();
        var r = s.Current;
        void Paragraph(string value, int size, Color color, int leading, int after = 0)
        {
            var lines = measure.WrapParagraph(value ?? "", DetailWidth - 5, size);
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
            var lines = measure.WrapParagraph(effect.Value, DetailWidth - 18, 11);
            rows.Add(new(20 + lines.Length * 18 + 8, (d, _) =>
            {
                d.Diamond(new(3, 6), 2, Lavender * .65f, true);
                Label(d, effect.Label, 12, 0, 10, Muted);
                for (int i = 0; i < lines.Length; i++) Label(d, lines[i], 12, 19 + i * 18, 11, effect.Tone == "bad" ? Red : effect.Tone == "live" ? Green : Ink);
            }));
        }
        var stage = measure.WrapParagraph(r.Acquisition ?? $"炼金等级 {s.Research(r).RequiredLevel} · 已研究", DetailWidth - 24, 12);
        rows.Add(new(39 + stage.Length * 20 + (r.Craftable ? 0 : 20), (d, _) =>
        {
            float h = 29 + stage.Length * 20 + (r.Craftable ? 0 : 20);
            d.Box(0, 0, DetailWidth - 3, h, Lavender * .025f);
            d.Frame(0, 0, DetailWidth - 3, h, Lavender * .15f);
            Label(d, r.Craftable ? "研究记录" : "获取方式", 11, 9, 10, Muted);
            for (int i = 0; i < stage.Length; i++) Label(d, stage[i], 11, 29 + i * 20, 12, Ink);
            if (!r.Craftable) Label(d, $"{r.Stage} · 持有 {s.Owned(r)}", 11, 30 + stage.Length * 20, 10, Muted);
        }));
        if (!r.Craftable) return rows;
        rows.Add(new(30, (d, _) =>
        {
            Label(d, "所需素材", 0, 7, 12, Ink, serif: true);
            Label(d, $"× {s.Quantity} 批", DetailWidth - 3, 9, 10, Muted, 1);
        }));
        foreach (var ingredient in r.Ingredients)
        {
            var mats = ingredient.Choices.Select(AlchemyCatalog.GetMaterial).ToArray();
            string name = string.Join(" / ", mats.Select(m => m.Name));
            string source = mats.Length > 1 ? "二者可合计使用" : mats[0].Source;
            var sourceLines = measure.WrapParagraph(source, 155, 9);
            string entry = mats.Length == 1 ? mats[0].Entry : null;
            float height = Math.Max(46, 24 + sourceLines.Length * 14);
            rows.Add(new(height, (d, hover) =>
            {
                int have = s.Available(ingredient), need = ingredient.Count * s.Quantity;
                d.Box(0, 0, DetailWidth - 3, height - 4, have < need ? Red * .04f : Lavender * .018f);
                if (hover && entry != null) d.Frame(0, 0, DetailWidth - 3, height - 4, Lavender * .35f);
                for (int i = 0; i < mats.Length; i++) d.Image(mats[i].IconPath, 2 + i * 12, 5 + i * 5, mats.Length == 1 ? 30 : 23, mats.Length == 1 ? 30 : 23, Color.White);
                Label(d, name, 40, 5, 11, entry != null ? Lavender : Ink);
                if (entry != null) d.Line(new(40, 20), new(40 + measure.Measure(name, 11), 20), Lavender * .35f);
                for (int i = 0; i < sourceLines.Length; i++) Label(d, sourceLines[i], 40, 22 + i * 14, 9, Muted * .85f);
                Label(d, $"{have} / {need}", DetailWidth - 8, 13, 10, have < need ? Red : Muted, 1);
            }, entry));
        }
        return rows;
    }

    internal static string CraftLabel(AlchemyNotebookState s, bool brewing)
        => brewing ? "炼制中…" : s.IsBusy ? "处理中…" : !s.IsUnlocked(s.Current) ? "研究"
            : !s.Current.Craftable ? s.Current.Stage == "商人" ? "商人售卖" : s.Current.Stage == "旅商" ? "旅商固定售卖" : "以太滴管取得"
            : s.MaxBatches(s.Current) < 1 ? "素材不足" : $"{(s.Category == "food" ? "制作" : "炼制")}  × {s.Current.Yield * s.Quantity}";

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

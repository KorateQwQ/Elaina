using System;
using System.Collections.Generic;
using System.Linq;

namespace 伊蕾娜.ElainaModAlchemy.UI;

// Shared card geometry for the native SUI catalog and the offline preview.
internal static class AlchemyCatalogLayout
{
    internal const int Columns = 5, CardWidth = 118, CardHeight = 132;
    internal const int GapX = 15, GapY = 24, ColumnStep = CardWidth + GapX, RowStep = CardHeight + GapY, GroupGap = 24;
    // Room inside the clipped viewport for the seal above each card, the tilt, the pickup lift
    // and scale with the motes rising from the seal, and the paper shadow.
    internal const int Left = 39, Top = 34, Bottom = 16;
    internal sealed record Slot(AlchemyCatalogRecipe Recipe, int X, int Y);
    internal static readonly IReadOnlyList<Slot> Slots = Build();
    internal static int Height => Slots.Max(s => s.Y) + CardHeight + Bottom;

    internal static IReadOnlyList<Slot> For(string category)
    {
        var recipes = (category == "all" ? AlchemyCatalog.Recipes : AlchemyCatalog.Recipes.Where(r => r.Category == category)).ToArray();
        return recipes.Select((recipe, i) => new Slot(recipe, Left + i % Columns * ColumnStep, Top + i / Columns * RowStep)).ToArray();
    }

    internal static int HeightFor(string category)
    {
        var slots = For(category);
        return slots.Count == 0 ? 0 : slots.Max(s => s.Y) + CardHeight + Bottom;
    }

    private static IReadOnlyList<Slot> Build()
    {
        var slots = new List<Slot>();
        int top = Top;
        foreach (var category in AlchemyCatalog.Categories)
        {
            var recipes = AlchemyCatalog.Recipes.Where(r => r.Category == category.Id).ToArray();
            for (int i = 0; i < recipes.Length; i++)
                slots.Add(new(recipes[i], Left + i % Columns * ColumnStep, top + i / Columns * RowStep));
            if (recipes.Length > 0)
                top += (recipes.Length - 1) / Columns * RowStep + CardHeight + GroupGap;
        }
        return slots;
    }

    internal static float CategoryOffset(string category, float viewportHeight)
        => 0;

    internal static float Reveal(string category, string id, float offset, float viewportHeight)
    {
        var slots = For(category);
        var slot = slots.First(s => s.Recipe.Id == id);
        int height = HeightFor(category);
        if (slot.Y - Top < offset) offset = slot.Y - Top;
        else if (slot.Y + CardHeight + Bottom > offset + viewportHeight) offset = slot.Y + CardHeight + Bottom - viewportHeight;
        return Math.Clamp(offset, 0, Math.Max(0, height - viewportHeight));
    }

    internal static float Reveal(string id, float offset, float viewportHeight)
        => Reveal("all", id, offset, viewportHeight);

    private static float Clamp(float offset, float viewportHeight)
        => Math.Clamp(offset, 0, Math.Max(0, Height - viewportHeight));

    internal static string Move(string id, int horizontal, int vertical)
        => Move("all", id, horizontal, vertical);

    internal static string Move(string category, string id, int horizontal, int vertical)
    {
        var slots = For(category);
        int index = slots.ToList().FindIndex(s => s.Recipe.Id == id);
        if (horizontal != 0) return slots[Math.Clamp(index + horizontal, 0, slots.Count - 1)].Recipe.Id;
        var current = slots[index];
        var rows = slots.Where(s => vertical > 0 ? s.Y > current.Y : s.Y < current.Y).ToArray();
        if (rows.Length == 0) return id;
        int y = vertical > 0 ? rows.Min(s => s.Y) : rows.Max(s => s.Y);
        return rows.Where(s => s.Y == y).OrderBy(s => Math.Abs(s.X - current.X)).First().Recipe.Id;
    }
}

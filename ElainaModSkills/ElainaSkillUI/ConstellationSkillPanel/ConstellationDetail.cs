using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KL.SkillSystem;
using Microsoft.Xna.Framework;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel.ConstellationDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

internal sealed record ConstellationDetailAction(Rectangle Area, string Kind, string Id, string Tooltip, bool Enabled = true);
internal sealed record ConstellationDetailRow(float Height, Action<ConstellationDrawing, string> Paint, ConstellationDetailAction[] Actions);
// One pill of the action bar; `Tail` is the slot key, or the study cost when `StudyCost` is set.
internal sealed record ConstellationPill(string Kind, string Label, string Tail, bool StudyCost, bool Gold, bool Enabled, string Tooltip);

// Right column of Tools/SkillPanelRedesign/index.html: text sits directly on the page and every
// section is split by the page's dotted rule. The scrolling body is rows; the requirement footer
// and the action pills stay pinned above the bottom rule.
internal sealed class ConstellationDetail
{
    internal const int Left = 776, Width = 276;
    internal const float ActionTop = 670, ActionHeight = 36;
    private const float FooterRowGap = 9, FooterRowHeight = 18, FooterLack = 18, MaxFooterRows = 4;
    internal readonly List<ConstellationDetailRow> Rows = [];
    internal float Top => 303;
    internal bool Owned { get; }
    internal bool Maxed { get; }
    internal int Level { get; }
    internal int Max { get; }
    internal bool HasActions { get; }
    // Learn / upgrade cost shown in the footer; null when maxed, hidden or nothing is selected.
    internal ConstellationRequirements Requirements { get; }
    internal float FooterHeight { get; }
    internal float FooterTop => (HasActions ? 641 : 708) - FooterHeight;
    internal float Bottom => FooterHeight > 0 ? FooterTop - 13 : FooterTop;
    internal float Height => Bottom - Top;
    internal float ContentHeight => Rows.Sum(r => r.Height);
    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static bool Latin(string unit) => !string.IsNullOrEmpty(unit) && unit.All(c => c is >= ' ' and <= '~');

    internal ConstellationDetail(ConstellationState state, ConstellationDrawing drawing)
    {
        var node = state.Current;
        HasActions = Pills(state).Length > 0;
        if (node == null || state.EmptyFilter) return;
        Owned = state.Learned.Contains(node.id);
        Level = state.Level(node.id);
        Max = state.MaxLevel(node.id) ?? 1;
        Maxed = Owned && Level >= Max;
        if (!state.Discovered(node)) { Paragraph(LocalizeUI("Hidden"), DetailText); return; }
        if (!Maxed) Requirements = state.Requirements(node.id, Owned);
        FooterHeight = Maxed ? 12 : 13 + Requirements.Rows.Take((int)MaxFooterRows).Sum(r => FooterRowGap + FooterRowHeight
            + (r.Met || r.Custom ? 0 : FooterLack)) + (Requirements.Rows.Count == 0 ? FooterRowGap + FooterRowHeight : 0);
        var growth = Owned && !Maxed ? node.ModSkill.GetConstellationUpgradePreview(Level + 1) ?? [] : [];

        if (node.Active)
        {
            string mana = node.ModSkill.MagicPointCost <= 0 ? null : node.ModSkill.MagicPointCost.ToString("0.##");
            string cooldown = node.Skill.MaxCD <= 0 ? null : Number(node.Skill.MaxCD);
            const float second = 159.5f;
            Add(44, (d, _) =>
            {
                Stat(d, 0, LocalizeUI("MagicPower"), mana, "MP");
                d.Dots(138, 2, 138, 42, DotColor);
                Stat(d, second, LocalizeUI("Cooldown"), cooldown, LocalizeUI("Seconds"));
            });
            DotRule();
        }
        string description = node.ModSkill.GetConstellationDescription();
        if (!string.IsNullOrWhiteSpace(description))
        {
            Heading(LocalizeUI("Effect"));
            var paragraph = new ConstellationRichText(description, Width, 13, 24, Ink * .86f);
            Add(paragraph.Height + 4, (d, _) => paragraph.Draw(d));
        }
        foreach (var (label, value) in node.ModSkill.GetConstellationStats(Math.Max(1, Level)) ?? [])
            Add(28, (d, _) =>
            {
                d.FittedText(label, 0, 7, 130, 12, new Color(214, 200, 224), spacing: 1.5f);
                d.FittedText(value, Width, 6, 136, 13, Ink, 1);
            });

        if (node.toggleable)
        {
            DotRule();
            bool on = Owned && !state.Disabled.Contains(node.id);
            Add(30, (d, hover) =>
            {
                d.Text(!Owned ? LocalizeUI("NotLearned") : on ? LocalizeUI("Enabled") : LocalizeUI("Disabled"), 0, 5, 11, Muted2, spacing: 1.5f);
                d.Toggle(node.id, Width - 37, 2, on, Owned, hover == "toggle");
            }, new ConstellationDetailAction(new Rectangle(Width - 39, 0, 39, 26), "toggle", node.id, on ? LocalizeUI("Disable") : LocalizeUI("Enable"), Owned));
            Relations(LocalizeUI("Related"), state.Affected(node.id).Where(state.Discovered).Select(s => s.id));
        }
        if (Owned && !state.Availability(node.id).Ok)
            Paragraph(state.Availability(node.id).Reason, new Color(214, 177, 155));

        if (growth.Length > 0)
        {
            DotRule();
            Add(22, (d, _) =>
            {
                HeadingText(d, LocalizeUI("NextLevel"));
                // "LV. n → LV. n+1", right-aligned; the new level is mint like the new values.
                string from = Level.ToString(), to = (Level + 1).ToString();
                float x = Width - d.LatinWidth(to, 12);
                d.LatinText(to, x, 0, 12, Mint);
                x -= 3 + d.LatinWidth("LV.", 8, 1);
                d.LatinText("LV.", x, 3.5f, 8, Muted, spacing: 1);
                x -= 6 + d.Measure("→", 12);
                d.Text("→", x, -1, 12, Muted * .7f);
                x -= 6 + d.LatinWidth(from, 12);
                d.LatinText(from, x, 0, 12, Muted);
                x -= 3 + d.LatinWidth("LV.", 8, 1);
                d.LatinText("LV.", x, 3.5f, 8, Muted, spacing: 1);
            });
            for (int i = 0; i < growth.Length; i++)
            {
                var (label, current, next, unit) = growth[i];
                bool first = i == 0;
                Add(28, (d, _) =>
                {
                    if (!first) d.Line(new Vector2(0, .5f), new Vector2(Width, .5f), new Color(236, 229, 247) * .06f);
                    float x = Width - Value(d, next ?? "--", unit, 20, Mint, Width, true);
                    x -= 6 + d.Measure("→", 12);
                    d.Text("→", x, 9, 12, Muted * .7f);
                    x -= 6;
                    x -= Value(d, current ?? "--", unit, 17, Muted, x, true);
                    d.FittedText(label, 0, 7, Math.Max(40, x - 10), 12, new Color(214, 200, 224), spacing: 1.5f);
                });
            }
        }
        if (node.prereqs.Length > 0)
        {
            DotRule();
            Relations(LocalizeUI("Prerequisites"), node.prereqs);
        }

        void Add(float height, Action<ConstellationDrawing, string> paint, params ConstellationDetailAction[] actions)
            => Rows.Add(new(height, paint, actions));
        // The page's dotted section rule: 13px above and below a 3px dot row.
        void DotRule()
        {
            if (Rows.Count > 0) Add(29, (d, _) => d.Dots(0, 14.5f, Width, 14.5f, DotColor));
        }
        void Heading(string text) => Add(21, (d, _) => HeadingText(d, text));
        void Paragraph(string text, Color color)
        {
            var lines = drawing.WrapParagraph(text ?? "", Width, 12);
            Add(lines.Length * 20 + 6, (d, _) =>
            {
                for (int i = 0; i < lines.Length; i++) d.Text(lines[i], 0, i * 20, 12, color);
            });
        }
        void Relations(string label, IEnumerable<string> ids)
        {
            var related = ids.Distinct().ToArray();
            if (related.Length == 0) return;
            int relationLeft = (int)Math.Max(32, drawing.Measure(label, 10, 2) + 12);
            int columns = Math.Max(1, (Width - relationLeft) / 44);
            var actions = related.Select((id, i) => new ConstellationDetailAction(
                new Rectangle(relationLeft + i % columns * 44, i / columns * 44, 36, 36), "relation", id,
                state.Find(id) is { } other ? state.Name(other) + " · " + (state.Availability(id).Ok ? LocalizeUI("Active") : state.Availability(id).Reason)
                    : LocalizeUI("MissingPrerequisite"), state.Find(id) != null)).ToArray();
            Add((related.Length + columns - 1) / columns * 44, (d, hover) =>
            {
                d.Text(label, 0, 12, 10, Muted2, spacing: 2);
                foreach (var action in actions)
                {
                    var other = state.Find(action.Id);
                    var center = new Vector2(action.Area.Center.X, action.Area.Center.Y);
                    if (other == null) { d.Icon("star", center, 22, Muted); continue; }
                    bool revealed = state.Discovered(other), met = state.Availability(other.id).Ok;
                    Color frame = hover == other.id ? new Color(212, 188, 229) : new Color(166, 138, 191) * (72 / 255f);
                    if (other.Active) d.Frame(action.Area.X + .5f, action.Area.Y + .5f, 35, 35, frame);
                    else { d.Corners(center, 40, 8, frame); d.Corners(center, 34, 3, frame * .55f); }
                    d.SkillIcon(other, center, 30, Color.White * (met ? 1 : .42f), !revealed, !met);
                    d.CrossStar(center + new Vector2(18), new Vector2(6), met ? new Color(223, 201, 239) : new Color(160, 131, 181), .5f, met);
                }
            }, actions);
        }
    }

    private static void HeadingText(ConstellationDrawing d, string text)
    {
        d.Spark(new Vector2(4, 7), 8, Lavender);
        d.Text(text, 14, 0, 11, Heading, spacing: 2);
    }

    // A number and its unit: Latin units follow the number in Gelasio, CJK units are sans.
    // Returns the drawn width; `right` aligns the group's right edge to `x`.
    private static float Value(ConstellationDrawing d, string value, string unit, float size, Color color, float x, bool right)
    {
        bool latin = Latin(unit);
        float unitSize = latin ? size * .65f : 10;
        float unitWidth = string.IsNullOrEmpty(unit) ? 0 : (latin ? 1 : 3) + (latin ? d.LatinWidth(unit, unitSize) : d.Measure(unit, unitSize, 1));
        float width = d.LatinWidth(value, size) + unitWidth;
        float left = right ? x - width : x, y = 14 - size * .5f;
        d.LatinText(value, left, y, size, color);
        if (unitWidth > 0)
        {
            float unitLeft = left + d.LatinWidth(value, size) + (latin ? 1 : 3);
            if (latin) d.LatinText(unit, unitLeft, y + (size - unitSize) * .78f, unitSize, color * .8f);
            else d.Text(unit, unitLeft, y + size * .78f - unitSize * .95f, unitSize, color * .8f, spacing: 1);
        }
        return width;
    }

    private static void Stat(ConstellationDrawing d, float x, string label, string value, string unit)
    {
        d.Text(label, x, 0, 10, Muted2, spacing: 2);
        if (value == null) { d.LatinText("—", x, 17, 24, Faint); return; }
        d.LatinText(value, x, 17, 24, Ink);
        float unitLeft = x + d.LatinWidth(value, 24) + 5;
        if (Latin(unit)) d.LatinText(unit, unitLeft, 28.5f, 11, Muted, spacing: .5f);
        else d.Text(unit, unitLeft, 28, 10, Muted, spacing: 1);
    }

    // Pinned above the action bar, in panel design coordinates.
    internal void DrawFooter(ConstellationDrawing d)
    {
        if (FooterHeight <= 0) return;
        float x = Left, y = FooterTop;
        if (Maxed)
        {
            d.Spark(new Vector2(x + 4, y + 6), 8, Gold);
            d.Text(LocalizeUI("MaxLevelReached"), x + 14, y, 10, Muted2, spacing: 2);
            return;
        }
        d.Spark(new Vector2(x + 4, y + 6.5f), 8, Lavender);
        d.Text(Owned ? LocalizeUI("UpgradeRequirements") : LocalizeUI("LearnRequirements"), x + 14, y, 10, Muted2, spacing: 2);
        if (Owned)
        {
            string level = $"LV. {Level + 1}";
            float levelLeft = x + Width - d.LatinWidth(level, 10, 1);
            d.LatinText(level, levelLeft, y + .5f, 10, Muted2, spacing: 1);
            d.Text(LocalizeUI("AfterUpgrade"), levelLeft - 5, y, 10, Muted2, 1, 2);
        }
        else d.Text(LocalizeUI("UnlockAfterLearning"), x + Width, y, 10, Muted2, 1, 2);
        y += 13;
        if (Requirements.Rows.Count == 0)
        {
            y += FooterRowGap;
            d.Text(LocalizeUI("NoCost"), x, y + 3, 12, new Color(214, 200, 224), spacing: 1);
            return;
        }
        foreach (var row in Requirements.Rows.Take((int)MaxFooterRows))
        {
            y += FooterRowGap;
            float nameLeft = x;
            if (row.Points)
            {
                d.Image("StudyBook", x, y + 1.5f, 15, 15, Gold);
                nameLeft += 23;
            }
            float valueWidth = 0;
            if (row.Custom)
            {
                if (!row.Met) valueWidth = d.Measure(LocalizeUI("ConditionsNotMet"), 10, 1.5f);
                if (!row.Met) d.Text(LocalizeUI("ConditionsNotMet"), x + Width, y + 4, 10, Shortfall, 1, 1.5f);
            }
            else
            {
                string needed = $" / {row.Needed}", owned = row.Owned.ToString();
                float neededWidth = d.LatinWidth(needed, 15);
                d.LatinText(needed, x + Width - neededWidth, y + 2.5f, 15, Muted);
                d.LatinText(owned, x + Width - neededWidth, y, 18, row.Met ? Gold : Shortfall, 1);
                valueWidth = neededWidth + d.LatinWidth(owned, 18);
            }
            // Long item names shrink a little, then end in an ellipsis; the learn tooltip has them in full.
            float room = Math.Max(40, x + Width - valueWidth - 10 - nameLeft);
            string name = row.Name;
            if (d.Measure(name, 12, 1) * .85f > room)
            {
                while (name.Length > 1 && d.Measure(name + "…", 12, 1) * .85f > room) name = name[..^1];
                name += "…";
            }
            d.FittedText(name, nameLeft, y + 2, room, 12, new Color(214, 200, 224), spacing: 1);
            y += FooterRowHeight;
            if (!row.Met && !row.Custom)
            {
                d.Text(LocalizeUI("MissingCount", Math.Max(0, row.Needed - row.Owned)), x + Width, y + 6, 10, Shortfall, 1, 1.5f);
                y += FooterLack;
            }
        }
    }

    internal static Rectangle PillArea(ConstellationPill pill)
    {
        const int gap = 10, width = (Width - gap) / 2;
        // Keep loadout actions on the right even after the upgrade action disappears at max level.
        int slot = pill.Kind is "equip" or "unequip" ? 1 : 0;
        return new Rectangle(Left + slot * (width + gap), (int)ActionTop, width, (int)ActionHeight);
    }

    internal static int PillAt(ConstellationPill[] pills, Vector2 point)
    {
        for (int i = 0; i < pills.Length; i++)
            if (PillArea(pills[i]).Contains((int)point.X, (int)point.Y)) return i;
        return -1;
    }

    internal static ConstellationPill[] Pills(ConstellationState state)
    {
        var node = state.Current;
        if (node == null || state.EmptyFilter) return [];
        if (!state.Learned.Contains(node.id))
        {
            string status = state.Status(node);
            if (status != "ready") return [new("status", ConstellationState.StatusName(status), null, false, false, false, null)];
            var learn = state.Requirements(node.id, false);
            return [new("learn", LocalizeUI("Learn"), learn.PointCost > 0 ? learn.PointCost.ToString() : null, learn.PointCost > 0,
                true, state.CanUnlock, learn.ActionTooltip(LocalizeUI("Learn")))];
        }
        var pills = new List<ConstellationPill>();
        if (state.Level(node.id) < (state.MaxLevel(node.id) ?? 1))
        {
            var upgrade = state.Requirements(node.id, true);
            pills.Add(new("upgrade", LocalizeUI("Upgrade"), upgrade.PointCost > 0 ? upgrade.PointCost.ToString() : null,
                upgrade.PointCost > 0, false, state.CanUpgrade(node.id), upgrade.ActionTooltip(LocalizeUI("Upgrade"))));
        }
        if (node.Active)
        {
            string key = ConstellationState.SlotLabels[Math.Clamp(state.TargetSlot, 0, ConstellationState.SlotLabels.Length - 1)];
            if (state.CanUnequip) pills.Add(new("unequip", LocalizeUI("Unequip"), key, false, false, true, null));
            else if (state.CanEquip) pills.Add(new("equip", LocalizeUI("Equip"), key, false, true, true, null));
            else pills.Add(new("equip", LocalizeUI("CannotEquip"), null, false, true, false, null));
        }
        return [.. pills];
    }
}

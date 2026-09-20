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

internal sealed class ConstellationDetail
{
    internal const int Left = 776, Width = 276;
    internal readonly List<ConstellationDetailRow> Rows = [];
    internal bool Compact { get; }
    internal float Top => Compact ? 262 : 272;
    internal float Bottom { get; }
    internal float Height => Bottom - Top;
    internal float ContentHeight => Rows.Sum(r => r.Height);
    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    internal ConstellationDetail(ConstellationState state, ConstellationDrawing drawing)
    {
        var node = state.Current;
        bool owned = node != null && state.Learned.Contains(node.id);
        int level = node == null ? 0 : state.Level(node.id), max = node == null ? 1 : state.MaxLevel(node.id) ?? 1;
        bool maxed = owned && level >= max;
        var growth = owned && !maxed ? node.ModSkill.GetConstellationUpgradePreview(level + 1) ?? [] : [];
        Compact = growth.Length > 0;
        Bottom = owned && !node.Active ? 708 : 659;
        if (node == null || state.EmptyFilter) return;
        if (!state.Discovered(node)) { Paragraph("未显现", DetailText); return; }

        RichParagraph(node.ModSkill.GetConstellationDescription(), Compact ? 8 : 12);
        if (node.Active)
        {
            string mana = node.ModSkill.MagicPointCost < 0 ? "--" : node.ModSkill.MagicPointCost.ToString();
            string cooldown = node.Skill.MaxCD < 0 ? "--" : Number(node.Skill.MaxCD);
            float secondX = drawing.LatinWidth(mana, 23) + 5 + drawing.Measure("MP", 10) + 34;
            Add(Compact ? 59 : 75, (d, _) =>
            {
                Stat(d, 0, "魔力", mana, "MP"); Stat(d, secondX, "冷却", cooldown, "秒");
                d.Line(new Vector2(0, Compact ? 50 : 60), new Vector2(Width, Compact ? 50 : 60), DetailRule);
            });
        }
        foreach (var (label, value) in node.ModSkill.GetConstellationStats(Math.Max(1, level)) ?? [])
            Add(26, (d, _) =>
            {
                d.FittedText(label, 0, 4, 130, 11, DetailLabel);
                d.FittedText(value, Width, 2, 136, 13, DetailValue, 1);
            });

        if (node.toggleable)
        {
            bool on = owned && !state.Disabled.Contains(node.id);
            Add(36, (d, hover) =>
            {
                d.Text(!owned ? "尚未习得" : on ? "已启用" : "已关闭", 0, 6, 11, new Color(195, 176, 208));
                d.Toggle(node.id, Width - 37, 2, on, owned, hover == "toggle");
            }, new ConstellationDetailAction(new Rectangle(Width - 39, 0, 39, 26), "toggle", node.id, on ? "关闭" : "启用", owned));
            Relations("关联", state.Affected(node.id).Where(state.Discovered).Select(s => s.id));
            Rule(14, 14);
        }
        if (owned && !state.Availability(node.id).Ok)
            Paragraph(state.Availability(node.id).Reason, new Color(214, 177, 155));

        Add(Compact ? 42 : 48, (d, _) =>
        {
            d.Text("等级", 0, 7, 11, new Color(188, 167, 206));
            d.LatinText(level.ToString(), 34, 0, 20, new Color(232, 216, 246));
            d.LatinText("/ " + max, 38 + d.LatinWidth(level.ToString(), 20), 7, 12, new Color(148, 131, 164));
            if (maxed) d.Text("满级", Width, 7, 10, new Color(184, 166, 202), 1);
            float y = Compact ? 30 : 32;
            d.Box(0, y, Width, 3, new Color(183, 153, 206) * (32 / 255f));
            float progress = Math.Clamp(level / (float)max, 0, 1);
            if (progress > 0) d.Gradient(0, y, Width * progress, 3,
                maxed ? new Color(180, 157, 119) : new Color(157, 128, 181),
                maxed ? new Color(224, 201, 158) : new Color(212, 188, 233));
        });
        float nextWidth = growth.Select(g => drawing.LatinWidth(g.Next ?? "--", 14)
            + drawing.Measure(g.Unit, 9)).DefaultIfEmpty(0).Max();
        float nextLeft = Width - nextWidth;
        for (int i = 0; i < growth.Length; i++)
        {
            var (label, current, next, unit) = growth[i];
            Add(i == growth.Length - 1 ? 18 : Compact ? 21 : 24, (d, _) =>
            {
                d.FittedText(label, 0, 3, nextLeft - 64, 11, GrowthLabel);
                d.LatinText(current ?? "--", nextLeft - 30, 1, 14, new Color(182, 163, 194), 1);
                d.Text("→", nextLeft - 15, 1, 12, new Color(131, 113, 142), .5f);
                d.LatinText(next ?? "--", nextLeft, 1, 14, GrowthNext);
                d.Text(unit, nextLeft + d.LatinWidth(next ?? "--", 14), 6, 9, GrowthNext);
            });
        }
        if (growth.Length > 0) Add(Compact ? 8 : 12, (_, _) => { });

        if (owned)
        {
            var requirements = state.Requirements(node.id, true);
            bool enabled = state.CanUpgrade(node.id);
            long points = requirements.PointCost;
            Add(35, (d, hover) =>
            {
                float alpha = enabled ? 1 : .65f;
                d.UpgradeButton(node.id, enabled, hover is "upgrade" or "upgrade-pressed", hover == "upgrade-pressed");
                string label = maxed ? "已满级" : enabled ? "进修" : "进修 · " + requirements.BlockReason;
                d.FittedText(label, 10, 10, 188, 12, !maxed && !enabled ? new Color(236, 156, 157)
                    : new Color(228, 207, 239) * alpha, serif: true);
                if (maxed) return;
                if (points > 0)
                {
                    float valueWidth = d.LatinWidth(points.ToString(), 11);
                    d.Image("StudyBook", Width - 29 - valueWidth, 10, 15, 15, new Color(224, 201, 158) * alpha);
                    d.LatinText(points.ToString(), Width - 10, 11, 11, new Color(224, 201, 158) * alpha, 1);
                }
                else if (requirements.Rows.Count == 0) d.Text("无消耗", Width - 10, 12, 10, new Color(184, 166, 202) * alpha, 1);
            }, new ConstellationDetailAction(new Rectangle(0, 0, Width, 35), "upgrade", node.id,
                maxed ? "已满级" : requirements.ActionTooltip($"进修至 {level + 1} 级"), enabled));
            if (!maxed && requirements.Rows.Count > 0) RequirementsBlock("进修所需", requirements);
        }
        else RequirementsBlock("研习所需", state.Requirements(node.id, false));
        Rule(Compact ? 10 : 14, Compact ? 8 : 14);
        Relations("依赖", node.prereqs);

        void Add(float height, Action<ConstellationDrawing, string> paint, params ConstellationDetailAction[] actions)
            => Rows.Add(new(height, paint, actions));
        void Rule(float before, float after) => Add(before + 1 + after,
            (d, _) => d.Line(new Vector2(0, before), new Vector2(Width, before), DetailRule));
        void Paragraph(string text, Color color)
        {
            var lines = drawing.WrapParagraph(text ?? "", Width, 11);
            Add(lines.Length * 18 + 8, (d, _) =>
            {
                for (int i = 0; i < lines.Length; i++) d.Text(lines[i], 0, i * 18, 11, color);
            });
        }
        void RichParagraph(string text, float margin)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var paragraph = new ConstellationRichText(text, Width, 12, Compact ? 20 : 21, DetailText);
            Add(paragraph.Height + margin, (d, _) => paragraph.Draw(d));
        }
        void RequirementsBlock(string title, ConstellationRequirements requirements)
        {
            Add(26, (d, _) =>
            {
                d.Text(title, 0, 10, 10, DetailLabel);
                if (requirements.Rows.Any(r => !r.Custom)) d.Text("持有 / 所需", Width - 18, 10, 9, DetailLabel * .8f, 1);
            });
            if (requirements.Rows.Count == 0) { Paragraph("无消耗", DetailText); return; }
            foreach (var requirement in requirements.Rows)
            {
                string count = requirement.Custom ? "" : $"{requirement.Owned} / {requirement.Needed}";
                float countWidth = drawing.LatinWidth(count, 11);
                bool stacked = countWidth > Width - 120;
                var names = drawing.WrapParagraph(requirement.Name, stacked ? Width - 20 : Width - countWidth - 38, 11);
                float mainHeight = Math.Max(26, names.Length * 18 + 8);
                float height = mainHeight + (stacked ? 18 : 0) + (requirement.Met ? 0 : 18);
                Color statusColor = requirement.Met ? GrowthNext * .78f : new Color(236, 156, 157);
                Add(height + 5, (d, _) =>
                {
                    for (int i = 0; i < names.Length; i++) d.Text(names[i], 0, 6 + i * 18, 11, DetailText);
                    if (!requirement.Custom)
                        d.LatinText(count, Width - 18, stacked ? mainHeight : 6, 11, statusColor, 1);
                    if (requirement.Met)
                    {
                        d.Line(new Vector2(Width - 9, 12), new Vector2(Width - 6, 15), statusColor, .9f);
                        d.Line(new Vector2(Width - 6, 15), new Vector2(Width - 1, 9), statusColor, .9f);
                    }
                    else
                    {
                        d.Text("!", Width - 5, 6, 11, statusColor, .5f);
                        d.Text(requirement.Custom ? "条件未满足" : $"还缺 {Math.Max(0, requirement.Needed - requirement.Owned)}",
                            0, mainHeight + (stacked ? 18 : 0), 10, statusColor);
                    }
                    d.Line(new Vector2(0, height), new Vector2(Width, height), DetailRule * .4f);
                });
            }
        }
        void Relations(string label, IEnumerable<string> ids)
        {
            var related = ids.Distinct().ToArray();
            if (related.Length == 0) return;
            const int columns = 5;
            var actions = related.Select((id, i) => new ConstellationDetailAction(
                new Rectangle(32 + i % columns * 44, i / columns * 44, 36, 36), "relation", id,
                state.Find(id) is { } other ? state.Name(other) + " · " + (state.Availability(id).Ok ? "生效" : state.Availability(id).Reason)
                    : "前置技能尚未接入面板", state.Find(id) != null)).ToArray();
            Add((related.Length + columns - 1) / columns * 44 + (Compact ? -4 : 4), (d, hover) =>
            {
                d.Text(label, 0, 12, 10, DetailLabel);
                foreach (var action in actions)
                {
                    var other = state.Find(action.Id);
                    var center = new Vector2(action.Area.Center.X, action.Area.Center.Y);
                    if (other == null) { d.Icon("star", center, 22, Muted); continue; }
                    bool revealed = state.Discovered(other), met = state.Availability(other.id).Ok;
                    Color frame = hover == other.id ? new Color(212, 188, 229) : new Color(166, 138, 191) * (72 / 255f);
                    if (other.Active)
                    {
                        d.Box(action.Area.X, action.Area.Y, 36, 36, new Color(34, 29, 44) * (68 / 255f));
                        d.Frame(action.Area.X, action.Area.Y, 36, 36, frame);
                    }
                    else { d.Corners(center, 40, 8, frame); d.Corners(center, 34, 3, frame * .55f); }
                    d.SkillIcon(other, center, 30, Color.White * (met ? 1 : .42f), !revealed, !met);
                    d.CrossStar(center + new Vector2(18), new Vector2(6), met ? new Color(223, 201, 239) : new Color(160, 131, 181), .5f, met);
                }
            }, actions);
        }
    }

    private static void Stat(ConstellationDrawing d, float x, string label, string value, string unit)
    {
        d.Text(label, x, 0, 10, DetailLabel);
        d.LatinText(value, x, 17, 23, DetailValue);
        d.Text(unit, x + d.LatinWidth(value, 23) + 5, 30, 10, new Color(164, 145, 179));
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview.PreviewDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview;

// Measured content rows are shared by the SUI scroll container and the offline FNA preview.
// Their coordinates are local to each row, never absolute positions in the notebook.
internal sealed record PreviewDetailAction(Rectangle Area, string Kind, string Id, string Tooltip, bool Enabled = true);
internal sealed record PreviewDetailRow(float Height, Action<PreviewDrawing, string> Paint, PreviewDetailAction[] Actions);

internal sealed class PreviewDetail
{
    internal const int Left = 776, Width = 276;
    internal readonly List<PreviewDetailRow> Rows = [];
    internal float Top { get; private set; }
    internal float Bottom { get; private set; }
    internal float Height => Bottom - Top;
    internal float ContentHeight => Rows.Sum(r => r.Height);
    internal static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    private static readonly Color Warning = new(214, 177, 155);

    internal PreviewDetail(PreviewState state, PreviewDrawing drawing)
    {
        var s = state.Current;
        bool secret = !state.Discovered(s), owned = state.Learned.Contains(s.id);
        bool effect = !secret && s.effect != null;
        Top = effect ? 262 : 272;
        Bottom = owned && !s.Active ? 708 : 659;
        if (state.EmptyFilter) return;
        var stats = state.Stats(s.id);
        string description = secret ? s.hint : effect
            ? $"依次生成 {s.effect.projectiles} 枚飞弹，追击敌人，每枚造成 {Number(stats.Damage)} 点伤害。\n每次命中附加 1 层印记。累计 {s.effect.markThreshold} 层时引爆，造成 {Number(stats.BurstDamage)} 点范围伤害，并回复 {Number(stats.ManaReturn)} 点魔力。"
            : s.desc;
        Paragraph(description, 12, effect ? 20 : 21, effect ? 8 : 12, new Color(201, 187, 211));
        if (secret) return;
        Add(effect ? 56 : 72, (d, _) =>
        {
            void Stat(float x, string label, string value, string unit)
            {
                d.Text(label, x, 0, 10, Muted);
                d.Text(value, x, 18, 23, Ink, serif: true);
                d.Text(unit, x + d.Measure(value, 23, serif: true) + 5, 30, 10, Muted);
            }
            if (s.Active) { Stat(0, "魔力", Number(stats.Mana), "MP"); Stat(110, "冷却", Number(stats.Cooldown), "秒"); }
            else Stat(0, s.id == "origin" ? "魔力上限" : "魔力消耗",
                s.id == "origin" ? "+" + stats.MaxManaBonus : "−" + stats.Discount, "%");
            d.Line(new Vector2(0, effect ? 48 : 58), new Vector2(Width, effect ? 48 : 58), LineColor * .6f);
        });
        if (s.toggleable)
        {
            bool on = owned && !state.Disabled.Contains(s.id);
            Add(36, (d, hover) =>
            {
                d.Text(!owned ? "尚未习得" : on ? "已启用" : "已关闭", 0, 6, 11, Muted);
                float alpha = owned ? 1 : .4f;
                d.Box(Width - 27, 2, 16, 21, (on ? Lavender * .3f : new Color(31, 27, 40)) * alpha);
                d.Image("Disc", Width - 37, 2, 21, 21, (on ? Lavender * .3f : new Color(31, 27, 40)) * alpha);
                d.Image("Disc", Width - 21, 2, 21, 21, (on ? Lavender * .3f : new Color(31, 27, 40)) * alpha);
                d.Image("Disc", Width - 34 + (on ? 15 : 0), 5, 15, 15, (on ? Ink : Muted) * alpha);
                if (hover == "toggle") d.Corners(new Vector2(Width - 18, 12), 27, 4, Lavender * .5f);
            }, new PreviewDetailAction(new Rectangle(Width - 39, 0, 39, 26), "toggle", s.id, on ? "关闭心得" : "启用心得", owned));
            Relations("关联", state.Affected(s.id).Where(state.Discovered).Select(x => x.id), true);
            Rule(14);
        }
        var availability = state.Availability(s.id);
        if (owned && !availability.Ok && !s.toggleable)
        {
            string[] lines = drawing.WrapParagraph(availability.Reason, Width - 20, 11);
            float h = lines.Length * 18 + 12;
            Add(h + 12, (d, _) =>
            {
                d.Box(0, 0, Width, h, new Color(185, 141, 114) * .065f);
                d.Box(0, 0, 2, h, Warning);
                for (int i = 0; i < lines.Length; i++) d.Text(lines[i], 9, 6 + i * 18, 11, Warning);
            });
        }
        int level = state.Level(s.id), max = state.MaxLevel(s.id);
        bool maxed = owned && level == max;
        Add(owned && !maxed ? 48 : 43, (d, _) =>
        {
            d.Text("等级", 0, 6, 11, Muted);
            string text = level.ToString();
            d.Text(text, 34, 0, 20, Ink, serif: true);
            d.Text("/ " + max, 39 + d.Measure(text, 20, serif: true), 8, 12, Muted, serif: true);
            if (maxed) d.Text("满级", Width, 8, 10, Muted, 1);
            d.Box(0, 34, Width, 3, Lavender * .12f);
            d.Box(0, 34, Width * Math.Clamp((float)level / max, 0, 1), 3, maxed ? Gold : Lavender);
        });
        if (owned && !maxed)
        {
            var next = state.Stats(s.id, level + 1);
            if (effect)
            {
                Change("飞弹伤害", stats.Damage, next.Damage);
                Change("引爆伤害", stats.BurstDamage, next.BurstDamage);
                Change("回复魔力", stats.ManaReturn, next.ManaReturn, " MP");
            }
            if (s.Active) { Change("魔力", stats.Mana, next.Mana, " MP"); Change("冷却", stats.Cooldown, next.Cooldown, " 秒"); }
            else Change(s.id == "origin" ? "魔力上限" : "魔力消耗",
                s.id == "origin" ? stats.MaxManaBonus : -stats.Discount,
                s.id == "origin" ? next.MaxManaBonus : -next.Discount, "%", s.id == "origin" ? "+" : "");
            Add(effect ? 8 : 12, (_, _) => { });
        }
        if (owned)
        {
            int? cost = state.NextCost(s.id);
            bool enabled = cost.HasValue && state.Points >= cost.Value;
            Add(35, (d, hover) =>
            {
                d.Box(0, 0, Width, 35, Lavender * (enabled ? hover == "upgrade" ? .2f : .085f : .02f));
                d.Frame(0, 0, Width, 35, Lavender * (enabled ? .36f : .17f));
                d.Text(maxed ? "已满级" : "进修", 10, 10, 12, Ink * (enabled ? 1 : .5f), serif: true, spacing: 1);
                if (cost.HasValue)
                {
                    d.CrossStar(new Vector2(Width - 37, 18), new Vector2(9), Gold * (enabled ? 1 : .5f), .2f);
                    d.Text(cost.Value.ToString(), Width - 11, 11, 11, Gold * (enabled ? 1 : .5f), 1, serif: true);
                }
            }, new PreviewDetailAction(new Rectangle(0, 0, Width, 35), "upgrade", s.id,
                maxed ? "已满级" : $"进修至 {level + 1} 级，消耗 {cost} 星尘", enabled));
            if (cost.HasValue && state.Points < cost.Value)
            {
                Add(6, (_, _) => { });
                Paragraph($"还缺 {cost.Value - state.Points} 星尘", 10, 16, 0, Warning);
            }
        }
        Rule(effect ? 18 : 28);
        if (s.activationRequires.Length > 0) Relations("依赖", s.activationRequires, true);
        if (!owned) Relations("前置", s.prereqs.Except(s.activationRequires), false);

        void Add(float height, Action<PreviewDrawing, string> paint, params PreviewDetailAction[] actions)
            => Rows.Add(new(height, paint, actions));
        void Paragraph(string text, float fontSize, float lineHeight, float margin, Color color)
        {
            string[] lines = drawing.WrapParagraph(text, Width, fontSize);
            Add(lines.Length * lineHeight + margin, (d, _) =>
            {
                for (int i = 0; i < lines.Length; i++) d.Text(lines[i], 0, i * lineHeight, fontSize, color);
            });
        }
        void Rule(float height) => Add(height, (d, _) =>
            d.Line(new Vector2(0, height / 2), new Vector2(Width, height / 2), LineColor * .6f));
        void Change(string label, double current, double next, string unit = "", string prefix = "")
            => Add(effect ? 21 : 24, (d, _) =>
            {
                d.Text(label, 0, 3, 11, Muted);
                d.Text(prefix + Number(current), 161, 0, 14, Muted, 1, serif: true);
                d.Text("→", 180, 1, 12, Muted * .7f, .5f);
                string value = prefix + Number(next);
                float unitWidth = d.Measure(unit, 9);
                d.Text(value, Width - unitWidth, 0, 14, new Color(180, 211, 198), 1, serif: true);
                d.Text(unit, Width, 5, 9, new Color(180, 211, 198), 1);
            });
        void Relations(string label, IEnumerable<string> ids, bool effective)
        {
            string[] related = ids.ToArray();
            if (related.Length == 0) return;
            const int columns = 5;
            var actions = related.Select((id, i) =>
            {
                bool revealed = state.Discovered(state.Find(id));
                bool met = effective ? state.Availability(id).Ok : state.Learned.Contains(id);
                string text = !revealed ? "未显现" : state.Find(id).name + " · " + (effective ? met ? "生效" : "未生效" : PreviewState.StatusName(state.Status(state.Find(id))));
                return new PreviewDetailAction(new Rectangle(38 + i % columns * 44, i / columns * 44 + 2, 36, 36), "relation", id, text);
            }).ToArray();
            Add((related.Length + columns - 1) / columns * 44 + 4, (d, hover) =>
            {
                d.Text(label, 0, 14, 10, Muted);
                foreach (var action in actions)
                {
                    var skill = state.Find(action.Id);
                    bool revealed = state.Discovered(skill);
                    bool met = effective ? state.Availability(skill.id).Ok : state.Learned.Contains(skill.id);
                    var area = action.Area;
                    var center = new Vector2(area.Center.X, area.Center.Y);
                    Color frame = hover == action.Id ? Ink : Lavender * .45f;
                    if (!skill.Active && revealed)
                    {
                        d.Corners(center, 40, 8, frame);
                        d.Corners(center, 34, 3, frame * .5f);
                    }
                    else { d.Box(area.X, area.Y, 36, 36, new Color(34, 29, 44)); d.Frame(area.X, area.Y, 36, 36, frame); }
                    d.SkillIcon(skill, center, 30, Color.White * (met ? 1 : .42f), !revealed, !met);
                    d.CrossStar(new Vector2(area.Right - 1, area.Bottom - 1), new Vector2(6), met ? Ink : Muted, .5f, met);
                }
            }, actions);
        }
    }
}

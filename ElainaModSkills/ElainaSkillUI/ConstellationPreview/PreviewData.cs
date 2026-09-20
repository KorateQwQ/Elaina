using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview;

// Deliberately independent of Skill, ModPlayer persistence, inventory and networking.
internal sealed class PreviewSkill
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public string type { get; set; } = "";
    public string mana { get; set; } = "";
    public string cooldown { get; set; } = "";
    public string icon { get; set; } = "star";
    public float x { get; set; }
    public float y { get; set; }
    public int cost { get; set; }
    public string[] prereqs { get; set; } = [];
    public bool initial { get; set; }
    public bool secret { get; set; }
    public string[] discovery { get; set; } = [];
    public string hint { get; set; } = "";
    public string desc { get; set; } = "";
    public PreviewGrowth growth { get; set; } = new();
    public bool toggleable { get; set; }
    public string[] activationRequires { get; set; } = [];
    public PreviewEffect effect { get; set; }
    internal bool Active => cooldown != "被动" && int.TryParse(mana, out _);
}

internal sealed class PreviewGrowth
{
    public int[] costs { get; set; } = [];
    public double manaStep { get; set; }
    public double cooldownStep { get; set; }
}
internal sealed class PreviewScalingValue
{
    public double @base { get; set; }
    public double perLevel { get; set; }
    internal double At(int level) => @base + (level - 1) * perLevel;
}
internal sealed class PreviewEffect
{
    public int projectiles { get; set; }
    public int markThreshold { get; set; }
    public PreviewScalingValue damage { get; set; } = new();
    public PreviewScalingValue burstDamage { get; set; } = new();
    public PreviewScalingValue manaReturn { get; set; } = new();
}
internal readonly record struct PreviewAvailability(bool Ok, string Reason = "", string Blocker = null);
internal readonly record struct PreviewStats(double Mana, double Cooldown, int Discount, int MaxManaBonus,
    double Damage = 0, double BurstDamage = 0, double ManaReturn = 0);

internal sealed class PreviewState
{
    internal static readonly string[] SlotLabels = ["01", "02", "03", "04", "05", "06", "07", "08"];
    internal PreviewSkill[] Skills { get; }
    internal HashSet<string> Learned { get; } = [];
    internal Dictionary<string, int> Levels { get; } = [];
    internal HashSet<string> Disabled { get; } = [];
    internal string[] Slots { get; } = new string[SlotLabels.Length];
    internal int TargetSlot;
    internal int Points => 18 - Skills.Where(s => Learned.Contains(s.id))
        .Sum(s => s.cost + s.growth.costs.Take(Level(s.id) - 1).Sum());
    internal string Selected = "origin";
    internal string Filter = "learned";
    internal bool EmptyFilter => !Skills.Any(s => Status(s) == Filter);
    internal PreviewSkill Current => Skills.First(s => s.id == Selected);

    internal PreviewState(Mod mod)
    {
        Skills = JsonSerializer.Deserialize<PreviewSkill[]>(mod.GetFileBytes(
            "ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets/Skills.json"))
            ?? throw new InvalidOperationException("Missing constellation preview data.");
        Reset();
    }

    internal void Reset()
    {
        Learned.Clear();
        Levels.Clear();
        Disabled.Clear();
        foreach (var skill in Skills.Where(s => s.initial)) Learned.Add(skill.id);
        var seen = new HashSet<string>();
        for (int i = 0; i < Slots.Length; i++)
            if (Slots[i] is not string id || !Learned.Contains(id) || !Find(id).Active || !seen.Add(id)) Slots[i] = null;
        if (Slots.All(id => id == null)) Slots[0] = "ember";
        Selected = "origin";
        Filter = "learned";
    }

    internal bool Discovered(PreviewSkill s) => !s.secret || s.discovery.All(Learned.Contains);
    internal string Status(PreviewSkill s) => Learned.Contains(s.id) ? "learned"
        : !Discovered(s) ? "hidden" : s.prereqs.All(Learned.Contains) ? "ready" : "locked";
    internal string Name(PreviewSkill s) => Discovered(s) ? s.name : "未显现";
    internal bool CanUnlock => !EmptyFilter && Status(Current) == "ready" && Points >= Current.cost;
    internal PreviewSkill Find(string id) => Skills.First(s => s.id == id);

    internal bool Unlock()
    {
        if (!CanUnlock) return false;
        Learned.Add(Selected);
        Levels[Selected] = 1;
        Filter = "learned";
        return true;
    }

    internal bool CanEquip => !EmptyFilter && Learned.Contains(Selected) && Current.Active && Slots[TargetSlot] != Selected;

    internal int Level(string id) => Learned.Contains(id) ? Levels.GetValueOrDefault(id, 1) : 0;
    internal int MaxLevel(string id) => Find(id).growth.costs.Length + 1;
    internal int? NextCost(string id) => Level(id) > 0 && Level(id) < MaxLevel(id)
        ? Find(id).growth.costs[Level(id) - 1] : null;
    internal bool Upgrade(string id)
    {
        if (NextCost(id) is not int cost || Points < cost) return false;
        Levels[id] = Level(id) + 1;
        return true;
    }
    internal bool Toggle(string id)
    {
        if (!Find(id).toggleable || !Learned.Contains(id)) return false;
        if (!Disabled.Remove(id)) Disabled.Add(id);
        return true;
    }
    internal PreviewAvailability Availability(string id) => Availability(id, []);
    private PreviewAvailability Availability(string id, HashSet<string> visiting)
    {
        var skill = Skills.FirstOrDefault(s => s.id == id);
        if (skill == null || !Learned.Contains(id))
            return new(false, skill == null ? "技能不存在" : $"尚未习得「{skill.name}」", id);
        if (!visiting.Add(id)) return new(false, "生效依赖存在循环", id);
        if (skill.toggleable && Disabled.Contains(id)) return new(false, $"需开启「{skill.name}」", id);
        foreach (string parent in skill.activationRequires)
        {
            var state = Availability(parent, visiting);
            if (!state.Ok) return state;
        }
        visiting.Remove(id);
        return new(true);
    }
    internal IEnumerable<PreviewSkill> Affected(string id) => Skills.Where(s => s.id != id && DependsOn(s.id, id, []));
    private bool DependsOn(string id, string target, HashSet<string> visited)
        => visited.Add(id) && Find(id).activationRequires.Any(p => p == target || DependsOn(p, target, visited));

    internal PreviewStats Stats(string id, int? requestedLevel = null)
    {
        var s = Find(id);
        int level = Math.Clamp(requestedLevel ?? Math.Max(1, Level(id)), 1, MaxLevel(id));
        if (!s.Active) return new(0, 0, s.toggleable ? level * 2 : 0, id == "origin" ? 20 + (level - 1) * 5 : 0);
        int discount = Availability("sense").Ok ? Level("sense") * 2 : 0;
        double mana = Math.Max(1, Math.Ceiling((double.Parse(s.mana, CultureInfo.InvariantCulture)
            - (level - 1) * s.growth.manaStep) * (1 - discount / 100d)));
        // JS Math.round for these nonnegative preview cooldowns, avoiding banker's rounding.
        double cooldown = Math.Floor(double.Parse(s.cooldown, CultureInfo.InvariantCulture)
            * (1 - (level - 1) * s.growth.cooldownStep) * 10 + .5) / 10;
        return new(mana, cooldown, discount, 0, s.effect?.damage.At(level) ?? 0,
            s.effect?.burstDamage.At(level) ?? 0, s.effect?.manaReturn.At(level) ?? 0);
    }
    internal void Select(string id)
    {
        Selected = id;
        Filter = Status(Find(id));
    }
    internal void SelectFilter(string filter)
    {
        Filter = filter;
        if (Status(Current) != filter && Skills.FirstOrDefault(s => Status(s) == filter) is { } match) Selected = match.id;
    }

    internal bool Equip()
    {
        if (!CanEquip) return false;
        int previous = Array.IndexOf(Slots, Selected);
        if (previous >= 0) Slots[previous] = null;
        Slots[TargetSlot] = Selected;
        return true;
    }

    internal HashSet<string> Ancestors()
    {
        var result = new HashSet<string>();
        void Visit(string id)
        {
            foreach (string parent in Find(id).prereqs)
                if (result.Add(parent)) Visit(parent);
        }
        if (Discovered(Current)) Visit(Selected);
        return result;
    }

    internal static string StatusName(string status) => status switch
    {
        "learned" => "已习得", "ready" => "待研习", "hidden" => "未显现", _ => "待启封"
    };
}

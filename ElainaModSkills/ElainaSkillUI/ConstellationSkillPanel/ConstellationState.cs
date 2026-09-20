using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

internal sealed class ConstellationNode
{
    internal Skill Skill;
    internal ElainaSkill ModSkill => (ElainaSkill)Skill.ModSkill;
    internal string id => ModSkill.GetType().FullName;
    internal string name
    {
        get
        {
            string key = $"Mods.伊蕾娜.SkillInfo.SkillName.{ModSkill.GetType().Name}";
            return Language.Exists(key) ? Language.GetTextValue(key) : ModSkill.GetType().Name;
        }
    }
    internal float x, y;
    internal string[] prereqs => (ModSkill.PrerequisiteSkills ?? []).Where(t => t != null).Select(t => t.FullName).ToArray();
    internal bool Active => !ModSkill.IsPassiveSkill;
    internal bool toggleable => ModSkill.IsToggleable;
}

internal readonly record struct ConstellationAvailability(bool Ok, string Reason = "", string Blocker = null);

internal sealed class ConstellationState
{
    internal static readonly string[] SlotLabels = ["01", "02", "03", "04", "05", "06", "07", "08"];
    private readonly ElainaSkillModPlayer _player;
    private readonly Dictionary<string, ConstellationNode> _nodes;
    private readonly Dictionary<string, Skill> _templates;
    internal ConstellationNode[] Skills { get; }
    internal HashSet<string> Learned { get; } = [];
    internal HashSet<string> Disabled { get; } = [];
    internal string[] Slots { get; } = new string[SlotLabels.Length];
    internal int TargetSlot;
    internal int Points => _player.SkillPoint;
    internal string Selected;
    internal string Filter = "learned";
    internal bool EmptyFilter => !Skills.Any(s => Status(s) == Filter);
    internal ConstellationNode Current => Find(Selected);
    internal Vector2 WorldSize { get; private set; }
    internal bool IsCurrentPlayer => !Main.gameMenu && _player.Player == Main.LocalPlayer && _player.Player.active;

    internal ConstellationState(ElainaSkillModPlayer player)
    {
        _player = player;
        var entries = Skill.RegisterSkill.Values
            .Where(s => s?.ModSkill is ElainaSkill)
            .Select(s => (Skill: s, Info: s.ModSkill.GetType().GetCustomAttribute<SkillUIInfoAttribute>()))
            .Where(e => e.Info != null).OrderBy(e => e.Info.State).ThenBy(e => e.Info.Pixels)
            .ThenBy(e => e.Skill.ModSkill.GetType().FullName, StringComparer.Ordinal).ToArray();
        // Preserve stage ordering, compact unused stages, and keep labels clear of adjacent nodes.
        var stages = entries.Select(e => e.Info.State).Distinct().ToArray();
        Skills = entries.Select(e => new ConstellationNode
        {
            Skill = e.Skill,
            x = ((ElainaSkill)e.Skill.ModSkill).ConstellationPosition?.X ?? 85 + Math.Max(0, e.Info.Pixels),
            y = ((ElainaSkill)e.Skill.ModSkill).ConstellationPosition?.Y ?? 80 + Array.IndexOf(stages, e.Info.State) * 140
        }).ToArray();
        foreach (var row in Skills.GroupBy(s => s.y))
        {
            float previous = -100;
            foreach (var node in row.OrderBy(s => s.x))
            {
                if (node.ModSkill.ConstellationPosition == null) node.x = Math.Max(node.x, previous + 160);
                previous = node.x;
            }
        }
        UpdateWorldSize();
        _nodes = Skills.ToDictionary(s => s.id);
        _templates = Skills.ToDictionary(s => s.id, s => s.Skill);
        Refresh();
        Selected = Skills.FirstOrDefault(s => Learned.Contains(s.id))?.id ?? Skills.FirstOrDefault()?.id;
        if (Current != null) Filter = Status(Current);
    }

    internal void UpdateWorldSize() => WorldSize = new Vector2(
        Math.Max(727, Skills.Select(s => s.x + 100).DefaultIfEmpty(727).Max()),
        Math.Max(550, Skills.Select(s => s.y + 110).DefaultIfEmpty(550).Max()));

    internal void Refresh()
    {
        Learned.Clear();
        Disabled.Clear();
        foreach (var node in Skills)
        {
            if (_player.UnlockedSkill.TryGetValue(node.ModSkill.GetType().Name, out var owned)
                && owned?.ModSkill?.GetType() == node.ModSkill.GetType())
            {
                node.Skill = owned;
                Learned.Add(node.id);
                if (node.toggleable && !node.ModSkill.IsEnabled) Disabled.Add(node.id);
            }
            else node.Skill = _templates[node.id];
        }
        for (int i = 0; i < Slots.Length; i++)
            Slots[i] = i < _player.ActiveSkill.Count ? _player.ActiveSkill[i]?.ModSkill?.GetType().FullName : null;
    }

#if DEBUG
    internal bool DebugGrantStudyPoints()
    {
        if (!IsCurrentPlayer) return false;
        _player.SkillPoint = (int)Math.Min(int.MaxValue, (long)_player.SkillPoint + 100);
        return true;
    }

    internal bool DebugResetSkills()
    {
        if (!IsCurrentPlayer) return false;
        // Include skills without SkillUIInfo and any equipped instance not present in the learned map.
        var skills = _player.UnlockedSkill.Values.Concat(_player.ActiveSkill)
            .Where(s => s?.ModSkill != null).Distinct().ToArray();
        foreach (var skill in skills)
        {
            skill.SkillSlot = -1;
            _player.LockSkill(skill);
            skill.ModSkill.IsEnabled = false;
            skill.ModSkill.Level = 0;
            skill.BasicStatus = Skill.SKillBasicStatus.Lock;
            skill.CurrentCD = 0;
            skill.ModSkill.CooldownDuration = 0;
            skill.Stack = skill.MaxStack;
        }
        _player.UnlockedSkill.Clear();
        for (int i = 0; i < _player.ActiveSkill.Count; i++) _player.ActiveSkill[i] = null;
        ElainaSkillModPlayer.CurrentSkillIndex = -1;
        Refresh();
        TargetSlot = 0;
        Selected = Skills.FirstOrDefault()?.id;
        Filter = Status(Current);
        _player.OnSkillsUpdated();
        return true;
    }
#endif

    internal ConstellationNode Find(string id) => id != null ? _nodes.GetValueOrDefault(id) : null;
    internal int DetailStamp()
    {
        var hash = new HashCode();
        hash.Add(Selected); hash.Add(Filter); hash.Add(Points); hash.Add(CanUnlock);
        foreach (var node in Skills)
        {
            hash.Add(Status(node)); hash.Add(Level(node.id)); hash.Add(node.ModSkill.IsEnabled);
        }
        if (Current != null)
        {
            hash.Add(Current.ModSkill.GetConstellationDescription());
            hash.Add(Current.ModSkill.MagicPointCost); hash.Add(Current.Skill.MaxCD);
            hash.Add(MaxLevel(Selected)); hash.Add(CanUpgrade(Selected)); hash.Add(UnlockDescription);
            hash.Add(UpgradeCondition(Selected)?.GetDescription(_player, Current.ModSkill));
            foreach (var requirement in Requirements(Selected, IsLearned(Selected)).Rows) hash.Add(requirement);
            foreach (var stat in Current.ModSkill.GetConstellationStats(Math.Max(1, Level(Selected))) ?? []) hash.Add(stat);
            if (Level(Selected) > 0 && Level(Selected) < MaxLevel(Selected))
                foreach (var stat in Current.ModSkill.GetConstellationUpgradePreview(Level(Selected) + 1) ?? []) hash.Add(stat);
        }
        return hash.ToHashCode();
    }
    private Skill Resolve(string id) => _player.UnlockedSkill.Values.FirstOrDefault(s => s?.ModSkill?.GetType().FullName == id) ?? Find(id)?.Skill;
    private bool IsLearned(string id) => Resolve(id) is { } skill
        && _player.UnlockedSkill.TryGetValue(skill.ModSkill.GetType().Name, out var owned) && ReferenceEquals(skill, owned);
    internal bool Discovered(ConstellationNode node) => node != null && (IsLearned(node.id) || node.Skill.BasicStatus != Skill.SKillBasicStatus.Hide);
    internal string Status(ConstellationNode node) => node == null ? "hidden" : IsLearned(node.id) ? "learned"
        : !Discovered(node) ? "hidden" : node.prereqs.All(IsLearned) ? "ready" : "locked";
    internal string Name(ConstellationNode node) => Discovered(node) ? node.name : "未显现";
    internal bool CanUnlock => IsCurrentPlayer && !EmptyFilter && Status(Current) == "ready"
        && Requirements(Selected, false).Satisfied;
    internal string UnlockDescription => Current?.ModSkill.UnlockCondition == null ? "解锁条件待补充。"
        : ConstellationRequirements.Localize(Current.ModSkill.GetUnlockConditionDescription(_player));

    internal ConstellationRequirements Requirements(string id, bool upgrade)
    {
        var skill = Find(id)?.ModSkill;
        return ConstellationRequirements.Read(upgrade ? UpgradeCondition(id) : skill?.UnlockCondition, _player, skill);
    }

    internal bool Unlock()
    {
        if (!CanUnlock) return false;
        var node = Current;
        // Registered skills are templates shared across players. Only commit a fresh owned instance.
        var skill = Skill.NewSkill(node.ModSkill.GetType(), node.Skill.Mod);
        // Preserve dynamically updated requirements (e.g. boss-related materials) from the displayed definition.
        skill.ModSkill.UnlockCondition = node.ModSkill.UnlockCondition;
        if (!skill.ModSkill.TryUnlockSkill(_player)) return false;
        _player.UnlockSkill(skill);
        Refresh();
        Filter = "learned";
        _player.OnSkillsUpdated();
        return true;
    }

    internal int Level(string id) => IsLearned(id) ? Resolve(id).Level : 0;
    internal int? MaxLevel(string id) => Find(id)?.Skill.MaxLevel;
    internal SkillUnlockCondition UpgradeCondition(string id)
    {
        var node = Find(id);
        int level = Level(id);
        return node == null || level <= 0 || MaxLevel(id) is not int max || level >= max
            ? null : node.ModSkill.GetConstellationUpgradeCondition(level + 1) ?? SkillUnlockCondition.None;
    }
    internal bool CanUpgrade(string id) => IsCurrentPlayer && UpgradeCondition(id) != null && Requirements(id, true).Satisfied;
    internal bool Upgrade(string id)
    {
        Refresh();
        if (!CanUpgrade(id)) return false;
        var node = Find(id);
        if (UpgradeCondition(id)?.TryUnlock(_player, node.ModSkill) != true) return false;
        node.ModSkill.TryLevelUp();
        _player.OnSkillsUpdated();
        Refresh();
        return true;
    }
    internal bool Toggle(string id)
    {
        Refresh();
        var node = Find(id);
        if (!IsCurrentPlayer || node == null || !node.toggleable || !IsLearned(id)) return false;
        node.ModSkill.IsEnabled = !node.ModSkill.IsEnabled;
        Refresh();
        return true;
    }
    internal ConstellationAvailability Availability(string id) => Availability(id, []);
    private ConstellationAvailability Availability(string id, HashSet<string> visiting)
    {
        var skill = Resolve(id);
        if (skill?.ModSkill == null || !IsLearned(id)) return new(false, "尚未习得" + (Find(id) is { } node ? $"「{node.name}」" : "前置技能"), id);
        if (!visiting.Add(id)) return new(false, "技能依赖存在循环", id);
        if (skill.ModSkill.IsToggleable && !skill.ModSkill.IsEnabled) return new(false, $"需开启「{Find(id)?.name ?? skill.ModSkill.GetType().Name}」", id);
        foreach (var parent in skill.ModSkill.PrerequisiteSkills ?? [])
        {
            if (parent == null) continue;
            var result = Availability(parent.FullName, visiting);
            if (!result.Ok) return result;
        }
        visiting.Remove(id);
        return new(true);
    }
    internal IEnumerable<ConstellationNode> Affected(string id) => Skills.Where(s => s.id != id && DependsOn(s.id, id, []));
    private bool DependsOn(string id, string target, HashSet<string> visited)
        => visited.Add(id) && (Find(id)?.prereqs.Any(p => p == target || DependsOn(p, target, visited)) ?? false);

    internal bool CanEquip => IsCurrentPlayer && !EmptyFilter && Current is { Active: true }
        && IsLearned(Selected) && Current.ModSkill.CanDragInSkillPanel()
        && TargetSlot >= 0 && TargetSlot < _player.ActiveSkill.Count && TargetSlot < Slots.Length && Slots[TargetSlot] != Selected;
    internal bool Equip()
    {
        Refresh();
        if (!CanEquip) return false;
        var replaced = _player.ActiveSkill[TargetSlot];
        if (replaced != null) replaced.SkillSlot = -1;
        KLSkillManager.EquipSkill(_player.ActiveSkill, Current.Skill, TargetSlot);
        Refresh();
        return true;
    }
    internal bool ClearSlot()
    {
        if (!IsCurrentPlayer || TargetSlot < 0 || TargetSlot >= _player.ActiveSkill.Count || _player.ActiveSkill[TargetSlot] is not { } skill) return false;
        skill.SkillSlot = -1;
        KLSkillManager.UnEquipSkill(_player.ActiveSkill, TargetSlot);
        Refresh();
        return true;
    }
    internal void Select(string id)
    {
        if (Find(id) == null) return;
        Selected = id;
        Filter = Status(Current);
    }
    internal void SelectFilter(string filter)
    {
        Filter = filter;
        if (Status(Current) != filter && Skills.FirstOrDefault(s => Status(s) == filter) is { } match) Selected = match.id;
    }
    internal HashSet<string> Ancestors()
    {
        var result = new HashSet<string>();
        void Visit(string id)
        {
            foreach (string parent in Find(id)?.prereqs ?? [])
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

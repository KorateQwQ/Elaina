using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.System;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

/// <summary>本地玩家每 30 帧比对一次星图状态，把新显现、新启封的技能交给提示横幅。记录随角色保存。</summary>
public sealed class ConstellationDiscoveryPlayer : ModPlayer
{
    private const int Interval = 30;
    internal readonly ConstellationDiscoveryTracker Tracker = new();
    internal static readonly ConstellationToastQueue Toasts = new();
    private ConstellationState _state;
    private int _timer;

    public override void Initialize() => Tracker.Clear();

    public override void OnEnterWorld()
    {
        _state = null;
        _timer = 0;
        Toasts.Clear();
    }

    public override void PostUpdate()
    {
        if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer || Main.gameMenu) return;
        if (!Player.GetModPlayer<ElainaModplayer>().Elaina || ++_timer < Interval) return;
        _timer = 0;
        var skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();
        if (_state == null || !_state.IsCurrentPlayer) _state = new ConstellationState(skillPlayer) { Fresh = Tracker.Fresh };
        _state.Refresh();
        var events = Tracker.Observe(_state.Skills.Select(s => (s.id, _state.Status(s))));
        if (events.Count > 0 && !ConstellationSkillPanel.PanelShown) Toasts.Push(events);
    }

    public override void SaveData(TagCompound tag)
    {
        if (!Tracker.Initialized) return;
        tag["discoveryIds"] = Tracker.Known.Keys.ToList();
        tag["discoveryStates"] = Tracker.Known.Values.ToList();
        tag["discoveryFresh"] = Tracker.Fresh.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        Tracker.Clear();
        var ids = tag.GetList<string>("discoveryIds");
        var states = tag.GetList<string>("discoveryStates");
        if (ids.Count == 0 || ids.Count != states.Count) return;
        for (int i = 0; i < ids.Count; i++) Tracker.Known[ids[i]] = states[i];
        foreach (string id in tag.GetList<string>("discoveryFresh")) Tracker.Fresh.Add(id);
        Tracker.Initialized = true;
    }
}

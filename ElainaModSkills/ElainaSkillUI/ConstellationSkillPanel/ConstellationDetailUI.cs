using System;
using System.Linq;
using Microsoft.Xna.Framework;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

public sealed partial class ConstellationSkillPanel
{
    private float _detailLayoutScale = 1;
    private void InitializeDetailScroll()
    {
        _detailScroll = new SUIScrollView
        {
            Positioning = Positioning.Absolute, Border = 0, Padding = new Margin(0),
            Gap = new Size(0), BackgroundColor = Color.Transparent
        }.Join(this);
        _detailScroll.Mask.IndependentRenderTarget = false;
        _detailScroll.Mask.Border = 0;
        _detailScroll.Mask.Padding = new Margin(0);
        var content = _detailScroll.Container;
        content.FlexDirection = FlexDirection.Column;
        content.MainAlignment = MainAlignment.Start;
        content.FlexWrap = false;
        content.Gap = new Size(0);
        content.Border = 0;
        content.Padding = new Margin(0);
        content.SetHeight(0, 0);
        content.FitHeight = true;
        _detailScroll.ScrollBar.Positioning = Positioning.Absolute;
        _detailScroll.ScrollBar.BackgroundColor = Color.Transparent;
        _detailScroll.ScrollBar.BarColor = (ConstellationDrawing.Muted * .33f, ConstellationDrawing.Lavender * .6f);
        _detailScroll.ScrollBar.Border = 0;
        _detailScroll.ScrollBar.Padding = new Margin(0);
        // Even at the scroll boundary, do not hand this wheel input back to the game.
        _detailScroll.MouseWheel += (_, e) => e.LockScroll(_detailScroll);
    }

    private void InvalidateDetail(bool resetScroll)
    {
        _detailDirty = true;
        _resetDetailScroll |= resetScroll;
    }

    private void RefreshDetail()
    {
        _detailDirty = false;
        _detail = new ConstellationDetail(_state, _draw);
        // Reuse native views; only the measured presentation rows change on state transitions.
        while (_detailRows.Count < _detail.Rows.Count)
        {
            int index = _detailRows.Count;
            var view = new PaintView((v, _) =>
            {
                var row = _detailRows[index].Row;
                if (row == null) return;
                var mouse = (Main.MouseScreen - v.Bounds.Position) / _scale;
                var action = _book.IsOpen && v.IsMouseHovering
                    ? row.Actions.FirstOrDefault(a => a.Area.Contains(mouse.ToPoint())) : null;
                var oldOrigin = _draw.Origin;
                _draw.Origin = v.Bounds.Position;
                string visualAction = action == null ? null : action.Kind == "relation" ? action.Id : action.Kind;
                if (action is { Kind: "upgrade", Enabled: true } && Main.mouseLeft && !Editing) visualAction = "upgrade-pressed";
                row.Paint(_draw, visualAction);
                _draw.Origin = oldOrigin;
                if (action != null)
                {
                    var position = Design(v.Bounds.Position);
                    RequestTooltip(action.Tooltip, new Rectangle((int)position.X + action.Area.X, (int)position.Y + action.Area.Y,
                        action.Area.Width, action.Area.Height), action.Kind + ":" + action.Id);
                }
            })
            {
                Border = 0, Padding = new Margin(0), FlexShrink = 0,
                FitWidth = false, FitHeight = false
            }.Join(_detailScroll.Container);
            view.LeftMouseClick += (_, e) =>
            {
                if (!_book.IsOpen || _state == null || _state.EmptyFilter) return;
                var row = _detailRows[index].Row;
                var local = (e.MousePosition - view.Bounds.Position) / _scale;
                var action = row?.Actions.FirstOrDefault(a => a.Enabled && a.Area.Contains(local.ToPoint()));
                if (action == null) return;
                if (Editing && action.Kind != "relation") return;
                if (action.Kind == "relation") Select(action.Id);
                else if (action.Kind == "toggle" && _state.Toggle(action.Id))
                {
                    InvalidateDetail(false);
                    Notify(_state.Find(action.Id).name + (_state.Disabled.Contains(action.Id) ? " · 已关闭" : " · 已启用"));
                }
                else if (action.Kind == "upgrade")
                {
                    if (_state.Upgrade(action.Id))
                    {
                        InvalidateDetail(false);
                        Notify($"{_state.Find(action.Id).name} · Lv. {_state.Level(action.Id)}");
                    }
                }
            };
            _detailRows.Add((view, null));
        }
        for (int i = 0; i < _detailRows.Count; i++)
        {
            var view = _detailRows[i].View;
            var row = i < _detail.Rows.Count ? _detail.Rows[i] : null;
            view.Invalid = row == null;
            _detailRows[i] = (view, row);
        }
        LayoutDetail();
        if (_resetDetailScroll)
        {
            _detailScroll.ScrollBar.SetScrollPosition(Vector2.Zero);
            _resetDetailScroll = false;
        }
    }

    private void LayoutDetail()
    {
        if (_detail == null || _detailScroll == null) return;
        _detailScroll.Invalid = _state.EmptyFilter;
        _detailScroll.SetLeft(_origin.X + ConstellationDetail.Left * _scale, 0, 0);
        _detailScroll.SetTop(_origin.Y + _detail.Top * _scale, 0, 0);
        _detailScroll.SetSize(ConstellationDetail.Width * _scale, _detail.Height * _scale, 0, 0);
        _detailScroll.ScrollBar.SetLeft((ConstellationDetail.Width + 3) * _scale, 0, 0);
        _detailScroll.ScrollBar.SetSize(3 * _scale, _detail.Height * _scale, 0, 0);
        _detailScroll.ScrollBar.Invalid = _detail.ContentHeight <= _detail.Height;
        var scroll = _detailScroll.ScrollBar.CurrentScrollPosition * (_scale / _detailLayoutScale);
        _detailLayoutScale = _scale;
        _detailScroll.ScrollBar.SetVScrollRange(_detail.Height * _scale, _detail.ContentHeight * _scale);
        _detailScroll.ScrollBar.SetScrollPosition(Vector2.Clamp(scroll,
            Vector2.Zero, _detailScroll.ScrollBar.GetScrollRange()));
        foreach (var (view, row) in _detailRows)
            if (row != null) view.SetSize(ConstellationDetail.Width * _scale, row.Height * _scale, 0, 0);
    }
}

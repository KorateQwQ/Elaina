using static 伊蕾娜.Localization.UIText;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using KL.Drawing.Snippets;
using Terraria;
using 伊蕾娜.System.UI;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

public sealed partial class ConstellationSkillPanel
{
    private void InitializeDetailScroll()
    {
        _detailScroll = new ElasticPageScrollView
        {
            Positioning = Positioning.Absolute, Border = 0, Padding = new Margin(0),
            Gap = new Size(0), BackgroundColor = Color.Transparent,
            AcceptInput = () => Enabled && IsInteractable && _book.IsOpen &&
                _state?.IsCurrentPlayer == true && !_state.EmptyFilter,
            // Switches and related skill icons retain their click gestures.
            CanDrag = e => !_detailRows.Any(r => r.View == e.Source && r.Row != null &&
                r.Row.Actions.Any(a => a.Kind != "term" && a.Area.Contains(((e.MousePosition - r.View.Bounds.Position) / _scale).ToPoint())))
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
                var action = _book.IsOpen && !_detailScroll.Motion.Dragging && v.IsMouseHovering
                    ? row.Actions.FirstOrDefault(a => a.Area.Contains(mouse.ToPoint())) : null;
                var oldOrigin = _draw.Origin;
                _draw.Origin = v.Bounds.Position;
                string visualAction = action?.Kind switch
                {
                    null => null,
                    "relation" => action.Id,
                    "term" => "term:" + action.Id,
                    _ => action.Kind
                };
                row.Paint(_draw, visualAction);
                _draw.Origin = oldOrigin;
                if (action != null)
                {
                    RequestTooltip(action.Tooltip, ConstellationDetail.TooltipAnchor(action, Design(v.Bounds.Position)),
                        action.Kind + ":" + action.Id, action.Kind == "term" ? KLTerms.Find(action.Id) : null);
                }
            })
            {
                Border = 0, Padding = new Margin(0), FlexShrink = 0,
                FitWidth = false, FitHeight = false
            }.Join(_detailScroll.Container);
            view.LeftMouseClick += (_, e) =>
            {
                if (!_book.IsOpen || _state == null || _state.EmptyFilter || _detailScroll.Motion.DidDrag) return;
                var row = _detailRows[index].Row;
                var local = (e.MousePosition - view.Bounds.Position) / _scale;
                var action = row?.Actions.FirstOrDefault(a => a.Enabled && a.Area.Contains(local.ToPoint()));
                if (action == null) return;
                if (Editing && action.Kind != "relation") return;
                if (action.Kind == "relation") Select(action.Id);
                else if (action.Kind == "toggle" && _state.Toggle(action.Id))
                {
                    InvalidateDetail(false);
                    Notify(_state.Find(action.Id).name + (_state.Disabled.Contains(action.Id) ? LocalizeUI("DisabledSuffix") : LocalizeUI("EnabledSuffix")));
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
            _detailScroll.ResetPosition();
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
        _detailScroll.SetPageRange(_detail.Height, _detail.ContentHeight, _scale);
        foreach (var (view, row) in _detailRows)
            if (row != null) view.SetSize(ConstellationDetail.Width * _scale, row.Height * _scale, 0, 0);
    }
}

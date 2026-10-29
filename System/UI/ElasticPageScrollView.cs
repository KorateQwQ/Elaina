using System;
using Microsoft.Xna.Framework;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using Terraria;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

namespace 伊蕾娜.System.UI;

internal sealed class ElasticPageScrollView : SUIScrollView
{
    internal ElasticScrollMotion Motion { get; } = new();
    internal Func<bool> AcceptInput;
    internal Func<UIMouseEvent, bool> CanDrag;
    private float _scale = 1;
    private readonly ConstellationUIClock _clock = new();

    internal ElasticPageScrollView()
    {
        // The page owns its offset; SUI's bar would clamp it during Draw.
        ScrollBar.Invalid = true;
        ScrollBar.DisableMouseInteraction = true;
    }

    internal void SetPageRange(float viewport, float content, float scale)
    {
        if (_scale != scale) Motion.Reset(Motion.Position);
        _scale = scale;
        Motion.SetRange(content - viewport);
        ApplyPosition();
    }

    internal void ResetPosition(float position = 0)
    {
        Motion.Reset(position);
        _clock.Stop();
        ApplyPosition();
    }

    private void ApplyPosition() => Container.UpdateScrollPosition(new Vector2(0, Motion.Position * _scale));

    public override void OnMouseWheel(UIScrollWheelEvent evt)
    {
        if (evt.ScrollElement != null) return;
        evt.LockScroll(this);
        if (AcceptInput?.Invoke() == true) Motion.Wheel(-evt.ScrollDelta);
        // Do not invoke SUIScrollView's hard-clamped wheel handler.
        Parent?.OnMouseWheel(evt);
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        Motion.BeginDrag(evt.MousePosition.Y / _scale,
            AcceptInput?.Invoke() == true && CanDrag?.Invoke(evt) == true);
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        Motion.DragTo(evt.MousePosition.Y / _scale);
        Motion.EndDrag();
        base.OnLeftMouseUp(evt);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        // SUI calls this while drawing with Main.gameTimeCache. That cached
        // update interval can be zero or reused by several draws.
        bool active = Main.hasFocus && AcceptInput?.Invoke() == true;
        float seconds = _clock.Sample(active);
        if (!active) Motion.Reset(Motion.Position);
        else
        {
            if (Main.mouseLeft) Motion.DragTo(Main.MouseScreen.Y / _scale);
            else Motion.EndDrag();
            Motion.Advance(seconds);
        }
        ApplyPosition();
        if (Motion.Dragging) Main.LocalPlayer.mouseInterface = true;
    }
}

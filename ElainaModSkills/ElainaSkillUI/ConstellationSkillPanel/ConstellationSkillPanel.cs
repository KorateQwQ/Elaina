using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.System;
using 伊蕾娜.System.UI;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel.ConstellationDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Constellation Skill Panel", 1000)]
public sealed partial class ConstellationSkillPanel : BaseBody
{
    public override bool IsInteractable => !Main.gameMenu && (_book.Moving || Main.LocalPlayer.itemAnimation <= 0);
    private ConstellationState _state;
    private ConstellationDrawing _draw;
    private readonly List<(UIView View, Rectangle Area)> _regions = [];
    private readonly List<(UIView View, Func<bool> Visible)> _conditionalButtons = [];
    private UIElementGroup _viewport;
    private ElasticPageScrollView _detailScroll;
    private ConstellationDetail _detail;
    private readonly List<(PaintView View, ConstellationDetailRow Row)> _detailRows = [];
    private bool _detailDirty = true, _resetDetailScroll = true;
    private float _scale = 1, _zoom = .84f, _clock, _flash;
    private Vector2 _origin, _pan, _dragStart, _panStart, _lastSize;
    private bool _focusing;
    private float _focusElapsed, _focusStartZoom, _focusTargetZoom;
    private Vector2 _focusStartPan, _focusTargetPan;
    private bool _dragging, _moved, _inputSuspended;
    private string _pressedNode, _hoverNode, _toast, _tooltip;
    private string _tooltipKey;
    private Rectangle? _tooltipAnchor;
    private readonly ConstellationTooltipDelay _tooltipDelay = new();
    private float _toastLife, _lastBlankClick = -1;
    private float _refreshTimer;
    private readonly ConstellationUIClock _controlsClock = new();
    private readonly ConstellationButterflyMotion _butterfly = new();
    private readonly ConstellationNodeEffects _nodeFx = new();
    private readonly ConstellationSky _sky = new();
    private bool _controlsHadFocus;
    private int _detailStamp;
    private HashSet<string> _ancestors = [];
    private const float DesignWidth = 1100, DesignHeight = 800, ScreenMargin = 16;
    private const int MapX = 26, MapY = 208, MapWidth = 727, MapHeight = 473;
    // The visible sky reaches past the camera frame (Map*) up to the tab rule; its edges fade into the
    // page (SkyEdgeFade). Camera framing, focus and pan limits still use the Map* frame.
    private static readonly Rectangle Sky = new(26, 150, 727, 558);
    private const int WorldWidth = 727, WorldHeight = 550;
    private static readonly string[] SlotKeys = ConstellationState.SlotLabels;
    private const int SlotLeft = 99, SlotTop = 741, SlotSize = 42, SlotStride = 48;
    private const float SlotHoldSeconds = .3f;
    private int _slotPress = -1, _slotDropTarget = -1;
    private float _slotPressAt;
    private Vector2 _slotPressPoint;
    private bool _slotDragging, _slotClickHandled;

    public ConstellationSkillPanel() { Enabled = false; }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Border = 0;
        Padding = new Margin(0);
        BackgroundColor = new Color(33, 29, 46);
        FitWidth = false;
        FitHeight = false;
        FitWindowToScreen(GraphicsDeviceHelper.GetBackBufferSizeByUIScale());
        SetLeft(0, 0, .5f);
        SetTop(0, 0, .5f);
        var mod = ModContent.GetInstance<global::伊蕾娜.伊蕾娜>();
        _draw = new ConstellationDrawing(mod);
        Recenter();

        _viewport = new UIElementGroup
        {
            Positioning = Positioning.Absolute, OverflowHidden = true,
            IndependentRenderTarget = false, Border = 0, Padding = new Margin(0)
        }.Join(this);
        _regions.Add((_viewport, Sky));
        new PaintView((_, _) => DrawMap())
        {
            Width = new Dimension(0, 1), Height = new Dimension(0, 1), IgnoreMouseInteraction = true
        }.Join(_viewport);
        _viewport.LeftMouseDown += (_, e) =>
        {
            if (_state == null || !_book.IsOpen) return;
            _focusing = false;
            _dragStart = Design(e.MousePosition);
            _panStart = _pan;
            _pressedNode = HitNode(_dragStart);
            if (Editing && _state.Find(_pressedNode) is { } node)
            {
                _nodeDragStart = new Vector2(node.x, node.y);
                Select(node.id);
            }
            _dragging = true;
            _moved = false;
        };
        _viewport.LeftMouseUp += (_, e) => { if (_dragging) DragTo(Design(e.MousePosition)); _dragging = false; };
        _viewport.LeftMouseClick += (_, e) =>
        {
            if (_state == null || !_book.IsOpen || _moved) return;
            string hit = HitNode(Design(e.MousePosition));
            if (hit != null && hit == _pressedNode) Select(hit);
            else if (hit == null && _pressedNode == null)
            {
                if (_clock - _lastBlankClick < .32f) Recenter();
                _lastBlankClick = _clock;
            }
        };
        _viewport.MouseWheel += (_, e) =>
        {
            if (_state == null || !_book.IsOpen) return;
            ChangeZoom(Math.Sign(e.ScrollDelta) * .1f, Design(e.MousePosition));
            e.LockScroll(_viewport);
        };

        string[] filters = ["learned", "ready", "locked", "hidden"];
        for (int i = 0; i < filters.Length; i++)
        {
            string filter = filters[i];
            int index = i;
            Button(new Rectangle(26 + i * 180, 84, 180, 61), () =>
            {
                _state.SelectFilter(filter);
                _ancestors = _state.Ancestors();
                if (!_state.EmptyFilter) CenterOn(_state.Current);
                InvalidateDetail(true);
            }, hover => DrawFilter(index, filter, hover));
        }
        // The capsule behind the three zoom controls is drawn with the chrome (DrawZoomCapsule).
        Button(new Rectangle(575, 162, 32, 28), () => ChangeZoom(-.1f),
            h => CameraControl("minus", h, _zoom > .5f), dynamicTooltip: () => LocalizeUI("ZoomOut"));
        Button(new Rectangle(647, 162, 32, 28), () => ChangeZoom(.1f),
            h => CameraControl("plus", h, _zoom < 1.5f), dynamicTooltip: () => LocalizeUI("ZoomIn"));
        Button(new Rectangle(690, 162, 40, 28), Recenter,
            h => CameraControl("center", h, true), dynamicTooltip: () => LocalizeUI("CenterMap"));
        Button(new Rectangle(ConstellationDetail.Left, (int)ConstellationDetail.ActionTop, ConstellationDetail.Width,
            (int)ConstellationDetail.ActionHeight), PrimaryAction, DrawActionButton);
        InitializeDetailScroll();
        for (int i = 0; i < SlotKeys.Length; i++)
        {
            int slot = i;
            var view = Button(new Rectangle(SlotLeft + i * SlotStride, SlotTop, SlotSize, SlotSize), () => ClickSlot(slot),
                h => DrawSlot(slot, h));
            view.LeftMouseDown += (_, e) => PressSlot(slot, Design(e.MousePosition));
            view.LeftMouseUp += (_, e) => { if (_slotPress == slot) ReleaseSlot(Design(e.MousePosition)); };
        }
        Button(new Rectangle(1032, 21, 42, 42), Close, DrawCloseButton, dynamicTooltip: () => LocalizeUI("CloseMap"));
        InitializeLayoutEditor();

    }

    private PaintView Button(Rectangle area, Action click, Action<bool> paint, string tooltip = null, Func<bool> visible = null, Func<string> dynamicTooltip = null)
    {
        var view = new PaintView((v, _) =>
        {
            if (_state == null) return;
            bool hover = _book.IsOpen && v.IsMouseHovering;
            paint(hover);
            if (hover)
            {
                string text = dynamicTooltip?.Invoke() ?? tooltip;
                if (text != null) RequestTooltip(text, area, "button:" + area);
            }
        }) { Positioning = Positioning.Absolute }.Join(this);
        view.LeftMouseClick += (_, _) =>
        {
            if (!_book.IsOpen || _state?.IsCurrentPlayer != true || visible?.Invoke() == false) return;
            click();
            RefreshConditionalButtons();
        };
        _regions.Add((view, area));
        if (visible != null)
        {
            _conditionalButtons.Add((view, visible));
            view.Invalid = !visible();
        }
        return view;
    }

    private void ClickSlot(int slot)
    {
        // A finished drag also ends on a click; the drop already handled it.
        if (_slotClickHandled) { _slotClickHandled = false; return; }
        _state.TargetSlot = slot;
        if (_state.Find(_state.Slots[slot]) is { } node) Select(node.id);
    }

    private void PressSlot(int slot, Vector2 point)
    {
        _slotClickHandled = false;
        if (_state == null || !_book.IsOpen || Editing || _state.Slots[slot] == null) return;
        _slotPress = slot;
        _slotPressAt = _clock;
        _slotPressPoint = point;
        _slotDragging = false;
    }

    private void ReleaseSlot(Vector2 point)
    {
        int from = _slotPress, to = SlotAt(point);
        bool dragged = _slotDragging;
        CancelSlotDrag();
        // Dropping back onto the pressed slot still counts as a click.
        if (!dragged || to == from) return;
        _slotClickHandled = true;
        if (to < 0) return;
        if (_state.MoveSlot(from, to)) _state.TargetSlot = to;
    }

    private void UpdateSlotDrag(bool suspended)
    {
        if (_slotPress < 0) return;
        if (suspended) { CancelSlotDrag(); return; }
        // Polling covers a release that lands before or without the view's mouse-up event.
        Vector2 point = Design(Main.MouseScreen);
        if (!Main.mouseLeft) { ReleaseSlot(point); return; }
        if (!_slotDragging && (_clock - _slotPressAt >= SlotHoldSeconds || (point - _slotPressPoint).LengthSquared() > 36))
            _slotDragging = true;
        _slotDropTarget = _slotDragging ? SlotAt(point) : -1;
    }

    private void CancelSlotDrag()
    {
        _slotPress = _slotDropTarget = -1;
        _slotDragging = false;
    }

    private static int SlotAt(Vector2 point)
    {
        // Half the gap between slots counts toward each neighbour so drops never fall through.
        const float slack = (SlotStride - SlotSize) / 2f;
        if (point.Y < SlotTop - slack || point.Y > SlotTop + SlotSize + slack) return -1;
        int slot = (int)MathF.Floor((point.X - SlotLeft + slack) / SlotStride);
        return slot >= 0 && slot < SlotKeys.Length ? slot : -1;
    }

    private void RefreshConditionalButtons()
    {
        foreach (var (view, visible) in _conditionalButtons) view.Invalid = !visible();
    }

    private void RequestTooltip(string text, Rectangle? anchor, string key)
    {
        _tooltip = text; _tooltipAnchor = anchor; _tooltipKey = key;
    }

    private void FitWindowToScreen(Size screenSize)
    {
        float fit = Math.Min(1f, Math.Min(Math.Max(1, screenSize.Width - ScreenMargin * 2) / DesignWidth,
            Math.Max(1, screenSize.Height - ScreenMargin * 2) / DesignHeight));
        SetSize(DesignWidth * fit, DesignHeight * fit, 0, 0);
    }

    protected override void OnScreenSizeChanged(Size newScreenSize, Size oldScreenSize)
    {
        FitWindowToScreen(newScreenSize);
        base.OnScreenSizeChanged(newScreenSize, oldScreenSize);
    }

    protected override void OnExitTree()
    {
        CloseImmediately();
        _state = null;
        _layoutEditor = null;
        _ancestors.Clear();
        InvalidateDetail(true);
        base.OnExitTree();
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        if (_state == null || !_state.IsCurrentPlayer) { CloseImmediately(); return; }
        if (!Main.hasFocus) _book.Finish();
        else _book.Advance(_bookClock.Sample());
        if (!_book.Moving) _bookClock.Stop();
        if (_book.IsClosed) { CloseImmediately(); return; }
        RefreshConditionalButtons();
        base.UpdateStatus(gameTime);
        float delta = _controlsClock.Sample();
        // Consume unfocused time without applying it; the first focused frame resumes at the last pose.
        if (!Main.hasFocus || !_controlsHadFocus) delta = 0;
        _controlsHadFocus = Main.hasFocus;
        _draw.AdvanceToggleAnimations(delta);
        _sky.Advance(delta);
        _clock += delta;
        _flash = Math.Max(0, _flash - delta);
        _toastLife = Math.Max(0, _toastLife - delta);
        _refreshTimer -= delta;
        if (_refreshTimer <= 0)
        {
            _refreshTimer = .25f;
            _state.Refresh();
            _ancestors = _state.Ancestors();
            int stamp = _state.DetailStamp();
            if (_detailStamp != stamp) { _detailStamp = stamp; InvalidateDetail(false); }
        }
        Vector2 size = new(Width.Pixels, Height.Pixels);
        if (size != _lastSize)
        {
            _lastSize = size;
            _scale = Math.Min(size.X / DesignWidth, size.Y / DesignHeight);
            _origin = (size - new Vector2(DesignWidth, DesignHeight) * _scale) / 2;
            foreach (var (view, area) in _regions)
            {
                view.SetLeft(_origin.X + area.X * _scale, 0, 0);
                view.SetTop(_origin.Y + area.Y * _scale, 0, 0);
                view.SetSize(area.Width * _scale, area.Height * _scale, 0, 0);
            }
            LayoutDetail();
        }
        if (_detailDirty) RefreshDetail();
        if (Main.hasFocus && Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
        {
            if (Editing) CancelLayoutEdit();
            else Close();
        }
        bool suspended = !Main.hasFocus || !Enabled || !IsInteractable || !_book.IsOpen;
        if (suspended && !_inputSuspended) StopDragging(false);
        _inputSuspended = suspended;
        UpdateSlotDrag(suspended);
        if (!suspended && !_dragging) AdvanceCamera(delta);
        if (_dragging)
        {
            DragTo(Design(Main.MouseScreen));
            if (!Main.mouseLeft) _dragging = false;
        }
        _hoverNode = _book.IsOpen && _viewport.IsMouseHovering ? HitNode(Design(Main.MouseScreen)) : null;
        if (_book.IsOpen) UpdateButterfly(delta);
    }

    private void UpdateButterfly(float delta)
    {
        var node = _state.Current;
        bool visible = node != null && (Editing || !_state.EmptyFilter && _state.Status(node) == _state.Filter);
        _butterfly.SetTarget(visible ? new Vector2(node.x, node.y) : null);
        _butterfly.Advance(delta);
        _nodeFx.Advance(delta, _butterfly, visible ? node.id : null);
    }

    private Vector2 Design(Vector2 mouse) => (mouse - Bounds.Position - _origin) / _scale;
    private Vector2 MapPoint(float x, float y) => new Vector2(MapX, MapY) + _pan + new Vector2(x, y) * _zoom;
    private void DragTo(Vector2 p)
    {
        Vector2 delta = p - _dragStart;
        if (delta.LengthSquared() > 25) { _moved = true; _lastBlankClick = -1; }
        if (!_moved) return;
        if (Editing && _pressedNode != null)
        {
            _layoutEditor.Move(_pressedNode, _nodeDragStart, delta, _zoom,
                Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift));
        }
        else { _pan = _panStart + delta; ClampPan(); }
    }
    private void ClampPan() => _pan = Vector2.Clamp(_pan,
        new Vector2(150, 140) - (_state?.WorldSize ?? new Vector2(WorldWidth, WorldHeight)) * _zoom,
        new Vector2(MapWidth - 150, MapHeight - 140));
    private string HitNode(Vector2 p)
    {
        if (!Sky.Contains(p.ToPoint())) return null;
        // The selected node is drawn last and must also win hit testing when nodes overlap.
        foreach (var s in _state.Skills.Where(s => s.id != _state.Selected).Append(_state.Current).Reverse())
        {
            if (s == null) continue;
            float radius = (NodeRadius + 4) * _zoom;
            if (Vector2.DistanceSquared(p, MapPoint(s.x, s.y)) <= radius * radius) return s.id;
        }
        return null;
    }
    private void Select(string id)
    {
        bool changed = _state.Selected != id;
        _state.Select(id);
        _ancestors = _state.Ancestors();
        _flash = 0;
        InvalidateDetail(changed);
        CenterOn(_state.Current);
    }
    private void ChangeZoom(float amount, Vector2? anchor = null)
    {
        _focusing = false;
        var local = (anchor ?? new Vector2(MapX + MapWidth / 2f, MapY + MapHeight / 2f)) - new Vector2(MapX, MapY);
        var world = (local - _pan) / _zoom;
        _zoom = Math.Clamp(_zoom + amount, .5f, 1.5f);
        _pan = local - world * _zoom;
        ClampPan();
        _dragging = false;
    }
    private void Recenter()
    {
        _focusing = false;
        _zoom = .84f;
        _pan = new Vector2((MapWidth - (_state?.WorldSize.X ?? WorldWidth) * _zoom) / 2, 30);
        ClampPan();
        _dragging = false;
    }
    private void CenterOn(ConstellationNode s)
    {
        if (s == null || Editing) return;
        _focusElapsed = 0;
        _focusStartZoom = _zoom;
        _focusStartPan = _pan;
        _focusTargetZoom = Math.Max(_zoom, 1.2f);
        _focusTargetPan = new Vector2(MapWidth / 2f, MapHeight / 2f) - new Vector2(s.x, s.y) * _focusTargetZoom;
        _focusing = true;
    }
    private void AdvanceCamera(float delta)
    {
        if (!_focusing) return;
        _focusElapsed = Math.Min(.35f, _focusElapsed + Math.Max(0, delta));
        float t = _focusElapsed / .35f;
        // Use the same easing for zoom and translation so the selected icon travels straight to center.
        float eased = t * t * (3 - 2 * t);
        _zoom = MathHelper.Lerp(_focusStartZoom, _focusTargetZoom, eased);
        _pan = Vector2.Lerp(_focusStartPan, _focusTargetPan, eased);
        if (t >= 1) _focusing = false;
    }
    private void StopDragging(bool cancelFocus = true)
    {
        if (cancelFocus) _focusing = false;
        _dragging = false;
        CancelSlotDrag();
        if (_detailScroll != null)
            _detailScroll.ResetPosition(_detailScroll.Motion.Position);
    }
    private void Close()
    {
        _layoutEditor?.Cancel();
        StopDragging();
        _tooltipDelay.Reset();
        _hoverNode = null;
        RequestBook(false);
    }

    private void CloseImmediately()
    {
        _layoutEditor?.Cancel();
        _draw?.ClearToggleAnimations();
        _butterfly.Reset();
        _nodeFx.Reset();
        _sky.Reset();
        _tooltipDelay.Reset();
        _book.Reset();
        _bookClock.Stop();
        _controlsClock.Stop();
        _controlsHadFocus = false;
        Enabled = false; StopDragging(); _lastBlankClick = -1; _toastLife = 0;
    }
    private void Notify(string text) { _toast = text; _toastLife = 3.5f; }
    private void PrimaryAction()
    {
        if (Editing || _state.EmptyFilter) return;
        _state.Refresh();
        var pills = ConstellationDetail.Pills(_state);
        int index = ConstellationDetail.PillAt(pills, Design(Main.MouseScreen));
        if (index < 0 || !pills[index].Enabled) return;
        string id = _state.Selected;
        switch (pills[index].Kind)
        {
            case "upgrade":
                if (!_state.Upgrade(id)) return;
                InvalidateDetail(false);
                _flash = 1;
                Notify($"{_state.Current.name} · Lv. {_state.Level(id)}");
                return;
            // The slot bar already reflects loadout changes, so they are not announced.
            case "unequip": _state.ClearSlot(); return;
            case "equip": _state.Equip(); return;
            case not "learn": return;
        }
        var before = _state.Skills.Where(_state.Discovered).Select(s => s.id).ToHashSet();
        if (!_state.Unlock()) return;
        InvalidateDetail(false);
        _flash = 1;
        int revealed = _state.Skills.Count(s => _state.Discovered(s) && !before.Contains(s.id));
        _ancestors = _state.Ancestors();
        Notify(LocalizeUI("SkillLearned", _state.Current.name) + (revealed > 0 ? LocalizeUI("StarsRevealed", revealed) : ""));
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_state == null) return;
        base.Draw(gameTime, spriteBatch);
        _draw.Batch = spriteBatch;
        _draw.Transform = SilkyUI.TransformMatrix;
        _draw.Origin = Bounds.Position + _origin;
        _draw.Scale = _scale;
        _tooltip = null;
        _tooltipAnchor = null;
        _tooltipKey = null;
        DrawChrome();
        DrawDetails();
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_state == null) return;
        base.DrawChildren(gameTime, spriteBatch);
        DrawRuleOrnaments();
        DrawSlotDrag();
        if (_toastLife > 0)
        {
            float alpha = Math.Min(1, _toastLife * 3);
            float width = Math.Min(650, _draw.Measure(_toast, 12) + 50);
            // A ghost pill above the bottom rule, centered on the map.
            _draw.RoundRect(390 - width / 2, 680, width, 34, 17, new Color(26, 22, 35) * (.92f * alpha), Lavender * (.5f * alpha), 1);
            _draw.FittedText(_toast, 390, 689, width - 30, 12, Ink * alpha, .5f);
        }
        if (!_book.IsOpen || !Main.hasFocus || Main.mouseLeft || _dragging || string.IsNullOrEmpty(_tooltip)) { _tooltipDelay.Reset(); return; }
        if (!_tooltipDelay.Ready(_tooltipKey, _clock)) return;
        _draw.Tooltip(_tooltip, Design(Main.MouseScreen), new Vector2(DesignWidth, DesignHeight), _tooltipAnchor);
    }

    private void DrawChrome()
    {
        var d = _draw;
        d.NotebookPage(DesignWidth, DesignHeight);
        // Title rule: bright on the left, fading right, broken by a small star.
        d.Dots(new Vector2(32, 83.5f), new Vector2(186, 83.5f), t => new Color(226, 206, 240) * MathHelper.Lerp(.85f, .3f, t));
        d.Dots(198.5f, 83.5f, 1068, 83.5f, new Color(236, 229, 247) * .24f);
        d.CrossStar(new Vector2(192, 83.5f), new Vector2(5), new Color(214, 192, 232), .5f);
        // The hat crest stays between the frame's dotted top edge and the title rule.
        d.Ring(new Vector2(60, 47), 29, 29, Lavender * .16f);
        d.Corners(new Vector2(60, 47), 40, 16, Lavender * .34f, MathHelper.PiOver4);
        d.Image("HeaderHat", 44, 25, 40, 40, Lavender);
        d.NotebookTitle(101, 23);
        string dust = _state.Points.ToString("00");
        float dustLeft = 752 - d.LatinWidth(dust, 25);
        string pointLabel = LocalizeUI("StudyPoints");
        float labelLeft = dustLeft - 12 - d.Measure(pointLabel, 12, 1, true);
        d.Image("StudyBook", labelLeft - 37, 28.5f, 27, 27, Gold);
        d.Text(pointLabel, labelLeft, 33, 12, SerifLabel, spacing: 1, serif: true);
        d.LatinText(dust, dustLeft, 26, 25, new Color(239, 223, 190));
        DrawPlayerLevel();

        // Collection belongs to the index row, above the detail page, not to character XP.
        const float collectionLeft = 791, collectionRight = 1045;
        d.DotSeparator(775.5f, 92, 136);
        d.Text(LocalizeUI("Collection"), collectionLeft, 101, 12, SerifLabel, spacing: 1, serif: true);
        string total = $"/ {_state.Skills.Length}";
        float learnedRight = collectionRight - d.LatinWidth(total, 11) - 6;
        string learned = _state.Learned.Count.ToString("00");
        float starsRight = learnedRight - d.LatinWidth(learned, 22) - 18;
        float starStep = (starsRight - collectionLeft - 4) / Math.Max(1, _state.Skills.Length - 1);
        for (int i = 0; i < _state.Skills.Length; i++)
        {
            bool lit = _state.Learned.Contains(_state.Skills[i].id);
            var center = new Vector2(collectionLeft + 3 + i * starStep, 126);
            if (lit) d.Image("Glow", center.X - 5, center.Y - 5, 10, 10, new Color(213, 170, 255) * .2f);
            d.CrossStar(center, new Vector2(5.66f),
                lit ? new Color(234, 214, 245) : new Color(138, 118, 155), .5f, lit);
        }
        d.LatinText(learned, learnedRight, 99, 22, new Color(230, 213, 240), 1);
        d.LatinText(total, collectionRight, 111, 11, new Color(159, 142, 172), 1);

        // Dotted rules; three sparks sit where the column rule meets the tab and bottom rules.
        d.Dots(26, 145.5f, 731, 145.5f, DotColor);
        d.Dots(776, 145.5f, 1074, 145.5f, DotColor);
        d.Dots(753.5f, 156, 753.5f, 700, DotColor);
        d.Dots(26, 725, 733, 725, DotColor);
        d.Trio(new Vector2(753.5f, 725), Ink);
        d.Dots(773, 725, 1074, 725, DotColor);
        // The zoom capsule and the layout hints sit inside the sky, so DrawMap paints them over it.
        DrawLoadoutCaption();
    }

    // The tab rule's active-filter flash and the trio where it meets the column rule hang a few pixels
    // below y=145.5 into the sky's top fade, which would cover them; they are drawn after the map.
    private void DrawRuleOrnaments()
    {
        _draw.Trio(new Vector2(753.5f, 145.5f), Ink);
        _draw.FilterMarker(_state.Filter);
    }

    private void DrawZoomCapsule()
    {
        // Zoom controls share one ghost capsule, split before 归位 by a short dotted rule.
        var d = _draw;
        Vector2 mouse = Design(Main.MouseScreen);
        bool zoomHover = _book.IsOpen && mouse.X >= 572 && mouse.X < 732 && mouse.Y >= 162 && mouse.Y < 190;
        d.RoundRect(572, 162, 160, 28, 14, zoomHover ? new Color(236, 229, 247) * .05f : new Color(26, 22, 35) * .45f,
            new Color(236, 229, 247) * (zoomHover ? .4f : .18f), 1);
        d.LatinText(MathF.Round(_zoom * 100) + "%", 627, 169, 11, Lavender, .5f);
        d.Dots(684.5f, 168.5f, 684.5f, 184, DotColor);
    }

    private void DrawPlayerLevel()
    {
        var d = _draw;
        var playerState = Main.LocalPlayer.GetModPlayer<ElainaStatePlayer>();
        d.PlayerLevel(playerState.GetLevel(), playerState.GetMaxLevel(),
            playerState.GetExperience(), playerState.GetExperienceToNextLevel());
    }

    private void DrawFilter(int index, string filter, bool hover)
    {
        // Four equal cells with centered content; the count follows its label and is colored by meaning.
        var d = _draw;
        float x0 = 26 + index * 180, center = x0 + 90;
        bool active = _state.Filter == filter;
        string label = ConstellationState.StatusName(filter);
        int count = _state.Skills.Count(s => _state.Status(s) == filter);
        string countText = count.ToString("00");
        float labelWidth = Math.Min(130, d.Measure(label, 14, 1)), countWidth = d.LatinWidth(countText, 12);
        float x = center - (9 + 8 + labelWidth + 8 + countWidth) / 2;
        Color color = filter == "ready" ? Gold : filter == "locked" ? Muted * .6f : Lavender;
        if (filter == "hidden") d.Ring(new Vector2(x + 4.5f, 113), 4, 4, color, 1, true);
        else d.CrossStar(new Vector2(x + 4.5f, 113), new Vector2(9), color, .5f, filter == "learned" || active && filter == "ready");
        d.FittedText(label, x + 17, 103, 130, 14, active ? new Color(242, 226, 248) : hover ? new Color(222, 206, 232) : new Color(171, 154, 185), spacing: 1);
        d.LatinText(countText, x + 17 + labelWidth + 8, 105.5f, 12, active ? new Color(228, 200, 239) : count == 0 ? Muted * .38f
            : filter == "ready" ? Gold * .92f : new Color(158, 139, 171));
        if (index > 0) d.Dots(x0, 104, x0, 125, new Color(236, 229, 247) * .2f);
    }

    private void CameraControl(string kind, bool hover, bool enabled)
    {
        var d = _draw;
        if (kind == "center") { d.Text(LocalizeUI("ResetView"), 709, 169, 10, hover ? Ink : Muted, .5f); return; }
        Color color = !enabled ? Muted * .3f : hover ? Ink : Lavender;
        float x = kind == "minus" ? 586 : 656;
        d.Line(new Vector2(x, 176), new Vector2(x + 12, 176), color, 1.1f);
        if (kind == "plus") d.Line(new Vector2(662, 170), new Vector2(662, 182), color, 1.1f);
    }

    private void DrawCloseButton(bool hover)
    {
        var d = _draw;
        Color color = hover ? Ink : Lavender;
        d.Corners(new Vector2(1053, 42), 29, 15, hover ? Ink : Lavender * .7f, MathHelper.PiOver4);
        d.Line(new Vector2(1048, 37), new Vector2(1058, 47), color, 1.2f);
        d.Line(new Vector2(1058, 37), new Vector2(1048, 47), color, 1.2f);
        d.LatinText("ESC", 1022, 55, 9, new Color(164, 149, 178), spacing: 1);
    }

    private void DrawMap()
    {
        var d = _draw;
        // Page-fixed layers first (Tools/SkillPanelRedesign/index.html): breathing stars and meteors,
        // then the portrait. Only the decorative stars, links and nodes follow the camera; the old
        // astronomical rings and ticks were dropped with the redesign.
        d.SkyStars(_sky);
        d.Portrait(_sky);
        Vector2 SkyPoint(float x, float y) => MapPoint(x / 880 * WorldWidth, y / 650 * WorldHeight);
        float sx = WorldWidth / 880f * _zoom, sy = WorldHeight / 650f * _zoom;
        d.CrossStar(SkyPoint(87, 182), new Vector2(16 * sx, 16 * sy), new Color(209, 188, 241) * .22f, .25f);
        d.CrossStar(SkyPoint(743, 549), new Vector2(14 * sx, 14 * sy), new Color(209, 188, 241) * .22f, .28f);
        d.CrossStar(SkyPoint(422, 477), new Vector2(10 * sx, 10 * sy), new Color(209, 188, 241) * .22f, .2f);
        foreach (var skill in _state.Skills)
        foreach (string parentId in skill.prereqs)
            if (_state.Find(parentId) is { } parent) DrawEdge(parent, skill);
        foreach (var skill in _state.Skills.Where(s => s.id != _state.Selected)) DrawNode(skill);
        if (_state.Current != null) DrawNode(_state.Current);
        foreach (var mote in _nodeFx.Dust)
            d.Dot(MapPoint(mote.Position.X, mote.Position.Y), mote.Radius * _zoom,
                new Color(229, 217, 245) * ConstellationNodeEffects.MoteOpacity(mote));
        if (_book.IsOpen && _butterfly.Visible)
            d.SelectionButterfly(MapPoint(_butterfly.Position.X, _butterfly.Position.Y), _butterfly.Frame, _zoom);
        d.SkyEdgeFade(Sky);
        DrawZoomCapsule();
        DrawLayoutHints();
    }

    private void DrawEdge(ConstellationNode parent, ConstellationNode skill)
    {
        bool related = (skill.id == _state.Selected || _ancestors.Contains(skill.id)) && _ancestors.Contains(parent.id);
        string status = _state.Status(skill);
        bool dim = !Editing && status != _state.Filter && _state.Status(parent) != _state.Filter;
        // Straight routes and the old ready/hidden dash pattern; links stop clear of the node rings
        // and their small constellations, which keep LinkClearance degrees around every link.
        Vector2 a = new(parent.x, parent.y), b = new(skill.x, skill.y), delta = b - a;
        float axis = Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)), length = delta.Length();
        if (axis < 77) return;
        a += delta / length * (NodeRadius + 12);
        b -= delta / length * (NodeRadius + 12);
        a = MapPoint(a.X, a.Y); b = MapPoint(b.X, b.Y);
        _draw.SkillLink(a, b, status == "ready" ? "available" : status, related, dim, _zoom);
    }

    private void DrawNode(ConstellationNode s)
    {
        var d = _draw;
        string status = Editing ? "ready" : _state.Status(s);
        bool hidden = !Editing && !_state.Discovered(s), selected = s.id == _state.Selected, hover = s.id == _hoverNode;
        bool insight = !hidden && !s.Active, owned = _state.Learned.Contains(s.id);
        bool off = !Editing && owned && !_state.Availability(s.id).Ok;
        // Unlearned nodes stay dormant even when their filter tab is active; only learned art is lit.
        bool dormant = !Editing && !owned && !hidden;
        var p = MapPoint(s.x, s.y);
        float alpha = Editing || status == _state.Filter ? 1 : hover ? .8f : .18f;
        // Every node is the same starry ring (Tools/SkillFramePreview/perch.html); spells and
        // insights are no longer told apart by their frames.
        float r = NodeRadius * _zoom, amount = _nodeFx.Amount(s.id);
        if (amount > 0) d.Image("Glow", p.X - 56 * _zoom, p.Y - 56 * _zoom, 112 * _zoom, 112 * _zoom, Gold * (.22f * amount * alpha));
        if (hover && !selected)
            d.Image("Glow", p.X - 50 * _zoom, p.Y - 50 * _zoom, 100 * _zoom, 100 * _zoom, new Color(183, 162, 234) * (.16f * alpha));
        d.Dot(p, r, NodePlate * (.95f * alpha));
        if (hidden)
        {
            if (status == "locked") d.Text("?", p.X, p.Y - 16 * _zoom, 30 * _zoom, Lavender * alpha, .5f);
            else d.CrossStar(p, new Vector2(14 * _zoom), Muted * (.35f * alpha), .2f);
            d.Ring(p, r, r, (hover ? Ink * .5f : Muted * .4f) * alpha, 1, true);
        }
        else
        {
            float tint = off ? .38f : status == "locked" ? .48f : dormant ? selected || hover ? .82f : .6f : 1;
            d.SkillIcon(s, p, 2 * NodeRadius * _zoom, Color.White * alpha * tint, false, off || dormant, true);
            float ringAlpha = hover || selected ? 1 : owned ? .72f : status == "ready" ? .6f : .4f;
            Color accent = hover && !selected ? Ink : status == "ready" ? Gold : Lavender;
            d.Ring(p, r, r, accent * (ringAlpha * alpha), 1.2f);
            if (!selected) d.Dot(p + Polar(NodeRadius + 5.5f, PerchAngle) * _zoom, _zoom, accent * (ringAlpha * .55f * alpha));
            d.NodeConstellation(p, _zoom, ConstellationNodeStars.For(s, _state.Skills), selected ? Gold : accent,
                ringAlpha * alpha, selected, _nodeFx.Time, _nodeFx.SinceTrace(s.id));
            d.NodeSelection(p, _zoom, amount, _nodeFx.Landed(s.id), _nodeFx.SinceLanding, _nodeFx.Time, _nodeFx.Flutter, alpha);
        }
        if (selected && _flash > 0) d.Ring(p, (NodeRadius + (1 - _flash) * 40) * _zoom,
            (NodeRadius + (1 - _flash) * 40) * _zoom, Gold * _flash * .7f, 1.5f);
        // Levels and slot keys live in the detail column and the slot bar; the map only names the star.
        string name = Editing ? s.name : _state.Name(s);
        float textWidth = Math.Min(136 * _zoom, d.Measure(name, 12 * _zoom, .5f * _zoom, true) + 16 * _zoom);
        d.Image("Glow", p.X - textWidth / 2, p.Y + 39 * _zoom, textWidth, 22 * _zoom, new Color(39, 32, 51) * alpha);
        d.FittedText(name, p.X, p.Y + 41 * _zoom, 136 * _zoom, 12 * _zoom,
            (selected ? Gold : off ? new Color(179, 160, 186) : owned ? Ink : Muted) * alpha, .5f, serif: !hidden, spacing: .5f * _zoom);
        if (hover)
        {
            string tooltip = hidden ? status == "locked" ? LocalizeUI("Locked") + " · " + s.ModSkill.ConstellationRevealHint : LocalizeUI("Hidden") : $"{s.name} · {(insight ? LocalizeUI("Insight") : LocalizeUI("Spell"))} · {ConstellationState.StatusName(status)}"
            + (owned ? $" · Lv. {_state.Level(s.id)}" + (_state.MaxLevel(s.id) is int max ? $" / {max}" : "") : "")
            + (off ? " · " + _state.Availability(s.id).Reason : "");
            if (Editing) tooltip = $"{s.name} · 拖动移动 · ({s.x:0.##}, {s.y:0.##})";
            RequestTooltip(tooltip, new Rectangle((int)(p.X - 34 * _zoom), (int)(p.Y - 34 * _zoom), (int)(68 * _zoom), (int)(68 * _zoom)), "node:" + s.id);
        }
    }

    private void DrawDetails()
    {
        var d = _draw; var s = _state.Current;
        if (_state.EmptyFilter)
        {
            d.Text(LocalizeUI("EmptyFilter", ConstellationState.StatusName(_state.Filter)), 914, 422, 15, Muted, .5f, serif: true);
            return;
        }
        bool hidden = !_state.Discovered(s), insight = !hidden && !s.Active, owned = _state.Learned.Contains(s.id);
        int level = _state.Level(s.id), max = _state.MaxLevel(s.id) ?? 1;
        bool maxed = owned && level >= max;
        // Kind row: a spark and the kind on the left, the status (gold when actionable or maxed) on the right.
        d.Spark(new Vector2(780, 167), 8, Lavender);
        d.Text(hidden ? ConstellationState.StatusName(_state.Status(s)) : insight ? LocalizeUI("Insight") : LocalizeUI("Spell"), 790, 160, 11, Heading, spacing: 2);
        string status = _state.Status(s);
        bool off = owned && !_state.Availability(s.id).Ok, golden = !off && (maxed || status == "ready");
        string statusText = off ? s.toggleable && _state.Disabled.Contains(s.id) ? LocalizeUI("Disabled") : LocalizeUI("Inactive")
            : maxed ? LocalizeUI("AlreadyMaxLevel") : ConstellationState.StatusName(status);
        Color statusColor = off ? new Color(214, 177, 155) : golden ? Gold : Muted2;
        d.Text(statusText, 1052, 161, 10, statusColor, 1, 1.5f);
        if (golden) d.Spark(new Vector2(1052 - d.Measure(statusText, 10, 1.5f) - 9, 167), 8, statusColor);

        var center = new Vector2(815, 229);
        d.Image("Glow", center.X - 45, center.Y - 45, 90, 90, Lavender * (owned ? .2f : .08f));
        if (hidden && status == "locked") d.Text("?", center.X, center.Y - 23, 40, Lavender, .5f);
        else if (hidden) d.SkillIcon(s, center, 50, Lavender, true);
        else d.SkillIcon(s, center, 56, Color.White * (owned ? 1 : .45f), uncolored: !owned);
        d.DetailIconFrame(center, maxed, !owned);
        d.FittedText(_state.Name(s), 872, 197, 180, 25, new Color(240, 231, 244), serif: true, spacing: 3);
        if (!hidden)
        {
            string limit = $"/ {max}";
            if (owned)
            {
                d.LatinText("LV.", 872, 236, 9, Muted, spacing: 1.5f);
                float x = 872 + d.LatinWidth("LV.", 9, 1.5f) + 5;
                d.LatinText(level.ToString(), x, 229, 18, maxed ? Gold : Ink);
                d.LatinText(limit, x + d.LatinWidth(level.ToString(), 18) + 5, 235, 11, Muted);
                d.ProgressBar(872, 259, 180, level / (float)max, maxed ? Gold * .85f : Lavender * .8f);
            }
            else
            {
                d.Text(LocalizeUI("NotLearnedShort"), 872, 232, 12, Muted2, spacing: 2);
                d.LatinText(limit, 1052, 235, 11, Muted, 1);
            }
        }
        d.Dots(ConstellationDetail.Left, 288.5f, ConstellationDetail.Left + ConstellationDetail.Width, 288.5f, DotColor);
        _detail?.DrawFooter(d);
        if (_detail?.HasActions == true)
            d.Dots(ConstellationDetail.Left, 655.5f, ConstellationDetail.Left + ConstellationDetail.Width, 655.5f, DotColor);
    }

    private void DrawActionButton(bool hover)
    {
        if (Editing)
        {
            _draw.Text("正在编辑布局", 914, 679, 13, Muted, .5f);
            return;
        }
        var pills = ConstellationDetail.Pills(_state);
        int hovered = hover ? ConstellationDetail.PillAt(pills, Design(Main.MouseScreen)) : -1;
        for (int i = 0; i < pills.Length; i++)
        {
            var pill = pills[i];
            var area = ConstellationDetail.PillArea(pill);
            _draw.ActionPill(pill.Kind, area.X, area.Y, area.Width, pill.Label, pill.Tail, pill.StudyCost, pill.Gold, pill.Enabled, i == hovered);
            if (i == hovered && pill.Tooltip != null) RequestTooltip(pill.Tooltip, area, pill.Kind + ":" + _state.Selected);
        }
    }

    private void DrawSlot(int slot, bool hover)
    {
        var d = _draw;
        float x = SlotLeft + slot * SlotStride;
        float y = SlotTop;
        var center = new Vector2(x + SlotSize / 2f, y + SlotSize / 2f);
        bool selected = _state.TargetSlot == slot;
        bool lifted = _slotDragging && _slotPress == slot, dropTarget = _slotDragging && _slotDropTarget == slot && !lifted;
        var gold = new Color(227, 193, 141);
        if (selected || dropTarget)
            d.Image("Glow", x - 12, y - 12, SlotSize + 24, SlotSize + 24, new Color(195, 162, 118) * (dropTarget ? .18f : .08f));
        d.Box(x, y, SlotSize, SlotSize, selected ? new Color(185, 155, 87) * .125f : new Color(33, 27, 44));
        // CSS inset shadow: a subtle three-pixel rim, with the artwork above it.
        Color inset = selected ? new Color(185, 155, 87) * .106f : new Color(52, 35, 62) * .333f;
        for (int i = 1; i <= 3; i++) d.Frame(x + i, y + i, SlotSize - i * 2, SlotSize - i * 2, inset);
        d.Frame(x, y, SlotSize, SlotSize, selected || dropTarget ? gold : hover && !_slotDragging ? Lavender : new Color(164, 136, 179) * .34f);
        if (dropTarget) d.Frame(x - 3, y - 3, SlotSize + 6, SlotSize + 6, gold * .45f);
        string id = _state.Slots[slot];
        if (id == null)
        {
            var plus = new Color(155, 125, 173);
            d.Line(center - new Vector2(7, 0), center + new Vector2(7, 0), plus);
            d.Line(center - new Vector2(0, 7), center + new Vector2(0, 7), plus);
        }
        else
        {
            bool effective = _state.Availability(id).Ok;
            // The lifted skill leaves a faint ghost while its icon follows the cursor.
            float alpha = (effective ? 1 : .3f) * (lifted ? .25f : 1);
            if (_state.Find(id) is { } node) d.SkillIcon(node, center, 32, Color.White * alpha, uncolored: !effective);
            else d.Icon("star", center, 24, Muted * (lifted ? .25f : 1));
        }
        float labelWidth = d.SlotLabelWidth(SlotKeys[slot], 9);
        // The number cuts the frame: its backing is the page texture itself, leaving no visible patch.
        d.SurfacePatch((int)x - 3, (int)y - 6, (int)MathF.Ceiling(labelWidth) + 6, 10);
        // Gold is reserved for the target slot; the others stay a soft pink.
        d.SlotLabel(SlotKeys[slot], x, y - 6, 9, selected || dropTarget ? gold
            : hover && !_slotDragging ? new Color(240, 192, 222) : new Color(222, 166, 200));
        if (selected) d.CrossStar(new Vector2(center.X, y + SlotSize + 2), new Vector2(8.5f), gold, .5f);
        if (hover && !_slotDragging) RequestTooltip(id == null ? LocalizeUI("EmptySlot", SlotKeys[slot]) : $"{SlotKeys[slot]} · {_state.Find(id)?.name ?? LocalizeUI("OtherSkill")} · Lv. {_state.Level(id)}"
            + (_state.Availability(id).Ok ? "" : " · " + _state.Availability(id).Reason) + "\n" + LocalizeUI("SlotHint"),
            new Rectangle((int)x, (int)y, SlotSize, SlotSize), "slot:" + slot);
    }

    private void DrawSlotDrag()
    {
        if (!_slotDragging || _slotPress < 0 || _state.Slots[_slotPress] is not { } id) return;
        var center = Design(Main.MouseScreen);
        var gold = new Color(227, 193, 141);
        _draw.Image("Glow", center.X - 33, center.Y - 33, 66, 66, new Color(195, 162, 118) * .16f);
        _draw.Box(center.X - SlotSize / 2f, center.Y - SlotSize / 2f, SlotSize, SlotSize, new Color(33, 27, 44) * .92f);
        _draw.Frame(center.X - SlotSize / 2f, center.Y - SlotSize / 2f, SlotSize, SlotSize, gold);
        if (_state.Find(id) is { } node) _draw.SkillIcon(node, center, 34, Color.White, uncolored: !_state.Availability(id).Ok);
        else _draw.Icon("star", center, 24, Muted);
    }

    private void DrawLoadoutCaption()
    {
        // The target slot is shown by its gold frame; the caption only counts what is equipped.
        var d = _draw;
        d.Spark(new Vector2(38, 749.5f), 12, SerifLabel);
        d.FittedText(LocalizeUI("Equip"), 49, 743, SlotLeft - 57, 13, SerifLabel, serif: true, spacing: 2);
        d.LatinText($"{_state.Slots.Count(id => id != null)} / {SlotKeys.Length}", 49, 763, 10, Muted2, spacing: 1);
        d.DotSeparator(502, 733, 783);
    }

    public static bool TogglePanel()
    {
        if (Main.dedServ || Main.gameMenu || SilkyUISystem.ServiceProvider == null) return false;
        if (!SilkyUIRenderSystem.Instance.TryGetInstance<ConstellationSkillPanel>(out var body)) return false;
        if (body._book.TargetOpen) body.Close();
        else
        {
            bool initializeCamera = body._state == null || !body._state.IsCurrentPlayer;
            if (initializeCamera)
            {
                body._state = new ConstellationState(ElainaSkillModPlayer.SkillModPlayer);
                body.LoadLayout();
                body.Recenter();
            }
            if (!body.Enabled)
            {
                body._state.Refresh();
                body._ancestors = body._state.Ancestors();
                body.InvalidateDetail(true);
                body._controlsClock.Restart();
                body._controlsHadFocus = Main.hasFocus;
            }
            body.Enabled = true;
            body.RequestBook(true);
            body.StopDragging();
            // Reopening keeps the last camera pose, including manual pan/zoom and interrupted focus.
            if (initializeCamera && !body._state.EmptyFilter) body.CenterOn(body._state.Current);
        }
        return true;
    }

    internal static void ResetForWorld()
    {
        if (Main.dedServ || SilkyUISystem.ServiceProvider == null) return;
        if (!SilkyUIRenderSystem.Instance.TryGetInstance<ConstellationSkillPanel>(out var body)) return;
        var state = body._state;
        Main.QueueMainThreadAction(() =>
        {
            // OnWorldUnload also runs during background save/quit. Reset UI input
            // on the main thread. Ignore an old world's
            // pending reset if this body has already been rebound to another player.
            if (!ReferenceEquals(body._state, state)) return;
            body.CloseImmediately();
            body._state = null;
            body._layoutEditor = null;
            body.InvalidateDetail(true);
        });
    }

    private sealed class PaintView(Action<PaintView, SpriteBatch> paint) : UIView
    {
        protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch) => paint(this, spriteBatch);
    }
}

public sealed class ConstellationPanelSystem : ModSystem
{
    public override void OnWorldUnload() => ConstellationSkillPanel.ResetForWorld();
}

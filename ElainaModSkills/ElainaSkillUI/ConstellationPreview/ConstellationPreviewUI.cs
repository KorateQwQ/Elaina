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
using 伊蕾娜.Config;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview.PreviewDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview;

[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Constellation Visual Preview", 1000)]
public sealed partial class ConstellationPreviewUI : BaseBody
{
    private PreviewState _state;
    private PreviewDrawing _draw;
    private readonly List<(UIView View, Rectangle Area)> _regions = [];
    private UIElementGroup _viewport;
    private PaintView _modalView;
    private SUIScrollView _detailScroll;
    private PreviewDetail _detail;
    private readonly List<(PaintView View, PreviewDetailRow Row)> _detailRows = [];
    private bool _detailDirty = true, _resetDetailScroll = true;
    private float _scale = 1, _zoom = .84f, _clock, _flash;
    private Vector2 _origin, _pan, _dragStart, _panStart, _lastSize;
    private bool _dragging, _moved, _inputSuspended;
    private string _pressedNode, _hoverNode, _modal, _toast, _tooltip;
    private float _toastLife, _lastBlankClick = -1;
    private HashSet<string> _ancestors = [];
    private const float DesignWidth = 1100, DesignHeight = 800, ScreenMargin = 16;
    private const int MapX = 26, MapY = 208, MapWidth = 727, MapHeight = 473;
    private const int WorldWidth = 727, WorldHeight = 550;
    private static readonly string[] SlotKeys = PreviewState.SlotLabels;
    private const int SlotLeft = 74, SlotTop = 741, SlotSize = 42, SlotStride = 48, TargetLeft = 464;
    private static string ToggleKeyLabel => KeyBind.OpenSkillPanel?.GetAssignedKeys().FirstOrDefault() ?? "技能面板快捷键";

    public ConstellationPreviewUI() { Enabled = false; }

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
        _state = new PreviewState(mod);
        _draw = new PreviewDrawing(mod);
        _ancestors = _state.Ancestors();
        Recenter();

        _viewport = new UIElementGroup
        {
            Positioning = Positioning.Absolute, OverflowHidden = true,
            IndependentRenderTarget = false, Border = 0, Padding = new Margin(0)
        }.Join(this);
        _regions.Add((_viewport, new Rectangle(MapX, MapY, MapWidth, MapHeight)));
        new PaintView((_, _) => DrawMap())
        {
            Width = new Dimension(0, 1), Height = new Dimension(0, 1), IgnoreMouseInteraction = true
        }.Join(_viewport);
        _viewport.LeftMouseDown += (_, e) =>
        {
            if (_modal != null) return;
            _dragStart = Design(e.MousePosition);
            _panStart = _pan;
            _pressedNode = HitNode(_dragStart);
            _dragging = true;
            _moved = false;
        };
        _viewport.LeftMouseUp += (_, e) => { if (_dragging) DragTo(Design(e.MousePosition)); _dragging = false; };
        _viewport.LeftMouseClick += (_, e) =>
        {
            if (_modal != null || _moved) return;
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
            if (_modal != null) return;
            ChangeZoom(Math.Sign(e.ScrollDelta) * .1f, Design(e.MousePosition));
            e.LockScroll(_viewport);
        };

        string[] filters = ["learned", "ready", "locked", "hidden"];
        for (int i = 0; i < filters.Length; i++)
        {
            string filter = filters[i];
            int index = i;
            int x = 35 + i * 145;
            Button(new Rectangle(x, 84, 110, 61), () =>
            {
                _state.SelectFilter(filter);
                _ancestors = _state.Ancestors();
                InvalidateDetail(true);
            }, hover => DrawFilter(index, filter, hover));
        }
        Button(new Rectangle(579, 164, 24, 22), () => ChangeZoom(-.1f),
            h => CameraControl("minus", 591, h, _zoom > .5f), "缩小星图");
        Button(new Rectangle(651, 164, 24, 22), () => ChangeZoom(.1f),
            h => CameraControl("plus", 663, h, _zoom < 1.5f), "放大星图");
        Button(new Rectangle(694, 164, 32, 22), Recenter,
            h => CameraControl("center", 706, h, true), "星图归位");
        Button(new Rectangle(776, 670, 276, 38), PrimaryAction, DrawActionButton);
        InitializeDetailScroll();
        for (int i = 0; i < SlotKeys.Length; i++)
        {
            int slot = i;
            Button(new Rectangle(SlotLeft + i * SlotStride, SlotTop, SlotSize, SlotSize), () => _state.TargetSlot = slot,
                h => DrawSlot(slot, h));
        }
        Button(new Rectangle(TargetLeft, 763, 60, 21), () =>
        {
            if (_state.Slots[_state.TargetSlot] == null) return;
            _state.Slots[_state.TargetSlot] = null;
            Notify($"{SlotKeys[_state.TargetSlot]} 槽已清空");
        }, DrawClearSlot);
        Button(new Rectangle(1038, 26, 30, 32), Close, DrawCloseButton, "关闭星图");
        Button(new Rectangle(820, 744, 50, 35), () => _modal = "help",
            h => _draw.Text("帮助", 845, 756, 11, h ? Ink : Muted, .5f));
        Button(new Rectangle(890, 744, 76, 35), () =>
        {
            _state.Reset(); _ancestors = _state.Ancestors(); InvalidateDetail(true);
            Notify("手札已重置 · 星尘 18");
        }, h => _draw.Text("重置手札", 928, 756, 11, h ? Ink : Muted, .5f));
        Button(new Rectangle(959, 744, 109, 35), Close,
            h => _draw.Text("返回", 1008, 756, 11, h ? Ink : Muted, .5f));

        _modalView = new PaintView((_, _) => DrawModal()) { Positioning = Positioning.Absolute, ZIndex = 50 }.Join(this);
        _regions.Add((_modalView, new Rectangle(0, 0, (int)DesignWidth, (int)DesignHeight)));
        _modalView.LeftMouseClick += (_, e) =>
        {
            Vector2 p = Design(e.MousePosition);
            if (!new Rectangle(335, 135, 430, 530).Contains(p.ToPoint()) ||
                new Rectangle(718, 149, 34, 34).Contains(p.ToPoint()) ||
                new Rectangle(368, 583, 364, 42).Contains(p.ToPoint())) _modal = null;
        };
    }

    private void Button(Rectangle area, Action click, Action<bool> paint, string tooltip = null)
    {
        var view = new PaintView((v, _) =>
        {
            bool hover = _modal == null && v.IsMouseHovering;
            paint(hover);
            if (hover && tooltip != null) _tooltip = tooltip;
        }) { Positioning = Positioning.Absolute }.Join(this);
        view.LeftMouseClick += (_, _) => { if (_modal == null) click(); };
        _regions.Add((view, area));
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
        Close();
        _draw?.DisposeGeneratedTextures();
        if (_state != null)
        {
            Array.Clear(_state.Slots);
            _state.TargetSlot = 0;
            _state.Reset();
            _ancestors = _state.Ancestors();
            InvalidateDetail(true);
            Recenter();
        }
        base.OnExitTree();
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        float delta = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .1f);
        _clock += delta;
        _flash = Math.Max(0, _flash - delta);
        _toastLife = Math.Max(0, _toastLife - delta);
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
            if (_modal != null) _modal = null;
            else Close();
        }
        _modalView.Invalid = _modal == null;
        foreach (var (view, _) in _regions)
            if (view != _modalView) view.DisableMouseInteraction = _modal != null;
        _detailScroll.DisableMouseInteraction = _modal != null;
        bool suspended = !Main.hasFocus || _modal != null || !Enabled;
        if (suspended && !_inputSuspended) StopDragging();
        _inputSuspended = suspended;
        if (_dragging)
        {
            DragTo(Design(Main.MouseScreen));
            if (!Main.mouseLeft) _dragging = false;
        }
        _hoverNode = _modal == null && _viewport.IsMouseHovering ? HitNode(Design(Main.MouseScreen)) : null;
    }

    private Vector2 Design(Vector2 mouse) => (mouse - Bounds.Position - _origin) / _scale;
    private Vector2 MapPoint(float x, float y) => new Vector2(MapX, MapY) + _pan + new Vector2(x, y) * _zoom;
    private void DragTo(Vector2 p)
    {
        Vector2 delta = p - _dragStart;
        if (delta.LengthSquared() > 25) { _moved = true; _lastBlankClick = -1; }
        if (_moved) { _pan = _panStart + delta; ClampPan(); }
    }
    private void ClampPan() => _pan = Vector2.Clamp(_pan,
        new Vector2(150 - WorldWidth * _zoom, 140 - WorldHeight * _zoom), new Vector2(MapWidth - 150, MapHeight - 140));
    private string HitNode(Vector2 p)
    {
        if (!new Rectangle(MapX, MapY, MapWidth, MapHeight).Contains(p.ToPoint())) return null;
        foreach (var s in _state.Skills.AsEnumerable().Reverse())
        {
            Vector2 distance = p - MapPoint(s.x, s.y);
            float half = 34 * _zoom;
            if (Math.Abs(distance.X) <= half && Math.Abs(distance.Y) <= half) return s.id;
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
    }
    private void ChangeZoom(float amount, Vector2? anchor = null)
    {
        var local = (anchor ?? new Vector2(MapX + MapWidth / 2f, MapY + MapHeight / 2f)) - new Vector2(MapX, MapY);
        var world = (local - _pan) / _zoom;
        _zoom = Math.Clamp(_zoom + amount, .5f, 1.5f);
        _pan = local - world * _zoom;
        ClampPan();
        _dragging = false;
    }
    private void Recenter()
    {
        _zoom = .84f;
        _pan = (new Vector2(MapWidth, MapHeight) - new Vector2(WorldWidth, WorldHeight) * _zoom) / 2;
        _dragging = false;
    }
    private void CenterOn(PreviewSkill s)
    {
        _pan = new Vector2(MapWidth / 2f, MapHeight / 2f) - new Vector2(s.x, s.y) * _zoom;
        ClampPan();
    }
    private void StopDragging()
    {
        _dragging = false;
        if (_detailScroll != null)
            _detailScroll.ScrollBar.OnLeftMouseUp(new UIMouseEvent(_detailScroll.ScrollBar, Main.MouseScreen));
    }
    private void Close() { Enabled = false; StopDragging(); _modal = null; _lastBlankClick = -1; }
    private void Notify(string text) { _toast = text; _toastLife = 3.5f; }
    private void PrimaryAction()
    {
        if (_state.EmptyFilter) return;
        if (_state.Learned.Contains(_state.Selected))
        {
            if (_state.Equip()) Notify($"{_state.Current.name}已装配至 {SlotKeys[_state.TargetSlot]} 槽");
            return;
        }
        var before = _state.Skills.Where(_state.Discovered).Select(s => s.id).ToHashSet();
        if (!_state.Unlock()) return;
        InvalidateDetail(false);
        _flash = 1;
        int revealed = _state.Skills.Count(s => _state.Discovered(s) && !before.Contains(s.id));
        Notify(_state.Current.name + "已习得" + (revealed > 0 ? $" · {revealed} 颗隐星显现了" : $" · 星尘 −{_state.Current.cost}"));
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        _draw.Batch = spriteBatch;
        _draw.Origin = Bounds.Position + _origin;
        _draw.Scale = _scale;
        _tooltip = null;
        DrawChrome();
        DrawDetails();
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.DrawChildren(gameTime, spriteBatch);
        if (_modal != null) return;
        if (_toastLife > 0)
        {
            float alpha = Math.Min(1, _toastLife * 3);
            float width = Math.Min(650, _draw.Measure(_toast, 12) + 50);
            _draw.Box(550 - width / 2, 693, width, 38, new Color(53, 37, 63) * alpha);
            _draw.Frame(550 - width / 2, 693, width, 38, Lavender * .5f * alpha);
            _draw.FittedText(_toast, 550, 705, width - 30, 12, Ink * alpha, .5f);
        }
        if (_tooltip == null) return;
        var p = Design(Main.MouseScreen) + new Vector2(12, 18);
        float w = _draw.Measure(_tooltip, 11) + 20;
        p = Vector2.Clamp(p, new Vector2(12), new Vector2(DesignWidth - w - 12, DesignHeight - 40));
        _draw.Box(p.X, p.Y, w, 27, new Color(26, 22, 35));
        _draw.Frame(p.X, p.Y, w, 27, LineColor);
        _draw.Text(_tooltip, p.X + 10, p.Y + 7, 11, Ink);
    }

    private void DrawChrome()
    {
        var d = _draw;
        d.Image("Page", 0, 0, DesignWidth, DesignHeight);
        d.Image("HeaderSurface", 0, 0, DesignWidth, 146);
        d.Image("StarfieldBase", 26, 146, 727, 578);
        d.Frame(1, 1, 1098, 798, Lavender * .5f);
        d.Frame(10, 10, 1080, 780, Lavender * .13f);
        d.PageCorner(new Vector2(-3, -3), 0);
        d.PageCorner(new Vector2(DesignWidth - 31, -3), MathHelper.PiOver2);
        d.PageCorner(new Vector2(-3, DesignHeight - 31), -MathHelper.PiOver2);
        d.PageCorner(new Vector2(DesignWidth - 31, DesignHeight - 31), MathHelper.Pi);
        d.Line(new Vector2(32, 83), new Vector2(1068, 83), new Color(186, 158, 208) * (43 / 255f));
        for (int i = 0; i < 161; i++) d.Box(32 + i, 83, 1, 1, new Color(207, 182, 225) * MathHelper.Lerp(140 / 255f, 48 / 255f, i / 160f));
        d.CrossStar(new Vector2(192, 83), new Vector2(4.24f), new Color(203, 179, 222), .5f);
        // Retain the approved hat artwork and its ornament geometry; only move the group.
        d.Corners(new Vector2(61, 42), 54, 27, Lavender * .32f, MathHelper.PiOver4);
        d.Ring(new Vector2(61, 42), 38, 38, Lavender * .14f);
        d.Image("HeaderHat", 35, 16, 52, 52, Lavender);
        d.NotebookTitle(101, 21);
        string dust = _state.Points.ToString("00");
        float dustLeft = 1013 - d.LatinWidth(dust, 25);
        d.Icon("star", new Vector2(dustLeft - 58, 42), 27, Gold);
        d.Text("星尘", dustLeft - 35, 36, 11, new Color(196, 183, 206), spacing: 1);
        d.LatinText(dust, dustLeft, 29, 25, new Color(239, 223, 190));
        for (int i = 0; i < _state.Skills.Length; i++)
            if (_state.Learned.Contains(_state.Skills[i].id))
                d.Image("Glow", 908 + i * 7, 108, 14, 14, new Color(210, 170, 245) * .5f);
        for (int i = 0; i < _state.Skills.Length; i++)
            d.CrossStar(new Vector2(915 + i * 7, 115), new Vector2(4.24f),
                _state.Learned.Contains(_state.Skills[i].id) ? new Color(234, 211, 250) : new Color(131, 113, 143), .5f,
                _state.Learned.Contains(_state.Skills[i].id));
        d.LatinText(_state.Learned.Count.ToString("00"), 1007, 104, 19, new Color(233, 215, 246));
        d.LatinText($"/ {_state.Skills.Length}", 1038, 109, 12, new Color(155, 138, 168));
        d.Box(754, 146, 320, 578, new Color(25, 24, 37) * .4f);
        d.Line(new Vector2(26, 145), new Vector2(1074, 145), new Color(183, 149, 209) * (27 / 255f));
        d.Line(new Vector2(26, 725), new Vector2(1074, 725), LineColor);
        d.Line(new Vector2(753, 146), new Vector2(753, 724), Lavender * .25f);
        d.CrossStar(new Vector2(753, 146), new Vector2(8, 10), Lavender, .25f, false);
        d.Frame(572, 162, 160, 28, LineColor * .65f);
        d.Text(MathF.Round(_zoom * 100) + "%", 627, 170, 11, Lavender, .5f);
        d.Line(new Vector2(686, 168), new Vector2(686, 184), LineColor);
        d.Frame(47, 695, 14, 14, Lavender * .8f);
        d.Frame(50, 698, 8, 8, Lavender * .4f);
        d.Text("术式", 69, 696, 11, Muted);
        d.Corners(new Vector2(122, 702), 14, 5, Lavender * .8f);
        d.Corners(new Vector2(122, 702), 8, 2, Lavender * .4f);
        d.Text("心得", 137, 696, 11, Muted);
        DrawLoadoutCaption();
        d.Line(new Vector2(880, 754), new Vector2(880, 767), LineColor);
        d.Line(new Vector2(978, 754), new Vector2(978, 767), LineColor);
        d.Frame(1030, 753, 31, 17, LineColor);
        d.Text("Esc", 1045, 756, 9, Muted, .5f);
    }

    private void DrawFilter(int index, string filter, bool hover)
    {
        var d = _draw;
        float x = 36 + index * 145;
        bool active = _state.Filter == filter;
        Color color = filter == "ready" ? Gold : filter == "locked" ? Muted * .6f : Lavender;
        if (filter == "hidden") d.Ring(new Vector2(x + 4, 113), 4, 4, color, 1, true);
        else d.CrossStar(new Vector2(x + 4, 113), new Vector2(9), color, .5f, filter == "learned");
        d.Text(PreviewState.StatusName(filter), x + 17, 104, 14,
            active || hover ? new Color(240, 222, 247) : new Color(171, 154, 185), spacing: 1);
        d.LatinText(_state.Skills.Count(s => _state.Status(s) == filter).ToString("00"), x + 73, 106, 12,
            active ? new Color(228, 200, 239) : new Color(158, 139, 171));
        if (active) d.ActiveFilterMarker(new Vector2(x + 55, 139));
    }

    private void CameraControl(string icon, float x, bool hover, bool enabled)
    {
        if (icon == "center") _draw.Text("归位", x, 170, 10, hover ? Ink : Muted, .5f);
        else _draw.Icon(icon, new Vector2(x, 175), 15, !enabled ? Muted * .3f : hover ? Ink : Lavender);
    }

    private void DrawCloseButton(bool hover)
    {
        _draw.Corners(new Vector2(1053, 42), 29, 15, hover ? Ink : Lavender * .7f, MathHelper.PiOver4);
        _draw.Icon("close", new Vector2(1053, 42), 15, hover ? Ink : Lavender);
        _draw.LatinText("ESC", 1022, 56, 9, new Color(164, 149, 178), spacing: 1);
    }

    private void DrawMap()
    {
        var d = _draw;
        // Exact seeded distribution, 1/2px sizes and opacity range from the old HTML.
        // Stars stay fixed while the astronomical grid and nodes follow the camera.
        int seed = 173;
        float RandomStar() { seed = (seed * 9301 + 49297) % 233280; return seed / 233280f; }
        for (int i = 0; i < 125; i++)
        {
            float size = RandomStar() < .9f ? 1 : 2;
            float x = MapX + RandomStar() * MapWidth, y = MapY + RandomStar() * MapHeight;
            float opacity = .15f + RandomStar() * .5f;
            d.Image("Glow", x + size / 2 - 3, y + size / 2 - 3, 6, 6,
                new Color(198, 161, 253) * (.4f * opacity));
            d.Image("Disc", x, y, size, size, new Color(221, 212, 248) * opacity);
        }
        // Map the old 880x650 astronomical grid into the current world, retaining
        // its concentric rings, tilted orbit and 72 ticks instead of the new crosshair.
        Vector2 SkyPoint(float x, float y) => MapPoint(x / 880 * WorldWidth, y / 650 * WorldHeight);
        Vector2 center = SkyPoint(445, 324);
        float sx = WorldWidth / 880f * _zoom, sy = WorldHeight / 650f * _zoom;
        Color orbitColor = new Color(189, 173, 209) * .14f;
        foreach (float r in new[] { 258f, 248f, 188f, 132f })
            d.Ring(center, r * sx, r * sy, orbitColor * (r == 188 ? .07f : 1), .7f, r == 132);
        Vector2 Orbit(float angle)
        {
            float x = MathF.Cos(angle) * 359, y = MathF.Sin(angle) * 215, rotation = -.454f;
            return SkyPoint(445 + x * MathF.Cos(rotation) - y * MathF.Sin(rotation),
                324 + x * MathF.Sin(rotation) + y * MathF.Cos(rotation));
        }
        for (int i = 0; i < 180; i++)
            d.Line(Orbit(i * MathHelper.TwoPi / 180), Orbit((i + 1) * MathHelper.TwoPi / 180), orbitColor * .07f, .7f);
        for (int i = 0; i < 72; i++)
        {
            float angle = i * MathHelper.TwoPi / 72, radius = i % 6 == 0 ? 269 : 263;
            d.Line(SkyPoint(445 + MathF.Cos(angle) * 258, 324 + MathF.Sin(angle) * 258),
                SkyPoint(445 + MathF.Cos(angle) * radius, 324 + MathF.Sin(angle) * radius), orbitColor, .7f);
        }
        foreach (var mark in new[] { new Vector4(445, 51, 445, 71), new Vector4(445, 577, 445, 597),
            new Vector4(172, 324, 197, 324), new Vector4(693, 324, 718, 324),
            new Vector4(252, 131, 268, 147), new Vector4(622, 485, 638, 501),
            new Vector4(252, 517, 268, 501), new Vector4(622, 163, 638, 147),
            new Vector4(434, 45, 445, 36), new Vector4(445, 36, 456, 45),
            new Vector4(434, 603, 445, 612), new Vector4(445, 612, 456, 603) })
            d.Line(SkyPoint(mark.X, mark.Y), SkyPoint(mark.Z, mark.W), orbitColor, .7f);
        d.CrossStar(SkyPoint(87, 182), new Vector2(16 * sx, 16 * sy), new Color(209, 188, 241) * .22f, .25f);
        d.CrossStar(SkyPoint(743, 549), new Vector2(14 * sx, 14 * sy), new Color(209, 188, 241) * .22f, .28f);
        d.CrossStar(SkyPoint(422, 477), new Vector2(10 * sx, 10 * sy), new Color(209, 188, 241) * .22f, .2f);
        foreach (var skill in _state.Skills)
        foreach (string parentId in skill.prereqs) DrawEdge(_state.Find(parentId), skill);
        foreach (var skill in _state.Skills.Where(s => s.id != _state.Selected)) DrawNode(skill);
        DrawNode(_state.Current);
    }

    private void DrawEdge(PreviewSkill parent, PreviewSkill skill)
    {
        bool related = (skill.id == _state.Selected || _ancestors.Contains(skill.id)) && _ancestors.Contains(parent.id);
        string status = _state.Status(skill);
        bool dim = status != _state.Filter && _state.Status(parent) != _state.Filter;
        // Straight routes and the old ready/hidden dash pattern; leave a gap around
        // unmasked artwork, since the new nodes no longer have an opaque backing.
        Vector2 a = new(parent.x, parent.y), b = new(skill.x, skill.y), delta = b - a;
        float axis = Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
        a += delta / axis * 36;
        b -= delta / axis * (skill.id == _state.Selected ? 41 : 36);
        a = MapPoint(a.X, a.Y); b = MapPoint(b.X, b.Y);
        _draw.SkillLink(a, b, status == "ready" ? "available" : status, related, dim, _zoom);
    }

    private void DrawNode(PreviewSkill s)
    {
        var d = _draw;
        string status = _state.Status(s);
        bool hidden = status == "hidden", selected = s.id == _state.Selected, hover = s.id == _hoverNode;
        bool insight = !hidden && !s.Active, owned = _state.Learned.Contains(s.id);
        bool off = owned && !_state.Availability(s.id).Ok;
        var p = MapPoint(s.x, s.y);
        float alpha = status == _state.Filter ? 1 : hover ? .8f : .18f;
        Color accent = (selected || status == "ready" ? Gold : hover ? Ink : Lavender) * alpha;
        if (selected || hover) d.Image("Glow", p.X - 50 * _zoom, p.Y - 50 * _zoom, 100 * _zoom, 100 * _zoom, Lavender * (.1f * alpha));
        if (hidden)
        {
            float r = 33 * _zoom;
            d.Dash(p + new Vector2(-r, -r), p + new Vector2(r, -r), accent * .4f, 1, 2 * _zoom, 4 * _zoom);
            d.Dash(p + new Vector2(r, -r), p + new Vector2(r, r), accent * .4f, 1, 2 * _zoom, 4 * _zoom);
            d.Dash(p + new Vector2(r, r), p + new Vector2(-r, r), accent * .4f, 1, 2 * _zoom, 4 * _zoom);
            d.Dash(p + new Vector2(-r, r), p + new Vector2(-r, -r), accent * .4f, 1, 2 * _zoom, 4 * _zoom);
        }
        else if (insight)
        {
            d.Corners(p, 70 * _zoom, 16 * _zoom, accent * .8f);
            d.Corners(p, 60 * _zoom, 6 * _zoom, accent * .44f);
        }
        else
        {
            d.Corners(p, (selected ? 58 : 54) * _zoom, (selected ? 13 : 9) * _zoom,
                accent * (selected || hover ? 1 : status == "locked" ? .42f : .7f), MathHelper.PiOver4);
            d.Corners(p, 56 * _zoom, 7 * _zoom, accent * .4f);
        }
        d.SkillIcon(s, p, (hidden ? 38 : 50) * _zoom, (hidden ? Muted : Color.White) * alpha
            * (off ? .38f : status == "locked" ? .48f : 1), hidden, off ? .6f : status == "locked" ? .55f : 0);
        if (selected && !insight) d.CrossStar(p - new Vector2(0, 48 * _zoom), new Vector2(7 * _zoom), Gold * alpha, .5f);
        if (selected && _flash > 0) d.Ring(p, (30 + (1 - _flash) * 45) * _zoom,
            (30 + (1 - _flash) * 45) * _zoom, Gold * _flash * .7f, 1.5f);
        if (owned)
        {
            var badge = p + new Vector2(0, 38 * _zoom);
            d.Box(badge.X - 24 * _zoom, badge.Y, 48 * _zoom, 14 * _zoom, new Color(39, 32, 51) * (.9f * alpha));
            d.Text("Lv.", badge.X - 18 * _zoom, badge.Y + 3 * _zoom, 8 * _zoom, Muted * alpha, serif: true);
            d.Text(_state.Level(s.id).ToString(), badge.X + 7 * _zoom, badge.Y, 11 * _zoom,
                (off ? new Color(173, 145, 152) : _state.Level(s.id) == _state.MaxLevel(s.id) ? Gold : Ink) * alpha, .5f, serif: true);
        }
        string name = _state.Name(s);
        float textWidth = Math.Min(136 * _zoom, d.Measure(name, 12 * _zoom, .5f * _zoom, true) + 16 * _zoom);
        d.Image("Glow", p.X - textWidth / 2, p.Y + 53 * _zoom, textWidth, 22 * _zoom, new Color(39, 32, 51) * alpha);
        d.FittedText(name, p.X, p.Y + 56 * _zoom, 136 * _zoom, 12 * _zoom,
            (selected ? Gold : off ? new Color(179, 160, 186) : owned ? Ink : Muted) * alpha, .5f, serif: !hidden, spacing: .5f * _zoom);
        int equipped = Array.IndexOf(_state.Slots, s.id);
        if (equipped >= 0)
        {
            d.Box(p.X + 23 * _zoom, p.Y - 44 * _zoom, 20 * _zoom, 15 * _zoom, new Color(48, 35, 57) * alpha);
            d.Frame(p.X + 23 * _zoom, p.Y - 44 * _zoom, 20 * _zoom, 15 * _zoom, Lavender * .5f * alpha);
            d.Text(SlotKeys[equipped], p.X + 33 * _zoom, p.Y - 42 * _zoom, 9 * _zoom, Lavender * alpha, .5f, serif: true);
        }
        if (hover) _tooltip = hidden ? "未显现" : $"{s.name} · {(insight ? "心得" : "术式")} · {PreviewState.StatusName(status)}"
            + (owned ? $" · Lv. {_state.Level(s.id)} / {_state.MaxLevel(s.id)}" : "")
            + (off ? " · " + _state.Availability(s.id).Reason : "");
    }

    private void DrawDetails()
    {
        var d = _draw; var s = _state.Current;
        if (_state.EmptyFilter)
        {
            d.Text(PreviewState.StatusName(_state.Filter) + " · 暂无", 914, 422, 15, Muted, .5f, serif: true);
            return;
        }
        bool hidden = !_state.Discovered(s), insight = !hidden && !s.Active;
        d.Text(hidden ? "未显现" : insight ? "心得" : "术式", 776, 164, 12, Muted, serif: true, spacing: 2);
        if (_state.Learned.Contains(s.id) && !_state.Availability(s.id).Ok)
            d.Text(s.toggleable && _state.Disabled.Contains(s.id) ? "已关闭" : "未生效", 1052, 165, 11, new Color(214, 177, 155), 1);
        var center = new Vector2(808.5f, s.effect != null && !hidden ? 221 : 226);
        if (insight)
        {
            d.Corners(center, 71, 16, Lavender * .6f);
            d.Corners(center, 61, 6, Lavender * .33f);
        }
        else
        {
            d.Frame(center.X - 32.5f, center.Y - 32.5f, 65, 65, Lavender * .6f);
            d.Frame(center.X - 28.5f, center.Y - 28.5f, 57, 57, Lavender * .25f);
        }
        d.SkillIcon(s, center, 50, hidden ? Lavender : Color.White, hidden);
        d.FittedText(_state.Name(s), 857, center.Y - 12, 195, 22, Ink, serif: true, spacing: 1);
    }



    private void DrawActionButton(bool hover)
    {
        var d = _draw; var s = _state.Current;
        bool owned = _state.Learned.Contains(s.id);
        if (_state.EmptyFilter || owned && !s.Active) return;
        string status = _state.Status(s);
        bool enabled = _state.CanUnlock || _state.CanEquip;
        bool equipped = owned && !_state.CanEquip;
        d.Box(776, 670, 276, 38, enabled ? hover ? new Color(238, 219, 247) : new Color(216, 195, 231)
            : equipped ? new Color(57, 40, 70) : new Color(46, 36, 55));
        d.Frame(776, 670, 276, 38, enabled ? new Color(239, 220, 247) : Lavender * .35f);
        d.Frame(780, 674, 268, 30, enabled ? new Color(115, 83, 131) * .45f : Lavender * .12f);
        string label = owned ? equipped ? $"已装配 · {SlotKeys[_state.TargetSlot]}" : "装配"
            : status == "hidden" ? "未显现" : status == "locked" ? "待启封" : _state.CanUnlock ? "研习" : "星尘不足";
        Color color = enabled ? new Color(50, 36, 62) : equipped ? new Color(229, 203, 247) : Muted;
        d.FittedText(label, enabled ? 888 : 914, 681, 245, 14, color, .5f, serif: true, spacing: 2);
        if (!enabled) return;
        d.Line(new Vector2(943, 680), new Vector2(943, 698), color * .35f);
        d.Text(_state.CanEquip ? SlotKeys[_state.TargetSlot] : s.cost.ToString(), 978, 681, 15, color, .5f, serif: true);
        if (_state.CanUnlock) d.CrossStar(new Vector2(958, 689), new Vector2(9), color, .2f);
    }

    private void DrawSlot(int slot, bool hover)
    {
        var d = _draw;
        float x = SlotLeft + slot * SlotStride;
        float y = SlotTop;
        var center = new Vector2(x + SlotSize / 2f, y + SlotSize / 2f);
        bool selected = _state.TargetSlot == slot;
        var gold = new Color(227, 193, 141);
        if (selected)
            d.Image("Glow", x - 12, y - 12, SlotSize + 24, SlotSize + 24, new Color(195, 162, 118) * .08f);
        d.Box(x, y, SlotSize, SlotSize, selected ? new Color(185, 155, 87) * .125f : new Color(33, 27, 44));
        // CSS inset shadow: a subtle three-pixel rim, with the artwork above it.
        Color inset = selected ? new Color(185, 155, 87) * .106f : new Color(52, 35, 62) * .333f;
        for (int i = 1; i <= 3; i++) d.Frame(x + i, y + i, SlotSize - i * 2, SlotSize - i * 2, inset);
        d.Frame(x, y, SlotSize, SlotSize, selected ? gold : hover ? Lavender : new Color(164, 136, 179) * .34f);
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
            d.SkillIcon(_state.Find(id), center, 32, Color.White * (effective ? 1 : .3f), grayscale: effective ? 0 : .8f);
        }
        float labelWidth = d.SlotLabelWidth(SlotKeys[slot], 9);
        d.Box(x - 3, y - 6, labelWidth + 6, 10, new Color(39, 32, 47));
        d.SlotLabel(SlotKeys[slot], x, y - 6, 9, new Color(194, 169, 213));
        if (selected) d.CrossStar(new Vector2(center.X, y + SlotSize + 2), new Vector2(8.5f), gold, .5f);
        if (hover) _tooltip = id == null ? $"{SlotKeys[slot]} · 未装配" : $"{SlotKeys[slot]} · {_state.Find(id).name} · Lv. {_state.Level(id)}"
            + (_state.Availability(id).Ok ? "" : " · " + _state.Availability(id).Reason);
    }

    private void DrawLoadoutCaption()
    {
        _draw.Text("装配", 34, 754, 13, Ink, spacing: 1, serif: true);
        _draw.Text("当前目标", TargetLeft, 746, 10, new Color(185, 161, 201), spacing: .5f);
        _draw.SlotLabel(SlotKeys[_state.TargetSlot], TargetLeft + 50, 746, 11, new Color(235, 208, 168));
    }

    private void DrawClearSlot(bool hover)
    {
        Color color = new Color(143, 124, 158);
        color = _state.Slots[_state.TargetSlot] == null ? color * .3f : hover ? Ink : color;
        _draw.Text("清空此槽", TargetLeft, 766, 10, color);
        _draw.Line(new Vector2(TargetLeft, 779), new Vector2(TargetLeft + _draw.Measure("清空此槽", 10), 779), color);
    }

    private void DrawModal()
    {
        if (_modal == null) return;
        var d = _draw;
        d.Box(0, 0, DesignWidth, DesignHeight, new Color(16, 13, 25) * .88f);
        d.Box(335, 135, 430, 530, new Color(44, 33, 56));
        d.Frame(335, 135, 430, 530, Lavender * .7f);
        d.Icon("close", new Vector2(735, 166), 16, Muted);
        d.Text("手札", 368, 171, 23, Ink, spacing: 1, serif: true);
        string[] titles = ["已习得 → 待研习 → 待启封 → 未显现", "术式 / 心得", "研习 / 进修", "关联", "操作"];
        string[] descriptions = ["已掌握、可学习、前置未满足、尚未发现。", "术式可装配。心得习得后生效，部分可开关。",
            "研习获得技能，进修提升等级，均消耗星尘。", "关闭心得会暂停依赖技能，保留等级与装配。",
            $"{ToggleKeyLabel} 打开手札 · Esc 返回。星图内拖动平移、滚轮缩放。详情内滚轮翻阅。"];
        float y = 222;
        for (int i = 0; i < titles.Length; i++)
        {
            d.Text(titles[i], 368, y, 13, Ink);
            y = d.Paragraph(descriptions[i], 368, y + 23, 364, 12, Muted, 21) + 12;
        }
        d.Text("独立预览 · 不修改角色成长或实际战斗", 550, 551, 11, Muted, .5f);
        d.Box(368, 583, 364, 42, Lavender);
        d.Text("返回", 550, 596, 14, new Color(50, 36, 62), .5f);
    }

    public static bool TogglePreview()
    {
        if (Main.dedServ || Main.gameMenu || SilkyUISystem.ServiceProvider == null) return false;
        if (!SilkyUIRenderSystem.Instance.TryGetInstance<ConstellationPreviewUI>(out var body)) return false;
        if (body.Enabled) body.Close();
        else body.Enabled = true;
        return true;
    }

    private sealed class PaintView(Action<PaintView, SpriteBatch> paint) : UIView
    {
        protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch) => paint(this, spriteBatch);
    }
}

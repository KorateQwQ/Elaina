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
using Terraria.GameInput;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;
using static 伊蕾娜.ElainaModAlchemy.UI.AlchemyNotebookPresentation;

namespace 伊蕾娜.ElainaModAlchemy.UI;

[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Alchemy Notebook", 1001)]
public sealed partial class AlchemyNotebookPanel : BaseBody
{
    private readonly AlchemyNotebookState _state = new();
    private readonly AlchemySelectionQuillMotion _quill = new();
    private readonly AlchemyCardMotion _cardMotion = new();
    private readonly List<(UIView View, Rectangle Area)> _regions = [];
    private readonly List<(PaintView View, AlchemyCatalogRecipe Recipe)> _cards = [];
    private readonly List<(PaintView View, AlchemyNotebookRow Row)> _rows = [];
    private readonly List<PaintView> _bag = [];
    private readonly List<UIView> _quantityControls = [];
    private AlchemyDrawing _draw;
    private ConstellationTween _tabMarker;
    private AlchemyPageScrollView _catalog, _detail;
    private SUIScrollView _bagScroll;
    private AlchemyCatalogMaterial[] _bagMaterials = [];
    private float _scale = 1, _lastScale = 1, _brewTime, _flash, _toastTime;
    private string _craftedId, _toast;
    private bool _dirty = true, _resetScroll = true;
    private bool _revealSelection;
    private KeyboardState _keyboard;
    private Player _owner;
    private float _inventoryRefresh;
    private int _lastResultSequence;
    private const float BrewDuration = .65f;
    private bool Brewing => _brewTime > 0;
    private float BrewProgress => Brewing ? Math.Clamp(1 - _brewTime / BrewDuration, .001f, .999f) : 0;
    public override bool IsInteractable => !Main.gameMenu;

    public AlchemyNotebookPanel() { Enabled = false; }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Border = 0;
        Padding = new Margin(0);
        BackgroundColor = Color.Transparent;
        FitWidth = FitHeight = false;
        SetLeft(0, 0, .5f);
        SetTop(0, 0, .5f);
        Fit(GraphicsDeviceHelper.GetBackBufferSizeByUIScale());
        _draw = new AlchemyDrawing();
        for (int i = 0; i < Navigation.Count; i++)
        {
            var category = Navigation[i];
            Button(new((int)MathF.Round(26 + i * TabWidth), 84, (int)MathF.Ceiling(TabWidth), 62), () =>
            {
                if (Brewing || _state.IsBusy) return;
                _state.SetCategory(category.Id);
                _catalog.ResetPosition();
                _bagScroll.ScrollBar.SetScrollPosition(Vector2.Zero);
                Dirty(true);
            }, (d, h) => Category(d, _state, category, h));
        }
        _catalog = (AlchemyPageScrollView)Scroll(new(CatalogX, CatalogY, CatalogWidth, CatalogHeight), false);
        _catalog.CanDrag = e => !_cards.Any(c => c.Recipe != null && c.View.ContainsPoint(e.MousePosition));
        _catalog.Container.FitHeight = false;
        _catalog.Container.FlexShrink = 0;
        for (int i = 0; i < AlchemyCatalog.Recipes.Count; i++)
        {
            int index = i;
            var view = new PaintView((v, batch) => WithLocal(v, batch, d =>
            {
                // The selected card is drawn by the overlay so it sits above its neighbours.
                if (_cards[index].Recipe is not { } r || r.Id == _state.SelectedId) return;
                Card(d, _state, r, v.IsMouseHovering, _craftedId == r.Id ? _flash : 0, PoseOf(r, v.IsMouseHovering));
            })) { Border = 0, Padding = new Margin(0), FlexShrink = 0, FitWidth = false, FitHeight = false }.Join(_catalog.Container);
            view.LeftMouseClick += (_, _) => { if (!_catalog.Motion.DidDrag && _cards[index].Recipe is { } r) Select(r.Id); };
            _cards.Add((view, null));
        }
        // The viewport overlay shares the catalog's native clipping and draws the picked-up
        // card and quill after every other card, including when the destination is offscreen.
        var quillLayer = new PaintView((_, batch) => DrawSelectionQuill(batch))
        {
            Positioning = Positioning.Absolute, ZIndex = 1, IgnoreMouseInteraction = true,
            Border = 0, Padding = new Margin(0), FitWidth = false, FitHeight = false
        }.Join(_catalog.Mask);
        quillLayer.SetSize(0, 0, 1, 1);
        _detail = (AlchemyPageScrollView)Scroll(new(DetailX, DetailY, DetailWidth, DetailHeight), true);
        _detail.CanDrag = e => !_rows.Any(r => r.View == e.Source && r.Row != null &&
            (r.Row.BlocksDragging || r.Row.EntryFor((e.MousePosition - r.View.Bounds.Position) / _scale) != null));
        _quantityControls.Add(Button(new(826, 615, 28, 27), () => ChangeQuantity(-1), (d, h) => SmallButton(d, "−", h, _state.Quantity > 1)));
        _quantityControls.Add(Button(new(882, 615, 28, 27), () => ChangeQuantity(1), (d, h) => SmallButton(d, "+", h, _state.Quantity < _state.MaxBatches(_state.Current))));
        _quantityControls.Add(Button(new(919, 615, 42, 27), () => { _state.SetQuantity(_state.MaxBatches(_state.Current)); Dirty(false); },
            (d, h) => Label(d, LocalizeUI("Max"), 21, 6, 10, h ? Ink : Muted, .5f)));
        Button(CraftBounds, PrimaryAction, (d, h) => CraftButton(d, _state, h, BrewProgress));
        Button(CloseButtonBounds, Close, CloseButton);
#if DEBUG
        Button(DebugLevelUpBounds, () =>
        {
            if (Brewing || _state.IsBusy) return;
            _state.DebugLevelUp(); ProcessResult();
        }, (d, h) => DebugLevelUpButton(d, _state, h, !Brewing && !_state.IsBusy));
        Button(new(608, 754, 66, 26), () => { if (Brewing || _state.IsBusy) return; _state.DebugRestock(); ProcessResult(); },
            (d, h) => DebugButton(d, LocalizeUI("AlchemyDebugRestock"), h));
        Button(new(680, 754, 66, 26), () =>
        {
            if (Brewing || _state.IsBusy) return;
            _state.DebugReset(); ProcessResult(); Dirty(false);
        }, (d, h) => DebugButton(d, LocalizeUI("AlchemyDebugReset"), h));
#endif
        _bagScroll = Scroll(BagBounds, false, Direction.Horizontal);
        _bagScroll.Container.FlexWrap = false;
        _bagScroll.Container.FlexShrink = 0;
        _bagScroll.Container.FitWidth = _bagScroll.Container.FitHeight = false;
        for (int i = 0; i < AlchemyCatalog.Materials.Count; i++)
        {
            int index = i;
            var view = new PaintView((v,batch) => WithLocal(v,batch,d =>
            {
                if(index < _bagMaterials.Length) MaterialSlot(d,_state,_bagMaterials[index],v.IsMouseHovering);
            })) { Border=0, Padding=new Margin(0), FlexShrink=0, FitWidth=false, FitHeight=false }.Join(_bagScroll.Container);
            view.LeftMouseClick += (_,_) =>
            {
                if (_book.IsOpen && index < _bagMaterials.Length && _bagMaterials[index].Entry is { } entry) Select(entry);
            };
            _bag.Add(view);
        }
        LayoutViews();
    }

    public override UIView GetElementAt(Vector2 mousePosition) => _book.Moving ? this : base.GetElementAt(mousePosition);

    private PaintView Button(Rectangle area, Action click, Action<AlchemyDrawing, bool> paint)
    {
        var view = new PaintView((v, batch) => WithLocal(v, batch, d => paint(d, v.IsMouseHovering)))
        { Positioning = Positioning.Absolute, Border = 0, Padding = new Margin(0) }.Join(this);
        view.LeftMouseClick += (_, _) => { if (_book.IsOpen) click(); };
        _regions.Add((view, area));
        return view;
    }

    private SUIScrollView Scroll(Rectangle area, bool column, Direction direction = Direction.Vertical)
    {
        SUIScrollView scroll = direction == Direction.Vertical
            ? new AlchemyPageScrollView { AcceptInput = () => _book.IsOpen && !_book.Moving }
            : new SUIScrollView(direction);
        scroll.Positioning = Positioning.Absolute;
        scroll.Border = 0;
        scroll.Padding = new Margin(0);
        scroll.Gap = new Size(0);
        scroll.BackgroundColor = Color.Transparent;
        scroll.Join(this);
        _regions.Add((scroll, area));
        scroll.Mask.IndependentRenderTarget = false;
        scroll.Mask.Border = 0;
        scroll.Mask.Padding = new Margin(0);
        var c = scroll.Container;
        c.FlexDirection = column ? FlexDirection.Column : FlexDirection.Row;
        c.MainAlignment = MainAlignment.Start;
        c.FlexWrap = !column;
        c.Border = 0;
        c.Padding = new Margin(0);
        c.Gap = new Size(0);
        c.SetHeight(0, 0);
        c.FitHeight = true;
        scroll.ScrollBar.Positioning = Positioning.Absolute;
        scroll.ScrollBar.BackgroundColor = Color.Transparent;
        // PaintScrollBar draws the page's own bar over SUI's hit area.
        scroll.ScrollBar.BarColor = (Color.Transparent, Color.Transparent);
        scroll.ScrollBar.Border = 0;
        scroll.ScrollBar.Padding = new Margin(0);
        scroll.MouseWheel += (_, e) => e.LockScroll(scroll);
        return scroll;
    }

    private void Fit(Size screen)
    {
        _scale = Math.Min(1, Math.Min(Math.Max(1, screen.Width - 40) / (float)DesignWidth, Math.Max(1, screen.Height - 40) / (float)DesignHeight));
        SetSize(DesignWidth * _scale, DesignHeight * _scale, 0, 0);
        if (_draw != null) LayoutViews();
    }
    protected override void OnScreenSizeChanged(Size current, Size previous) { Fit(current); base.OnScreenSizeChanged(current, previous); }

    private void WithLocal(UIView view, SpriteBatch batch, Action<AlchemyDrawing> paint)
    {
        _draw.Batch = batch;
        _draw.Origin = view.Bounds.Position;
        _draw.Scale = _scale;
        paint(_draw);
    }
    private void Dirty(bool reset) { _dirty = true; _resetScroll |= reset; }
    private AlchemyCardPose PoseOf(AlchemyCatalogRecipe r, bool hover)
        => _cardMotion.Pose(r.Id, Number(r), r.Id == _state.SelectedId, hover);
    private void DrawSelectionQuill(SpriteBatch batch)
    {
        var (selected, recipe) = _cards.FirstOrDefault(c => c.Recipe?.Id == _state.SelectedId);
        if (selected == null) { _quill.Reset(); return; }
        _draw.Batch = batch;
        _draw.Scale = _scale;
        _draw.Origin = selected.Bounds.Position;
        var pose = PoseOf(recipe, selected.IsMouseHovering);
        Card(_draw, _state, recipe, selected.IsMouseHovering, _craftedId == recipe.Id ? _flash : 0, pose);
        if (!_book.IsOpen || _book.Moving) return;
        _draw.Origin = _quill.ResolvePosition(_state.SelectedId, selected.Bounds.Position + CardOffset(recipe) * _scale,
            _catalog.Container.InnerBounds.Position, _catalog.Container.ScrollOffset, _scale);
        SelectionQuill(_draw, pose);
    }
    private void Select(string id)
    {
        if (Brewing || !_book.IsOpen) return;
        if (id != _state.SelectedId) _cardMotion.PickUp(id);
        _state.Select(id);
        _bagScroll.ScrollBar.SetScrollPosition(Vector2.Zero);
        _revealSelection = true;
        Dirty(true);
    }
    private void ChangeQuantity(int delta) { if (Brewing || !_book.IsOpen) return; _state.SetQuantity(_state.Quantity + delta); Dirty(false); }
    private void Notify(string message) { _toast = message; _toastTime = 3; }
    private void PrimaryAction()
    {
        if (Brewing || !_book.IsOpen || _state.IsBusy) return;
        _state.Refresh();
        if (_state.IsUnlocked(_state.Current)) _state.TryCraft();
        else _state.TryResearch();
        ProcessResult();
        Dirty(false);
    }

    private void ProcessResult()
    {
        var result = _state.LastResult;
        if (result == null || result.Sequence == _lastResultSequence) return;
        _lastResultSequence = result.Sequence;
        if (result.Success && result.Action == AlchemyNotebookAction.Craft)
        {
            _craftedId = result.EntryId; _toast = result.Message;
            _toastTime = 0; _brewTime = BrewDuration;
        }
        else
        {
            if (result.Success && result.Action == AlchemyNotebookAction.Research)
            {
                _toastTime = 0;
                _craftedId = result.EntryId;
                _flash = 1;
            }
            else Notify(result.Message);
        }
        Dirty(false);
    }

    private void RefreshViews()
    {
        _dirty = false;
        var visible = _state.Visible;
        for (int i = 0; i < _cards.Count; i++)
        {
            var view = _cards[i].View;
            var r = i < visible.Count ? visible[i] : null;
            view.Invalid = r == null;
            _cards[i] = (view, r);
        }
        var rows = DetailRows(_draw, _state);
        while (_rows.Count < rows.Count)
        {
            int index = _rows.Count;
            var view = new PaintView((v, batch) => WithLocal(v, batch, d =>
                _rows[index].Row?.Paint(d, v.IsMouseHovering ? (Main.MouseScreen - v.Bounds.Position) / _scale : null)))
            { Border = 0, Padding = new Margin(0), FlexShrink = 0, FitWidth = false, FitHeight = false }.Join(_detail.Container);
            view.LeftMouseClick += (_, e) =>
            {
                if (!_detail.Motion.DidDrag && _rows[index].Row?.EntryFor((e.MousePosition - view.Bounds.Position) / _scale) is { } entry) Select(entry);
            };
            _rows.Add((view, null));
        }
        for (int i = 0; i < _rows.Count; i++)
        {
            var view = _rows[i].View;
            var row = i < rows.Count ? rows[i] : null;
            view.Invalid = row == null;
            _rows[i] = (view, row);
        }
        _bagMaterials = BagMaterials(_state);
        for (int i = 0; i < _bag.Count; i++) _bag[i].Invalid = i >= _bagMaterials.Length;
        foreach (var view in _quantityControls) view.Invalid = !_state.Current.Craftable || !_state.IsUnlocked(_state.Current);
        LayoutViews();
        if (_resetScroll)
        {
            if (_revealSelection)
            {
                // Glide to the picked entry like a wheel scroll instead of cutting to it.
                float offset = _catalog.Motion.Target;
                float target = AlchemyCatalogLayout.Reveal(_state.Category, _state.SelectedId, offset, CatalogHeight);
                if (target != offset) _catalog.Motion.Reveal(target);
            }
            else _catalog.ResetPosition(_catalog.Motion.Position);
            _detail.ResetPosition();
            _revealSelection = false;
            _resetScroll = false;
        }
    }

    private void LayoutViews()
    {
        foreach (var (view, area) in _regions)
        {
            view.SetLeft(area.X * _scale, 0, 0);
            view.SetTop(area.Y * _scale, 0, 0);
            view.SetSize(area.Width * _scale, area.Height * _scale, 0, 0);
        }
        if (_catalog == null || _detail == null) return;
        _catalog.Container.FitHeight = true;
        _catalog.Container.Gap = new Size(AlchemyCatalogLayout.GapX * _scale, AlchemyCatalogLayout.GapY * _scale);
        _catalog.Container.Padding = new Margin(AlchemyCatalogLayout.Left * _scale, AlchemyCatalogLayout.Top * _scale, 0, AlchemyCatalogLayout.Bottom * _scale);
        foreach (var (v, _) in _cards) v.SetSize(CardWidth * _scale, CardHeight * _scale, 0, 0);
        foreach (var (v, row) in _rows) if (row != null) v.SetSize(DetailContentWidth * _scale, row.Height * _scale, 0, 0);
        float catalogContent = AlchemyCatalogLayout.HeightFor(_state.Category);
        _catalog.SetPageRange(CatalogHeight, catalogContent, _scale);
        _detail.SetPageRange(DetailHeight, _rows.Sum(r => r.Row?.Height ?? 0), _scale);
        if (_bagScroll != null)
        {
            float content = Math.Max(BagBounds.Width, _bagMaterials.Length * 48);
            _bagScroll.Container.SetSize(content * _scale, 60 * _scale, 0, 0);
            foreach (var v in _bag) v.SetSize(48 * _scale, 60 * _scale, 0, 0);
            var bar = _bagScroll.ScrollBar;
            var position = bar.CurrentScrollPosition * (_scale / _lastScale);
            PlaceBar(bar, BagBounds, BagScrollBar);
            bar.Invalid = content <= BagBounds.Width;
            bar.SetHScrollRange(BagBounds.Width * _scale, content * _scale);
            KeepPosition(bar, position);
        }
        _lastScale = _scale;
    }
    private void PlaceBar(SUIScrollbar bar, Rectangle region, Rectangle hit)
    {
        bar.SetLeft((hit.X - region.X) * _scale, 0, 0);
        bar.SetTop((hit.Y - region.Y) * _scale, 0, 0);
        bar.SetSize(hit.Width * _scale, hit.Height * _scale, 0, 0);
    }
    // Rescale or clamp only when needed, so a running glide or wheel scroll is not cut short.
    private static void KeepPosition(SUIScrollbar bar, Vector2 position)
    {
        position = Vector2.Clamp(position, Vector2.Zero, bar.GetScrollRange());
        if (position != bar.CurrentScrollPosition) bar.SetScrollPosition(position);
    }
    // The page's own scroll bar (dotted track, lavender thumb) over SUI's wider hit area.
    private void PaintScrollBar(AlchemyDrawing d, SUIScrollbar bar)
    {
        if (bar.Invalid) return;
        Vector2 at = (bar.InnerBounds.Position - Bounds.Position) / _scale, size = (Vector2)bar.InnerBounds.Size / _scale;
        Vector2 thumb = bar.GetBarSize(), offset = bar.GetBarPosition(thumb) / _scale;
        thumb /= _scale;
        Color color = bar.LeftMousePressed || bar.BarContainsPoint() ? new Color(222, 206, 245) * .95f : Lavender * .7f;
        if (bar.ScrollDirection == Direction.Vertical)
        {
            float x = at.X + size.X / 2;
            d.Dots(x, at.Y + 1.5f, x, at.Y + size.Y - 1.5f, AlchemyDrawing.DotColor);
            d.RoundedBar(x - 1.5f, at.Y + offset.Y, 3, thumb.Y, color);
        }
        else
        {
            float y = at.Y + size.Y / 2;
            d.Dots(at.X + 1.5f, y, at.X + size.X - 1.5f, y, AlchemyDrawing.DotColor);
            d.RoundedBar(at.X + offset.X, y - 1.5f, thumb.X, 3, color);
        }
    }
    private int ActiveTab()
    {
        for (int i = 0; i < Navigation.Count; i++) if (Navigation[i].Id == _state.Category) return i;
        return 0;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        if (Main.gameMenu || _owner == null || _owner != Main.LocalPlayer || !_owner.active) { CloseImmediately(); return; }
        if (!Main.hasFocus) _book.Finish();
        else _book.Advance(_bookClock.Sample());
        if (!_book.Moving) _bookClock.Stop();
        if (_book.IsClosed) { CloseImmediately(); return; }
        if (!Main.hasFocus || !Main.mouseLeft) StopDragging();
        float dt = Main.hasFocus ? Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0, .1f) : 0;
        _quill.Advance(dt);
        _tabMarker ??= new ConstellationTween(ActiveTab(), .18f);
        _tabMarker.SetTarget(ActiveTab());
        _tabMarker.Advance(dt);
        string hovered = _cards.FirstOrDefault(c => c.Recipe != null && c.View.IsMouseHovering).Recipe?.Id;
        _cardMotion.Advance(dt, _state.Visible, _state.SelectedId, hovered);
        _inventoryRefresh -= dt;
        if (_inventoryRefresh <= 0)
        {
            _inventoryRefresh = .2f;
            if (_state.Refresh()) Dirty(false);
            ProcessResult();
        }
        if (Brewing)
        {
            _brewTime = Math.Max(0, _brewTime - dt);
            if (!Brewing) { _flash = 1; _toastTime = 3; _state.SetQuantity(_state.Quantity); Dirty(false); }
        }
        _flash = Math.Max(0, _flash - dt * 1.25f);
        _toastTime = Math.Max(0, _toastTime - dt);
        if (_dirty) RefreshViews();
        foreach (var (view, _) in _cards) view.DisableMouseInteraction = Brewing;
        foreach (var view in _quantityControls) view.DisableMouseInteraction = Brewing;
        var keys = Keyboard.GetState();
        if (Main.hasFocus && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest)
        {
            bool Press(Keys key) => keys.IsKeyDown(key) && !_keyboard.IsKeyDown(key);
            if (Press(Keys.Escape)) Close();
            if (!Brewing && _book.IsOpen)
            {
                int horizontal = Press(Keys.Left) ? -1 : Press(Keys.Right) ? 1 : 0;
                int vertical = Press(Keys.Up) ? -1 : Press(Keys.Down) ? 1 : 0;
                if (horizontal != 0 || vertical != 0)
                    Select(AlchemyCatalogLayout.Move(_state.Category, _state.SelectedId, horizontal, vertical));
            }
        }
        _keyboard = keys;
        base.UpdateStatus(gameTime);
        if (IsMouseHovering || _book.Moving) Main.LocalPlayer.mouseInterface = true;
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        WithLocal(this, spriteBatch, d =>
        {
            Chrome(d, _state);
            Hero(d, _state, BrewProgress);
            Footer(d, _state, Brewing);
            if (_state.Visible.Count == 0) Label(d, LocalizeUI("EmptyPage"), 384, 315, 14, Muted, .5f, true);
        });
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.DrawChildren(gameTime, spriteBatch);
        WithLocal(this, spriteBatch, d =>
        {
            d.CatalogFade();
            if (_detail.Motion.Position + DetailHeight < _rows.Sum(r => r.Row?.Height ?? 0) - 1)
                d.DetailFade();
            // Above the catalog's top fade, like the HTML chrome layer over the catalog.
            TabMarker(d, _tabMarker?.Value ?? ActiveTab());
            PaintScrollBar(d, _bagScroll.ScrollBar);
        });
        WithLocal(this, spriteBatch, d =>
        {
            int hovered = _bag.FindIndex(v => !v.Invalid && v.IsMouseHovering);
            if (hovered >= 0 && hovered < _bagMaterials.Length)
            {
                var material = _bagMaterials[hovered];
                string text = material.Name + " · " + LocalizeUI("AlchemyOwned", _state.MaterialCount(material.Id)) + "\n" + material.Source;
                var lines = d.WrapParagraph(text, 250, 11);
                float height = lines.Length * 18 + 16;
                float tipX = Math.Clamp((_bag[hovered].Bounds.X - Bounds.X) / _scale, 26, DesignWidth - 296);
                float y = BagBounds.Y - height - 6;
                d.RoundRect(tipX, y, 270, height, 6, new Color(26, 22, 35) * .97f, Ink * .2f, 1);
                for (int i = 0; i < lines.Length; i++) Label(d, lines[i], tipX + 10, y + 8 + i * 18, 11, Ink);
            }
            if (_toastTime <= 0) return;
            float width = Math.Min(640, d.Measure(_toast, 12) + 40), x = (DesignWidth - width) / 2;
            d.Box(x, 676, width, 36, new Color(53, 37, 63));
            d.Frame(x, 676, width, 36, Lavender * .5f);
            Label(d, _toast, DesignWidth / 2, 687, 12, Ink, .5f);
        });
    }
    private void SmallButton(AlchemyDrawing d, string text, bool hover, bool enabled)
    {
        Label(d, text, 14, 5, 14, enabled ? hover ? Ink : Lavender : Muted * .3f, .5f);
    }
    private void StopDragging()
    {
        _catalog?.Motion.EndDrag();
        _detail?.Motion.EndDrag();
        _bagScroll?.ScrollBar.OnLeftMouseUp(new UIMouseEvent(_bagScroll.ScrollBar, Main.MouseScreen));
    }
    public void Close() { RequestBook(false); }
    private void CloseImmediately()
    {
        StopDragging();
        _book.Reset();
        _quill.Reset();
        _cardMotion.Reset();
        _bookClock.Stop();
        Enabled = false;
        _keyboard = default;
    }
    protected override void OnExitTree() { CloseImmediately(); _owner = null; _state.Clear(); base.OnExitTree(); }
    public static bool TogglePanel()
    {
        if (Main.dedServ || Main.gameMenu || SilkyUISystem.ServiceProvider == null || !SilkyUIRenderSystem.Instance.TryGetInstance<AlchemyNotebookPanel>(out var body)) return false;
        if (body._book.TargetOpen) { body.Close(); return true; }
        if (body._owner != Main.LocalPlayer)
        {
            body.CloseImmediately(); body._state.Bind(new AlchemyNotebookSource(Main.LocalPlayer));
            body._revealSelection = false;
            body._lastResultSequence = 0; body._brewTime = body._flash = 0; body._tabMarker = null; body.Dirty(true);
        }
        else if (body._state.Refresh()) body.Dirty(false);
        body._owner = Main.LocalPlayer;
        body._keyboard = Keyboard.GetState();
        body.Enabled = true;
        body.RequestBook(true);
        return true;
    }
    internal static void ResetWorld()
    {
        if (Main.dedServ || SilkyUISystem.ServiceProvider == null) return;
        if (!SilkyUIRenderSystem.Instance.TryGetInstance<AlchemyNotebookPanel>(out var body)) return;
        var owner = body._owner;
        Main.QueueMainThreadAction(() =>
        {
            if (!ReferenceEquals(body._owner, owner)) return;
            body.CloseImmediately();
            body._owner = null;
        });
    }
    private sealed class PaintView(Action<PaintView, SpriteBatch> paint) : UIView
    {
        protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch) => paint(this, spriteBatch);
    }
}

public sealed class AlchemyNotebookSystem : ModSystem
{
    internal static ModKeybind OpenNotebook;
    public override void Load() { if (!Main.dedServ) OpenNotebook = KeybindLoader.RegisterKeybind(Mod, "OpenAlchemyNotebook", "P"); }
    public override void Unload() => OpenNotebook = null;
    public override void OnWorldUnload() => AlchemyNotebookPanel.ResetWorld();
}
public sealed class AlchemyNotebookInput : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Main.hasFocus && Player.whoAmI == Main.myPlayer && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest && AlchemyNotebookSystem.OpenNotebook?.JustPressed == true)
            AlchemyNotebookPanel.TogglePanel();
    }
}

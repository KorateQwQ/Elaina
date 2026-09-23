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
using static 伊蕾娜.ElainaModAlchemy.UI.AlchemyNotebookPresentation;

namespace 伊蕾娜.ElainaModAlchemy.UI;

[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Alchemy Notebook", 1001)]
public sealed partial class AlchemyNotebookPanel : BaseBody
{
    private readonly AlchemyNotebookState _state = new();
    private readonly List<(UIView View, Rectangle Area)> _regions = [];
    private readonly List<(PaintView View, AlchemyCatalogRecipe Recipe)> _cards = [];
    private readonly List<(PaintView View, AlchemyNotebookRow Row)> _rows = [];
    private readonly List<PaintView> _bag = [];
    private readonly List<UIView> _quantityControls = [];
    private AlchemyDrawing _draw;
    private SUIScrollView _catalog, _detail;
    private PaintView _note, _helpLayer;
    private AlchemyCatalogMaterial[] _bagMaterials = [];
    private float _scale = 1, _lastScale = 1, _brewTime, _flash, _toastTime;
    private string _craftedId, _toast;
    private bool _dirty = true, _resetScroll = true, _help;
    private KeyboardState _keyboard;
    private Player _owner;
    private Vector2 _quill, _quillTarget;
    private bool _quillReady;
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
        for (int i = 0; i < AlchemyCatalog.Categories.Count; i++)
        {
            var category = AlchemyCatalog.Categories[i];
            Button(new(36 + i * 169, 87, 156, 55), () => { if (Brewing || _state.IsBusy) return; _state.SetCategory(category.Id); Dirty(true); }, (d, h) => Category(d, _state, category, h));
        }
        _catalog = Scroll(new(CatalogX, CatalogY, CatalogWidth, CatalogHeight), false);
        for (int i = 0; i < AlchemyCatalog.Recipes.Count; i++)
        {
            int index = i;
            var view = new PaintView((v, batch) => WithLocal(v, batch, d =>
            {
                if (_cards[index].Recipe is not { } r) return;
                Card(d, _state, r, v.IsMouseHovering, _craftedId == r.Id ? _flash : 0);
            })) { Border = 0, Padding = new Margin(0), FlexShrink = 0, FitWidth = false, FitHeight = false }.Join(_catalog.Container);
            view.LeftMouseClick += (_, _) => { if (_cards[index].Recipe is { } r) Select(r.Id); };
            _cards.Add((view, null));
        }
        _note = new PaintView((v, batch) => WithLocal(v, batch, d => PageNote(d, _state)))
        { Border = 0, Padding = new Margin(0), FlexShrink = 0, IgnoreMouseInteraction = true }.Join(_catalog.Container);
        _detail = Scroll(new(DetailX, DetailY, DetailWidth, DetailHeight), true);
        _quantityControls.Add(Button(new(842, 628, 26, 24), () => ChangeQuantity(-1), (d, h) => SmallButton(d, "−", h, _state.Quantity > 1)));
        _quantityControls.Add(Button(new(904, 628, 26, 24), () => ChangeQuantity(1), (d, h) => SmallButton(d, "+", h, _state.Quantity < _state.MaxBatches(_state.Current))));
        _quantityControls.Add(Button(new(938, 628, 36, 24), () => { _state.SetQuantity(_state.MaxBatches(_state.Current)); Dirty(false); },
            (d, h) => Label(d, "最大", 18, 5, 10, h ? Ink : Muted, .5f)));
        Button(new(780, 662, 272, 38), PrimaryAction, (d, h) => CraftButton(d, _state, h, BrewProgress));
        Button(new(1040, 26, 28, 32), Close, (d, h) =>
        {
            d.Diamond(new(14, 14), 17, h ? Ink : Lavender * .65f, false);
            d.Line(new(10, 10), new(18, 18), Lavender);
            d.Line(new(10, 18), new(18, 10), Lavender);
            d.LatinText("ESC", 14, 35, 8, Muted, .5f);
        });
        Button(new(714, 744, 82, 35), () => _help = true, (d, h) => Label(d, "帮助", 41, 11, 10, h ? Ink : Muted, .5f));
#if DEBUG
        Button(new(802, 744, 82, 35), () => { if (Brewing || _state.IsBusy) return; _state.DebugRestock(); ProcessResult(); },
            (d, h) => DebugButton(d, "补充素材", h));
        Button(new(890, 744, 82, 35), () =>
        {
            if (Brewing || _state.IsBusy) return;
            _state.DebugReset(); ProcessResult(); Dirty(false);
        }, (d, h) => DebugButton(d, "重置手记", h));
#endif
        Button(new(978, 744, 82, 35), Close, (d, h) => Label(d, "返回", 41, 11, 10, h ? Ink : Muted, .5f));
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            var view = Button(new(95 + i * 46, 745, 38, 38), () =>
            {
                if (index < _bagMaterials.Length && _bagMaterials[index].Entry is { } entry) Select(entry);
            }, (d, h) =>
            {
                if (index >= _bagMaterials.Length) return;
                var m = _bagMaterials[index];
                d.Frame(0, 0, 38, 38, h ? Gold * .65f : Lavender * .3f);
                d.Image(m.IconPath, 4, 1, 28, 28, Color.White);
                Label(d, _state.MaterialCount(m.Id).ToString(), 35, 26, 9, Ink, 1);
            });
            _bag.Add(view);
        }
        _helpLayer = new PaintView((v, batch) => WithLocal(v, batch, DrawHelp)) { Positioning = Positioning.Absolute, ZIndex = 100, Invalid = true }.Join(this);
        _regions.Add((_helpLayer, new(0, 0, DesignWidth, DesignHeight)));
        _helpLayer.LeftMouseClick += (_, e) =>
        {
            var p = (e.MousePosition - Bounds.Position) / _scale;
            if (!new Rectangle(310, 190, 480, 418).Contains(p.ToPoint()) || new Rectangle(343, 547, 414, 38).Contains(p.ToPoint())) _help = false;
        };
        LayoutViews();
    }

    public override UIView GetElementAt(Vector2 mousePosition) => _book.Moving ? this : _help ? _helpLayer : base.GetElementAt(mousePosition);

    private PaintView Button(Rectangle area, Action click, Action<AlchemyDrawing, bool> paint)
    {
        var view = new PaintView((v, batch) => WithLocal(v, batch, d => paint(d, v.IsMouseHovering)))
        { Positioning = Positioning.Absolute, Border = 0, Padding = new Margin(0) }.Join(this);
        view.LeftMouseClick += (_, _) => { if (!_help && _book.IsOpen) click(); };
        _regions.Add((view, area));
        return view;
    }

    private SUIScrollView Scroll(Rectangle area, bool column)
    {
        var scroll = new SUIScrollView { Positioning = Positioning.Absolute, Border = 0, Padding = new Margin(0), Gap = new Size(0), BackgroundColor = Color.Transparent }.Join(this);
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
        scroll.ScrollBar.BarColor = (Muted * .33f, Lavender * .6f);
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
    private void Select(string id) { if (Brewing || !_book.IsOpen) return; _state.Select(id); Dirty(true); }
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
            Notify(result.Message);
            if (result.Success && result.Action == AlchemyNotebookAction.Research) { _craftedId = result.EntryId; _flash = 1; }
        }
        Dirty(false);
    }

    private void RefreshViews()
    {
        _dirty = false;
        var visible = _state.Visible;
        _note.Invalid = visible.Count == 0;
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
            var view = new PaintView((v, batch) => WithLocal(v, batch, d => _rows[index].Row?.Paint(d, v.IsMouseHovering)))
            { Border = 0, Padding = new Margin(0), FlexShrink = 0, FitWidth = false, FitHeight = false }.Join(_detail.Container);
            view.LeftMouseClick += (_, _) => { if (_rows[index].Row?.Entry is { } entry) Select(entry); };
            _rows.Add((view, null));
        }
        for (int i = 0; i < _rows.Count; i++)
        {
            var view = _rows[i].View;
            var row = i < rows.Count ? rows[i] : null;
            view.Invalid = row == null;
            _rows[i] = (view, row);
        }
        var ids = _state.IsUnlocked(_state.Current) ? _state.Current.Ingredients.SelectMany(i => i.Choices).Distinct().ToList() : [];
        if (_state.IsUnlocked(_state.Current) && ids.Count == 0 && _state.Current.Material is { } material) ids.Add(material);
        _bagMaterials = ids.Select(AlchemyCatalog.GetMaterial).ToArray();
        for (int i = 0; i < _bag.Count; i++) _bag[i].Invalid = i >= _bagMaterials.Length;
        foreach (var view in _quantityControls) view.Invalid = !_state.Current.Craftable || !_state.IsUnlocked(_state.Current);
        LayoutViews();
        if (_resetScroll)
        {
            _catalog.ScrollBar.SetScrollPosition(Vector2.Zero);
            _detail.ScrollBar.SetScrollPosition(Vector2.Zero);
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
        _catalog.Container.Gap = new Size(12 * _scale);
        foreach (var (v, _) in _cards) v.SetSize(CardWidth * _scale, CardHeight * _scale, 0, 0);
        _note.SetSize(660 * _scale, NoteHeight(_draw, _state) * _scale, 0, 0);
        foreach (var (v, row) in _rows) if (row != null) v.SetSize(DetailWidth * _scale, row.Height * _scale, 0, 0);
        float catalogContent = _state.Visible.Count == 0 ? 0 : MathF.Ceiling(_state.Visible.Count / 4f) * (CardHeight + 12) + NoteHeight(_draw, _state);
        SetRange(_catalog, CatalogWidth, CatalogHeight, catalogContent);
        SetRange(_detail, DetailWidth, DetailHeight, _rows.Sum(r => r.Row?.Height ?? 0));
        _lastScale = _scale;
    }
    private void SetRange(SUIScrollView scroll, float w, float h, float content)
    {
        var position = scroll.ScrollBar.CurrentScrollPosition * (_scale / _lastScale);
        scroll.ScrollBar.SetLeft((w - 3) * _scale, 0, 0);
        scroll.ScrollBar.SetSize(3 * _scale, h * _scale, 0, 0);
        scroll.ScrollBar.Invalid = content <= h;
        scroll.ScrollBar.SetVScrollRange(h * _scale, content * _scale);
        scroll.ScrollBar.SetScrollPosition(Vector2.Clamp(position, Vector2.Zero, scroll.ScrollBar.GetScrollRange()));
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
        _helpLayer.Invalid = !_help;
        foreach (var (view, _) in _cards) view.DisableMouseInteraction = Brewing;
        foreach (var view in _quantityControls) view.DisableMouseInteraction = Brewing;
        var keys = Keyboard.GetState();
        if (Main.hasFocus && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest)
        {
            bool Press(Keys key) => keys.IsKeyDown(key) && !_keyboard.IsKeyDown(key);
            if (Press(Keys.Escape)) { if (_help) _help = false; else Close(); }
            if (!_help && !Brewing && _book.IsOpen)
            {
                var entries = _state.Visible;
                int index = entries.ToList().FindIndex(r => r.Id == _state.SelectedId);
                int offset = Press(Keys.Left) ? -1 : Press(Keys.Right) ? 1 : Press(Keys.Up) ? -4 : Press(Keys.Down) ? 4 : 0;
                if (offset != 0 && entries.Count > 0) Select(entries[Math.Clamp(index + offset, 0, entries.Count - 1)].Id);
            }
        }
        _keyboard = keys;
        base.UpdateStatus(gameTime);
        if (IsMouseHovering || _help || _book.Moving) Main.LocalPlayer.mouseInterface = true;
        var selected = _cards.FirstOrDefault(c => c.Recipe?.Id == _state.SelectedId).View;
        if (selected != null)
        {
            _quillTarget = (selected.Bounds.Position - Bounds.Position) / _scale + new Vector2(CardWidth - 7, CardHeight - 49);
            if (!_quillReady) { _quill = _quillTarget; _quillReady = true; }
            _quill = Vector2.Lerp(_quill, _quillTarget, 1 - MathF.Exp(-dt * 12));
        }
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        WithLocal(this, spriteBatch, d =>
        {
            Chrome(d, _state);
            Hero(d, _state, BrewProgress);
            if (_state.IsUnlocked(_state.Current) && _state.Current.Craftable)
            {
                Label(d, "制作批数", 780, 635, 10, Muted);
                Label(d, _state.Quantity.ToString(), 886, 633, 12, Ink, .5f);
                Label(d, $"产出 ×{_state.Current.Yield * _state.Quantity}", 1052, 635, 10, Gold, 1);
            }
            Label(d, Hint(_state, Brewing), 916, 708, 9, Muted, .5f);
            if (_state.Visible.Count == 0) Label(d, "这一页暂时没有符合条件的条目", 384, 315, 14, Muted, .5f, true);
        });
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.DrawChildren(gameTime, spriteBatch);
        if (_help) return;
        WithLocal(this, spriteBatch, d =>
        {
            if (_book.IsOpen && _quillReady && _state.Visible.Any(r => r.Id == _state.SelectedId) && _quill.Y >= CatalogY && _quill.Y + QuillHeight <= CatalogY + CatalogHeight)
                SelectionQuill(d, _quill.X, _quill.Y);
            if (_toastTime <= 0) return;
            float width = Math.Min(640, d.Measure(_toast, 12) + 40), x = (DesignWidth - width) / 2;
            d.Box(x, 676, width, 36, new Color(53, 37, 63));
            d.Frame(x, 676, width, 36, Lavender * .5f);
            Label(d, _toast, DesignWidth / 2, 687, 12, Ink, .5f);
        });
    }
    private void SmallButton(AlchemyDrawing d, string text, bool hover, bool enabled)
    {
        d.Frame(0, 0, 26, 24, Lavender * (enabled ? hover ? .7f : .35f : .12f));
        Label(d, text, 13, 4, 13, enabled ? Ink : Muted * .4f, .5f);
    }
    private void DrawHelp(AlchemyDrawing d)
    {
        d.Box(0, 0, DesignWidth, DesignHeight, new Color(10, 8, 18) * .84f);
        d.Box(310, 190, 480, 418, new Color(36, 29, 46));
        d.Frame(310, 190, 480, 418, Lavender * .65f);
        Label(d, "炼金手记", 344, 222, 24, Ink, serif: true);
        float y = 274;
        foreach (string p in new[] { "在魔药、奇物、料理与素材之间翻页。问号代表未研究的造物，满足等级或配方条件后点击研究。", "制作沿用原版合成材料规则，包括背包、打开的箱子、个人容器、虚空袋及模组提供的材料。", "制作成功后获得产物与炼金经验。研究记录、等级和经验随角色保存；灰赝尘默认不参与炼金。", "P 打开或合上手记 · Esc 返回 · 方向键选择条目。" })
            y = d.Paragraph(p, 344, y, 412, 12, Muted, 23) + 16;
        d.Frame(343, 547, 414, 38, Lavender * .5f);
        Label(d, "返回", 550, 558, 14, Ink, .5f, true);
    }
    private void StopDragging()
    {
        _catalog?.ScrollBar.OnLeftMouseUp(new UIMouseEvent(_catalog.ScrollBar, Main.MouseScreen));
        _detail?.ScrollBar.OnLeftMouseUp(new UIMouseEvent(_detail.ScrollBar, Main.MouseScreen));
    }
    public void Close() { _help = false; RequestBook(false); }
    private void CloseImmediately()
    {
        StopDragging();
        _book.Reset();
        _bookClock.Stop();
        Enabled = false;
        _help = false;
        _keyboard = default;
    }
    private void DebugButton(AlchemyDrawing d, string label, bool hover)
    {
#if DEBUG
        d.Box(0, 3, 82, 29, Gold * (hover ? .13f : .045f));
        d.Frame(0, 3, 82, 29, Gold * (hover ? .7f : .32f));
        Label(d, label, 41, 11, 10, hover ? Ink : Gold, .5f);
#endif
    }
    protected override void OnExitTree() { CloseImmediately(); _owner = null; _state.Clear(); base.OnExitTree(); }
    public static bool TogglePanel()
    {
        if (Main.dedServ || Main.gameMenu || SilkyUISystem.ServiceProvider == null || !SilkyUIRenderSystem.Instance.TryGetInstance<AlchemyNotebookPanel>(out var body)) return false;
        if (body._book.TargetOpen) { body.Close(); return true; }
        if (body._owner != Main.LocalPlayer)
        {
            body.CloseImmediately(); body._state.Bind(new AlchemyNotebookSource(Main.LocalPlayer));
            body._lastResultSequence = 0; body._brewTime = body._flash = 0; body.Dirty(true);
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

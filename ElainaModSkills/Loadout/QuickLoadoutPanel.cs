using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using KL.Drawing;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Config;

namespace 伊蕾娜.ElainaModSkills.Loadout;

#if DEBUG
/// <summary>快捷配装：列出 SkillBalance.json 的 19 个阶段推荐配装，一键换上并把魔力与生命设为该阶段数值。</summary>
[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Quick Loadout", 1001)]
public sealed class QuickLoadoutPanel : BaseBody
{
    private const float DesignWidth = 760, DesignHeight = 532, ScreenMargin = 16;
    private const float ListX = 16, ListY = 84, RowHeight = 21, ListWidth = 236;
    private const float RightX = 268, RightWidth = 476;
    private static readonly Color Background = new(30, 26, 42), Surface = new(42, 36, 58), LineColor = new(96, 80, 120),
        Lavender = new(186, 158, 208), Ink = new(240, 228, 247), Muted = new(158, 142, 174), Gold = new(232, 198, 140),
        Warn = new(226, 150, 140);
    private static string[] ArmorParts => [LocalizeUI("Head"), LocalizeUI("Body"), LocalizeUI("Legs")];

    private readonly List<(UIView View, RectangleF Area)> _regions = [];
    private readonly Dictionary<(int Type, int Prefix), Item> _previews = [];
    private SpriteBatch _batch;
    private float _scale = 1, _toastLife;
    private Vector2 _origin, _lastSize;
    private int _selected = 1, _current = 1;
    private string _toast;

    private static string ToggleKeyLabel => KeyBind.OpenLoadoutPanel?.GetAssignedKeys().FirstOrDefault() ?? LocalizeUI("LoadoutKey");
    private SkillLoadout SelectedLoadout => SkillBalanceSystem.Current.Loadouts.FirstOrDefault(l => l.Stage == _selected);

    public QuickLoadoutPanel() { Enabled = false; }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Border = 0;
        Padding = new Margin(0);
        BackgroundColor = Color.Transparent;
        FitWidth = false;
        FitHeight = false;
        FitWindowToScreen(GraphicsDeviceHelper.GetBackBufferSizeByUIScale());
        SetLeft(0, 0, .5f);
        SetTop(0, 0, .5f);

        for (int i = 0; i < SkillBalanceConfig.LoadoutStages; i++)
        {
            int stage = i + 1;
            Button(new RectangleF(ListX, ListY + i * RowHeight, ListWidth, RowHeight), () => _selected = stage, hover => DrawRow(stage, hover));
        }
        Button(new RectangleF(DesignWidth - 46, 16, 30, 30), Close, DrawCloseButton);
        Button(new RectangleF(RightX + RightWidth - 170, 448, 170, 36), ApplySelected, DrawApplyButton);
    }

    private void Button(RectangleF area, Action click, Action<bool> paint)
    {
        var view = new PaintView((v, _) => paint(v.IsMouseHovering)) { Positioning = Positioning.Absolute }.Join(this);
        view.LeftMouseClick += (_, _) => click();
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
        base.OnExitTree();
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        _toastLife = Math.Max(0, _toastLife - Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, .1f));
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
        }
        if (Main.hasFocus && Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape)) Close();
        if (IsMouseHovering) Main.LocalPlayer.mouseInterface = true;
    }

    private void Close() { Enabled = false; _toastLife = 0; }
    private void Notify(string text) { _toast = text; _toastLife = 5f; }

    private void ApplySelected()
    {
        var player = Main.LocalPlayer;
        if (SelectedLoadout is not { } loadout) { Notify(LocalizeUI("NoStageLoadout")); return; }
        if (!LoadoutApplier.CanApply(player, out string reason)) { Notify(reason); return; }
        Notify(LoadoutApplier.Apply(player, loadout));
        SoundEngine.PlaySound(SoundID.Grab);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        _batch = spriteBatch;
        Box(0, 0, DesignWidth, DesignHeight, Background * .97f);
        Frame(0, 0, DesignWidth, DesignHeight, Lavender * .55f);
        Text(LocalizeUI("Loadout"), 24, 14, 24, Ink, serif: true);
        Text(LocalizeUI("CurrentStage", _current, LoadoutItems.StageLabels[_current - 1], ToggleKeyLabel),
            24, 50, 12, Muted, maxWidth: DesignWidth - 90);
        Box(16, 74, DesignWidth - 32, 1 / _scale, LineColor);
        Box(ListX - 4, ListY - 4, ListWidth + 8, SkillBalanceConfig.LoadoutStages * RowHeight + 8, Surface * .6f);
        DrawDetail();
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.DrawChildren(gameTime, spriteBatch);
        _batch = spriteBatch;
        string line = _toastLife > 0 ? _toast : Hint();
        Color color = _toastLife > 0 ? Ink * Math.Min(1, _toastLife * 3) : Muted;
        if (_toastLife > 0) Box(RightX, 494, RightWidth, 26, Surface * Math.Min(1, _toastLife * 3));
        Text(line, RightX + 8, 499, 11, color, maxWidth: RightWidth - 16);
    }

    private string Hint()
    {
        if (!LoadoutApplier.CanApply(Main.LocalPlayer, out string reason)) return reason;
        return LocalizeUI("LoadoutHint");
    }

    private void DrawRow(int stage, bool hover)
    {
        float y = ListY + (stage - 1) * RowHeight;
        bool selected = stage == _selected, current = stage == _current;
        if (selected) Box(ListX, y, ListWidth, RowHeight, Lavender * .22f);
        else if (hover) Box(ListX, y, ListWidth, RowHeight, Lavender * .1f);
        if (selected) Box(ListX, y, 2 / _scale, RowHeight, Gold);
        Text(stage.ToString("00"), ListX + 10, y + 3, 11, selected ? Gold : Muted);
        Text(LoadoutItems.StageLabels[stage - 1], ListX + 36, y + 3, 11, selected || hover ? Ink : new Color(205, 192, 216),
            maxWidth: current ? 150 : 190);
        if (current) Text(LocalizeUI("Current"), ListX + ListWidth - 10, y + 3, 10, Gold, 1);
    }

    private void DrawCloseButton(bool hover)
    {
        float x = DesignWidth - 46, y = 16;
        if (hover) Box(x, y, 30, 30, Lavender * .18f);
        Frame(x, y, 30, 30, Lavender * (hover ? .8f : .4f));
        Text("×", x + 15, y + 4, 18, hover ? Ink : Lavender, .5f);
    }

    private void DrawApplyButton(bool hover)
    {
        float x = RightX + RightWidth - 170, y = 448;
        bool enabled = SelectedLoadout != null && LoadoutApplier.CanApply(Main.LocalPlayer, out _);
        Box(x, y, 170, 36, enabled ? hover ? new Color(238, 219, 247) : new Color(216, 195, 231) : new Color(52, 42, 64));
        Frame(x, y, 170, 36, enabled ? new Color(239, 220, 247) : Lavender * .3f);
        Text(LocalizeUI("ApplyLoadout"), x + 85, y + 9, 15, enabled ? new Color(50, 36, 62) : Muted, .5f);
    }

    private void DrawDetail()
    {
        var player = Main.LocalPlayer;
        var loadout = SelectedLoadout;
        if (loadout == null)
        {
            Text(LocalizeUI("NoLoadouts"), RightX, ListY + 4, 15, Ink);
            Text(LocalizeUI("ConfigureLoadouts"), RightX, ListY + 32, 11, Muted, maxWidth: RightWidth);
            return;
        }
        Text(loadout.Name, RightX, ListY - 2, 16, Ink, maxWidth: RightWidth, serif: true);
        Text(LocalizeUI("LoadoutStage", loadout.Stage, LoadoutItems.StageLabels[loadout.Stage - 1], loadout.Stage * 5), RightX, ListY + 24, 11, Muted);

        Section(LocalizeUI("Armor"), 132);
        int[] armor = LoadoutApplier.Armor(loadout);
        bool knownArmor = loadout.Armor == "none" || LoadoutItems.Armors.ContainsKey(loadout.Armor);
        for (int i = 0; i < 3; i++)
        {
            float x = RightX + i * 160, y = 154;
            ItemCell(x, y, 40, armor[i], 0);
            Text(ArmorParts[i], x + 48, y + 2, 10, Muted);
            Text(armor[i] > 0 ? Lang.GetItemNameValue(armor[i]) : knownArmor ? LocalizeUI("KeepEquipment") : LocalizeUI("UnknownEquipment", loadout.Armor),
                x + 48, y + 19, 11, armor[i] > 0 ? Ink : knownArmor ? Muted : Warn, maxWidth: 106);
        }

        int usable = Enumerable.Range(3, 7).Count(player.IsItemSlotUnlockedAndUsable);
        Section(LocalizeUI("AccessorySlots", loadout.Slots, usable), 212);
        for (int i = 0; i < loadout.Slots && i < loadout.Items.Count; i++)
        {
            var entry = loadout.Items[i];
            float x = RightX + i % 2 * 240, y = 234 + i / 2 * 46;
            if (LoadoutItems.TryGetAccessory(entry.Id, out int type))
            {
                int prefix = LoadoutItems.Prefixes.GetValueOrDefault(entry.Prefix);
                ItemCell(x, y, 36, type, prefix);
                Text(Lang.GetItemNameValue(type), x + 44, y + 1, 11, Ink, maxWidth: 190);
                Text(prefix > 0 ? LocalizeUI("PrefixLabel") + Lang.prefix[prefix].Value : LocalizeUI("NoPrefix"), x + 44, y + 19, 10, prefix > 0 ? Gold : Muted);
            }
            else
            {
                ItemCell(x, y, 36, 0, 0);
                bool keep = LoadoutItems.IsEmptySlot(entry.Id);
                Text(entry.Id == "empty" ? LocalizeUI("EmptyAccessory") : keep ? LocalizeUI("Utility") : LocalizeUI("UnknownEquipment", entry.Id), x + 44, y + 1, 11, keep ? Ink : Warn, maxWidth: 190);
                Text(LocalizeUI("KeepAccessory"), x + 44, y + 19, 10, Muted);
            }
        }

        Section(LocalizeUI("Attributes"), 418);
        Stat(LocalizeUI("BaseMana"), loadout.BaseMana, LoadoutApplier.BaseMana(player), RightX + 44);
        Stat(LocalizeUI("MaxLife"), loadout.MaxLife, LoadoutApplier.BaseLife(player), RightX + 160);
    }

    private void Section(string title, float y)
    {
        Text(title, RightX, y, 11, Lavender);
        float width = Measure(title, 11) + 10;
        Box(RightX + width, y + 9, RightWidth - width, 1 / _scale, LineColor);
    }

    private void Stat(string label, int target, int current, float x)
    {
        Text(label, x - 44, 438, 10, Muted);
        Text(target.ToString(), x + 8, 436, 13, target != current ? Gold : Ink);
        Text(target != current ? LocalizeUI("CurrentValue", current) : LocalizeUI("Same"), x + 8 + Measure(target.ToString(), 13) + 6, 438, 10, Muted);
    }

    /// <summary>物品格；悬停时交给原版鼠标提示显示带前缀的完整属性。</summary>
    private void ItemCell(float x, float y, float size, int type, int prefix)
    {
        Box(x, y, size, size, Surface);
        Frame(x, y, size, size, type > 0 ? Lavender * .45f : LineColor * .6f);
        if (type <= 0) return;
        Main.instance.LoadItem(type);
        var texture = TextureAssets.Item[type].Value;
        Rectangle frame = Main.itemAnimations[type]?.GetFrame(texture) ?? texture.Frame();
        float scale = Math.Min(1f, (size - 8) / Math.Max(frame.Width, frame.Height)) * _scale;
        _batch.Draw(texture, P(x + size / 2, y + size / 2), frame, Color.White, 0, frame.Size() / 2, scale, SpriteEffects.None, 0);
        var mouse = (Main.MouseScreen - Bounds.Position - _origin) / _scale;
        if (!IsMouseHovering || !new RectangleF(x, y, size, size).Contains(mouse.X, mouse.Y)) return;
        if (!_previews.TryGetValue((type, prefix), out var item))
        {
            item = new Item(type);
            if (prefix > 0) item.Prefix(prefix);
            _previews[(type, prefix)] = item;
        }
        Main.HoverItem = item.Clone();
        Main.hoverItemName = item.Name;
    }

    private Vector2 P(float x, float y) => Bounds.Position + _origin + new Vector2(x, y) * _scale;

    private void Box(float x, float y, float w, float h, Color color) =>
        _batch.Draw(TextureAssets.MagicPixel.Value, P(x, y), new Rectangle(0, 0, 1, 1), color, 0, Vector2.Zero,
            new Vector2(w, h) * _scale, SpriteEffects.None, 0);

    private void Frame(float x, float y, float w, float h, Color color)
    {
        float t = 1 / _scale;
        Box(x, y, w, t, color);
        Box(x, y + h - t, w, t, color);
        Box(x, y, t, h, color);
        Box(x + w - t, y, t, h, color);
    }

    // 与魔女手札一致：标题用 NotoSerifSC，正文用 HarmonyOS Sans；字号以一个汉字的宽度为 1em。
    private static DynamicSpriteFont GetFont(bool serif) => serif ? FontManager.NotoSerifSC.Value : FontManager.HarmonyOS_Sans_SC.Value;
    private static float FontScale(DynamicSpriteFont font, float size) => size / Math.Max(1f, font.MeasureString("国").X);

    private static float Measure(string text, float size, bool serif = false)
    {
        var font = GetFont(serif);
        return font.MeasureString(text).X * FontScale(font, size);
    }

    private void Text(string text, float x, float y, float size, Color color, float anchor = 0, float maxWidth = 0, bool serif = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (maxWidth > 0 && Measure(text, size, serif) > maxWidth)
        {
            while (text.Length > 1 && Measure(text + "…", size, serif) > maxWidth) text = text[..^1];
            text += "…";
        }
        var font = GetFont(serif);
        float factor = FontScale(font, size);
        _batch.DrawString(font, text, P(x - Measure(text, size, serif) * anchor, y), color, 0, Vector2.Zero,
            factor * _scale, SpriteEffects.None, 0);
    }

    public static bool TogglePanel()
    {
        if (Main.dedServ || Main.gameMenu || SilkyUISystem.ServiceProvider == null) return false;
        if (!SilkyUIRenderSystem.Instance.TryGetInstance<QuickLoadoutPanel>(out var body)) return false;
        if (body.Enabled) { body.Close(); return true; }
        body._current = body._selected = LoadoutApplier.CurrentStage(Main.LocalPlayer);
        body._previews.Clear();
        body.Enabled = true;
        return true;
    }

    internal static void ResetWorld()
    {
        if (Main.dedServ || SilkyUISystem.ServiceProvider == null) return;
        if (SilkyUIRenderSystem.Instance.TryGetInstance<QuickLoadoutPanel>(out var body)) Main.QueueMainThreadAction(body.Close);
    }

    private readonly record struct RectangleF(float X, float Y, float Width, float Height)
    {
        public bool Contains(float x, float y) => x >= X && x < X + Width && y >= Y && y < Y + Height;
    }

    private sealed class PaintView(Action<PaintView, SpriteBatch> paint) : UIView
    {
        protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch) => paint(this, spriteBatch);
    }
}

public sealed class QuickLoadoutSystem : ModSystem
{
    public override void OnWorldUnload() => QuickLoadoutPanel.ResetWorld();
}

public sealed class QuickLoadoutInput : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Main.hasFocus && Player.whoAmI == Main.myPlayer && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest
            && KeyBind.OpenLoadoutPanel?.JustPressed == true)
            QuickLoadoutPanel.TogglePanel();
    }
}
#endif

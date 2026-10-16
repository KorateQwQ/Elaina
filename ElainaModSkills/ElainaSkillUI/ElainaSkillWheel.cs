using System;
using KL.Drawing;
using KL.SkillSystem;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using 伊蕾娜.Config;
using 伊蕾娜.ElainaModSkills.UICore;
using static 伊蕾娜.Localization.UIText;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.SkillWheelDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

/// <summary>轮盘选择方式：按住打开键弹出已装备术式，指向某个术式后松开即切换；在中心死区松开则保留当前术式。</summary>
[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Skill Wheel", 1002)]
public sealed class ElainaSkillWheel : BaseBody
{
    private static Texture2D _veil;
    private static Effect _cdEffect;

    private readonly List<int> _slots = [];
    private readonly List<Skill> _skills = [];
    private readonly float[] _hover = new float[MaxItems];
    private readonly float[] _extentAtClose = new float[MaxItems];
    private readonly float[] _opacityAtClose = new float[MaxItems];
    private readonly SkillWheelDrawing.View _view = new();
    private Asset<Texture2D> _ornaments;
    private Vector2 _anchor;
    private bool _open, _anchorPending;
    private float _time, _closeTime = -1, _presence;
    private int _hovered = -1, _chosen = -1;

    public ElainaSkillWheel() { Enabled = false; }

    public override bool IsInteractable => _open;

    private int Count => _skills.Count;

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Border = 0;
        Padding = new Margin(0);
        BackgroundColor = BorderColor = Color.Transparent;
        FitWidth = FitHeight = false;
        SetLeft(0, 0, 0);
        SetTop(0, 0, 0);
        FitScreen(GraphicsDeviceHelper.GetBackBufferSizeByUIScale());
        _ornaments = ModContent.Request<Texture2D>(ElainaSkillBar.ArtPath + "Ornaments");
    }

    private void FitScreen(Size screen) => SetSize(screen.Width, screen.Height, 0, 0);

    protected override void OnScreenSizeChanged(Size newScreenSize, Size oldScreenSize)
    {
        FitScreen(newScreenSize);
        base.OnScreenSizeChanged(newScreenSize, oldScreenSize);
    }

    protected override void OnExitTree()
    {
        CloseImmediately();
        base.OnExitTree();
    }

    private static bool TryGet(out ElainaSkillWheel wheel)
    {
        wheel = null;
        return !Main.dedServ && SilkyUISystem.ServiceProvider != null
            && SilkyUIRenderSystem.Instance.TryGetInstance(out wheel);
    }

    private static bool CanStayOpen => Main.hasFocus && !Main.gameMenu && !Main.drawingPlayerChat
        && !Main.ingameOptionsWindow && !Main.inFancyUI && Main.LocalPlayer is { active: true, dead: false }
        && ElainaSkillManager.SelectionMode == SkillSelectionMode.Wheel;

    private static bool CanOpen => CanStayOpen && !Main.editSign && !Main.editChest && !Main.LocalPlayer.mouseInterface
        && !ConstellationSkillPanel.ConstellationSkillPanel.PanelShown;

    internal static void HandleInput(ModKeybind key)
    {
        if (key == null || !TryGet(out var wheel)) return;
        if (wheel._open)
        {
            if (!CanStayOpen) wheel.Close(false);
            else if (!key.Current) wheel.Close(true);
        }
        else if (key.JustPressed && CanOpen) wheel.Open();
    }

    internal static void ResetWorld()
    {
        if (TryGet(out var wheel)) Main.QueueMainThreadAction(wheel.CloseImmediately);
    }

    internal static void Unload()
    {
        var veil = _veil;
        _veil = null;
        _cdEffect = null;
        if (veil != null) Main.QueueMainThreadAction(veil.Dispose);
    }

    private void Open()
    {
        var skills = ElainaSkillModPlayer.GetActiveSkill;
        _slots.Clear();
        _skills.Clear();
        for (int i = 0; i < skills.Count && i < MaxItems; i++)
        {
            if (skills[i] == null) continue;
            _slots.Add(i);
            _skills.Add(skills[i]);
        }
        if (Count == 0) return;

        Array.Clear(_hover);
        _open = _anchorPending = true;
        _time = _presence = 0;
        _closeTime = -1;
        _hovered = _chosen = -1;
        Enabled = true;
    }

    private void Close(bool commit)
    {
        if (!_open) return;
        _open = false;
        _chosen = commit && SkillsUnchanged() ? _hovered : -1;
        for (int i = 0; i < Count; i++)
        {
            _extentAtClose[i] = OpeningExtent(_time, i);
            _opacityAtClose[i] = OpeningOpacity(_time, i);
        }
        _closeTime = 0;

        if (_chosen < 0 || _slots[_chosen] == ElainaSkillModPlayer.CurrentSkillIndex) return;
        ElainaSkillModPlayer.CurrentSkillIndex = _slots[_chosen];
        ElainaSkillModPlayer.ElainaSkillBar?.UpdateSkillBar();
    }

    private void CloseImmediately()
    {
        _open = _anchorPending = false;
        _closeTime = -1;
        _hovered = _chosen = -1;
        _slots.Clear();
        _skills.Clear();
        Enabled = false;
    }

    private bool SkillsUnchanged()
    {
        var skills = ElainaSkillModPlayer.GetActiveSkill;
        for (int i = 0; i < Count; i++)
            if (_slots[i] >= skills.Count || skills[_slots[i]] != _skills[i]) return false;
        return true;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        float dt = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0, .1f);
        if (_open && (!CanStayOpen || !SkillsUnchanged())) Close(false);

        if (_anchorPending)
        {
            _anchorPending = false;
            Vector2 size = new(Width.Pixels, Height.Pixels);
            float margin = RadiusFor(Count) + BandHalf + ScreenMargin;
            Vector2 mouse = Main.MouseScreen - Bounds.Position;
            _anchor = new Vector2(
                size.X > margin * 2 ? Math.Clamp(mouse.X, margin, size.X - margin) : size.X / 2,
                size.Y > margin * 2 ? Math.Clamp(mouse.Y, margin, size.Y - margin) : size.Y / 2);
        }

        if (_open)
        {
            _time += dt;
            _presence = Math.Min(1, _presence + dt / FadeIn);
            int hovered = Pick(Main.MouseScreen - Bounds.Position - _anchor, Count);
            if (hovered != _hovered && hovered >= 0) SoundEngine.PlaySound(SoundID.MenuTick);
            _hovered = hovered;
            Main.LocalPlayer.mouseInterface = true;
        }
        else if (_closeTime >= 0)
        {
            _closeTime += dt;
            _presence = Math.Max(0, _presence - dt / FadeOut);
            if (_closeTime >= ChosenClose) CloseImmediately();
        }

        for (int i = 0; i < Count; i++)
            _hover[i] = MathHelper.Lerp(_hover[i], _open && i == _hovered ? 1 : 0, Math.Min(1, dt / .12f));
    }

    private (float Extent, float Opacity) ItemMotion(int i) => _open
        ? (OpeningExtent(_time, i), OpeningOpacity(_time, i))
        : ClosingMotion(_closeTime, i == _chosen, _extentAtClose[i], _opacityAtClose[i]);

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Count == 0 || _ornaments == null) return;
        FillView();
        if (_veil == null)
        {
            const int size = 256;
            _veil = new Texture2D(Main.instance.GraphicsDevice, size, size);
            _veil.SetData(VeilPixels(size));
        }
        _cdEffect ??= ModContent.Request<Effect>("KL/Effects/Content/CDEffect", AssetRequestMode.ImmediateLoad).Value;
        var art = new SkillWheelDrawing.Art(_ornaments.Value, new SkillBarDrawing.StateArt(UIDrawKit.Pixel, UIDrawKit.GlowDot, UIDrawKit.DiamondFill),
            _veil, _cdEffect, UIDrawKit.TitleFont, FontManager.HarmonyOS_Sans_SC?.Value ?? UIDrawKit.TitleFont, UIDrawKit.NumberFont);
        SkillWheelDrawing.Draw(spriteBatch, art, _view, PaintSector, Restart);
    }

    private void PaintSector(Vector2 center, float inner, float outer, float start, float end,
        Color fill, float border, Color borderColor) =>
        DrawAnnularSectorUI(center, inner, outer, start, end, fill, border, borderColor, SilkyUI.TransformMatrix);

    private void Restart(SpriteBatch batch, SpriteSortMode mode, Effect effect)
    {
        batch.End();
        batch.Begin(mode, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, SilkyUI.ScissorRasterizerState, effect, SilkyUI.TransformMatrix);
    }

    private void FillView()
    {
        SkillWheelDrawing.View view = _view;
        view.Count = Count;
        view.Center = Bounds.Position + _anchor;
        view.Pointer = Main.MouseScreen;
        view.ScreenSize = new Vector2(Width.Pixels, Height.Pixels);
        view.Open = _open;
        view.Hovered = _open ? _hovered : -1;
        view.Presence = _presence;
        view.RingScale = _open ? OpeningRingScale(_time) : MathHelper.Lerp(.94f, 1, _presence);
        view.Seconds = _time + .5f;
        view.ShowNumber = ElainaSkillManager.ShowCD;

        float radius = RadiusFor(Count);
        for (int i = 0; i < Count; i++)
        {
            Skill skill = _skills[i];
            var (extent, opacity) = ItemMotion(i);
            bool chosen = i == _chosen && !_open;
            view.Items[i] = new SkillWheelDrawing.WheelItem
            {
                Position = view.Center + Direction(AngleOf(i, Count)) * (radius * extent + 4 * _hover[i]),
                Opacity = opacity,
                Hover = _hover[i],
                Flash = chosen ? 1 - Math.Clamp(_closeTime / ChosenClose, 0, 1) : 0,
                Cooldown = skill.CurrentCD,
                Remaining = skill.CooldownRemainingFraction,
                Current = _slots[i] == ElainaSkillModPlayer.CurrentSkillIndex,
                Chosen = chosen,
                Slot = _slots[i],
                Stack = skill.Stack,
                MaxStack = skill.MaxStack,
                Icon = skill.SkillIcon?.Value,
            };
        }

        int shown = _open ? _hovered : _chosen;
        if (shown < 0)
        {
            view.Name = LocalizeUI("SkillWheelPrompt");
            view.Sub = LocalizeUI("SkillWheelKeep");
            return;
        }
        Skill target = _skills[shown];
        string key = $"Mods.伊蕾娜.SkillInfo.SkillName.{target.ModSkill?.GetType().Name}";
        view.Name = Language.Exists(key) ? Language.GetTextValue(key) : target.ModSkill?.GetType().Name ?? "";
        view.Sub = _slots[shown] == ElainaSkillModPlayer.CurrentSkillIndex ? LocalizeUI("SkillWheelCurrent")
            : target.Stack <= 0 && target.CurrentCD > 0 ? LocalizeUI("SkillWheelCooldown", target.CurrentCD.ToString("0.0"))
            : LocalizeUI("SkillWheelSlot", _slots[shown] + 1);
    }
}

public sealed class SkillWheelSystem : ModSystem
{
    public override void OnWorldUnload() => ElainaSkillWheel.ResetWorld();
    public override void Unload() => ElainaSkillWheel.Unload();
}

public sealed class SkillWheelInput : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Player.whoAmI == Main.myPlayer) ElainaSkillWheel.HandleInput(KeyBind.OpenSkillWheel);
    }
}

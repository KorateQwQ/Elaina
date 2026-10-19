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
using 伊蕾娜.Items;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.SkillWheelDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

/// <summary>轮盘选择方式：按住打开键弹出已装备术式，指向某个术式后松开即切换；在中心死区松开则保留当前术式。</summary>
[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Skill Wheel", 1002)]
public sealed class ElainaSkillWheel : BaseBody
{
    private static Effect _cdEffect;

    private readonly List<int> _slots = [];
    private readonly List<Skill> _skills = [];
    private readonly float[] _hover = new float[MaxItems];
    private readonly float[] _extentAtClose = new float[MaxItems];
    private readonly float[] _opacityAtClose = new float[MaxItems];
    private readonly SkillWheelDrawing.View _view = new();
    private Matrix _matrix;
    private Vector2 _anchor;
    private bool _open, _anchorPending;
    private float _time, _closeTime = -1, _presence, _focusAge;
    private int _hovered = -1, _chosen = -1, _focus = -1;

    public ElainaSkillWheel() { Enabled = false; }

    public override bool IsInteractable => _open;

    private int Count => _skills.Count;

    private static float Scale => Math.Clamp(ElainaSkillManager.WheelScale, .3f, 1.5f);

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

    private static bool HoldingWand => Main.LocalPlayer.HeldItem.ModItem is ElainaWand;

    private static bool CanStayOpen => HoldingWand && Main.hasFocus && !Main.gameMenu && !Main.drawingPlayerChat
        && !Main.ingameOptionsWindow && !Main.inFancyUI && Main.LocalPlayer is { active: true, dead: false }
        && ElainaSkillManager.SelectionMode == SkillSelectionMode.Wheel && !ElainaHealingWheel.IsShowing;

    private static bool CanOpen => CanStayOpen && !Main.editSign && !Main.editChest && !Main.LocalPlayer.mouseInterface
        && !ConstellationSkillPanel.ConstellationSkillPanel.PanelShown;

    internal static void HandleInput(ModKeybind key)
    {
        if (key == null || !TryGet(out var wheel)) return;
        if (!HoldingWand)
        {
            wheel.CloseImmediately();
            return;
        }
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

    internal static void Unload() => _cdEffect = null;

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
        _time = _presence = _focusAge = 0;
        _closeTime = -1;
        _hovered = _chosen = _focus = -1;
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
        if (!HoldingWand)
        {
            CloseImmediately();
            return;
        }
        float dt = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0, .1f);
        if (_open && (!CanStayOpen || !SkillsUnchanged())) Close(false);

        if (_anchorPending)
        {
            _anchorPending = false;
            Vector2 size = new(Width.Pixels, Height.Pixels);
            float margin = (RadiusFor(Count) + BandHalf + ScreenMargin) * Scale;
            Vector2 mouse = Main.MouseScreen - Bounds.Position;
            _anchor = new Vector2(
                size.X > margin * 2 ? Math.Clamp(mouse.X, margin, size.X - margin) : size.X / 2,
                size.Y > margin * 2 ? Math.Clamp(mouse.Y, margin, size.Y - margin) : size.Y / 2);
        }

        if (_open)
        {
            _time += dt;
            _presence = Math.Min(1, _presence + dt / FadeIn);
            int hovered = Pick((Main.MouseScreen - Bounds.Position - _anchor) / Scale, Count);
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

        // 中心展示：指向的术式；指针在死区时展示当前术式；关闭后保持最后一项随轮盘淡出。
        int focus = !_open ? (_chosen >= 0 ? _chosen : _focus)
            : _hovered >= 0 ? _hovered : _slots.IndexOf(ElainaSkillModPlayer.CurrentSkillIndex);
        if (focus != _focus)
        {
            _focus = focus;
            _focusAge = 0;
        }
        _focusAge += dt;
    }

    private (float Extent, float Opacity) ItemMotion(int i) => _open
        ? (OpeningExtent(_time, i), OpeningOpacity(_time, i))
        : ClosingMotion(_closeTime, i == _chosen, _extentAtClose[i], _opacityAtClose[i]);

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Count == 0 || !HoldingWand) return;
        FillView();
        _cdEffect ??= ModContent.Request<Effect>("伊蕾娜/Effects/Content/SkillSeal", AssetRequestMode.ImmediateLoad).Value;
        var art = new SkillWheelDrawing.Art(new SkillBarDrawing.StateArt(UIDrawKit.Pixel, UIDrawKit.GlowDot, UIDrawKit.DiamondFill),
            _cdEffect, UIDrawKit.NumberFont);
        // 整个轮盘绕自身中心按配置倍率缩放，绘制结束后恢复 SUI 的变换。
        _matrix = ScaleAround(_view.Center, Scale) * SilkyUI.TransformMatrix;
        Restart(spriteBatch, SpriteSortMode.Deferred, null);
        SkillWheelDrawing.Draw(spriteBatch, art, _view, PaintSector, Restart);
        _matrix = SilkyUI.TransformMatrix;
        Restart(spriteBatch, SpriteSortMode.Deferred, null);
    }

    private void PaintSector(Vector2 center, float inner, float outer, float start, float end,
        Color fill, float border, Color borderColor) =>
        DrawAnnularSectorUI(center, inner, outer, start, end, fill, border, borderColor, _matrix);

    private void Restart(SpriteBatch batch, SpriteSortMode mode, Effect effect)
    {
        batch.End();
        batch.Begin(mode, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, SilkyUI.ScissorRasterizerState, effect, _matrix);
    }

    private void FillView()
    {
        SkillWheelDrawing.View view = _view;
        view.Count = Count;
        view.Center = Bounds.Position + _anchor;
        view.Pointer = view.Center + (Main.MouseScreen - view.Center) / Scale;
        view.Open = _open;
        view.Hovered = _open ? _hovered : -1;
        view.Chosen = _open ? -1 : _chosen;
        view.Release = !_open && _chosen >= 0 && _closeTime >= 0 ? Math.Clamp(_closeTime / ChosenClose, 0, 1) : -1;
        view.Focus = _focus;
        view.FocusAge = _focusAge;
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
                Slot = _slots[i],
                Stack = skill.Stack,
                MaxStack = skill.MaxStack,
                Icon = skill.SkillIcon?.Value,
                Sealed = skill.ModSkill is ElainaSealedSkill { IsSealed: true },
                SealDeniedAge = skill.ModSkill is ElainaSealedSkill seal ? seal.SealDeniedAge : 9,
            };
        }
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

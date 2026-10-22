using System;
using Microsoft.Xna.Framework.Input;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria.Audio;
using Terraria.ID;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;
using 伊蕾娜.ElainaModSkills.UICore;
using 伊蕾娜.Items;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.SkillWheelDrawing;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 玩偶指令盘：外观与技能盘相同（图标环带 + 中心法阵），开合与输入沿用治疗选择盘：
/// 左键按下打开，指向某个指令松开即确认；在中心松开、右键或 Esc 取消。
/// </summary>
[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Doll Command Wheel", 1004)]
public sealed class DollCommandWheel : BaseBody
{
    private const int Count = DollCommands.Count;
    private static Effect _cdEffect;
    private static Asset<Texture2D>[] _icons;

    private readonly float[] _hover = new float[Count];
    private readonly float[] _extentAtClose = new float[Count];
    private readonly float[] _opacityAtClose = new float[Count];
    private readonly SkillWheelDrawing.View _view = new();
    private Func<bool> _validSource;
    private Action<DollCommand> _confirmed;
    private DollCommand? _current;
    private Matrix _matrix;
    private Vector2 _anchor;
    private int _hovered = -1, _chosen = -1, _focus = -1;
    private float _time, _closeTime = -1, _presence, _focusAge, _scale = 1;
    private bool _open, _closing, _anchorPending;

    public DollCommandWheel() => Enabled = false;
    public override bool IsInteractable => _open || _closing;
    public static bool IsShowing => TryGet(out var wheel) && wheel._open;
    /// <summary>打开中，或关闭后仍在等这次按键松开；期间世界标记与魔杖都不应响应这次按键。</summary>
    public static bool OwnsPointer => TryGet(out var wheel) && (wheel._open || wheel._closing);

    private bool Fading => _closeTime >= 0;
    private void SyncEnabled() => Enabled = _open || _closing || Fading;

    private static bool TryGet(out DollCommandWheel wheel)
    {
        wheel = null;
        return !Main.dedServ && SilkyUISystem.ServiceProvider != null
            && SilkyUIRenderSystem.Instance.TryGetInstance(out wheel);
    }

    /// <param name="current">正在使用的标记工具，环带内沿以亮弧标出。</param>
    internal static bool TryOpen(Func<bool> validSource, DollCommand? current, Action<DollCommand> confirmed)
    {
        if (Main.gameMenu || !Main.hasFocus || !TryGet(out var wheel)) return false;
        wheel.Cancel();
        wheel._closing = false;
        wheel._closeTime = -1;
        wheel._validSource = validSource;
        wheel._confirmed = confirmed;
        wheel._current = current;
        wheel._open = wheel._anchorPending = true;
        wheel._time = wheel._presence = wheel._focusAge = 0;
        wheel._hovered = wheel._focus = wheel._chosen = -1;
        Array.Clear(wheel._hover);
        wheel.SyncEnabled();
        return true;
    }

    internal static void ResetWorld()
    {
        if (TryGet(out var wheel)) Main.QueueMainThreadAction(wheel.CloseImmediately);
    }

    internal static void Unload()
    {
        _cdEffect = null;
        _icons = null;
    }

    private void Clear(bool holdPointer)
    {
        if (_open && !_anchorPending)
        {
            for (int i = 0; i < Count; i++)
            {
                _extentAtClose[i] = OpeningExtent(_time, i);
                _opacityAtClose[i] = OpeningOpacity(_time, i);
            }
            _closeTime = 0;
        }
        _open = false;
        // 与治疗盘相同：保留透明根节点吃掉确认/取消这次按键，直到松开。
        _closing = holdPointer && (Main.mouseLeft || Main.mouseRight);
        _validSource = null;
        _confirmed = null;
        SyncEnabled();
    }

    private void Cancel()
    {
        if (!_open) return;
        _chosen = -1;
        Clear(Main.hasFocus && !Main.gameMenu);
    }

    private void CloseImmediately()
    {
        Cancel();
        _closing = false;
        _closeTime = -1;
        SyncEnabled();
    }

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

    private void FitScreen(Size size) => SetSize(size.Width, size.Height, 0, 0);
    protected override void OnScreenSizeChanged(Size size, Size previous)
    {
        FitScreen(size);
        base.OnScreenSizeChanged(size, previous);
    }

    protected override void OnExitTree()
    {
        CloseImmediately();
        base.OnExitTree();
    }

    private bool CanStayOpen => !Main.gameMenu && Main.hasFocus && !Main.hideUI && !Main.drawingPlayerChat
        && !Main.ingameOptionsWindow && !Main.inFancyUI && !Main.editSign && !Main.editChest
        && Main.LocalPlayer is { active: true, dead: false } && Main.LocalPlayer.HeldItem.ModItem is ElainaWand
        && _validSource?.Invoke() == true;

    protected override void Update(GameTime gameTime)
    {
        if (_closing)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (!Main.hasFocus || !Main.mouseLeft && !Main.mouseRight)
            {
                _closing = false;
                SyncEnabled();
            }
            return;
        }
        if (_open && (!CanStayOpen || Main.mouseRight
                || Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape)))
        {
            Cancel();
            return;
        }
        if (_open) Main.LocalPlayer.mouseInterface = true;
        base.Update(gameTime);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        float dt = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0, .1f);
        if (_open)
        {
            if (CanStayOpen) Track(dt);
            else Cancel();
        }

        if (Fading)
        {
            _closeTime += dt;
            _presence = Math.Max(0, _presence - dt / FadeOut);
            if (_closeTime >= (_chosen >= 0 ? ChosenClose : Math.Max(FadeOut, ItemClose)))
            {
                _closeTime = -1;
                SyncEnabled();
                return;
            }
        }

        int lit = _open ? _hovered : _chosen;
        for (int i = 0; i < Count; i++)
            _hover[i] = MathHelper.Lerp(_hover[i], i == lit ? 1 : 0, Math.Min(1, dt / .12f));

        // 圆心展示所指指令，回到中心显示“松开取消”；关闭后保持所选项随轮盘淡出。
        int focus = _open ? _hovered : _chosen >= 0 ? _chosen : _focus;
        if (focus != _focus)
        {
            _focus = focus;
            _focusAge = 0;
        }
        _focusAge += dt;
    }

    private void Track(float dt)
    {
        _time += dt;
        _presence = Math.Min(1, _presence + dt / FadeIn);
        Vector2 size = new(Width.Pixels, Height.Pixels);
        float margin = RadiusFor(Count) + BandHalf + 42;
        _scale = Math.Max(.1f, Math.Min(Math.Clamp(ElainaSkillManager.WheelScale, .3f, 1.5f),
            Math.Min(size.X, size.Y) / (2 * margin)));
        if (_anchorPending)
        {
            _anchor = Main.MouseScreen - Bounds.Position;
            _anchorPending = false;
        }
        margin *= _scale;
        _anchor.X = size.X > margin * 2 ? Math.Clamp(_anchor.X, margin, size.X - margin) : size.X / 2;
        _anchor.Y = size.Y > margin * 2 ? Math.Clamp(_anchor.Y, margin, size.Y - margin) : size.Y / 2;
        int hovered = Pick((Main.MouseScreen - Bounds.Position - _anchor) / _scale, Count);
        if (hovered >= 0 && hovered != _hovered) SoundEngine.PlaySound(SoundID.MenuTick);
        _hovered = hovered;
        // 打开这次按键属于魔杖，SUI 不一定派发 MouseUp；在拾取之后轮询，用松开时的指针位置确认。
        if (!Main.mouseLeft) Confirm(hovered);
    }

    private void Confirm(int slot)
    {
        if (!_open) return;
        if (slot < 0 || slot >= Count) { Cancel(); return; }
        Action<DollCommand> confirmed = _confirmed;
        Main.LocalPlayer.mouseInterface = true;
        _chosen = slot;
        Clear(true);
        SoundEngine.PlaySound(SoundID.MenuTick);
        confirmed?.Invoke((DollCommand)slot);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!_open && !Fading) return;
        FillView();
        _cdEffect ??= ModContent.Request<Effect>("伊蕾娜/Effects/Content/SkillSeal", AssetRequestMode.ImmediateLoad).Value;
        var art = new SkillWheelDrawing.Art(new SkillBarDrawing.StateArt(UIDrawKit.Pixel, UIDrawKit.GlowDot, UIDrawKit.DiamondFill),
            _cdEffect, UIDrawKit.NumberFont);
        _matrix = ScaleAround(_view.Center, _scale) * SilkyUI.TransformMatrix;
        Restart(spriteBatch, SpriteSortMode.Deferred, null);
        try
        {
            SkillWheelDrawing.Draw(spriteBatch, art, _view, PaintSector, Restart);
            DrawLabels(spriteBatch, art);
        }
        finally
        {
            _matrix = SilkyUI.TransformMatrix;
            Restart(spriteBatch, SpriteSortMode.Deferred, null);
        }
    }

    /// <summary>圆心图标下写指令名；攻击尚未实装，额外注明。环带下方是操作说明。</summary>
    private void DrawLabels(SpriteBatch batch, in SkillWheelDrawing.Art art)
    {
        float alpha = _view.Presence;
        if (alpha <= 0) return;
        Vector2 center = _view.Center;
        float width = HubRadiusFor(Count) * 1.7f;
        float fade = Math.Clamp(_focusAge / FocusFade, 0, 1);
        if (_focus >= 0 && _focus < Count)
        {
            DollCommand command = (DollCommand)_focus;
            HealingWheelDrawing.Text(batch, art.Number, LocalizeUI("DollCommand" + command), center + new Vector2(0, 40),
                14, width, new Color(245, 237, 255) * (alpha * fade));
            if (command == DollCommand.Attack)
                HealingWheelDrawing.Text(batch, art.Number, LocalizeUI("DollCommandUnavailable"), center + new Vector2(0, 57),
                    10, width, new Color(189, 176, 206) * (alpha * fade));
        }
        else if (_open)
        {
            HealingWheelDrawing.Text(batch, art.Number, LocalizeUI("HealingCancel"), center, 14, width,
                new Color(200, 187, 218) * (alpha * fade));
        }
        float outer = RadiusFor(Count) + BandHalf;
        HealingWheelDrawing.Text(batch, art.Number, LocalizeUI("DollCommandControls"), center + new Vector2(0, outer + 26),
            11, outer * 2, new Color(236, 225, 247) * alpha);
    }

    private void FillView()
    {
        if (_icons == null)
        {
            _icons = new Asset<Texture2D>[Count];
            for (int i = 0; i < Count; i++)
                _icons[i] = ModContent.Request<Texture2D>(DollCommands.IconPath((DollCommand)i), AssetRequestMode.ImmediateLoad);
        }

        SkillWheelDrawing.View view = _view;
        view.Count = Count;
        view.Center = Bounds.Position + _anchor;
        view.Pointer = view.Center + (Main.MouseScreen - view.Center) / _scale;
        view.Open = _open;
        view.Hovered = _open ? _hovered : -1;
        view.Chosen = _open ? -1 : _chosen;
        view.Release = !_open && _chosen >= 0 && Fading ? Math.Clamp(_closeTime / ChosenClose, 0, 1) : -1;
        view.Focus = _focus;
        view.FocusAge = _focusAge;
        view.Presence = _presence;
        view.RingScale = _open ? OpeningRingScale(_time) : MathHelper.Lerp(.94f, 1, _presence);
        view.Seconds = _time + .5f;
        view.ShowNumber = false;

        float radius = RadiusFor(Count);
        for (int i = 0; i < Count; i++)
        {
            var (extent, opacity) = _open
                ? (OpeningExtent(_time, i), OpeningOpacity(_time, i))
                : ClosingMotion(_closeTime, i == _chosen, _extentAtClose[i], _opacityAtClose[i]);
            bool attack = (DollCommand)i == DollCommand.Attack;
            view.Items[i] = new SkillWheelDrawing.WheelItem
            {
                Position = view.Center + Direction(AngleOf(i, Count)) * (radius * extent + 4 * _hover[i]),
                Opacity = opacity,
                Hover = _hover[i],
                Flash = i == view.Chosen && view.Release >= 0 ? 1 - view.Release : 0,
                Current = _current == (DollCommand)i,
                Slot = i,
                Stack = 1,
                MaxStack = 1,
                Icon = _icons[i].Value,
                // 攻击指令尚未实装，借用封印外观表示不可用；仍可选中，确认后不产生效果。
                Sealed = attack,
                SealDeniedAge = 9,
            };
        }
    }

    private void PaintSector(Vector2 center, float inner, float outer, float start, float end,
        Color fill, float border, Color borderColor)
        => DrawAnnularSectorUI(center, inner, outer, start, end, fill, border, borderColor, _matrix);

    private void Restart(SpriteBatch batch, SpriteSortMode mode, Effect effect)
    {
        batch.End();
        batch.Begin(mode, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, SilkyUI.ScissorRasterizerState, effect, _matrix);
    }
}

public sealed class DollCommandWheelSystem : ModSystem
{
    public override void OnWorldUnload() => DollCommandWheel.ResetWorld();
    public override void Unload() => DollCommandWheel.Unload();
}

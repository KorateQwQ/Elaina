using System;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria.Audio;
using Terraria.ID;
using 伊蕾娜.ElainaModSkills.UICore;
using 伊蕾娜.Items;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.SkillWheelDrawing;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

/// <summary>Healing-only target picker; the generic KL picker remains available to other consumers.</summary>
[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Healing Wheel", 1003)]
public sealed class ElainaHealingWheel : BaseBody
{
    private const int MaxTargets = HealingWheelDrawing.MaxTargets;

    private readonly List<int> _targets = [];
    private readonly HealingWheelDrawing.Target[] _portraits = new HealingWheelDrawing.Target[MaxTargets];
    private readonly float[] _itemHover = new float[MaxTargets];
    private readonly float[] _extentAtClose = new float[MaxTargets];
    private readonly float[] _opacityAtClose = new float[MaxTargets];
    private readonly SkillWheelDrawing.View _frame = new();
    private Func<int, bool> _validTarget;
    private Func<bool> _validSource;
    private Action<int> _confirmed;
    private Action _cancelled;
    private Matrix _matrix;
    private Vector2 _anchor;
    // _count 为正在绘制的目标数：关闭后 _targets 已清空，淡出动画仍按最后一帧的布局播完。
    private int _count, _focus = -1, _hover = -1, _chosen = -1, _refresh;
    private float _time, _closeTime = -1, _presence, _focusAge, _scale = 1;
    private bool _open, _closing, _anchorPending;

    public ElainaHealingWheel() => Enabled = false;
    public override bool IsInteractable => _open || _closing;
    public static bool IsShowing => TryGet(out var wheel) && wheel._open;
    /// <summary>打开中，或关闭后仍在等这次按键松开；期间魔杖不应开始新的使用。</summary>
    public static bool OwnsPointer => TryGet(out var wheel) && (wheel._open || wheel._closing);

    private bool Fading => _closeTime >= 0;

    private void SyncEnabled() => Enabled = _open || _closing || Fading;

    private static bool TryGet(out ElainaHealingWheel wheel)
    {
        wheel = null;
        return !Main.dedServ && SilkyUISystem.ServiceProvider != null
            && SilkyUIRenderSystem.Instance.TryGetInstance(out wheel);
    }

    internal static bool TryOpen(Func<int, bool> validTarget, Func<bool> validSource,
        Action<int> confirmed, Action cancelled)
    {
        if (Main.gameMenu || !Main.hasFocus || !TryGet(out var wheel)) return false;
        wheel.Cancel();
        wheel._closing = false;
        wheel._closeTime = -1;
        wheel._count = 0;
        wheel._validTarget = validTarget;
        wheel._validSource = validSource;
        wheel._confirmed = confirmed;
        wheel._cancelled = cancelled;
        wheel._open = true;
        wheel._anchorPending = true;
        wheel._time = wheel._presence = wheel._focusAge = 0;
        wheel._refresh = 0;
        wheel._hover = wheel._focus = wheel._chosen = -1;
        Array.Clear(wheel._itemHover);
        wheel.SyncEnabled();
        wheel.RefreshTargets();
        return wheel._open;
    }

    internal static void CancelOpen()
    {
        if (TryGet(out var wheel)) wheel.Cancel();
    }

    internal static void ResetWorld()
    {
        if (TryGet(out var wheel)) Main.QueueMainThreadAction(wheel.CloseImmediately);
    }

    private void Clear(bool holdPointer)
    {
        // 还没摆出过的轮盘（首帧前就关闭）没有可淡出的画面。
        if (_open && _count > 0 && !_anchorPending) BeginFade();
        _open = false;
        // Keep the transparent modal root alive for the confirming/cancelling gesture.
        // The shared input shield can then retain ownership through its release frame.
        _closing = holdPointer && (Main.mouseLeft || Main.mouseRight);
        _validTarget = null;
        _validSource = null;
        _confirmed = null;
        _cancelled = null;
        _targets.Clear();
        RemoveAllChildren();
        SyncEnabled();
    }

    /// <summary>记下关闭瞬间各目标的展开程度，未展开完的目标从当前位置开始收拢。</summary>
    private void BeginFade()
    {
        for (int i = 0; i < _count; i++)
        {
            _extentAtClose[i] = OpeningExtent(_time, i);
            _opacityAtClose[i] = OpeningOpacity(_time, i);
        }
        _closeTime = 0;
    }

    private void Cancel()
    {
        if (!_open) return;
        Action cancelled = _cancelled;
        _chosen = -1;
        Clear(Main.hasFocus && !Main.gameMenu);
        cancelled?.Invoke();
    }

    private void CloseImmediately()
    {
        Cancel();
        _closing = false;
        _closeTime = -1;
        _count = 0;
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
        if (_open && (!CanStayOpen || Main.keyState.IsKeyDown(Keys.Escape)
                && Main.oldKeyState.IsKeyUp(Keys.Escape)))
        {
            Cancel();
            return;
        }
        if (_open)
        {
            Main.LocalPlayer.mouseInterface = true;
            // Keep the last displayed sectors on release; Confirm revalidates that exact target.
            if (Main.mouseLeft && ++_refresh >= 10) { _refresh = 0; RefreshTargets(); }
        }
        base.Update(gameTime);
    }

    private void RefreshTargets()
    {
        if (!_open) return;
        // Keep existing sectors stable while players move. New candidates enter by distance;
        // the caster always has a slot even when more than eight players overlap.
        var next = Enumerable.Range(0, Main.maxPlayers).Where(i => _validTarget(i))
            .OrderBy(i => i == Main.myPlayer ? 0 : 1)
            .ThenBy(i => Vector2.DistanceSquared(Main.LocalPlayer.Center, Main.player[i].Center))
            .ThenBy(i => i).Take(MaxTargets).ToList();
        if (next.Count == 0) { Cancel(); return; }
        var stable = _targets.Where(next.Contains).ToList();
        stable.AddRange(next.Where(i => !stable.Contains(i)));
        if (_targets.SequenceEqual(stable)) return;
        _targets.Clear();
        _targets.AddRange(stable);
        _count = _targets.Count;
        _hover = -1;
        RemoveAllChildren();
        // Sectors extend beyond the visible ring; the hub represents cancellation.
        for (int i = -1; i < _targets.Count; i++)
        {
            var region = new TargetRegion(this, i).Join(this);
            region.SetSize(0, 0, 1, 1);
            region.SetLeft(0);
            region.SetTop(0);
        }
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        float dt = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0, .1f);
        if (_open && _targets.Count > 0)
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
                _count = 0;
                SyncEnabled();
                return;
            }
        }

        // 选中项在松开后保持高亮直到淡出，其余项的悬停光平滑收回。
        int lit = _open ? _hover : _chosen;
        for (int i = 0; i < _count; i++)
            _itemHover[i] = MathHelper.Lerp(_itemHover[i], i == lit ? 1 : 0, Math.Min(1, dt / .12f));

        // 圆心展示所指目标，回到中心显示“松开取消”；关闭后保持最后一项随轮盘淡出。
        int focus = _open ? _hover : _chosen >= 0 ? _chosen : _focus;
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
        float margin = HealingWheelDrawing.OuterRadius(_targets.Count) + 42;
        _scale = Math.Min(Math.Clamp(ElainaSkillManager.WheelScale, .3f, 1.5f),
            Math.Min(size.X, size.Y) / (2 * margin));
        _scale = Math.Max(.1f, _scale);
        if (_anchorPending)
        {
            _anchor = Main.MouseScreen - Bounds.Position;
            _anchorPending = false;
        }
        margin *= _scale;
        _anchor.X = size.X > margin * 2 ? Math.Clamp(_anchor.X, margin, size.X - margin) : size.X / 2;
        _anchor.Y = size.Y > margin * 2 ? Math.Clamp(_anchor.Y, margin, size.Y - margin) : size.Y / 2;
        int hover = Pick(Main.MouseScreen);
        if (hover >= 0 && hover != _hover) SoundEngine.PlaySound(SoundID.MenuTick);
        _hover = hover;
        // The opening press belongs to the wand, so SUI may not dispatch MouseUp here.
        // Poll after picking in UI coordinates to use the release position, never the previous frame.
        if (!Main.mouseLeft) Confirm(hover);
    }

    private int Pick(Vector2 point) => HealingWheelDrawing.Pick((point - Bounds.Position - _anchor) / _scale, _targets.Count);
    private void Confirm(int slot)
    {
        if (!_open) return;
        if (!CanStayOpen) { Cancel(); return; }
        if (slot < 0 || slot >= _targets.Count || !_validTarget(_targets[slot])) { Cancel(); return; }
        int target = _targets[slot];
        Action<int> confirmed = _confirmed;
        Main.LocalPlayer.mouseInterface = true;
        _chosen = slot;
        Clear(true); // Clear callbacks before gameplay; retain only input ownership until release.
        SoundEngine.PlaySound(SoundID.MenuTick);
        confirmed?.Invoke(target);
    }

    public override void OnRightMouseDown(UIMouseEvent evt)
    {
        base.OnRightMouseDown(evt);
        if (_open) Cancel();
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_count == 0 || !_open && !Fading) return;
        FillFrame();
        var art = new SkillWheelDrawing.Art(new SkillBarDrawing.StateArt(UIDrawKit.Pixel, UIDrawKit.GlowDot, UIDrawKit.DiamondFill),
            null, UIDrawKit.NumberFont);
        var labels = new HealingWheelDrawing.Labels(LocalizeUI("HealingTarget"), LocalizeUI("HealingSelf"),
            LocalizeUI("HealingCritical"), LocalizeUI("HealingFull"), LocalizeUI("HealingMissing", "{0}"),
            LocalizeUI("HealingControls"), LocalizeUI("HealingCancel"));
        _matrix = ScaleAround(_frame.Center, _scale) * SilkyUI.TransformMatrix;
        Restart(spriteBatch, SpriteSortMode.Deferred, null);
        try
        {
            SkillWheelDrawing.DrawFrame(spriteBatch, art, _frame, PaintSector, Restart);
            HealingWheelDrawing.DrawTargets(spriteBatch, art, _frame, _portraits, labels, PaintSector, DrawHead);
        }
        finally
        {
            _matrix = SilkyUI.TransformMatrix;
            Restart(spriteBatch, SpriteSortMode.Deferred, null);
        }
    }

    /// <summary>与技能盘相同的开合节奏：环带回弹展开、目标依次飞出；松开时选中项原地闪光，其余向内收拢淡出。</summary>
    private void FillFrame()
    {
        SkillWheelDrawing.View frame = _frame;
        HealingWheelDrawing.ConfigureFrame(frame, _count);
        frame.Center = Bounds.Position + _anchor;
        frame.Pointer = frame.Center + (Main.MouseScreen - frame.Center) / _scale;
        frame.Open = _open;
        frame.Hovered = _open ? _hover : -1;
        frame.Chosen = _open ? -1 : _chosen;
        frame.Release = !_open && _chosen >= 0 ? Math.Clamp(_closeTime / ChosenClose, 0, 1) : -1;
        frame.Focus = _focus;
        frame.FocusAge = _focusAge;
        frame.Presence = _presence;
        frame.RingScale = _open ? OpeningRingScale(_time) : MathHelper.Lerp(.94f, 1, _presence);
        frame.Seconds = _time + .5f;

        float radius = HealingWheelDrawing.Radius(_count);
        for (int i = 0; i < _count; i++)
        {
            var (extent, opacity) = _open
                ? (OpeningExtent(_time, i), OpeningOpacity(_time, i))
                : ClosingMotion(_closeTime, i == _chosen, _extentAtClose[i], _opacityAtClose[i]);
            frame.Items[i] = new SkillWheelDrawing.WheelItem
            {
                Position = frame.Center + Direction(AngleOf(i, _count)) * (radius * extent + 4 * _itemHover[i]),
                Opacity = opacity,
                Hover = _itemHover[i],
                Flash = i == frame.Chosen && frame.Release >= 0 ? 1 - frame.Release : 0,
            };
            // 淡出期间沿用最后一帧的名字与血量，目标离线也不会读到空位。
            if (!_open) continue;
            Player player = Main.player[_targets[i]];
            _portraits[i] = new(player.whoAmI, player.name, player.statLife, player.statLifeMax2, player.whoAmI == Main.myPlayer);
        }
    }

    private void PaintSector(Vector2 center, float inner, float outer, float start, float end,
        Color fill, float border, Color borderColor)
        => DrawAnnularSectorUI(center, inner, outer, start, end, fill, border, borderColor, _matrix);

    private void Restart(SpriteBatch batch, SpriteSortMode mode, Effect effect)
    {
        batch.End();
        batch.Begin(mode, BlendState.AlphaBlend, mode == SpriteSortMode.Immediate ? SamplerState.PointClamp : SamplerState.LinearClamp,
            DepthStencilState.None, SilkyUI.ScissorRasterizerState, effect, _matrix);
    }

    private void DrawHead(int index, Vector2 center, float scale, float alpha)
    {
        if (alpha <= 0 || !Main.player[index].active) return;
        // Immediate mode applies the same UI vertex transform before Terraria renders its head layers/dyes.
        Restart(Main.spriteBatch, SpriteSortMode.Immediate, null);
        try { DrawStaticPlayerHead(Main.player[index], center + new Vector2(-3), alpha, scale, Color.Transparent); }
        finally { Restart(Main.spriteBatch, SpriteSortMode.Deferred, null); }
    }

    private sealed class TargetRegion : UIElementGroup
    {
        private readonly ElainaHealingWheel _owner;
        private readonly int _slot;
        public TargetRegion(ElainaHealingWheel owner, int slot)
        {
            _owner = owner;
            _slot = slot;
            Positioning = Positioning.Absolute;
            FitWidth = FitHeight = false;
            Border = 0;
            BackgroundColor = BorderColor = Color.Transparent;
        }
        public override bool ContainsPoint(Vector2 point) => _owner._open && _owner.Pick(point) == _slot;
    }
}

public sealed class HealingWheelSystem : ModSystem
{
    public override void OnWorldUnload() => ElainaHealingWheel.ResetWorld();
}

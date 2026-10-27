using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

/// <summary>
/// 技能栏右上方的显现/启封提示，占用原手札按钮的位置；手札只通过快捷键打开。
/// 仅在有提示时接收鼠标：悬停保持显示，点击打开手札并选中提示中的第一个技能。
/// </summary>
[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Constellation Discovery Toast", 999)]
public sealed class ConstellationDiscoveryToastUI : BaseBody
{
    private ConstellationDrawing _drawing;
    private ConstellationState _state;
    private readonly ConstellationUIClock _clock = new();
    private static ConstellationToastQueue Toasts => ConstellationDiscoveryPlayer.Toasts;
    public override bool IsInteractable => !Main.gameMenu && Toasts.Current != null && !ConstellationSkillPanel.PanelShown;

    protected override void OnInitialize()
    {
        Border = 0; Padding = new Margin(0);
        BackgroundColor = Color.Transparent;
        FitWidth = FitHeight = false;
        SetSize(ConstellationToastPainter.Width, ConstellationToastPainter.Height, 0, 0);
        _drawing = new ConstellationDrawing(ModContent.GetInstance<global::伊蕾娜.伊蕾娜>());
        _clock.Restart();
        LeftMouseClick += (_, _) =>
        {
            if (!IsInteractable || Toasts.Current?.Ids.FirstOrDefault() is not { } id) return;
            Toasts.Clear();
            ConstellationSkillPanel.OpenAt(id);
        };
        Position();
    }

    /// <summary>原手札按钮的区域，开合动画与炼金手札仍以它为飞出/收回的锚点。</summary>
    internal static Rectangle DockBounds()
    {
        var screen = GraphicsDeviceHelper.GetBackBufferSizeByUIScale();
        // Same anchor as the existing rack; remains deterministic before its first layout pass.
        Vector2 rack = new((screen.Width - SkillBarDrawing.Width) * .95f, (screen.Height - SkillBarDrawing.Height) * .95f);
        if (SilkyUISystem.ServiceProvider != null &&
            SilkyUIRenderSystem.Instance.TryGetInstance<ElainaSkillBar>(out var bar) && bar.Bounds.Width > 0)
            rack = bar.Bounds.Position;
        return ConstellationBookLayout.DockBounds(new Vector2(screen.Width, screen.Height), rack);
    }

    private void Position()
    {
        var bounds = ConstellationToastPainter.Bounds(DockBounds());
        SetLeft(bounds.X, 0, 0); SetTop(bounds.Y, 0, 0);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        Position();
        float seconds = _clock.Sample();
        if (Main.gameMenu) Toasts.Clear();
        // The panel shows the same news as fresh markers; the queue waits while it is open or the game is unfocused.
        else if (!ConstellationSkillPanel.PanelShown && Main.hasFocus) Toasts.Advance(seconds, IsInteractable && IsMouseHovering);
        if (IsInteractable && IsMouseHovering) Main.LocalPlayer.mouseInterface = true;
        base.UpdateStatus(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Toasts.Current is not { } toast || ConstellationSkillPanel.PanelShown) return;
        var skillPlayer = Main.LocalPlayer.GetModPlayer<ElainaSkillModPlayer>();
        if (_state == null || !_state.IsCurrentPlayer) _state = new ConstellationState(skillPlayer);
        _state.Refresh();
        _drawing.Batch = spriteBatch; _drawing.Transform = SilkyUI.TransformMatrix;
        _drawing.Origin = Bounds.Position; _drawing.Scale = 1;
        ConstellationToastPainter.Paint(_drawing, _state, toast, IsInteractable && IsMouseHovering);
    }
}

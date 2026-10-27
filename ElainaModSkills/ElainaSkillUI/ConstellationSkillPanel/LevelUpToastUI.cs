using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Layout;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

/// <summary>
/// 人物头顶上方的升级提示。只显示不接收鼠标，不打断操作；停留结束后原地淡出，不进入技能栏旁的停靠位。
/// </summary>
[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Level Up Toast", 998)]
public sealed class LevelUpToastUI : BaseBody
{
    internal static readonly LevelUpToastQueue Toasts = new();
    private ConstellationDrawing _drawing;
    private readonly ConstellationUIClock _clock = new();
    public override bool IsInteractable => false;

    protected override void OnInitialize()
    {
        Border = 0; Padding = new Margin(0);
        BackgroundColor = Color.Transparent;
        FitWidth = FitHeight = false;
        SetSize(LevelUpToastPainter.Width * LevelUpToastQueue.CenterScale, LevelUpToastPainter.Height * LevelUpToastQueue.CenterScale, 0, 0);
        _drawing = new ConstellationDrawing(ModContent.GetInstance<global::伊蕾娜.伊蕾娜>());
        _clock.Restart();
        Position();
    }

    private static Vector2 TopLeft(float scale)
    {
        var screen = GraphicsDeviceHelper.GetBackBufferSizeByUIScale();
        return new Vector2((screen.Width - LevelUpToastPainter.Width * scale) / 2,
            screen.Height * LevelUpToastQueue.CenterY - LevelUpToastPainter.Height * scale / 2);
    }

    private void Position()
    {
        var at = TopLeft(LevelUpToastQueue.CenterScale);
        SetLeft(at.X, 0, 0); SetTop(at.Y, 0, 0);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        Position();
        float seconds = _clock.Sample();
        if (Main.gameMenu) Toasts.Clear();
        else if (Main.hasFocus) Toasts.Advance(seconds);
        base.UpdateStatus(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Toasts.Current is not { } toast || Main.gameMenu) return;
        float scale = LevelUpToastQueue.CenterScale * LevelUpToastQueue.PopScale(toast);
        _drawing.Batch = spriteBatch; _drawing.Transform = SilkyUI.TransformMatrix;
        _drawing.Scale = scale;
        _drawing.Origin = TopLeft(scale) + new Vector2(0, LevelUpToastQueue.Rise(toast) * LevelUpToastQueue.CenterScale);
        LevelUpToastPainter.Paint(_drawing, toast);
    }
}

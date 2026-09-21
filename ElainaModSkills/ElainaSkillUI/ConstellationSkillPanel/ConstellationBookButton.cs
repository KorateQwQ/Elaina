using System;
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
using 伊蕾娜.Config;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

[RegisterUI("Vanilla: Radial Hotbars", "Elaina: Notebook Button", 999)]
public sealed class ConstellationBookButton : BaseBody
{
    private ConstellationDrawing _drawing;
    public override bool IsInteractable => !Main.gameMenu && !ConstellationSkillPanel.BookMoving && Main.LocalPlayer.itemAnimation <= 0;

    protected override void OnInitialize()
    {
        Border = 0; Padding = new Margin(0);
        BackgroundColor = Color.Transparent;
        FitWidth = FitHeight = false;
        SetSize(96, 30, 0, 0);
        _drawing = new ConstellationDrawing(ModContent.GetInstance<global::伊蕾娜.伊蕾娜>());
        LeftMouseClick += (_, _) => { if (IsInteractable) ConstellationSkillPanel.TogglePanel(); };
        PositionButton();
    }

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

    private void PositionButton()
    {
        var bounds = DockBounds();
        SetLeft(bounds.X, 0, 0); SetTop(bounds.Y, 0, 0);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        PositionButton();
        base.UpdateStatus(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        _drawing.Batch = spriteBatch; _drawing.Origin = Bounds.Position; _drawing.Scale = 1;
        string key = KeyBind.OpenSkillPanel?.GetAssignedKeys().FirstOrDefault() ?? "—";
        ConstellationBookRenderer.PaintDock(_drawing, key, IsInteractable && IsMouseHovering, ConstellationSkillPanel.BookDockGlow);
    }
}

using KL.SkillSystem.AbstractClass;
using KL.SkillSystem.TemplateSkillUI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria.ModLoader;
using 伊蕾娜.Config;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

[RegisterUI("Vanilla: Radial Hotbars", "ElainaMod: ElainaSkillBar", int.MinValue)]
public class ElainaSkillBar : BasicSkillBar
{
    internal const string ArtPath = "伊蕾娜/ElainaModSkills/ElainaSkillUI/BattleBar/";
    private Asset<Texture2D> _rack;
    private bool _moving;

    public override bool Enabled
    {
        get => base.Enabled && !Main.gameMenu && Main.LocalPlayer.active
            && Main.LocalPlayer.GetModPlayer<ElainaModplayer>().Elaina;
        set => base.Enabled = value;
    }

    // 技能栏完全不接收鼠标：战斗中点到它既不会被拦截，也不会误拖；位置只由快捷键调整。
    public override bool IsInteractable => false;
    public override int MaxSkillSlot { get; set; } = 8;

    protected override void OnInitialize()
    {
        ApplyLayout();
        SetSize(SkillBarDrawing.Width, SkillBarDrawing.Height);
        FlexDirection = FlexDirection.Row;
        FitWidth = FitHeight = false;
        Border = 0;
        Padding = new Margin(27, 25, 13, 9);
        Gap = 18;
        BackgroundColor = BorderColor = Color.Transparent;
        OverflowHidden = false;
        Enabled = true;
        ZIndex = -100;
        _rack = ModContent.Request<Texture2D>(ArtPath + "Rack");
        ElainaSkillModPlayer.ElainaSkillBar = this;
        for (int i = 0; i < MaxSkillSlot; i++)
            new ElainaSkillSlot(null) { SlotIndex = i }.Join(this);
        UpdateSkillBar();
    }

    public void UpdateSkillBar()
    {
        var skills = ElainaSkillModPlayer.GetActiveSkill;
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is not ElainaSkillSlot slot) continue;
            var skill = i < skills.Count ? skills[i] : null;
            if (slot.GetSlotSkill() != skill)
            {
                if (skill != null) slot.AddSkillToSlot(skill);
                else
                {
                    slot.RemoveAllChildren();
                    slot.SkillIcon = null;
                }
            }
            if (i == ElainaSkillModPlayer.CurrentSkillIndex) slot.HandleSelected();
            else slot.HandleDeselected();
        }
    }

    protected override SkillSlot CreateSkillSlot(SkillIcon icon = null) => new ElainaSkillSlot(icon);

    /// <summary>按住“调整技能栏位置”快捷键时，技能栏以鼠标为中心跟随；松开后保存为全局位置。</summary>
    private void UpdateMoveKey()
    {
        bool held = KeyBind.MoveSkillBar?.Current == true && Main.hasFocus
            && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest && !Main.blockInput;
        if (held)
        {
            var screen = GraphicsDeviceHelper.GetBackBufferSizeByUIScale();
            var size = new Vector2(SkillBarDrawing.Width, SkillBarDrawing.Height);
            SkillBarLayout.MoveTo(Main.MouseScreen - size / 2, new Vector2(screen.Width, screen.Height), size);
        }
        else if (_moving) SkillBarLayout.Save();
        _moving = held;
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateMoveKey();
        ApplyLayout();
        base.Update(gameTime);
    }

    private void ApplyLayout()
    {
        var anchor = SkillBarLayout.Anchor;
        SetLeft(pixels: 0, alignment: anchor.X);
        SetTop(pixels: 0, alignment: anchor.Y);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        SkillBarDrawing.DrawRack(spriteBatch, _rack.Value, Bounds.Position);
    }
}

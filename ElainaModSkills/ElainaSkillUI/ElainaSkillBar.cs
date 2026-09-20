using KL.SkillSystem.AbstractClass;
using KL.SkillSystem.TemplateSkillUI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

[RegisterUI("Vanilla: Radial Hotbars", "ElainaMod: ElainaSkillBar", int.MinValue)]
public class ElainaSkillBar : BasicSkillBar
{
    internal const string ArtPath = "伊蕾娜/ElainaModSkills/ElainaSkillUI/BattleBar/";
    private Asset<Texture2D> _rack;
    public override bool IsInteractable => Main.LocalPlayer.itemAnimation <= 0;
    public override int MaxSkillSlot { get; set; } = 8;

    protected override void OnInitialize()
    {
        SetLeft(alignment: 0.95f);
        SetTop(alignment: 0.95f);
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

    protected override void Update(GameTime gameTime)
    {
        SetLeft(pixels: 0, alignment: 0.95f);
        SetTop(pixels: 0, alignment: 0.95f);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        SkillBarDrawing.DrawRack(spriteBatch, _rack.Value, Bounds.Position);
    }
}

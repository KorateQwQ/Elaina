using System;
using KL.SkillSystem;
using KL.SkillSystem.AbstractClass;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SilkyUIFramework;
using SilkyUIFramework.Extensions;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills.UICore;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

public class ElainaSkillSlot(SkillIcon skillIcon) : SkillSlot(skillIcon)
{
    internal int SlotIndex { get; init; }
    protected override float SlotBorder { get; set; } = 0;
    protected override Color SlotBackgroundColor { get; set; } = Color.Transparent;
    protected override Vector4 SlotBorderRadius { get; set; } = new(2);
    protected override Vector2 SlotSize { get; set; } = new(60);
    private Asset<Texture2D> _ornaments;
    private bool _selected;
    private float _selectionTime;
    private float _hover;

    protected override void OnInitialize()
    {
        base.OnInitialize();
        OverflowHidden = false;
        BorderColor = Color.Transparent;
        _ornaments = ModContent.Request<Texture2D>(ElainaSkillBar.ArtPath + "Ornaments");
    }

    public ElainaSkill GetSkill() => GetSlotSkill()?.ModSkill as ElainaSkill;
    public Skill GetSlotSkill() => HasSkill ? (Children[0] as ElainaSkillIcon)?.Skill : null;

    protected override void Update(GameTime gameTime)
    {
        float dt = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0, 0.1f);
        bool selected = SlotIndex == ElainaSkillModPlayer.CurrentSkillIndex;
        _selectionTime = selected && _selected ? _selectionTime + dt : 0;
        _selected = selected;
        _hover = MathHelper.Lerp(_hover, IsMouseHovering ? 1 : 0, Math.Min(1, dt / .14f));
        if (IsMouseHovering) Main.LocalPlayer.mouseInterface = true;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        SkillBarDrawing.DrawSlotBackground(spriteBatch, _ornaments.Value,
            Bounds.Center, _selected, HasSkill, _hover, _selectionTime);
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.DrawChildren(gameTime, spriteBatch);
        SkillBarDrawing.DrawSlotFrame(spriteBatch, _ornaments.Value,
            Bounds.Center, _selected, HasSkill, _hover, _selectionTime);
        if (GetSlotSkill() is { } skill)
        {
            var seal = skill.ModSkill as ElainaSealedSkill;
            bool bound = seal != null && SkillBarDrawing.HasSealOverlay(seal.IsSealed, seal.SealReleaseAge);
            SkillBarDrawing.DrawSkillState(spriteBatch, StateArt, UIDrawKit.NumberFont, Bounds.Center,
                skill.CurrentCD, skill.CooldownRemainingFraction, skill.Stack, skill.MaxStack, ElainaSkillManager.ShowCD && !bound);
            if (bound)
                SkillBarDrawing.DrawSealState(spriteBatch, StateArt, Bounds.Center, (float)Main.GlobalTimeWrappedHourly,
                    seal.SealDeniedAge, seal.IsSealed ? -1 : seal.SealReleaseAge);
        }
    }

    private static SkillBarDrawing.StateArt StateArt => new(UIDrawKit.Pixel, UIDrawKit.GlowDot, UIDrawKit.DiamondFill);

    public ElainaSkillSlot AddSkillToSlot(Skill skill)
    {
        RemoveAllChildren();
        SkillIcon = new ElainaSkillIcon(skill).Join(this);
        return this;
    }
}

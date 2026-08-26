using System;
using KL.Extensions;
using KL.SkillSystem;
using KL.SkillSystem.AbstractClass;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SilkyUIFramework;
using SilkyUIFramework.Extensions;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.UICore;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

public class ElainaSkillSlot(SkillIcon skillIcon) : SkillSlot(skillIcon)
{
    protected override float SlotBorder { get; set; } = 4f;
    protected override Color SlotBorderColor { get; set; } = Color.Black;
    protected override Color SlotBackgroundColor { get; set; } = Color.Black * 0.0f;
    protected override Vector4 SlotBorderRadius { get; set; } = new Vector4(25);
    protected override float SlotPadding { get; set; } = 0f;
    protected override Vector2 SlotSize { get; set; } = new Vector2(58);

    /// <summary>图标绘制尺寸(正方形)。</summary>
    private const float IconDrawSize = 36f;
    /// <summary>底部悬浮光环的宽高。</summary>
    private const float HaloWidth = 30f;
    private const float HaloHeight = 6f;
    /// <summary>环绕光弧半径。</summary>
    private const float OrbitRadius = 22f;
    /// <summary>悬停时的放大比例。</summary>
    private const float HoverScale = 1.08f;

    private SpringFloat _hoverSpring = new(0f, 300f, 22f);
    private float _readyFlash;
    private float _press;
    private float _prevCD = -1f;
    private bool _prevInCD;

    protected override void OnInitialize()
    {
        base.OnInitialize();
    }

    public ElainaSkill GetSkill()
    {
        if (HasSkill)
            return (Children[0] as ElainaSkillIcon)?.Skill.ModSkill as ElainaSkill;
        return null;
    }

    public Skill GetSlotSkill()
    {
        if (!HasSkill) return null;
        return (Children[0] as ElainaSkillIcon)?.Skill;
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0f, 1f / 20f);

        SetTop(0);
        SetSize(58,58);
        BorderRadius = new Vector4(29);

        Border = 0.0f;
        BorderColor = Color.White;
        Padding = new Margin(0, 0);
        Margin = new Margin(5, 2);

        // 悬停弹簧插值
        Vector2 m = Main.MouseScreen;
        Vector2 center = Bounds.Position + SlotSize / 2;
        bool hovered = MathF.Abs(m.X - center.X) < SlotSize.X * 0.55f &&
                       MathF.Abs(m.Y - center.Y) < SlotSize.Y * 0.55f;
        _hoverSpring.Update(hovered ? 1f : 0f, dt);
        if (hovered) Main.LocalPlayer.mouseInterface = true;

        // 就绪闪光检测 + 施放下压脉冲
        Skill skill = GetSlotSkill();
        if (skill != null)
        {
            bool inCD = skill.InCD;
            if (_prevInCD && !inCD) _readyFlash = 1f;
            _prevInCD = inCD;

            float cd = skill.CurrentCD;
            if (_prevCD >= 0f && cd - _prevCD > 0.5f) _press = 1f;
            _prevCD = cd;
        }
        else
        {
            _prevInCD = false;
            _prevCD = -1f;
        }
        if (_readyFlash > 0f) _readyFlash -= dt * 1.8f;
        if (_press > 0f) _press -= dt * 3.4f;

        if (HasSkill) GetSkill().SelectedInSkillBar = true;

        base.Update(gameTime);
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = Bounds.Position + SlotSize / 2;
        Skill skill = GetSlotSkill();
        bool occupied = skill != null;

        // 绘制技能图标(子元素)
        base.DrawChildren(gameTime, spriteBatch);

        // 层数角标
        if (occupied && skill.MaxStack > 1)
        {
            UIDrawKit.DrawText(spriteBatch, UIDrawKit.NumberFont, $"{skill.Stack}",
                center + new Vector2(SlotSize.X * 0.30f, SlotSize.Y * 0.24f),
                Color.White, 0.66f, new Vector2(0.5f));
        }
        
    }


    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
    }

    public ElainaSkillSlot AddSkillToSlot(Skill skill)
    {
        RemoveAllChildren();
        var skillUI = new ElainaSkillIcon(skill).Join(this);
        SkillIcon = skillUI;
        return this;
    }
}

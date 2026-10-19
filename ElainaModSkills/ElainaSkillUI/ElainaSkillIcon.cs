using System;
using KL.SkillSystem;
using KL.SkillSystem.AbstractClass;
using SilkyUIFramework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

public class ElainaSkillIcon(Skill skill) : SkillIcon(skill)
{
    private Effect sealEffect;
    protected override void OnInitialize()
    {
        base.OnInitialize();
        if (Parent != null)
        {
            //适应skillSlot的大小和圆角效果
            SetSize(Parent.Width.Pixels,Parent.Height.Pixels);
            BorderRadius = new Vector4(2);
            Padding = 0;
            
            ImageScale = new Vector2(50f / Math.Max(Texture2D.Width(), Texture2D.Height()));

            SetLeft(alignment: 0.5f);
            SetTop(alignment: 0.5f);
        
            FitWidth = false;
            FitHeight = false;
            ImageAlign = new Vector2(0.5f);
            BackgroundColor = Color.Black*0;
            
            //忽略鼠标交互，伊蕾娜的技能图标不需要拖动，鼠标会直接穿透和技能栏交互拖动
            IgnoreMouseInteraction = true;

        }
    }

    public override bool CanDragAt(Vector2 mousePosition)
    {
        return false;
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        //Main.NewText("Mouse Up");
        base.OnLeftMouseUp(evt);
    }
    
    protected override void Update(GameTime gameTime)
    {
        // 技能栏刷新后，当前帧的 UI 缓存仍可能更新已移除的旧图标。
        var parent = Parent;
        if (parent == null) return;

        BackgroundColor = Color.Black*0.0f;
        SetSize(parent.Width.Pixels,parent.Height.Pixels);
        BorderRadius = new Vector4(2);
        ImageScale = new Vector2(50f / Math.Max(Texture2D.Width(), Texture2D.Height()));
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        /*EndBeginDrawUI();
        EndBeginDrawUI();*/
        if (Skill?.ModSkill is ElainaSealedSkill sealedSkill
            && SkillBarDrawing.HasSealOverlay(sealedSkill.IsSealed, sealedSkill.SealReleaseAge))
        {
            sealEffect ??= ModContent.Request<Effect>("伊蕾娜/Effects/Content/SkillSeal", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            sealEffect.Parameters["time"].SetValue(1 - Skill.CooldownRemainingFraction);
            sealEffect.Parameters["flashTime"].SetValue(0f);
            sealEffect.Parameters["ready"].SetValue(Skill.Stack > 0 ? 1f : 0f);
            sealEffect.Parameters["seal"].SetValue(SkillBarDrawing.SealTone(sealedSkill.IsSealed, sealedSkill.SealReleaseAge));
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, SilkyUI.ScissorRasterizerState, sealEffect, SilkyUI.TransformMatrix);
            Texture2D texture = Texture2D.Value;
            if (Skill.ModSkill.PreDrawSkillIcon(GetDrawCenter(), GetDrawSize(), Color.White, sealEffect))
                spriteBatch.Draw(texture, GetDrawCenter(), null, Color.White, 0, new Vector2(texture.Width, texture.Height) / 2,
                    ImageScale, SpriteEffects.None, 0);
            Skill.ModSkill.PostDrawSkillIcon(GetDrawCenter(), GetDrawSize(), Color.White, sealEffect);
        }
        else base.Draw(gameTime, spriteBatch);
        // Skill callbacks use the legacy UI batch; restore the current SUI transform.
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, SilkyUI.ScissorRasterizerState, null, SilkyUI.TransformMatrix);
        if (Skill?.ModSkill is ElainaSealedSkill { IsSealed: false } released)
        {
            float flash = SkillBarDrawing.SealReleasePulse(released.SealReleaseAge);
            if (flash > 0)
            {
                Texture2D texture = Texture2D.Value;
                spriteBatch.Draw(texture, GetDrawCenter(), null, new Color(245, 222, 255, 0) * flash, 0,
                    new Vector2(texture.Width, texture.Height) / 2, ImageScale, SpriteEffects.None, 0);
            }
        }
    }
}

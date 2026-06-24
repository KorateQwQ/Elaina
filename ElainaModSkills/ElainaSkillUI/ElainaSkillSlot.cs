using System;
using KL.Extensions;
using KL.SkillSystem;
using KL.SkillSystem.AbstractClass;
using KL.SkillSystem.SilkyUI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SilkyUIFramework;
using SilkyUIFramework.Extensions;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

public class ElainaSkillSlot(SkillIcon skillIcon) : SkillSlot(skillIcon)
{
    protected override float SlotBorder { get; set; } = 4f;
    
    protected override Color SlotBorderColor { get; set; } = Color.Black;
    
    protected override Color SlotBackgroundColor { get; set; } = Color.Black * 0.5f;
    
    //圆角角度
    protected override Vector4 SlotBorderRadius { get; set; } = new Vector4(4);
    
    protected override float SlotPadding { get; set; } = 0f;
    
    protected override Vector2 SlotSize{ get; set; } = new Vector2(56, 56);
    
    static Texture2D tex = null;
    
    protected override void OnInitialize()
    {
        tex ??= ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/ElainaSkillSlot_BackGround", AssetRequestMode.ImmediateLoad).Value;
        base.OnInitialize();
        //AddSkillToSlot(Skill.NewSkill(typeof(FireLaserSkill)));

    }

    public ElainaSkill GetSkill()
    {
        if (HasSkill)
        {
            return ((Children[0] as ElainaSkillIcon)?.Skill.ModSkill as ElainaSkill);
        }

        return null;
    }

    protected override void Update(GameTime gameTime)
    {
        Color pink = new Color(203,142,177);
        Color black =new Color(69,66,75);
        Color white =new Color(237,240,243);
        SetTop(-5);
        BorderRadius = new Vector4(6);
        Border = 0.0f;
        BackgroundColor = new Color(255, 89, 182, 155)*0;
        BorderColor = new Color(255, 255, 255, 255);
        
        Padding = new Margin(0,0);
        Margin = new Margin(5,2);
        if (HasSkill)
        {
            GetSkill().SelectedInSkillBar = true;
            if (GetSkill() != null && GetSkill().SkillSlot == ElainaSkillModPlayer.CurrentSkillIndex)
            {
            }
        }
        //PrintText(GetSkill() == null ? "null" : GetSkill().GetType().Name);
        
        base.Update(gameTime);
    }
    
    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        /*Texture2D circle  =  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/Texture/Icon_Circle", AssetRequestMode.ImmediateLoad).Value;
        var position = Bounds.Position + SlotSize / 2;
        DrawInScreen(circle, position,new Color(69,66,75),new Vector2(0.095f));*/
        base.DrawChildren(gameTime, spriteBatch);
        
        var position = Bounds.Position + SlotSize / 2;
        float border = 1.5f;
        Effect partRectangle = ModContent.Request<Effect>("KL/Effects/Content/BasicShape/RoundedRectBorderSegment", AssetRequestMode.ImmediateLoad).Value;

        DrawCrossStar(position+new Vector2(-00,-32f), new Vector2(15), Color.White,crossStarTipScale:new Vector4(1,0.7f,1,0.7f),crossStarCurve:1f,border:border,borderColor:Color.White,filled:true);
        
        DrawCrossStar(position+new Vector2(-00,32f), new Vector2(15), Color.White,crossStarTipScale:new Vector4(1,0.7f,1,0.7f),crossStarCurve:1f,border:border,borderColor:Color.White,filled:true);
        
        EndBeginDrawUI(2,1);
        partRectangle.SetValue("width",56);
        partRectangle.SetValue("height",56);
        partRectangle.SetValue("cornerRadius",13);
        partRectangle.SetValue("borderColor",new Vector4(1));
        partRectangle.SetValue("lineAngle",0);
        partRectangle.SetValue("lineLength",0.37f);
        partRectangle.SetValue("lineWidth",2);
        partRectangle.Apply();
        DrawInScreen(PanelSkillIcon.NullTexture.Value,position,scale:new Vector2(1.02f),color:Color.White);
        
        partRectangle.SetValue("lineAngle",180);
        partRectangle.Apply();
        DrawInScreen(PanelSkillIcon.NullTexture.Value,position,scale:new Vector2(1.02f),color:Color.White);

        EndBeginDrawUI();
    }

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {

        base.HandleDraw(gameTime, spriteBatch);


        Vector2 size = new Vector2(8, 15);
        float xOffset = 8;
        float yOffset = -3.5f;

        float border = 6;
        float corner = 6;
        Color background = new Color(203,142,177);
        
        //DrawDiamond(position+new Vector2(-xOffset,yOffset+SlotSize.Y / 2),size,rotation:-1.2f, color:background,border:border,corner:corner,filled:true);
        //DrawDiamond(position+new Vector2(xOffset,yOffset+SlotSize.Y / 2),size,rotation:1.2f, color:background,border:border,corner:corner,filled:true);
        //DrawDiamond(position+new Vector2(0,SlotSize.Y / 2-3f),size,color:background,border:border,corner:corner,filled:true);


    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        /*EndBeginDrawUI(2);
        DrawInScreen(tex, Bounds.Position + SlotSize / 2,Color.White, new Vector2(1.0f,1f)*0.05f);
        EndBeginDrawUI();*/
        if (GetSkill() != null&& GetSkill().SkillSlot==ElainaSkillModPlayer.CurrentSkillIndex)
        {
            /*EndBeginDrawUI(1);
            //DrawSelectedBorder();

            EndBeginDrawUI();*/
        }


    }

    void DrawSelectedBorder()
    {
        Texture2D Icon_Angle2  =  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/Texture/Icon_Angle_Bloom2", AssetRequestMode.ImmediateLoad).Value;
        Texture2D light  =  ModContent.Request<Texture2D>("KL/Effects/Tex/LightBloom", AssetRequestMode.ImmediateLoad).Value;

        var position = Bounds.Position + SlotSize / 2;

        float rotation = 0;
        Vector2 scale = Vector2.One*0.8f*DrawManager.FrameTime(0.95f,1.05f,60,(int)Main.timeForVisualEffects);
        Vector2 move = new Vector2(-5.2f)*DrawManager.FrameTime(0.9f,1.1f,60,(int)Main.timeForVisualEffects);
        Color color = Color.White;//new Color(255,194,249,255);
        color.A = (byte)DrawManager.FrameTime(50, 205, 60, (int)Main.timeForVisualEffects);
        //EndBeginDraw(1);
        float lightScale = 0.2f;
        float lightColorScale = 1f;
        
        DrawInScreen(Icon_Angle2, position+move,color,scale,rotation);
        //DrawInScreen(light, position+move + new Vector2(-17),color*lightColorScale,scale*lightScale,rotation);

        rotation += MathHelper.Pi / 2f;
        move = move.RotatedBy(MathHelper.Pi / 2f);
        DrawInScreen(Icon_Angle2, position+move,color,scale,rotation);
        //DrawInScreen(light, position+move + new Vector2(17,-17),color*lightColorScale,scale*lightScale,rotation);

        rotation += MathHelper.Pi / 2f;
        move = move.RotatedBy(MathHelper.Pi / 2f);
        DrawInScreen(Icon_Angle2, position+move,color,scale,rotation);
        //DrawInScreen(light, position+move + new Vector2(17),color*lightColorScale,scale*lightScale,rotation);

        rotation += MathHelper.Pi / 2f;
        move = move.RotatedBy(MathHelper.Pi / 2f);
        DrawInScreen(Icon_Angle2, position+move,color,scale,rotation);
        //DrawInScreen(light, position+move + new Vector2(-17,17),color*lightColorScale,scale*lightScale,rotation);

        EndBeginDrawUI();


    }
    
    
    public ElainaSkillSlot AddSkillToSlot(Skill skill)
    {
        RemoveAllChildren();
        var skillUI = new ElainaSkillIcon(skill).Join(this);
        SkillIcon = skillUI;
        return this;
    }
}
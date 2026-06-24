using System.Linq;
using KL.Drawing;
using KL.SkillSystem;
using KL.SkillSystem.AbstractClass;
using KL.SkillSystem.SilkyUI;
using KL.SkillSystem.TemplateSkillUI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills.Skills.Fire;


namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

[RegisterUI("Vanilla: Radial Hotbars", "ElainaMod: ElainaSkillBar", int.MinValue)]

public class ElainaSkillBar : BasicSkillBar
{
    public override bool IsInteractable => Main.LocalPlayer.itemAnimation <= 0;
    public virtual int MaxSkillSlot => ElainaSkillModPlayer.GetActiveSkill.Count;

    BookIconSkillBar bookIcon_SkillBar = null;
    private static Texture2D circle;
    private static Texture2D icon_Line;
    
    protected override void OnInitialize()
    {
        icon_Line  ??=  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/Texture/Icon_Line_Non", AssetRequestMode.ImmediateLoad).Value;
        circle  ??=  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/Texture/Icon_Circle", AssetRequestMode.ImmediateLoad).Value;
        
        SetLeft(alignment: 0.5f);
        SetTop(alignment: 0.05f);

        //排列方向
        FlexDirection = FlexDirection.Row;

        //自适应子元素大小
        FitWidth = true;
        FitHeight = true;

        //圆角
        BorderRadius = new Vector4(30);
        Border = 2;

        //内边距
        Padding = new Margin(30,8,8,8);
        Enabled = true;
        

        /*bookIcon_SkillBar = new BookIconSkillBar().Join(this);
        bookIcon_SkillBar.elainaSkillBar = this;*/
        
        ZIndex = -100;
        BorderColor = Color.Black*0.0f;
        BackgroundColor = Color.Black*0.0f;
        ElainaSkillModPlayer.ElainaSkillBar = this;
        RegisterSkillSlot();
        GetSkillSlot(ElainaSkillModPlayer.CurrentSkillIndex)?.HandleSelected();

    }

    void RegisterSkillSlot()
    {
        List<Skill> skills = ElainaSkillModPlayer.GetActiveSkill;
        for (int i = 0; i < MaxSkillSlot; i++)
        {
            var slotUI = CreateSkillSlot().Join(this);
            if (i < skills.Count&& skills[i] != null)
            {
                (slotUI as ElainaSkillSlot)?.AddSkillToSlot(skills[i]);
            }
            //slotUI.SlotIndex = i;
        }
    }

    ElainaSkillSlot GetSkillSlot(int index)
    {
        if (index >= 0 && index < MaxSkillSlot)
        {
            if(Children[index] is ElainaSkillSlot) return Children[index] as ElainaSkillSlot;
        }
        return null;
    }

    public void UpdateSkillBar()
    {
        List<Skill> skills = ElainaSkillModPlayer.GetActiveSkill;
        for (int i = 0; i < MaxSkillSlot; i++)
        {
            if (skills[i] != null)
            {
                GetSkillSlot(i)?.AddSkillToSlot(skills[i]).HandleDeselected();
            }
            else
            {
                GetSkillSlot(i)?.RemoveAllChildren();
                GetSkillSlot(i)?.HandleDeselected();
            }
        }
        GetSkillSlot(ElainaSkillModPlayer.CurrentSkillIndex)?.HandleSelected();
    }
    protected override SkillSlot CreateSkillSlot(SkillIcon icon = null)
    {
        return new ElainaSkillSlot(icon);
    }
    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        base.OnLeftMouseUp(evt);
    }
    public override UIView GetElementAt(Vector2 mousePosition)
    {
        //此处重写是因为父UI以外的位置的子UI（伊蕾娜UI的左侧灵性部分），点击会没有反应，导致无法拖动
        if (DisableMouseInteraction) return null;

        foreach (var child in ElementsInOrder.Reverse<UIView>())
        {
            var target = child.GetElementAt(mousePosition);
            if (target != null) return target;
        }

        if (!ContainsPoint(mousePosition)) return null;

        // 所有子元素都不符合条件, 如果自身不忽略鼠标交互, 则返回自己
        return IgnoreMouseInteraction ? null : this;
    }

    protected override void Update(GameTime gameTime)
    {
        //Enabled = false;

        //PrintText(GetAvailableSkillNum());
        //BackgroundColor = Color.Black*0;
        Padding = new Margin(8,8,8,8);
        SetLeft(pixels:0, alignment: 0.90f);
        SetTop(pixels:0, alignment: 0.95f);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        EndBeginDrawUI(2);

        var position = Bounds.Position+new Vector2(-10,0);
        float length = 1.5f;
        Color pink = new Color(203,142,177);
        Color black =new Color(69,66,75);
        Color white =new Color(237,240,243);
        Color purple =new Color(183,101,193,255) ;


        /*for (int i = 0; i < 3; i++)
        {
            DrawInScreen(new TextureInfo(icon_Line,originOffset:new Vector2(-icon_Line.Width/2f,0)), 
                position+new Vector2(3,0),color:white,scale: new Size(length,0.3f));
        
            DrawInScreen(new TextureInfo(icon_Line,originOffset:new Vector2(-icon_Line.Width/2f,0)), 
                position+new Vector2(3.5f,63),color:white,scale: new Size(length*0.7f,0.3f));
        
            DrawInScreen(new TextureInfo(icon_Line,originOffset:new Vector2(-icon_Line.Width/2f,0)), 
                position+new Vector2(0,67),color:white,scale: new Size(length*0.65f,0.15f));
        }*/

        EndBeginDrawUI(2);

        foreach (var child in Children)
        {
            if (child is ElainaSkillSlot slot)
            {
                var pos = slot.Bounds.Position + slot.Bounds.Size / 2;
                //DrawInScreen(circle, pos,black,new Vector2(0.11f));
                DrawRectangle(pos, new Vector2(65f), black,corner:8,border:0.5f,filled:true);

            }
        }

        EndBeginDrawUI();
        EndBeginDrawUI();
        
        base.Draw(gameTime, spriteBatch);
    }

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);
        
    }
    
}
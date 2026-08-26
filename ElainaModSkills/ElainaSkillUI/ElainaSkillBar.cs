using System;
using System.Linq;
using KL.Drawing;
using KL.Extensions;
using KL.SkillSystem;
using KL.SkillSystem.AbstractClass;
using KL.SkillSystem.SilkyUI;
using KL.SkillSystem.TemplateSkillUI;
using KL.Utils;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills.UICore;


namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

[RegisterUI("Vanilla: Radial Hotbars", "ElainaMod: ElainaSkillBar", int.MinValue)]

public class ElainaSkillBar : BasicSkillBar
{
    public override bool IsInteractable => Main.LocalPlayer.itemAnimation <= 0;
    public virtual int MaxSkillSlot => ElainaSkillModPlayer.GetActiveSkill.Count;


    BookIconSkillBar bookIcon_SkillBar = null;

    protected override void OnInitialize()
    {
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
        
        //图标之间的间距
        Gap = 10;
        
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
        SetLeft(pixels: 0, alignment: 0.95f);
        SetTop(pixels: 0, alignment: 0.95f);
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        float phase = (float)Main.timeForVisualEffects;
        int selectedIndex = ElainaSkillModPlayer.CurrentSkillIndex;

        /*// 先绘制所有槽位的灰色圆底背景
        foreach (var child in Children)
        {
            if (child is not ElainaSkillSlot slot) continue;
            if (slot.GetSlotSkill() == null) continue;

            Vector2 center = slot.Bounds.Position + slot.Bounds.Size / 2f;
            UIDrawKit.DrawGlow(spriteBatch, center, 35f, new Color(128, 128, 128) * 0.8f);
        }*/

        // 绘制技能槽内容
        base.Draw(gameTime, spriteBatch);

        // 绘制所有槽位的白色圆形外框和选中提示(在图标上层)
        EndBeginDrawUI(1, 1);
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is not ElainaSkillSlot slot) continue;
            if (slot.GetSlotSkill() == null) continue;

            Vector2 center = slot.Bounds.Position + slot.Bounds.Size / 2f;

            // 白色圆形外框
            DrawCircleFrame(spriteBatch, center, 28f, 1f, Color.White * 0.9f);

            // 选中槽位的顶部十字星提示
            if (i == selectedIndex)
            {
                Vector2 topPos = center + new Vector2(0f, -38f);
                UIDrawKit.DrawSparkle(spriteBatch, topPos, 58f, phase * 0.02f, Color.White * 0.85f);
            }
        }

        BackgroundColor = Color.Black;
        EndBeginDrawUI();
        var tex = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.ElainaSkillUI.ElainaSkillBar_BackGround");
        var slotTex = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.ElainaSkillUI.ElainaSkillSlot");

        Vector2 size = Bounds.Size;
        var position = Bounds.Position+ new Vector2(size.X/2, size.Y/2);

        
        DrawInScreen(tex,position,scale:new Vector2(1,1.1f));
        //DrawInScreen(slotTex, position);

    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch sb)
    {
        base.DrawChildren(gameTime, sb);

        FitWidth = false;
        SetWidth(620);
        var slotTex = AssetManager.GetTexture("伊蕾娜.ElainaModSkills.ElainaSkillUI.ElainaSkillSlot");
        Vector2 size = Bounds.Size;
        var position = Bounds.Position+ new Vector2(size.X/2, size.Y/2);
        //内边距
        Padding = new Margin(5,2,10,8);
        //图标之间的间距
        Gap = 11f;
        DrawInScreen(slotTex, position,scale:new Vector2(1,1.1f));
    }

    /// <summary>绘制圆形边框</summary>
    private void DrawCircleFrame(SpriteBatch sb, Vector2 center, float radius, float thickness, Color color)
    {
        const int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = MathHelper.TwoPi * i / segments;
            float angle2 = MathHelper.TwoPi * (i + 1) / segments;
            Vector2 p1 = center + radius * new Vector2(MathF.Cos(angle1), MathF.Sin(angle1));
            Vector2 p2 = center + radius * new Vector2(MathF.Cos(angle2), MathF.Sin(angle2));
            UIDrawKit.DrawLine(sb, p1, p2, thickness, color);
        }
    }

}

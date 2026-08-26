using KL.SkillSystem;
using KL.SkillSystem.AbstractClass;
using SilkyUIFramework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

public class ElainaSkillIcon(Skill skill) : SkillIcon(skill)
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        if (Parent != null)
        {
            //适应skillSlot的大小和圆角效果
            SetSize(Parent.Width.Pixels,Parent.Height.Pixels);
            BorderRadius = Parent.BorderRadius;
            
            ImageScale = new Vector2(Parent.Width.Pixels/Texture2D.Width()*1f, Parent.Height.Pixels/Texture2D.Height()*1f);

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
        BackgroundColor = Color.Black*0.0f;
        SetSize(Parent.Width.Pixels,Parent.Height.Pixels);
        BorderRadius = Parent.BorderRadius;
        ImageScale = new Vector2(Parent.Width.Pixels/Texture2D.Width(), Parent.Height.Pixels/Texture2D.Height())*0.98f;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        /*EndBeginDrawUI();
        EndBeginDrawUI();*/
        base.Draw(gameTime, spriteBatch);
    }
}
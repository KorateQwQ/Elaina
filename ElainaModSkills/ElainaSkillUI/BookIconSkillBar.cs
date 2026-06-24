using KL.Drawing;
using KL.SkillSystem.SilkyUI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

//[RegisterUI("Vanilla: Radial Hotbars", "BookIconSkillBar", int.MinValue)]
public class BookIconSkillBar : SUIImage,IDraggableUI
{
    public UIView DragUI => this;

    public bool IsDragging { get; set; }
    
    public Vector2 LastMousePosition { get; set; } = Vector2.Zero;

    public ElainaSkillBar elainaSkillBar;
    
    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        //((IDraggableUI)this).StartDrag(Main.MouseScreen);
        if (elainaSkillBar != null)
        {
            elainaSkillBar.IsDragging = true;
            elainaSkillBar.LastMousePosition = Main.MouseScreen;
            elainaSkillBar.OnLeftMouseDown(evt);
        }
        //base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        //((IDraggableUI)this).StopDrag();
        if (elainaSkillBar != null)
        {
            elainaSkillBar.IsDragging = false;
            elainaSkillBar.OnLeftMouseUp(evt);
        }
        //base.OnLeftMouseUp(evt);
    }
    
    protected override void Update(GameTime gameTime)
    {
        ZIndex = 1;//设置图层顺序
        if (elainaSkillBar != null && elainaSkillBar.IsDragging)
        {
            elainaSkillBar.IsDragging = true;
        }
        
        ((IDraggableUI)this).UpdateDrag(); // 调用接口的拖拽更新
        SetLeft(-80,alignment: 0f);
        SetTop(-20,alignment: 0f);
        BackgroundColor = Color.Black;
        
        Border = 2;
        BorderColor = Color.White;

        //自适应子元素大小
        FitWidth = false;
        FitHeight = false;
        
        SetWidth(80);
        SetHeight(80);
        base.Update(gameTime);
    }

    protected override void OnInitialize()
    {
        SetLeft(alignment: 0.5f);
        SetTop(alignment: 0.05f);
        SetWidth(200);
        SetHeight(200);
        Positioning = Positioning.Absolute;
        
        base.OnInitialize();
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {

        Texture2D BookIcon  =  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/BookIcon", AssetRequestMode.ImmediateLoad).Value;
        Texture2D Icon_Diamond  =  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/Texture/Icon_Diamond_Medium", AssetRequestMode.ImmediateLoad).Value;
        Texture2D Icon_Angle  =  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/Texture/Icon_Angle4", AssetRequestMode.ImmediateLoad).Value;
        Texture2D Elaina_Icon  =  ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/Elaina_Icon", AssetRequestMode.ImmediateLoad).Value;
 
        
        Color pink = new Color(203,142,177);
        Color black =new Color(69,66,75);
        Color white =new Color(237,240,243);

        var position = Bounds.Position+new Vector2(-10,0)+new Vector2(38,42);
        var size = Bounds.Size;
        //DrawInScreen(BookIcon,position+new Vector2(38,38),scale: new Size(1.1f));
        //DrawDiamondBackGround(position+new Vector2(0,0),28,black);
        /*
        DrawDiamond(position+new Vector2(-0,0),new Vector2(105),black,border:1,filled:true,borderColor:Color.White);

        //DrawInScreen(Elaina_Icon,position+new Vector2(0,0),scale: new Size(1.00f));
        EndBeginDrawUI(2);
        DrawDiamond(position+new Vector2(-0,0),new Vector2(120),black,border:5,filled:false,borderColor:Color.White);
        */

        //DrawDiamond(position+new Vector2(-5,0),28,pink,2);

        string text1 = "Exp:";
        string text2 = "100/500";

        DynamicSpriteFont font = FontManager.HarmonyOS_Sans_SC.Value;
        Vector2 textScale = new Vector2(0.5f);
        Vector2 text1Origin = font.MeasureString(text1) * 0.5f;
        Vector2 text2Origin = font.MeasureString(text2) * 0.5f;
        Vector2 text1Position = position + new Vector2(0, -10);
        Vector2 text2Position = position + new Vector2(0, 8);
        
        //spriteBatch.DrawString(font, text1, text1Position, Color.White, 0f, text1Origin, textScale, SpriteEffects.None, 0f);
        //spriteBatch.DrawString(font, text2, text2Position, Color.White, 0f, text2Origin, textScale, SpriteEffects.None, 0f);
        EndBeginDrawUI(2,0,ss:SamplerState.LinearClamp);

        //DrawDiamond(position+new Vector2(-40,-42)+new Vector2(-5,5),new Vector2(60),color:black, border:10,filled:true);
        //DrawDiamond(position+new Vector2(-52,23)+new Vector2(-15),new Vector2(45),color:black,border:10,filled:true);

        //DrawDiamond2(position,width:50,height:50,drawInWolrd:false);

        EndBeginDrawUI(1);

        Vector2 move = new Vector2(0,-7.2f);
        float rotation = 0;
        Vector2 scale = Vector2.One;
        
        //DrawInScreen(Icon_Angle, position+move,white,scale,rotation);
        rotation += MathHelper.Pi;
        move = move.RotatedBy(MathHelper.Pi);
        //DrawInScreen(Icon_Angle, position+move,white,scale,rotation);
        
        EndBeginDrawUI();


        base.Draw(gameTime, spriteBatch);

    }
    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);

    }
}
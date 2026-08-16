using System;
using System.Linq;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using KL.UI;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using Terraria.GameContent;
using 伊蕾娜.ElainaModSkills;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

[RegisterUI("Vanilla: Radial Hotbars", "Elaina: ElainaSkillPanel", int.MinValue)]
//[RegisterGlobalUI]
public class ElainaSkillPanel : SkillPanelUI
{
    public override bool IsInteractable => Main.LocalPlayer.itemAnimation <= 0;

    private Color ElainaPanelBorderColor => new Color(255,255,255, 255);
    private Color ElainaPanelBackgroundColor => new Color(150,142,255,255) * 0.2f;
    private Color ElainaSkillBackgroundColor => new Color(150,142,255,255) * 0.5f;
    private Color ElainaSkillSubUIBackgroundColor => new Color(80,66,110,255);
    private Color pink => new Color(203,142,177, 255);
    private Color black =new Color(69,66,75);

    public override float SkillSlotOutBorder => 1f;
    public override float SkillSlotBorder => 1;
    public override Color SkillSlotBorderColor => ElainaPanelBorderColor;
    public override Color SkillSlotBackgroundColor => ElainaSkillBackgroundColor;

    public override float SkillLevelHintBorder => 1;
    public override Color SkillLevelHintBorderColor => ElainaPanelBorderColor;
    public override Color SkillLevelHintBackgroundColor => ElainaSkillSubUIBackgroundColor;  

    public override float SkillAddButtonBorder => 2;
    public override Color SkillAddButtonBorderColor => ElainaPanelBorderColor;
    public override Color SkillAddButtonBackgroundColor => ElainaSkillSubUIBackgroundColor;

    public override float PreviewSkillBarBorder =>1;
    public override Color PreviewSkillBarBorderColor => ElainaPanelBorderColor;
    public override Color PreviewSkillBarBackgroundColor => ElainaPanelBackgroundColor;
    public override Vector4 PreviewSkillBarBorderRadius => new Vector4(8);

    public override List<Skill> GetActiveSkillList => ElainaSkillModPlayer.GetActiveSkill;
    
    protected override KLSkillModPlayer GetSkillPlayer()
    {
        return Main.LocalPlayer.GetModPlayer<ElainaSkillModPlayer>();
    }

    protected override SkillToolTip CreateSkillToolTip()
    {
        return new ElainaSkillToolTip();
    }

    static Texture2D background;
    
    protected override Dictionary<string, Skill> GetAllSkill()
    {
        //从字典中获取所有Mod为Elaina的技能
        //Skill.RegisterSkill.First().Value.Mod == 伊蕾娜.ElainaModInstance;
        
        return Skill.RegisterSkill.Where(x => ReferenceEquals(x.Value.Mod, 伊蕾娜.ElainaModInstance)).ToDictionary(x => x.Key, x => x.Value);
    }

    protected override void OnInitialize()
    {
        BorderColor = Color.White*0;
        BackgroundColor = Color.White*0;

        background ??= ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/ElainaSkillPanel_BackGround4", AssetRequestMode.ImmediateLoad).Value;
        base.OnInitialize();
        ElainaSkillModPlayer.ElainaSkillPanel = this;
        
        scrollView.BackgroundColor = ElainaPanelBackgroundColor;
        scrollView.Border = 1;
        scrollView.BorderColor = ElainaPanelBorderColor;
        Enabled = false;
    }
    

    protected override void Update(GameTime gameTime)
    {
        SkillToolTip.BackgroundColor = ElainaPanelBackgroundColor;
        SkillToolTip.Border = 1;
        SkillToolTip.BorderColor = ElainaPanelBorderColor;
        //SkillToolTip.setw

        
        //PrintText(666);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        background = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/ElainaSkillUI/ElainaSkillPanel_BackGround4", AssetRequestMode.ImmediateLoad).Value;

        Vector2 size = Bounds.Size;

        OverflowHidden = false;
        //Vector2 scale = ;
        var position = Bounds.Position+ new Vector2(size.X/2, size.Y/2);
        
        
        DrawRectangle(position, size, Color.White,texture:background,corner:20,border:2,borderColor:ElainaPanelBorderColor,rotation:0);
        //DrawInScreen(background, position,Color.White, Vector2.One/(background.Size()/size));
        
        base.Draw(gameTime, spriteBatch);
    }
}

public class ElainaSkillToolTip : SkillToolTip
{
    private static Texture2D lineTexture;
    private static Texture2D crossTexture;

    protected override ToggleButton CreateToggleButton()
    {
        return new ToggleButton
        {
            ToggleSize = new Vector2(26f, 16f),
            ThumbDiameter = 10f,
            ThumbInset = 3f,
            OffBackgroundColor = new Color(69, 66, 75),
            OnBackgroundColor = new Color(255, 142, 255),
            OffThumbColor = new Color(203, 142, 177),
            OnThumbColor = Color.White,
        };
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        lineTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLineSPA", AssetRequestMode.ImmediateLoad).Value;
        crossTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/Cross", AssetRequestMode.ImmediateLoad).Value;

        Vector2 center = Bounds.Position + new Vector2(135f, 35f);
        DrawInScreen(lineTexture, center + new Vector2(70f, 0f), scale: new Vector2(0.6f, 0.1f), color: new Color(255, 255, 255, 0));
        DrawInScreen(lineTexture, center - new Vector2(70f, 0f), scale: new Vector2(0.6f, 0.1f), color: new Color(255, 255, 255, 0));
        DrawInScreen(crossTexture, center, scale: new Vector2(0.04f, 0.02f), color: new Color(255, 255, 255, 0));

        base.Draw(gameTime, spriteBatch);
    }
}
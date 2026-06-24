using KL.DamageSystem;
using KL.Drawing;
using KL.Drawing.Snippets;
using KL.SkillSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills;

public abstract class ElainaSkill : ModSkill
{
    protected Vector2 WandCenter =>Main.LocalPlayer.MountedCenter+ new Vector2(1, 0).RotatedBy(Main.LocalPlayer.itemRotation)*45f;

    protected Vector2 WandDirection => new Vector2(1, 0).RotatedBy((Main.MouseWorld-Main.LocalPlayer.MountedCenter).ToRotation());
    //图标分为底层和光效层
    public virtual Asset<Texture2D> ExtraIcon
    {
        get
        {
            return _extraIcon ??= ModContent.Request<Texture2D>(SkillTexturePath+"_Mask", AssetRequestMode.ImmediateLoad);
        }
        set { _extraIcon = value; }
    }
    protected Asset<Texture2D> _extraIcon;

    /// <summary>
    /// 是否已经装备在技能栏中，可以用来判定是否显示cd。
    /// </summary>
    public bool SelectedInSkillBar = false;

    /// <summary>
    /// 按当前技能对应的曲线获取指定进度的技能伤害。
    /// </summary>
    protected int GetSkillDamage(float bossState)
    {
        return DpsHelper.GetSkillDamage(GetType().Name, bossState);
    }

    /// <summary>
    /// 按当前技能对应的曲线获取当前默认进度的技能伤害。
    /// </summary>
    protected int GetSkillDamage()
    {
        return GetSkillDamage(1f);
    }

    public override SkillUnlockCondition UnlockCondition => base.UnlockCondition;

    public override bool CanDragInSkillPanel()
    {
        if(IsPassiveSkill) return false;
        return base.CanDragInSkillPanel();
    }

    public override bool PreUpdateCD()
    {
        return base.PreUpdateCD();
    }

    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale,Color color, Effect effect = null)
    {
        //BasicStatus = Skill.SKillBasicStatus.Learned;
        /*if (ExtraIcon != null)
        {
            EndBeginDrawUI(1,1,shader: effect);
            DrawInScreen(ExtraIcon.Value,position ,color,scale);

        }*/
        EndBeginDrawUI(ss:SamplerState.LinearClamp);
        if (SelectedInSkillBar&&CurrentCD > 0&&ElainaSkillManager.ShowCD)
        {
            DynamicSpriteFont font = FontManager.LoliFont.Value;
            string text = $"{CurrentCD:F1}";
            Main.spriteBatch.DrawString(font, text, position, Color.White,0,
                font.MeasureString(text)*new Vector2(0.5f,0.35f),0.78f,SpriteEffects.None,0);
        }
        SelectedInSkillBar = false;
        base.PostDrawSkillIcon(position, scale,color, effect);
    }

    public override void OnUnlockSkill()
    {
        ElainaSkillModPlayer.SkillModPlayer.UnlockSkill(Skill);
        base.OnUnlockSkill();
    }

    public override bool TryGetToolTip(out string name, out string level, out string desc)
    {
        /*name = GetType().Name;
        level = $"Lv. {Level}";
        desc = "造成100" +ElementType.Fire.GetIcon(offsetY:2) + "火元素伤害";
        toolTipWidth = 200;*/
        return base.TryGetToolTip(out name, out level, out desc);
    }

    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale,Color color, Effect effect = null)
    {
        return base.PreDrawSkillIcon(position, scale,color, effect);
    }
    
    
}
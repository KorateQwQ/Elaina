using KL.SkillSystem;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader.IO;
using 伊蕾娜.Config;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;
using 伊蕾娜.ElainaModSkills.Skills.Fire;
using 伊蕾娜.ElainaModSkills.Skills.Lightning;
using 伊蕾娜.ElainaModSkills.Skills.MagicMissile;
using 伊蕾娜.ElainaModSkills.Skills.Water;
using 伊蕾娜.ElainaModSkills.Skills.Wind;
using 伊蕾娜.Projectiles.MagicMissile;
using System.Linq;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills;

public class ElainaSkillModPlayer : KLSkillModPlayer
{

    public static int CurrentSkillIndex = -1;
    
    public static ElainaSkillBar ElainaSkillBar;
    public static ElainaSkillPanel ElainaSkillPanel;
    public static ElainaSkillModPlayer SkillModPlayer => Main.LocalPlayer.GetModPlayer<ElainaSkillModPlayer>();
    
    public new static List<Skill> GetActiveSkill => SkillModPlayer.ActiveSkill;
    public new static Dictionary<string,Skill> GetUnlockedSkill => SkillModPlayer.UnlockedSkill;

    public override void Load()
    {
        On_Player.QuickGrapple += On_PlayerOnQuickGrapple;
        base.Load();
    }

    private void On_PlayerOnQuickGrapple(On_Player.orig_QuickGrapple orig, Player self)
    {
        List<string> skillButton = new List<string>(2);
        if(KeyBind.SwitchNextSkill.GetAssignedKeys().Count > 0) skillButton.Add(KeyBind.SwitchNextSkill.GetAssignedKeys()[0]);
        if(KeyBind.SwitchPreviousSkill.GetAssignedKeys().Count > 0) skillButton.Add(KeyBind.SwitchPreviousSkill.GetAssignedKeys()[0]);

        List<string> grappleButton = PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus["Grapple"];
        // 检查skillButton和grappleButton是否有重复的按键
        var duplicateKeys = skillButton.Intersect(grappleButton).ToList();
        if (duplicateKeys.Count > 0)
        {
            return;
        }
        orig(self);
    }

    public override void SaveData(TagCompound tag)
    {
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        base.LoadData(tag);
    }

    public override void ResetEffects()
    {
        base.ResetEffects();
    }

    public override void PostUpdate()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            HandleOpenCloseSkillPanel();
            HandleSwitchNextSkill();
            HandleSwitchPreviousSkill();
        }
        base.PostUpdate();
    }

    void HandleOpenCloseSkillPanel()
    {
        //技能面板开关控制
        if (KeyBind.OpenSkillPanel.JustPressed)
        {
            if (ElainaSkillPanel != null)
            {
                ElainaSkillPanel.Enabled =  !ElainaSkillPanel.Enabled;
                if (ElainaSkillBar != null)
                {
                    ElainaSkillBar.Enabled =  !ElainaSkillBar.Enabled;
                }
            }
        }
    }

    void HandleSwitchNextSkill()
    {
        if (KeyBind.SwitchNextSkill.JustPressed)
        {
            SwitchNextSkill();
        }
    }

    void HandleSwitchPreviousSkill()
    {
        if (KeyBind.SwitchPreviousSkill.JustPressed)
        {
            SwitchPreviousSkill();
        }
    }
    public ElainaSkill GetCurrentSkill()
    {
        return null;
    }

    public static void SwitchNextSkill()
    {
        int startIndex = CurrentSkillIndex;
        for (int i = 1; i < GetActiveSkill.Count; i++)
        {
            int index = startIndex - i;
            if (index < 0) index += GetActiveSkill.Count;
            if (GetActiveSkill[index] != null) CurrentSkillIndex = index;
        }

        
        PrintText($"SwitchNextSkill {CurrentSkillIndex}");
    }

    public static void SwitchPreviousSkill()
    {
        int startIndex = CurrentSkillIndex;
        for (int i = 1; i < GetActiveSkill.Count; i++)
        {
            int index = startIndex + i;
            if (index >= GetActiveSkill.Count) index -= GetActiveSkill.Count;
            if (GetActiveSkill[index] != null) CurrentSkillIndex = index;
        }
        PrintText($"SwitchPreviousSkill {CurrentSkillIndex}");
    }
    
    //技能信息更新时，如果当前技能index为空，试图重新选择第一个有效的技能。
    public override void OnSkillsUpdated()
    {
        ReSetCurrentSkillIndex();
        ElainaSkillBar.UpdateSkillBar();
        base.OnSkillsUpdated();
    }

    void ReSetCurrentSkillIndex()
    {
        if (CurrentSkillIndex < 0||CurrentSkillIndex>=GetActiveSkill.Count|| GetActiveSkill[CurrentSkillIndex]== null)
        {
            CurrentSkillIndex = -1;
            for (int i = 0; i < GetActiveSkill.Count; i++)
            {
                if (GetActiveSkill[i] != null)
                {
                    CurrentSkillIndex = i;
                    break;
                }
            }
        }
    }
    
    //增加技能栏等技能栏变动事件，kl还没写。
    protected override void OnSkillsUIUpdated()
    {
        base.OnSkillsUIUpdated();
    }

    public bool CanUseSkill()
    {
        if(CurrentSkillIndex<0||CurrentSkillIndex>=GetActiveSkill.Count|| GetActiveSkill[CurrentSkillIndex]== null)return false;
        return !GetActiveSkill[CurrentSkillIndex].InCD;
    }
    public override void UseSkill(int index=0, IEntitySource source = null)
    {
        if (index >= 0 && index < GetActiveSkill.Count)
        {
            GetActiveSkill[index].UseSkill(source);
        }
    }
    public void UseSkill(IEntitySource source = null)
    {
        UseSkill(CurrentSkillIndex, source);
    }

    public override void OnEnterWorld()
    {
        CurrentSkillIndex = -1;
        ReSetCurrentSkillIndex();
        base.OnEnterWorld();
    }

    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        //modifiers.DisableCrit();
        base.ModifyHitNPCWithProj(proj, target, ref modifiers);
    }

    public static int GetCurrentSkillType()
    {
        return ModContent.ProjectileType<FinalLightning>();
    }
}
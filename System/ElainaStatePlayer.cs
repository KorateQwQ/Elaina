using System;
using KL.Utils;
using KL.Utils.Net;
using Terraria;
using Terraria.ModLoader.IO;
using 伊蕾娜.System.Experience;

namespace 伊蕾娜.System;

public class ElainaStatePlayer : KLModPlayer
{
    //游戏进度
    public float Progress = 0;

    //当前角色等级。最大等级由 GetMaxLevel() 根据 boss 进度决定。
    public int CurrentLevel = 1;

    // 暂用固定升级经验，正式数值统一从 ExperienceRules 调整。
    public const int ExperiencePerLevel = ExperienceRules.ExperiencePerLevel;
    public int Experience;
    
    //已经获取的进度
    public List<float> ProgressSet = new ();

    public override void Load()
    {
        base.Load();
    }

    public override void SaveData(TagCompound tag)
    {
        tag["progress"] = Progress;
        tag["currentLevel"] = CurrentLevel;
        tag["experience"] = Experience;
        tag["progressSet"] = ProgressSet ?? [];
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        tag.TryGet("progress", out Progress);
        if (tag.ContainsKey("currentLevel")) tag.TryGet("currentLevel", out CurrentLevel);
        // 兼容旧存档：旧版 GetLevel() 就是最大等级，已有进度的存档按当时的满级状态迁移。
        else CurrentLevel = tag.ContainsKey("progress") ? GetMaxLevel() : 1;
        tag.TryGet("experience", out Experience);
        tag.TryGet("progressSet", out ProgressSet);
        NormalizeLevel();
        base.LoadData(tag);
    }
    
    //仅本地触发，所有参与boss击杀的伊蕾娜（的客户端）都会接收此事件。
    public void OnKillBoss(NPC boss)
    {
        if (Main.LocalPlayer.GetModPlayer<ElainaModplayer>().Elaina)
        {
            float bossProgress = KLGameStateManager.GetBossState(boss);
            if (bossProgress > Progress)
            {
                UpdateProgress(bossProgress);
            }

            ProgressSet??= [];
            if (!ProgressSet.Contains(bossProgress))
            {
                OnFirstKillBoss(boss);
                ProgressSet.Add(bossProgress);
            }
        
            // 纯净基石玩法尚未完成，暂不在击败 Boss 时掉落。
        }
    }

    void UpdateProgress(float bossProgress)
    {
        Progress = bossProgress;
        NormalizeLevel();
        ExperienceRules.Add(ref CurrentLevel, ref Experience, GetMaxLevel(), 0);
        //PrintText("Elaina: Progress Updated: " + bossProgress);
    }

    void OnFirstKillBoss(NPC boss)
    {
        //PrintText("Elaina: First Kill Boss : " + boss.FullName);
    }

    public override void FrameEffects()
    {
        //PrintText(Player.name+" "+GetLevel());
        //PrintText(Player.name+" "+KLDpsHelper.GetStateDps(Progress));

        base.FrameEffects();
    }

    //当前等级。
    public int GetLevel()
    {
        NormalizeLevel();
        return CurrentLevel;
    }

    //最大等级，来源于 KL 的 boss checklist state。
    public int GetMaxLevel()
    {
        return Math.Max(1, KLGameStateManager.GetLevelCap(Progress));
    }

    public int GetExperience() => Math.Clamp(Experience, 0, ExperiencePerLevel);

    public int GetExperienceToNextLevel() => ExperiencePerLevel;

    /// <summary>经验由角色拥有端保存；联机光球奖励只能通过服务器确认包进入这里。</summary>
    public int AddExperience(int amount)
    {
        if (Main.netMode == Terraria.ID.NetmodeID.Server || Player.whoAmI != Main.myPlayer || amount <= 0)
            return 0;
        return ExperienceRules.Add(ref CurrentLevel, ref Experience, GetMaxLevel(), amount);
    }

    private void NormalizeLevel()
    {
        CurrentLevel = Math.Clamp(CurrentLevel, 1, GetMaxLevel());
        Experience = Math.Clamp(Experience, 0, ExperiencePerLevel);
    }

#if DEBUG
    public void DebugResetProgress()
    {
        Progress = 0;
        ProgressSet = [];
        CurrentLevel = 1;
        Experience = 0;
    }

    public bool DebugLevelUp()
    {
        NormalizeLevel();
        if (CurrentLevel >= GetMaxLevel()) return false;
        CurrentLevel++;
        Experience = 0;
        return true;
    }

    public bool DebugMaxLevel()
    {
        NormalizeLevel();
        int cap = GetMaxLevel();
        if (CurrentLevel >= cap) return false;
        CurrentLevel = cap;
        Experience = 0;
        return true;
    }

    public bool DebugGetNextBoss(out string bossName, out float bossState)
    {
        return KLGameStateManager.TryGetNextBoss(Progress, out bossName, out bossState);
    }

    public string DebugGetNextBossTooltip()
    {
        if (!DebugGetNextBoss(out string bossName, out float bossState))
            return $"角色 Progress {Progress:0.##} · 当前 Boss 列表中没有更高进度的 Boss";

        return $"下一个 Boss：{bossName} · 角色 Progress {Progress:0.##} → {bossState:0.##}"
            + $"\n等级上限 Lv. {GetMaxLevel()} → Lv. {KLGameStateManager.GetLevelCap(bossState)}"
            + "\n仅推进当前角色，不改变世界 Boss 击杀状态";
    }

    public bool DebugAdvanceMaxLevel(out string bossName, out float bossState)
    {
        if (!DebugGetNextBoss(out bossName, out bossState)) return false;

        UpdateProgress(bossState);
        ProgressSet ??= [];
        if (!ProgressSet.Contains(bossState)) ProgressSet.Add(bossState);
        return true;
    }
#endif
}

using System;
using System.Linq;
using KL.Utils;
using KL.Utils.Net;
using Terraria;
using Terraria.ModLoader.IO;
using 伊蕾娜.System.EssenceSystemFolder;

namespace 伊蕾娜.System;

public class ElainaStatePlayer : KLModPlayer
{
    //游戏进度
    public float Progress = 0;

    //当前角色等级。最大等级由 GetMaxLevel() 根据 boss 进度决定。
    public int CurrentLevel = 1;

    //经验系统尚未接入，先保留一个稳定的展示区间供 UI 使用。
    public const int ExperiencePerLevel = 100;
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
        
            //是否应该只有伊蕾娜击败boss才会产生基质呢？还是说每个玩家本地生成一个不同步的基质？
            EssenceItemSystem.MakeNewEssences(boss);
        }
    }

    void UpdateProgress(float bossProgress)
    {
        Progress = bossProgress;
        NormalizeLevel();
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

    public bool DebugAdvanceMaxLevel(out string bossName, out float bossState)
    {
        bossName = null;
        bossState = Progress;

        var nextBoss = KLGameStateManager.GetBossInfos().Values
            .Where(info => info.isBoss && info.progression > Progress)
            .OrderBy(info => info.progression)
            .ThenBy(info => info.displayName?.Value)
            .FirstOrDefault();
        if (nextBoss == null) return false;

        Progress = nextBoss.progression;
        bossState = nextBoss.progression;
        bossName = nextBoss.displayName?.Value ?? "未知 Boss";
        ProgressSet ??= [];
        if (!ProgressSet.Contains(bossState)) ProgressSet.Add(bossState);
        NormalizeLevel();
        return true;
    }
#endif
}

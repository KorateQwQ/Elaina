using System;
using KL.Utils;
using KL.Utils.Net;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;
using 伊蕾娜.ElainaModSkills.Skills.DollControl;
using 伊蕾娜.System.Experience;

namespace 伊蕾娜.System;

public class ElainaStatePlayer : KLModPlayer
{
    //游戏进度
    public float Progress = 0;

    //当前角色等级。最大等级由 GetMaxLevel() 根据 boss 进度决定。
    public int CurrentLevel = 1;

    public int Experience;
    // 调试可提前领取到新上限；实际升级只结算超过此等级的部分。
    private int studyPointRewardLevel = 1;
    private const int CurrentStudyPointBalanceVersion = 2;
    private int studyPointBalanceVersion = CurrentStudyPointBalanceVersion;
    private HashSet<string> bossExperienceClaims = new(StringComparer.Ordinal);
    // 奖励金额在击杀前的上限上取快照；光球抵达的服务器确认才允许消费一次。
    private readonly Dictionary<string, int> pendingBossExperience = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> bossKillNumbers = new(StringComparer.Ordinal);
    // 旧存档只记录浮点进度，保守视为这些历史阶段已经结算，防止更新后重复领首杀。
    private List<float> legacyBossExperienceProgress = new();
    
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
        tag["studyPointRewardLevel"] = studyPointRewardLevel;
        tag["studyPointBalanceVersion"] = studyPointBalanceVersion;
        tag["progressSet"] = ProgressSet ?? [];
        tag["bossExperienceClaims"] = new List<string>(bossExperienceClaims);
        tag["legacyBossExperienceProgress"] = legacyBossExperienceProgress;
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        pendingBossExperience.Clear();
        bossKillNumbers.Clear();
        tag.TryGet("progress", out Progress);
        if (tag.ContainsKey("currentLevel")) tag.TryGet("currentLevel", out CurrentLevel);
        // 兼容旧存档：旧版 GetLevel() 就是最大等级，已有进度的存档按当时的满级状态迁移。
        else CurrentLevel = tag.ContainsKey("progress") ? GetMaxLevel() : 1;
        tag.TryGet("experience", out Experience);
        tag.TryGet("progressSet", out ProgressSet);
        bossExperienceClaims = tag.TryGet("bossExperienceClaims", out List<string> claims)
            ? new HashSet<string>(claims, StringComparer.Ordinal) : new(StringComparer.Ordinal);
        legacyBossExperienceProgress = tag.ContainsKey("bossExperienceClaims")
            ? tag.TryGet("legacyBossExperienceProgress", out List<float> legacy) ? legacy : []
            : new List<float>(ProgressSet ?? []);
        // 差额留到进入世界后结算，不依赖技能组件的加载顺序。
        ExperienceRules.Add(ref CurrentLevel, ref Experience, GetMaxLevel(), 0);
        studyPointRewardLevel = tag.TryGet("studyPointRewardLevel", out int rewardedLevel)
            ? Math.Max(CurrentLevel, Math.Max(1, rewardedLevel)) : CurrentLevel;
        studyPointBalanceVersion = tag.TryGet("studyPointBalanceVersion", out int balanceVersion) ? balanceVersion : 1;
        base.LoadData(tag);
    }
    
    //仅本地触发，所有参与boss击杀的伊蕾娜（的客户端）都会接收此事件。
    public void OnKillBoss(NPC boss, int realMaxHp = 0)
    {
        if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer
            && Player.GetModPlayer<ElainaModplayer>().Elaina && !boss.SpawnedFromStatue)
        {
            float bossProgress = KLGameStateManager.GetBossState(boss);
            string key = ExperienceNpcRules.BossKey(boss);
            int number = bossKillNumbers.GetValueOrDefault(key) + 1;
            bossKillNumbers[key] = number;
            bool firstReward = bossExperienceClaims.Add(key)
                && !legacyBossExperienceProgress.Contains(bossProgress);
            int reward = firstReward ? ExperienceRules.ForFirstBoss(GetMaxLevel())
                : ExperienceRules.ForRepeatBoss(realMaxHp, GetLevel());
            if (bossProgress > Progress)
            {
                UpdateProgress(bossProgress);
            }

            ProgressSet??= [];
            if (bossProgress >= 0 && !ProgressSet.Contains(bossProgress))
            {
                OnFirstKillBoss(boss);
                ProgressSet.Add(bossProgress);
            }
            // 此处只开上限并预留奖励；经验由服务器确认光球抵达后入账。
            if (reward > 0) pendingBossExperience[ExperienceNpcRules.BossRewardKey(key, number)] = reward;
        
            // 纯净基石玩法尚未完成，暂不在击败 Boss 时掉落。
        }
    }

    public override void OnEnterWorld()
    {
        LevelUpToastUI.Toasts.Clear();
        pendingBossExperience.Clear();
        bossKillNumbers.Clear();
        ApplyStudyPointBalanceUpdate();
    }

    private void ApplyStudyPointBalanceUpdate()
    {
        if (studyPointBalanceVersion >= CurrentStudyPointBalanceVersion || Player == null
            || Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer) return;
        var skills = Player.GetModPlayer<ElainaSkillModPlayer>();
        long refund = ExperienceRules.LegacyStudyPointIncrease(studyPointRewardLevel);
        if (skills.TryGetUnlockedSkill<DollControlSkill>(out var doll))
            refund += (DollControlProgression.ClampLevel(doll.Level) - 1L) * (15 - DollControlProgression.UpgradeCost);
        skills.SkillPoint = (int)Math.Min(int.MaxValue, (long)skills.SkillPoint + refund);
        studyPointBalanceVersion = CurrentStudyPointBalanceVersion;
    }

    internal bool HasPendingBossExperience(string key) => pendingBossExperience.ContainsKey(key);

    internal int TakePendingBossExperience(string key)
    {
        if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer) return 0;
        return pendingBossExperience.Remove(key, out int amount) ? amount : 0;
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

    public int GetExperience() => Math.Clamp(Experience, 0, GetExperienceToNextLevel());

    public int GetExperienceToNextLevel() => ExperienceRules.RequiredForLevel(CurrentLevel);

    /// <summary>经验由角色拥有端保存；联机光球奖励只能通过服务器确认包进入这里。</summary>
    public int AddExperience(int amount)
    {
        if (Main.netMode == Terraria.ID.NetmodeID.Server || Player.whoAmI != Main.myPlayer || amount <= 0)
            return 0;
        int previousLevel = CurrentLevel;
        int accepted = ExperienceRules.Add(ref CurrentLevel, ref Experience, GetMaxLevel(), amount);
        Celebrate(previousLevel, GrantLevelStudyPoints(previousLevel, CurrentLevel));
        return accepted;
    }

    private void NormalizeLevel()
    {
        int previousLevel = CurrentLevel;
        ExperienceRules.Add(ref CurrentLevel, ref Experience, GetMaxLevel(), 0);
        Celebrate(previousLevel, GrantLevelStudyPoints(previousLevel, CurrentLevel));
    }

    /// <summary>返回本次实际发放的研习点。</summary>
    private long GrantLevelStudyPoints(int previousLevel, int reachedLevel)
    {
        if (Player == null || Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer) return 0;
        long reward = ExperienceRules.StudyPointsForLevels(Math.Max(previousLevel, studyPointRewardLevel), reachedLevel);
        if (reward <= 0) return 0;
        var skills = Player.GetModPlayer<ElainaSkillModPlayer>();
        skills.SkillPoint = (int)Math.Min(int.MaxValue, (long)skills.SkillPoint + reward);
        studyPointRewardLevel = reachedLevel;
        return reward;
    }

    /// <summary>当前等级高于 previousLevel 时播放升级特效（随弹幕同步）并在头顶上方提示。仅本地玩家、游戏内。</summary>
    private void Celebrate(int previousLevel, long studyPoints)
    {
        if (CurrentLevel <= previousLevel || Player == null || Main.gameMenu
            || Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer) return;
        LevelUpEffect.Spawn(Player);
        LevelUpToastUI.Toasts.Show(previousLevel, CurrentLevel, studyPoints);
    }

#if DEBUG
    public void DebugResetProgress()
    {
        Progress = 0;
        ProgressSet = [];
        CurrentLevel = 1;
        Experience = 0;
        bossExperienceClaims.Clear();
        pendingBossExperience.Clear();
        legacyBossExperienceProgress.Clear();
        Player.GetModPlayer<ElainaSkillModPlayer>().SkillPoint = 0;
        studyPointRewardLevel = 1;
        studyPointBalanceVersion = CurrentStudyPointBalanceVersion;
        // 不重置本世界击杀序号，与服务器保持一致；重置后可以重新领首杀。
    }

    public bool DebugLevelUp()
    {
        NormalizeLevel();
        if (CurrentLevel >= GetMaxLevel()) return false;
        CurrentLevel++;
        Celebrate(CurrentLevel - 1, GrantLevelStudyPoints(CurrentLevel - 1, CurrentLevel));
        Experience = 0;
        return true;
    }

    public bool DebugMaxLevel()
    {
        NormalizeLevel();
        int cap = GetMaxLevel();
        if (CurrentLevel >= cap) return false;
        int previousLevel = CurrentLevel;
        CurrentLevel = cap;
        Celebrate(previousLevel, GrantLevelStudyPoints(previousLevel, CurrentLevel));
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

        int currentCap = GetMaxLevel(), nextCap = KLGameStateManager.GetLevelCap(bossState);
        long reward = nextCap > currentCap
            ? ExperienceRules.StudyPointsForLevels(Math.Max(CurrentLevel, studyPointRewardLevel), nextCap) : 0;
        return $"下一个 Boss：{bossName} · 角色 Progress {Progress:0.##} → {bossState:0.##}"
            + $"\n等级上限 Lv. {currentCap} → Lv. {nextCap}"
            + $"\n补发 {reward} 研习点（提前结算至新上限）"
            + "\n仅推进当前角色，不改变世界 Boss 击杀状态";
    }

    public bool DebugAdvanceMaxLevel(out string bossName, out float bossState)
    {
        if (!DebugGetNextBoss(out bossName, out bossState)) return false;

        int previousLevel = CurrentLevel;
        int previousCap = GetMaxLevel();
        UpdateProgress(bossState);
        if (GetMaxLevel() > previousCap) GrantLevelStudyPoints(previousLevel, GetMaxLevel());
        ProgressSet ??= [];
        if (!ProgressSet.Contains(bossState)) ProgressSet.Add(bossState);
        return true;
    }
#endif
}

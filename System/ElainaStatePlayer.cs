using KL.Utils;
using KL.Utils.Net;
using Terraria.ModLoader.IO;
using 伊蕾娜.System.EssenceSystemFolder;

namespace 伊蕾娜.System;

public class ElainaStatePlayer : KLModPlayer
{
    //游戏进度
    public float Progress = 0;
    
    //已经获取的进度
    public List<float> ProgressSet = new ();

    public override void Load()
    {
        base.Load();
    }

    public override void SaveData(TagCompound tag)
    {
        tag["progress"] = Progress;
        if(ProgressSet?.Count>0)tag["progressSet"]= ProgressSet;
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        tag.TryGet("progress", out Progress);
        tag.TryGet("progressSet", out ProgressSet);
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
        //PrintText("Elaina: Progress Updated: " + bossProgress);
    }

    void OnFirstKillBoss(NPC boss)
    {
        //PrintText("Elaina: First Kill Boss : " + boss.FullName);
    }

    public override void FrameEffects()
    {
        base.FrameEffects();
    }
}
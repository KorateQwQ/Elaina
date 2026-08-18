using System;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaAttribute;

public class ElainaAttributeModPlayer : ModPlayer
{
    public delegate void OnMagicPointChangedHandler(float oldMagicPoint, float magicPoint);

    public event OnMagicPointChangedHandler MagicPointChanged;
    
    //public OnMagicPointChangedHandler MagicPointChanged;
    
    private float magicPoint = 100;
    public float MagicPoint
    {
         get => magicPoint;
         set
         {
             float oldMagicPoint = magicPoint;
             magicPoint = Math.Clamp(value, 0f, MaxMagicPoint);
             if (Math.Abs(magicPoint - oldMagicPoint) > 0.001f)
             {
                 MagicPointChanged?.Invoke(oldMagicPoint, magicPoint);   
             }
         }
    }
    
    private float maxMagicPoint = 100;

    public float MaxMagicPoint
    {
        get => maxMagicPoint;
        set => maxMagicPoint = value;
    }

    /// <summary>
    /// 每秒回复的魔力点数。
    /// </summary>
    public float MagicPointRecovery { get; set; } = 1f;

    //战斗中
    public bool InBattle = false;

    private int inBattleCount = 0;

    public override void Load()
    {
        base.Load();
    }

    public override void OnEnterWorld()
    {
        base.OnEnterWorld();
    }

    public override void ResetEffects()
    {
        MagicPointRecovery = 5;
        maxMagicPoint = 0;
        base.ResetEffects();
    }

    public override void FrameEffects()
    {
        base.FrameEffects();
    }

    public override void SaveData(TagCompound tag)
    {
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        base.LoadData(tag);
    }

    /// <summary>
    /// 以固定数值回蓝
    /// </summary>
    /// <param name="recovery"></param>
    public void RegenMagicPoint(float recovery)
    {
        MagicPoint += recovery;
    }

    /// <summary>
    /// 检查并按需消耗魔力点。
    /// </summary>
    /// <param name="cost">消耗的魔力点。</param>
    /// <param name="consume">是否实际扣除魔力点。</param>
    /// <returns>魔力点足够时返回 true，否则返回 false。</returns>
    public bool ConsumeMagicPoint(float cost, bool consume = true)
    {
        if (MagicPoint < cost) return false;
        if (consume) MagicPoint -= cost;
        if (inBattleCount < 300) InBattleState(300);
        return true;
    }
    
    /// <summary>
    /// 按最大魔力或已损失魔力的百分比回蓝。
    /// </summary>
    /// <param name="recoveryPercent">恢复比例，例如 0.15f 表示 15%。</param>
    /// <param name="basedOnMissingMagic">是否按已损失魔力计算。</param>
    public void RegenPercentMagicPoint(float recoveryPercent, bool basedOnMissingMagic = false)
    {
        float recoveryBase = basedOnMissingMagic ? MaxMagicPoint - MagicPoint : MaxMagicPoint;
        MagicPoint += recoveryBase * recoveryPercent/100f;
        //PrintText(recoveryBase * recoveryPercent/100f);
    }

    public override void PostUpdateMiscEffects()
    {
        base.PostUpdateMiscEffects();
        //PrintText("原版最大魔力： "+Player.statManaMax2);
    }
    

    public float GetMagicPointRecovery()
    {
        if (Player.dead) return 0f;
        if(!InBattle)return MaxMagicPoint*0.5f;
        return MagicPointRecovery + Player.manaRegenBonus / 10f + MpManger.GetAdditionalMagicPointRecovery(Player);
    }
 
    public override void PostUpdate()
    {
        base.PostUpdate();
        
        //float RealMagicPointRecovery = GetMagicPointRecovery();

        /*if (Player.dead || RealMagicPointRecovery <= 0f)
            return;*/

        if (inBattleCount > 0) inBattleCount--;
        else InBattle = false;
        
        MagicPoint += GetMagicPointRecovery() / 60f;
        //PrintText(Player.manaRegenBonus + " " + Player.manaRegen+" "+GetMagicPointRecovery());

    }


    public override void OnHitAnything(float x, float y, Entity victim)
    {
        if (victim is NPC npc)
        {
            if(npc.immortal) InBattleState(180);
            else InBattleState();
        }
        else InBattleState();
        base.OnHitAnything(x, y, victim);
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        InBattleState();
        base.PostHurt(info);
    }

    /// <summary>
    /// 进入战斗后默认十秒才能恢复为脱战状态。
    /// </summary>
    /// <param name="BattleTime"></param>
    public void InBattleState(int BattleTime = 600)
    {
        InBattle = true;
        inBattleCount = BattleTime;
    }
}
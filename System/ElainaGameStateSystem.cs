using KL.Utils;
using Terraria;
using Terraria.ID;
using 伊蕾娜.System.Experience;

namespace 伊蕾娜.System;

public class ElainaGameStateSystem : KLGameStateManager
{
    public override void Load()
    {
        //测试伤害类型。
        //Main.LocalPlayer.GetDamage<MagicDamageClass>() += 0.1f;
        base.Load();
    }

    protected override void OnKillBoss(NPC self, List<int> killers, int realMaxHp)
    {
        // 战斗结束与参与名单直接采用 KL 的回调结果；拥有端先开上限，服务器再生成光球。
        if (!self.SpawnedFromStatue)
        {
            if (Main.netMode != NetmodeID.Server && killers.Contains(Main.myPlayer))
                Main.LocalPlayer.GetModPlayer<ElainaStatePlayer>().OnKillBoss(self, realMaxHp);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                BossExperienceDrop.Spawn(self, killers);
        }
        base.OnKillBoss(self, killers, realMaxHp);
    }
    
}

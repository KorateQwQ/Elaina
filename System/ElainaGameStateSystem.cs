using KL.Utils;
using KL.Utils.Net;
using NotImplementedException = System.NotImplementedException;

namespace 伊蕾娜.System;

public class ElainaGameStateSystem : KLGameStateManager
{
    public override void Load()
    {
        //测试伤害类型。
        //Main.LocalPlayer.GetDamage<MagicDamageClass>() += 0.1f;
        base.Load();
    }

    protected override void OnKillBoss(NPC self, List<int> killers, int damage)
    {
        //本地玩家接受boss击杀事件。
        if (killers.Contains(Main.myPlayer))
        {
            Main.LocalPlayer.GetModPlayer<ElainaStatePlayer>().OnKillBoss(self);
        }
        base.OnKillBoss(self, killers, damage);
    }
    
}
using KL.ActionsSystem;
using KL.Utils.Net;

namespace 伊蕾娜.ElainaActions;

public class ElainaActionsModPlayer : KLModPlayer
{
    public override void PostUpdate()
    {
        
        if (Main.mouseLeft)
        {
            //Player.SetCompositeArmFront(true,Player.CompositeArmStretchAmount.Full,Main.GameUpdateCount%1200/10f);//
            //Player.HandPosition += new Vector2(-10);

        }
        base.PostUpdate();
    }

    /// <summary>多重射击引导没有常驻弹幕承载朝向，由拥有者直接广播。</summary>
    internal void SyncMultiMissileAim(int actionToken, float rotation) =>
        RPC(nameof(ReceiveMultiMissileAim), [actionToken, rotation]);

    public void ReceiveMultiMissileAim(int actionToken, float rotation)
    {
        if (Player.GetModPlayer<ActionModPlayer>().CurrentAnimAction is Action_MultiMissile action)
            action.ApplySyncedAim(actionToken, rotation);
    }

    internal void SyncIceConeAim(int actionToken, float rotation) =>
        RPC(nameof(ReceiveIceConeAim), [actionToken, rotation]);

    public void ReceiveIceConeAim(int actionToken, float rotation)
    {
        if (Player.GetModPlayer<ActionModPlayer>().CurrentAnimAction is Action_IceCone action)
            action.ApplySyncedAim(actionToken, rotation);
    }
}

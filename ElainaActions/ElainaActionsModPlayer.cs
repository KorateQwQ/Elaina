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
}
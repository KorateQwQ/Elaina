using KL.ActionsSystem;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaActions;

public class Action_SimpleShoot : ElainaAction
{
    float rotation;
    public Action_SimpleShoot() : base(2)
    {
        
    }

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        base.OnStart(actionPlayer);
    }

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        float armRotation;

        if (owner.direction == 1)
        {
            armRotation = StartRotation - 1.8f;
        }
        else
        {
            armRotation = StartRotation + 1.8f - MathHelper.Pi;
        }

        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, MathHelper.WrapAngle(armRotation));

        base.Update(actionPlayer, actionFrame, actionProgress);
    }

    public override void Draw(ActionModPlayer actionPlayer, ref PlayerDrawSet drawInfo, int actionFrame, float actionProgress,
        ActionDrawLayerType drawLayerType)
    {

        
       //owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ((Main.MouseWorld-owner.MountedCenter).ToRotation()- 1.8f));

        base.Draw(actionPlayer, ref drawInfo, actionFrame, actionProgress, drawLayerType);
    }
}
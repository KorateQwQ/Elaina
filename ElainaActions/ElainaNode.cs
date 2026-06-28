using KL.ActionsSystem;
using KL.Extensions;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaActions;

public abstract class ElainaNode : ActionNode
{
    protected ElainaNode(int startFrame, int endFrame) : base(startFrame, endFrame)
    {
    }

    protected static Player GetOwner(ActionModPlayer actionPlayer)
    {
        return actionPlayer.Player;
    }

    protected static Vector2 GetLocalDrawOffset(Player owner, Vector2 offset)
    {
        offset.Y *= owner.gravDir;
        return offset;
    }

    protected static Vector2 GetMountedLocalDrawPosition(Player owner, Vector2 offset)
    {
        return owner.MountedCenter + GetLocalDrawOffset(owner, offset).RotatedBy(owner.fullRotation);
    }

    protected static void RestoreActionDrawBatch()
    {
        EndBeginDraw(0, 1);
    }

    protected static bool IsTargetLayer(ActionDrawLayerType currentLayer, ActionDrawLayerType targetLayer)
    {
        return currentLayer == targetLayer;
    }

    protected static Vector2 GetArmCenter(Player owner)
    {
        Vector2 rotationCenter = owner.position.Floor();
        Vector2 wandStartCenter = owner.MountedCenter.Floor()
            + new Vector2(-3 * owner.direction, -4 * owner.gravDir + owner.gfxOffY)
            + ArmDrawingFixer.GetFrontArmTotalOffset(owner) * 0.5f;

        wandStartCenter = rotationCenter + (wandStartCenter - rotationCenter).RotatedBy(owner.fullRotation);
        return wandStartCenter.Floor();
    }

    protected static Vector2 GetWandCenter(Player owner)
    {
        return GetArmCenter(owner).Floor() + new Vector2(1, 0).RotatedBy(owner.itemRotation + owner.fullRotation) * 45f;
    }
}

using KL.ActionsSystem;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaActions;

public class Action_Cast : ElainaAction
{
    protected override bool DrawWandTrail => false;

    protected override bool DrawActionStar => false;

    public Action_Cast() : base(90)
    {
        AddNode(new SlashTrailActionNode(0, 45, new SlashTrailActionNode.SlashTrailSegment
                {
                    StartProgress = 0f,
                    EndProgress = 0.65f,
                    StartPosition = new Vector2(-14f, 35f),
                    EndPosition = new Vector2(44f, -12f),
                    ControlPointA = new Vector2(-8f, 48f),
                    ControlPointB = new Vector2(34f, 32f),
                    TrailLength = 0.82f,
                    EndRecoverDuration =0.2f,
                    ProgressMap =
                    {
                        new SlashTrailActionNode.ProgressKey(0f, 0f),
                        new SlashTrailActionNode.ProgressKey(0.5f, 0.95f),
                        new SlashTrailActionNode.ProgressKey(1f,1.3f)
                    }
                },
                new SlashTrailActionNode.SlashTrailSegment
                {
                    StartProgress = 0.65f,
                    EndProgress = 1f,
                    StartPosition = new Vector2(44f, -12f),
                    EndPosition = new Vector2(-42f, 18f),
                    ControlPointA = new Vector2(28f, 32f),
                    ControlPointB = new Vector2(-34f, 52f),
                    TrailLength = 0.6f,
                    EndRecoverDuration = 0.2f,
                    ProgressMap =
                    {
                        new SlashTrailActionNode.ProgressKey(0f, 0f),
                        new SlashTrailActionNode.ProgressKey(0.8f, 0.95f),
                        new SlashTrailActionNode.ProgressKey(1f,1.3f)
                    }
                }
            ))
            .AddNode(new ArmActionNode(
                0,
                15,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Full,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                0f,
                -1.8f))
            .AddNode(new ArmActionNode(
                15,
                25,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                Player.CompositeArmStretchAmount.Quarter,
                -1.8f,
                -2.0f))
            .AddNode(new ArmActionNode(
                25,
                35,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Quarter,
                Player.CompositeArmStretchAmount.Full,
                -2.0f,
                1.0f))
            .AddNode(new ArmActionNode(
                35,
                45,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Full,
                Player.CompositeArmStretchAmount.Full,
                1.0f,
                0.0f));
    }

    public override void Draw(ActionModPlayer actionPlayer, ref PlayerDrawSet drawInfo, int actionFrame,
        float actionProgress,
        ActionDrawLayerType drawLayerType)
    {
        base.Draw(actionPlayer, ref drawInfo, actionFrame, actionProgress, drawLayerType);
    }
}
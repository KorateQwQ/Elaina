using System;
using KL.Drawing.ThreeD;
using KL.Utils;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public partial class IceCone3DUnit
{
    internal static IceCone3DUnit CreatePrepared(Vector3 position, Vector3 target, Vector3 velocity,
        int count, int index, int timeOrigin = 0, Vector3? camera = null)
    {
        if (count != 15) throw new InvalidOperationException("齐射排程需要同步当前冰锥数量。");
        int launch = (timeOrigin + ShardbindSequence.LaunchTick) + ShardbindSequence.ShotOffset(index);
        Vector3 offset = ShardbindDepthLayout.AdjustOffset(position - target, index);
        position = target + offset;
        velocity = -Vector3.Normalize(offset) * velocity.Length();
        Vector3 rest = IceCone3DUnit.MeasureOriginalRest(position, target, velocity);
        var distances = ShardbindMotion.Measure();
        // 先从原布局求终点与后撤速度，再拉远起点，不能用新起点重算穿入深度。
        float speed = Vector3.Distance(position, target + rest) / (distances.Flight - distances.Retreat);
        velocity = -Vector3.Normalize(offset) * speed;
        Vector3 finalVelocity = velocity;
        offset = ShardbindDepthLayout.ExpandSpawnOffset(offset);
        position = target + offset;
        Vector3 originalPosition = position;
        Vector3 projectionCamera = camera ?? (Main.dedServ
            ? new Vector3(target.X, target.Y, -2687.077f) : GraphicsUtils.CameraPos(MathF.PI / 6f));
        position = ShardbindDepthLayout.PullTowardCamera(position, index, projectionCamera);
        offset = position - target;
        Vector3 impact = rest - finalVelocity * distances.LastFlight;
        velocity = Vector3.Normalize(target + impact - position) * speed;
        Vector3 ready = offset - velocity * distances.Retreat;
        // 前四步加速覆盖新增距离，最后一步保持原速度，命中帧和最终停留位置均不变。
        Vector3 flightVelocity = (impact - ready) / (distances.Flight - distances.LastFlight);
        // 先显现待命；每枚在自己的发射点前连续后撤25帧，紧接冲刺。
        int appear = timeOrigin
            + (int)MathF.Round(index * (ShardbindSequence.LockTicks - 25f) / (count - 1));
        return new IceCone3DUnit(position, target, velocity, appear, timeOrigin + IceCycleBalance.LockBurstFrame + 20)
        { LaunchTick = launch, ReadyOffset = ready, RestOffset = rest, preparedOffset = offset,
            FlightVelocity = flightVelocity, FinalVelocity = finalVelocity,
            NearCamera = position != originalPosition, preparedCameraOffset = projectionCamera - target };
    }

    internal int LaunchTick = -1;
    private bool Prepared => LaunchTick >= 0;
    internal Vector3 ReadyOffset, RestOffset;
    private Vector3 preparedOffset;
    private Vector3 FlightVelocity;
    private Vector3 FinalVelocity;
    private bool NearCamera;
    private int MotionTime => !Prepared ? Timer - Delay
        : Math.Max(0, BackwardFlyTime + Timer - LaunchTick);
    private bool WindDue => !Prepared ? Timer - Delay == BackwardFlyTime - 1
        : Timer == LaunchTick;
    private bool Waiting => Prepared && Timer < LaunchTick - BackwardFlyTime;
    readonly ShardbindMotion preparedMotion = new();
    readonly ShardbindTrail preparedTrail = new();



    private void UpdatePreparedMovement(int time)
    {
        if (Waiting) { currentVelocity3D = -forwardVelocity3D; return; }
        if (time >= BackwardFlyTime + BurstFlyTime) return;
        Vector3 velocity = time < BackwardFlyTime ? forwardVelocity3D
            : time < BackwardFlyTime + BurstFlyTime - 1 ? FlightVelocity : FinalVelocity;
        currentVelocity3D = preparedMotion.Evaluate(velocity, time);
        preparedOffset += currentVelocity3D;
        if (!HasArrived && time == BackwardFlyTime + BurstFlyTime - 2)
        {
            HasArrived = ArrivedThisUpdate = true;
            arrivalTime = time;
        }
        if (time == BackwardFlyTime + BurstFlyTime - 1)
        {
            if (Vector3.Distance(preparedOffset, RestOffset) > .05f)
                throw new InvalidOperationException("拉远起点后的速度积分改变了原穿刺终点。");
            preparedOffset = RestOffset;
        }
        Position3D = targetPosition3D + preparedOffset;
    }

    private static Vector3 MeasureOriginalRest(Vector3 position, Vector3 target, Vector3 velocity)
    {
        // 直接运行当前正式冰凌运动的抽取方法，避免硬编码旧版穿入距离。
        var reference = new IceCone3DUnit(position, target, velocity, 0, 1000);
        for (int t = 0; t < BackwardFlyTime + BurstFlyTime; t++) reference.UpdateLegacyMovement(t);
        return reference.Position3D - target;
    }

    private Vector3 GetStableModelForward(bool isBackwardFlying)
    {
        if (!Prepared || !NearCamera || MotionTime >= BackwardFlyTime + BurstFlyTime - 1)
            return GetLegacyModelForward(isBackwardFlying);
        float t = MathHelper.Clamp((MotionTime - BackwardFlyTime) / (float)(BurstFlyTime - 1), 0f, 1f);
        t = t * t * (3f - 2f * t);
        // 近景保留斜向长面，避免正对尾端只看到一块截面；飞行中转回原穿刺朝向。
        Vector3 direction = Vector3.Normalize(Vector3.Lerp(Vector3.Normalize(FlightVelocity),
            Vector3.Normalize(FinalVelocity), MathHelper.Lerp(.65f, 1f, t)));
        return isBackwardFlying ? -direction : direction;
    }

    // IceCone2.obj 局部 +Y 端的尾尖顶点，随模型使用同一姿态与透视。
    private static Vector3 TrailSocket => new(-.154737f, 8.168592f, 0f);

    private void UpdatePreparedTrail(bool sampleOnly = false)
    {
        if (!Prepared || !Started || Main.dedServ) return;
        if (MotionTime < BackwardFlyTime - 1 || HasArrived && !ArrivedThisUpdate) return;
        Vector3 tail = Vector3.Transform(TrailSocket, GetModelMatrix(MotionTime));
        float cameraZ = GetProjectionCamera().Z;
        float perspective = MathF.Abs(cameraZ / (cameraZ - tail.Z));
        preparedTrail.Advance(ProjectToWorldPlane(tail), tail.Z < 0f, perspective, MotionTime, ArrivedThisUpdate, sampleOnly);
    }
}

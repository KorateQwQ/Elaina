using System;
using KL.Drawing.ThreeD;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public partial class IceCone3DUnit
{
    // 保留施法时相对于目标的视点；镜头移动只改变整组效果在屏幕上的位置。
    private Vector3 preparedCameraOffset;

    private Vector3 GetProjectionCamera() => Prepared
        ? targetPosition3D + preparedCameraOffset : GraphicsUtils.CameraPos(Fov);

    private Matrix GetProjectionTransform(Vector3 currentCamera)
    {
        if (!Prepared) return Matrix.Identity;
        Vector3 camera = GetProjectionCamera();
        Matrix transform = Matrix.Identity;
        // 将固定视点的透视映射到当前游戏镜头。Z=0 的 NPC 平面完全不变，
        // 模型世界坐标也不变，确保深度分层、运动和命中仍使用原始几何。
        transform.M31 = (currentCamera.X - camera.X) / camera.Z;
        transform.M32 = (currentCamera.Y - camera.Y) / camera.Z;
        transform.M33 = currentCamera.Z / camera.Z;
        return transform;
    }

    private Matrix GetViewProjectionMatrix() =>
        GetProjectionTransform(GraphicsUtils.CameraPos(Fov)) * GraphicsUtils.GetVPMatrix(ProjectionMode, Fov);

    internal Vector2 GetImpactDirection()
    {
        // 取抵达前后沿飞行轨迹的两个投影点，保留冰锥从景深方向飞入时的屏幕分量。
        Vector2 impact = ProjectToWorldPlane(Position3D);
        Vector2 approach = ProjectToWorldPlane(Position3D - currentVelocity3D);
        Vector2 direction = impact - approach;
        return direction.LengthSquared() > .0001f ? Vector2.Normalize(direction) : Vector2.Zero;
    }

    // 返回投影后的二维世界坐标，由普通世界绘制统一处理镜头平移、缩放与重力。
    private Vector2 ProjectToWorldPlane(Vector3 position)
    {
        if (!Prepared) return GraphicsUtils.ProjectWorldToScreen(position, ProjectionMode, Fov);
        Vector3 camera = GetProjectionCamera();
        float distance = camera.Z - position.Z;
        if (MathF.Abs(distance) <= .0001f)
            throw new InvalidOperationException("冰凌锁投影点位于相机平面。");
        float perspective = camera.Z / distance;
        return new Vector2(camera.X + (position.X - camera.X) * perspective,
            camera.Y + (position.Y - camera.Y) * perspective);
    }
}

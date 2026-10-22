using KL.Drawing;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>
/// 把回城法阵提交到 KL 的 Projectiles.After bloom 层：法阵与同层其他 bloom 请求共用一次后处理，
/// 过曝（感知亮度大于 1）的线条部分产生光晕。离线预览编译本文件并回放真实 bloom 管线。
/// </summary>
internal static class RecallPortalVisual
{
    const string EffectPath = "KL/Effects/Content/VertexDraw";

    public static void Request(Vector2 screenCenter, float radius, float age, int delay, int lifetime, bool destination, bool hovered, float burstStart)
    {
        LayerDrawRequestSystem.RequestBloom("Elaina:RecallPortal", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, () =>
            {
                // 先结束 bloom 捕获时的 SpriteBatch，直接绘制顶点后再按 KL 惯例重新开始。
                EndBeginDraw();
                GraphicsDevice device = Main.instance.GraphicsDevice;
                Matrix transform = Main.GameViewMatrix.TransformationMatrix *
                    Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
                Effect effect = ModContent.Request<Effect>(EffectPath, AssetRequestMode.ImmediateLoad).Value;
                RecallPortalDrawing.Draw(device, effect, transform, screenCenter, radius, age, delay, lifetime, destination, hovered, burstStart);
                EndBeginDraw();
            });
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>可替换的占位特效。只绘制，不负责传送、配对或计时；预览直接链接本文件。</summary>
internal static class RecallPortalDrawing
{
    public static bool Contains(Vector2 offset, float radius) =>
        offset.X * offset.X / (radius * radius) + offset.Y * offset.Y / (radius * radius * .09f) <= 1;

    /// <summary>
    /// 绘制回城魔法阵；本方法只负责外观，不负责传送、配对或推进计时。
    /// </summary>
    /// <param name="batch">当前绘制批次，游戏中为 Main.spriteBatch；直接用它绘制贴图。</param>
    /// <param name="pixel">
    /// 画线、色块用的白色贴图。游戏传入的原版 MagicPixel 实际为 1×1000，
    /// 按单像素缩放画线时必须使用源矩形 new Rectangle(0, 0, 1, 1)，不能缩放整张纹理。
    /// </param>
    /// <param name="center">
    /// 法阵的屏幕绘制中心，已由 Projectile.Center - Main.screenPosition 得到，
    /// 不要再次减去摄像机位置；游戏缩放由当前 SpriteBatch 的变换矩阵处理。
    /// </param>
    /// <param name="radius">
    /// 绘制半径，当前传入 160 像素。占位椭圆横向半径为 radius，纵向半径为 radius * 0.3f。
    /// 仅改变绘制大小不会改变传送判定；更改外形时应同步调整 Contains 的鼠标命中区域。
    /// </param>
    /// <param name="age">
    /// 总计时进度，单位为游戏更新帧（每秒 60 帧），不是实际绘制次数。
    /// 等待服务器确认时为 -1，此阶段应保持不可见；确认成功后开始从 0 蓄力。
    /// 起点从 0 开始；出生地出口生成时从约 delay 开始，因此出口不再经历一次蓄力。
    /// </param>
    /// <param name="delay">首次群体传送的蓄力时间，当前为 90 帧（1.5 秒）；age 小于 delay 时正在蓄力。</param>
    /// <param name="lifetime">
    /// 激活后的残留时间，当前为 1800 帧（30 秒），不包含蓄力；age 达到 delay + lifetime 时到期。
    /// </param>
    /// <param name="destination">是否为出生地出口：false 为施法地点，true 为出生地，可用于区分两端特效。</param>
    /// <param name="hovered">鼠标是否处于法阵的椭圆区域，可用于高亮；不代表距离、队伍等交互条件已满足。</param>
    /// <remarks>
    /// 常用动画量：秒数 = age / 60f；蓄力进度 = Math.Clamp(age / delay, 0f, 1f)；
    /// 激活后秒数 = Math.Max(0f, age - delay) / 60f；
    /// 剩余秒数 = Math.Max(0f, delay + lifetime - age) / 60f；
    /// 最后半秒淡出系数 = Math.Clamp((delay + lifetime - age) / 30f, 0f, 1f)。
    /// 每一端的出现动画可用 Math.Max(0f, destination ? age - delay : age) 作为计时帧数，
    /// 让出生地出口也能从零播放展开动画。
    /// </remarks>
    public static void Draw(SpriteBatch batch, Texture2D pixel, Vector2 center, float radius,
        float age, int delay, int lifetime, bool destination, bool hovered)
    {
        float charge = Math.Clamp(age / delay, 0, 1);
        float opacity = Math.Min(Math.Clamp(age / 15, 0, 1), Math.Clamp((delay + lifetime - age) / 30, 0, 1));
        Color color = (destination ? new Color(123, 215, 240) : new Color(205, 160, 246)) * opacity;
        float rotation = age * .012f;
        Vector2 Point(float angle, float r) => center + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r * .3f);
        void Line(Vector2 a, Vector2 b, Color tint, float width)
        {
            Vector2 delta = b - a;
            // 原版 MagicPixel 是 1×1000；只取一个像素，避免整张纹理把线宽放大千倍。
            batch.Draw(pixel, a, new Rectangle(0, 0, 1, 1), tint, MathF.Atan2(delta.Y, delta.X), new Vector2(0, .5f),
                new Vector2(delta.Length(), width), SpriteEffects.None, 0);
        }
        for (int i = 0; i < 96; i++)
        {
            float a = i * MathHelper.TwoPi / 96, b = (i + 1) * MathHelper.TwoPi / 96;
            Line(Point(a, radius), Point(b, radius), color * (hovered ? 1 : .7f), hovered ? 3 : 2);
            Line(Point(a, radius * .82f), Point(b, radius * .82f), color * .5f, 1);
            if (i / 96f < charge) Line(Point(a, radius * .92f), Point(b, radius * .92f), color, 2);
        }
        for (int i = 0; i < 6; i++)
        {
            float angle = rotation + i * MathHelper.TwoPi / 6;
            Line(Point(angle, radius * .66f), Point(angle + MathHelper.TwoPi / 3, radius * .66f), color * .65f, 1.5f);
            Line(Point(angle, radius * .84f), Point(angle, radius * .98f), color, 2);
        }
        float pulse = 1 + .15f * MathF.Sin(age * .07f);
        Line(center - new Vector2(8 * pulse, 0), center + new Vector2(8 * pulse, 0), color, 2);
        Line(center - new Vector2(0, 5 * pulse), center + new Vector2(0, 5 * pulse), color, 2);
    }
}

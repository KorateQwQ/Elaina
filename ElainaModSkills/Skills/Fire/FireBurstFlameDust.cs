using System;
using KL.Drawing;
using KL.Dusts;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>
/// 炎爆喷发的序列帧火焰。贴图为 KL Dusts/Fire/Eff_Fire_10 的预乘版（Tools/FireBurstPreview/premultiply_textures.py 生成），
/// 在原版尘埃批次（AlphaBlend）中绘制，淡入淡出同时作用于亮度与覆盖度。
/// </summary>
public class FireBurstFlameDust : KLBasicDust
{
    public override bool PreDraw(Dust dust)
    {
        // 可调参数（热重载修改方法体即可生效）
        int frames = 16;
        float fadeIn = .1f, fadeOut = .3f;
        float grow = .35f;
        // 后半段冷却成橙红：只压低绿、蓝通道，避免黄色降低覆盖度后与背景混成橄榄色
        Vector3 cooled = new(1f, .45f, .25f);
        float coolStart = .4f;

        float life = dust.LifeProgress();
        int frame = Math.Min(frames - 1, (int)(life * frames));
        float fade = MathHelper.Clamp(life / fadeIn, 0f, 1f) * MathHelper.Clamp((1f - life) / fadeOut, 0f, 1f);
        Vector3 tint = Vector3.Lerp(Vector3.One, cooled, MathHelper.Clamp((life - coolStart) / (1f - coolStart), 0f, 1f));
        Color color = new Color(dust.color.ToVector4() * new Vector4(tint, 1f)) * fade;
        DrawHelper.DrawInWorld(new TextureInfo(MainTexture, frame, 4, 4), dust.position, color,
            dust.Size() * (1f + grow * life), dust.rotation + MathHelper.PiOver2);
        return false;
    }

    public override bool Update(Dust dust)
    {
        dust.velocity *= .9f;
        Lighting.AddLight(dust.position, new Vector3(1f, .55f, .2f) * (1f - dust.LifeProgress()) * .6f);
        return base.Update(dust);
    }
}

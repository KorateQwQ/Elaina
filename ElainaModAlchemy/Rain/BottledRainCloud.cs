using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

/// <summary>同一弹幕从瓶子转为雨云。ai[0] 阶段，ai[1] 年龄，ai[2] 飞行帧数/展开高度。</summary>
public sealed class BottledRainCloud : ModProjectile
{
    private float[] rainLengths;
    private bool releaseSoundPlayed;
    public override string Texture => "伊蕾娜/ElainaModAlchemy/item/ExampleAssets/BottledRain_Pixel";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 480;

    public override void SetDefaults()
    {
        Projectile.width = RainThrowTrajectory.BottleWidth;
        Projectile.height = RainThrowTrajectory.BottleHeight;
        Projectile.aiStyle = -1;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 180;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.tileCollide = false;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (Projectile.ai[0] == 0)
        {
            Projectile.rotation += Projectile.velocity.X * .035f;
            int flight = Projectile.ai[2] == 0 ? 45 : (int)Projectile.ai[2];
            Vector2 center = Projectile.Center;
            RainThrowTrajectory.Step(ref center, ref Projectile.velocity, flight);
            Projectile.Center = center;
            if (++Projectile.ai[1] >= Math.Abs(flight)) Release();
            return;
        }

        Projectile.tileCollide = false;
        Projectile.velocity = Vector2.Zero;
        float age = ++Projectile.ai[1];
        if (age >= RainCloudDrawing.Duration) { Projectile.Kill(); return; }
        // 年龄随 ai 同步；中途加入也使用剩余时长，不重新获得完整一分钟。
        Projectile.timeLeft = Math.Max(2, RainCloudDrawing.Duration - (int)age);
        if (!Main.dedServ)
        {
            if (!releaseSoundPlayed)
            {
                releaseSoundPlayed = true;
                if (age < 10) SoundEngine.PlaySound(SoundID.Shatter with { Volume = .55f, Pitch = .15f }, Projectile.Center);
            }
            rainLengths ??= new float[RainCloudDrawing.Columns];
            if (age <= 1 || (int)age % 15 == 0 || rainLengths[0] == 0)
                RainPlantGrowth.MeasureRain(Projectile.Center, rainLengths);
            Lighting.AddLight(Projectile.Center, .13f, .20f, .25f);
        }
        if (Main.netMode != NetmodeID.MultiplayerClient && age >= 30 && (int)age % 10 == 0)
            RainPlantGrowth.Water(Projectile.Center);
    }

    public override bool OnTileCollide(Vector2 oldVelocity) => false;

    private void Release()
    {
        float originY = Projectile.Center.Y;
        // 雨云向上展开，低天花板会限制高度，不穿过屋顶去浇灌另一层。
        Vector2 center = RainThrowTrajectory.CloudCenter(Projectile.Center);
        Projectile.Center = center;
        Projectile.velocity = Vector2.Zero;
        Projectile.tileCollide = false;
        Projectile.ai[0] = 1;
        Projectile.ai[1] = 0;
        Projectile.ai[2] = originY - center.Y;
        Projectile.timeLeft = RainCloudDrawing.Duration;
        Projectile.netUpdate = true;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Projectile.ai[0] == 0)
        {
            Texture2D bottle = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(bottle, Projectile.Center - Main.screenPosition, null, Color.White,
                Projectile.rotation, bottle.Size() * .5f, .8f, SpriteEffects.None);
        }
        else
        {
            rainLengths ??= new float[RainCloudDrawing.Columns];
            if (rainLengths[0] == 0) RainPlantGrowth.MeasureRain(Projectile.Center, rainLengths);
            RainCloudShaderRenderer shader = RainCloudVisualSystem.GetRenderer();
            Texture2D spriteCloud = null;
            if (shader == null)
            {
                Main.instance.LoadProjectile(ProjectileID.RainCloudRaining);
                spriteCloud = TextureAssets.Projectile[ProjectileID.RainCloudRaining].Value;
            }
            RainCloudDrawing.Draw(Main.spriteBatch, TextureAssets.MagicPixel.Value,
                spriteCloud,
                Projectile.Center - Main.screenPosition, Projectile.ai[1], rainLengths, Projectile.identity, Projectile.ai[2]);
            if (shader != null)
            {
                shader.Draw(Main.spriteBatch, Projectile.Center - Main.screenPosition, Projectile.ai[1], Main.GameViewMatrix.TransformationMatrix);
                KL.Drawing.DrawHelper.EndBeginDraw();
            }
        }
        return false;
    }
}

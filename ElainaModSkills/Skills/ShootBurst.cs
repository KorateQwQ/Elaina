using KL.Drawing;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaModSkills.Skills;

public class ShootBurst : KLProjectile
{
    public override void SetDefaults()
    {
        Projectile.friendly = false;
        Projectile.timeLeft = 90;
        base.SetDefaults();
    }

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Asset<Texture2D> MainTex = TextureAssets.Projectile[Projectile.type];
        Texture2D waterNoi = Mod.Assets.Request<Texture2D>("ReProjs/Water/waterNoi", AssetRequestMode.ImmediateLoad).Value;

        //DrawInWorld(MainTex.Value,Projectile.Center);
        float Radius = 500;
        float time = 0.25f;
        RenderHelper.SaveScreenTarget();
        RenderHelper.SwitchRender(RenderHelper.Render);

        DrawManager.绘制球体(MainTex.Value,Projectile.Center, Radius*0.95f, 伊蕾娜.skillColor[伊蕾娜.SkillType.Water], alpha: 1f, 绘制次数: 4, blendState: 1,
            uTimeX: time, uTimeY: 0.5f,
            disStrength: 0f,
            扰动偏移: new Vector2(0, 0),
            0, 0,waterNoi,
            消融偏移: new Vector2(0));
        
        RenderHelper.ReDrawScreenTarget();
        
        Projectile.endBegin(1,1);
        Main.spriteBatch.Draw(RenderHelper.Render, Vector2.Zero, Color.White);//

        EndBeginDraw();
        
        return base.PreDraw(ref lightColor);
    }
    

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }
}
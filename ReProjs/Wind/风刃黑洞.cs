using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Items;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Buffs;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ReProjs.Wind
{
    internal class 风刃黑洞 : ElainaProj
    {
        Asset<Texture2D> 光圈;
        float size;
        float size2;
        bool end = false;
        Color color;
        bool transForm = false;
        public override void SetDefaults()
        {
            if (光圈 == null)
            {
                光圈 = Mod.Assets.Request<Texture2D>("ReProjs/Wind/光圈", AssetRequestMode.ImmediateLoad);
            }
            size = 0.5f;
            Projectile.tileCollide = false;
            Projectile.width = Projectile.height = 150;
            Projectile.friendly = true;
            Projectile.damage = 100;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ownerHitCheck = false;
        }

        public override bool? Colliding(Rectangle myRect, Rectangle targetRect)
        {
            if (end) return false;
            var player = Main.player[Projectile.owner];
            if (targetRect.Intersects(new Rectangle((int)Projectile.Center.X- Projectile.width * 2, (int)Projectile.Center.Y- Projectile.height * 2, Projectile.width * 4, Projectile.height * 4)))
                return true;
            return false;
        }
        #region 元素转换相关
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.knockBack);
            base.SendExtraAI(writer);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.knockBack = reader.ReadSingle();
            base.ReceiveExtraAI(reader);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.wet || target.HasBuff(BuffID.Wet))
            {
                TryTransform(0);
            }
            else if (伊蕾娜.NpcIsOnFire(target))
            {
                TryTransform(1);
            }
            else
            {
                if (target.realLife >= 0 || target.GetGlobalNPC<Count>().parentid >= 0)
                {
                    target = Main.npc[target.realLife >= 0 ? target.realLife : target.GetGlobalNPC<Count>().parentid];
                }
                if (target.GetGlobalNPC<Count>().冻结槽 > 0 || target.HasBuff<冻结>())
                {
                    TryTransform(2);
                }
            }
            if (hit.Knockback != Projectile.knockBack)
            {
                Projectile.netUpdate = true;
            }
            base.OnHitNPC(target, hit, damageDone);
        }

        void TryTransform(int type = -1)//风魔法可以被环境转化（水，岩浆，雨，雪），也可以命中转化（潮湿，燃烧，冻结或有冻结值）
        {
            if (transForm) return;
            //水属性相关
            if (type == -1)//环境转化
            {
                if (!Projectile.lavaWet && Projectile.wet)
                {
                    if (!Collision.LavaCollision(Projectile.Center, Projectile.width, Projectile.height))
                    {
                        transForm = true;
                        Projectile.knockBack = (float)伊蕾娜.HitType.HitByWater;
                        return;
                    }

                }
                if (Projectile.lavaWet)
                {
                    transForm = true;
                    Projectile.knockBack = (float)伊蕾娜.HitType.HitByFlame;
                    return;
                }
                if (InIceEnvironment())
                {
                    transForm = true;
                    Projectile.knockBack = (float)伊蕾娜.HitType.HitByIce;
                    return;
                }
            }
            else
            {
                switch (type)
                {
                    case 0:
                        transForm = true;
                        Projectile.knockBack = (float)伊蕾娜.HitType.HitByWater;
                        return;
                    case 1:
                        Projectile.knockBack = (float)伊蕾娜.HitType.HitByFlame;
                        return;
                    case 2:
                        transForm = true;
                        Projectile.knockBack = (float)伊蕾娜.HitType.HitByIce;
                        return;
                }
            }
        }
        bool InIceEnvironment()
        {
            if (Projectile.owner == Main.myPlayer)
            {
                if (Main.SceneMetrics.SnowTileCount > 3000 || Main.snowDust > 30)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }
        public override void AI()
        {
            FrameCounter(15);
            if(!transForm) TryTransform();

            if (Main.mouseRight)
            {
                if (Projectile.frameCounter%5==0&& !Owner.CheckMana(Owner.HeldItem, EXP.GetMana(1f), true))
                {
                    end = true;
                }
                Owner.manaRegenDelay = 60;
                float 渐进距离 = 150;//小于这个值时，弹幕开始刹车
                float 弹幕最大速度 = 5;
                Vector2 v = Main.MouseWorld - Projectile.Center;
                if (v.Length() > 渐进距离)
                {
                    Projectile.velocity = v.SafeNormalize(v) * 弹幕最大速度;
                }
                else Projectile.velocity = Vector2.Lerp( Vector2.Zero, v.SafeNormalize(v) * 弹幕最大速度, v.Length() / 渐进距离);

                if (Projectile.frameCounter%5==0)
                {

                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<环形风刃>(),
                    0, Main.rand.Next(0, 2), Owner.whoAmI, Main.rand.NextFloat(100, 300), Main.rand.NextFloat(50, 70),Projectile.knockBack);

                }
                if (Main.GameUpdateCount % 3 == 0)size = Main.rand.NextFloat(0.6f, 0.8f);
                Projectile.timeLeft = 120;
                size2 = 1.1f;
            }
            else
            {
                end = true;
                Projectile.Kill();
            }
            float effect = 1;
            if (end)
            {
                effect = MathHelper.Lerp(0, 1, Projectile.timeLeft / 120f);
                size = size * effect;
                size2 = size2*effect;
                Projectile.velocity = Vector2.Zero;
                if (effect < 0.1f) Projectile.Kill();
                effect = MathHelper.Lerp(0, 1, (Projectile.timeLeft-90) / 30f);
                color *= MathHelper.Lerp(0, 1, Projectile.timeLeft / 120f);
                //Projectile.width = (int)(Projectile.width*effect);
                //Projectile.height =(int)(Projectile.width * effect);
            }
            else  吸附();
            Lighting.AddLight(Projectile.Center, color.ToVector3()*2 * effect);
            Projectile.rotation += Main.rand.NextFloat(0.1f, 0.2f);

            base.AI();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            //Projectile.knockBack = (float)伊蕾娜.HitType.HitByFlame;
            if (Projectile.knockBack != (float)伊蕾娜.HitType.HitByWind)
            {
                color = 伊蕾娜.SkillDamageColor((伊蕾娜.HitType)Projectile.knockBack);
                if (!transForm) transForm = true;
            }
            else color = new(255, 160, 239);
            color = new(255, 160, 239);

            Asset<Texture2D> noise = Mod.Assets.Request<Texture2D>("Projectiles/Perlin");
            Asset<Texture2D> wind = Mod.Assets.Request<Texture2D>("ReProjs/Wind/wind");

            Effect 虚化shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/两端虚化", AssetRequestMode.ImmediateLoad).Value;
            float 渐变 = 60 - (float)Main.GameUpdateCount % 120;
            float uTime = (Main.GameUpdateCount % 60) / 60f;
            //size += 0.005f * (Projectile.frame % 2 == 0 ? 1 : -1);
            Color c = color;



            DrawHelper.SaveScreen();
            Projectile.endBegin(1, 1);
            for (int i = 0; i < 2; i++)
            {
                Projectile.DrawSelf(size2, c * 0.75f);
            }

            //DrawHelper.SwitchRender(伊蕾娜.render);
            Projectile.endBegin(2, 1);
            DrawManager.上色shader(c, c * 0.1f, false);
            Projectile.DrawSelf(1 * size, new Color(255, 255, 255, 255));

            Projectile.endBegin(1, 1);
            DrawManager.上色shader(c * 0f,c, false);
            //Projectile.Draw(wind.Value, 0.96f * size);
            Projectile.Draw(光圈.Value, 0.3846f * size);
            Projectile.Draw(wind.Value, 0.846f * size);

            DrawHelper.SwitchRender(伊蕾娜.render);
            Projectile.endBegin(0, 1, true,SamplerState.LinearWrap);
            虚化shader.Parameters["uTime"].SetValue(uTime);
            虚化shader.CurrentTechnique.Passes["move2"].Apply();
            Projectile.Draw(noise.Value, 2);

            DrawHelper.SwitchRender(Main.screenTarget);
            DrawManager.扰动shader(0.01f* size, 0,伊蕾娜.render);
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);


            Projectile.endBegin();

            return false;
        }
        void 吸附()
        {
            foreach (Item item in Main.item)
            {
                bool result = (Projectile.Center-item.Center).Length()<750&& (Projectile.Center - item.Center).Length()>50;
                if (result)
                {
                    Vector2 v = (Projectile.Center - item.Center).SafeNormalize(Vector2.Zero);
                    item.velocity = (v * 105f + item.velocity) / 15f;
                }
            }
            foreach (NPC target in Main.npc)
            {
                if (!(target.CountsAsACritter && Main.player[Projectile.owner].dontHurtCritters) && !target.dontTakeDamage && target.active && !target.immortal && !target.friendly && !target.HasBuff<冻结>() && !target.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
                {
                    bool result = (Projectile.Center - target.Center).Length() < 750 && (Projectile.Center - target.Center).Length() > 50;

                    if (result)
                    {
                        Vector2 v = (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero);
                        target.velocity = (v * 105f + target.velocity) / 15f;
                        target.position.X += v.X;
                        Vector2 center = target.position;
                        //if (!target.noTileCollide)
                        {
                            if (Collision.SolidCollision(target.position, target.width, target.height))
                            {
                                target.position.X = center.X;
                            }
                        }
                        //target.position.Y += v.Y;
                        if (!target.noTileCollide)
                        {
                            if (Collision.SolidCollision(target.position, target.width, target.height))
                            {
                                target.position.Y = center.Y;
                            }
                        }

                    }
                }
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }
    }
}

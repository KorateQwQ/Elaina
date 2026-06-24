using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using System;
using 伊蕾娜.Buffs;
using Terraria.Audio;
using ReLogic.Utilities;

namespace 伊蕾娜.Projectiles.MagicWind
{
    public class AirSlash : ModProjectile
    {
        static Asset<Texture2D> texture;
        static Effect 风刃shader;
        static Effect 扰动shader;
        static Asset<Texture2D> voronoi;
        static Asset<Texture2D> noise;
        float width = 350;
        float height = 350;
        float resetHit = 0;
        float extraHit = 0;
        bool ifmodifyHeight = false;
        bool transForm = false;
        ActiveSound result;
        SlotId s;
        SoundStyle Windsound = (new SoundStyle($"伊蕾娜/Projectiles/MagicWind/风刃", 1, SoundType.Sound)) with
        {
            Volume = 1f,
            MaxInstances = 15,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
            PitchVariance = 0.5f,
        };
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("风刃");
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            //writer.WriteVector2(Projectile.Center);
            //writer.WriteVector2(towards);
            writer.Write(Projectile.knockBack);
            base.SendExtraAI(writer);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {   
            //Projectile.Center = reader.ReadVector2();
            //towards = reader.ReadVector2();
            Projectile.knockBack = reader.ReadSingle();
            base.ReceiveExtraAI(reader);
        }
        public override void SetDefaults()
        {
            if(风刃shader==null) 风刃shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/风刃").Value;
            if (texture == null) texture = Mod.Assets.Request<Texture2D>("Projectiles/MagicWind/AirSlash");
            if (voronoi == null) voronoi = Mod.Assets.Request<Texture2D>("Projectiles/voronoi");
            if (noise == null) noise = Mod.Assets.Request<Texture2D>("Projectiles/Perlin");
            if (扰动shader == null) 扰动shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/扰动").Value;

            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.friendly = true;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 350;
            Projectile.height = 350;
            Projectile.damage = 20;
            Projectile.timeLeft = 300;
            Projectile.knockBack = 2;
            Projectile.extraUpdates = 3;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            //Projectile.CritChance = (int)Main.player[Projectile.owner].GetTotalCritChance(DamageClass.Magic);
            //Projectile.extraUpdates = ;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!ifmodifyHeight)
            {
                if (Main.myPlayer != Projectile.owner)
                {
                    Projectile.height = (int)(Projectile.ai[0] * Projectile.height);
                    if (Projectile.ai[0] == 1.2) Projectile.width = (int)(1.5 * Projectile.width);

                }
                else Main.player[Projectile.owner].itemTime = 15;
                ifmodifyHeight = true;
                //Projectile.Center = Projectile.position;
                s = SoundEngine.PlaySound(Windsound, Projectile.position);//62
            }
            int index = (int)(Main.GameUpdateCount % 60);
            float time = Utils.GetLerpValue(0, 1, index / 60f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            //Projectile.velocity.Normalize();
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null);
            gd.SetRenderTarget(Main.screenTargetSwap);//在这个上面绘制一遍原图，相当于“保存”
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            gd.SetRenderTarget(伊蕾娜.render2);//在伊蕾娜.render2上面绘制基本风刃
            gd.Clear(Color.Transparent);//用透明清除
            //sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            风刃shader.Parameters["uTime"].SetValue(time);//在render2上画出正常版本的风刃，之后再用render（一张噪声图）同时扭曲屏幕和风刃
            风刃shader.Parameters["tex0"].SetValue(voronoi.Value);
            Color color = new(255, 160, 239);
            //Projectile.knockBack = (float)伊蕾娜.HitType.HitByFlame;
            if (Projectile.knockBack != (float)伊蕾娜.HitType.HitByWind)
            {
                color = 伊蕾娜.SkillDamageColor((伊蕾娜.HitType)Projectile.knockBack);
                if (!transForm) transForm = true;
            }
            风刃shader.Parameters["color"].SetValue(new Vector4(color.R, color.G, color.B, color.A) / 255f* Projectile.alpha / 255f);
            风刃shader.CurrentTechnique.Passes["move"].Apply();//开启shader
            float extraRotation = 0;
            Vector2 extraPosition = Vector2.Zero; 
            Vector2 v = Vector2.Normalize(Projectile.velocity);
            Vector2 scale = new(Projectile.width / width, Projectile.height / height * 0.7f);
            Vector2 extraScale = Vector2.One;
            float extraPositionScale = 1;
            if (Projectile.ai[0] == 1.5f)
            {
                extraRotation = 0.3f;
                extraPosition= Projectile.velocity.RotatedBy(Math.PI / 2f);
                extraScale = new Vector2(1.3f, 1.3f);
                extraPositionScale = 7;
                sb.Draw(texture.Value, Projectile.Center - extraPosition * extraPositionScale - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), Color.White, Projectile.rotation + extraRotation, texture.Size() * 0.5f, scale* extraScale, SpriteEffects.None, 0f);//* new Vector2(1.5f, 0.5f)
                sb.Draw(texture.Value, Projectile.Center - extraPosition * extraPositionScale - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), Color.White, Projectile.rotation + extraRotation, texture.Size() * 0.5f, scale* extraScale, SpriteEffects.None, 0f);//* new Vector2(1.5f, 0.5f)
                //sb.Draw(texture.Value, Projectile.Center - Projectile.velocity * 2 - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), Color.White, Projectile.rotation, texture.Size() * 0.5f, scale * new Vector2(1, 1.5f), SpriteEffects.None, 0f);

            }

            //sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            sb.Draw(texture.Value, Projectile.Center + extraPosition * extraPositionScale - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), Color.White, Projectile.rotation-extraRotation, texture.Size() * 0.5f, scale * extraScale, SpriteEffects.None, 0f);
            sb.Draw(texture.Value, Projectile.Center + extraPosition * extraPositionScale - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), Color.White, Projectile.rotation- extraRotation, texture.Size() * 0.5f, scale * extraScale, SpriteEffects.None, 0f);



            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            
            gd.SetRenderTarget(伊蕾娜.render);//设置成自己的RenderTarget进行绘制
            gd.Clear(Color.Transparent);//用透明清除
            风刃shader.Parameters["tex1"].SetValue(noise.Value);
            风刃shader.CurrentTechnique.Passes["noise"].Apply();

            Vector2 noisescale = new Vector2(Projectile.width / width * 1.2f, Projectile.height / height )*1.5f;
            
            sb.Draw(texture.Value, Projectile.Center  + Vector2.Normalize(Projectile.velocity)*70 - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), Color.White, Projectile.rotation, texture.Size() * 0.5f, noisescale, SpriteEffects.None, 0f);
            //Main.spriteBatch.Draw(texture.Value, Projectile.Center+ v*2 - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), Color.White, Projectile.rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            gd.SetRenderTarget(Main.screenTarget);//换回屏幕renderTarget
            gd.Clear(Color.Transparent);//用透明清除

            扰动shader.Parameters["tex0"].SetValue(伊蕾娜.render);
            扰动shader.Parameters["uTime"].SetValue(0);
            扰动shader.Parameters["strength"].SetValue(0.03f* Projectile.alpha / 255f);
            扰动shader.CurrentTechnique.Passes["move"].Apply();//开启shader
            sb.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);//绘制自己的rendertarget
            sb.Draw(伊蕾娜.render2, Vector2.Zero, Color.White);
            //sb.Draw(伊蕾娜.render, Vector2.Zero, Color.White);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            Vector2 length = Vector2.Normalize(Projectile.velocity)*270* Projectile.width / this.width;
            float width = 350*Projectile.height / this.height;
            if (Projectile.ai[0] == 1.5f)
            {
                width += 200f;
            }
                bool result = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + length, width, ref point);
            return result;

        }
        public override void AI()//ai0储存大小倍率，ai1储存形态，默认0
        {
            if (result == null)
            {
                SoundEngine.TryGetActiveSound(s, out result);
            }
            if(result != null) result.Volume *= 0.98f;

            if (extraHit > 0) extraHit--;
            Projectile.alpha = (int)(255 * Utils.GetLerpValue(0,1f, Projectile.velocity.Length() / 7.5f, true));
            if(resetHit>0)resetHit--;//最小重置受击时间
            //Main.NewText(Projectile.alpha+"  "+ Projectile.velocity.Length());
            float light = Projectile.height / height * 5* Projectile.alpha / 255f;
            Lighting.AddLight(Projectile.Center, light, light, light);
            foreach(Item item in Main.item)
            {
                float point = 0f;
                Vector2 length = Vector2.Normalize(Projectile.velocity) * 270 * Projectile.width / this.width;
                float width = 350 * Projectile.height / this.height;
                bool result = Collision.CheckAABBvLineCollision(item.getRect().TopLeft(), item.getRect().Size(), Projectile.Center, Projectile.Center + length, width, ref point);
                if (result)
                {
                    item.velocity = Projectile.velocity;
                }
            }
            foreach(NPC target in Main.npc)
            {
                if (!(target.CountsAsACritter && Main.player[Projectile.owner].dontHurtCritters)&& !target.dontTakeDamage&& target.active&&!target.immortal && !target.friendly && (!target.boss || Projectile.ai[0]>1)&&!target.HasBuff<冻结>() && !target.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
                {
                    float point = 0f;
                    Vector2 length = Vector2.Normalize(Projectile.velocity) * 270 * Projectile.width / this.width;
                    float width = 350 * Projectile.height / this.height;
                    bool result = Collision.CheckAABBvLineCollision(target.getRect().TopLeft(), target.getRect().Size(), Projectile.Center, Projectile.Center + length, width, ref point);
                    if (result)
                    {
                        Vector2 v = Projectile.velocity;
                        //v = Vector2.Normalize(v) * 15;
                        Vector2 center = target.position;
                        target.position.X += v.X;
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
                        if (center != target.position )
                        {
                            if (extraHit <= 0)
                            {
                                NPC.HitInfo hitInfo = new();
                                hitInfo.Knockback = 0;
                                hitInfo.Damage = Projectile.damage * 2;
                                hitInfo.Crit = false;
                                hitInfo.HideCombatText = true;
                                target.StrikeNPC(hitInfo, false, false);

                                //target.GetGlobalNPC<Count>().OnHitByProjectile(target, Projectile, hitInfo, 0);

                                //Projectile.Damage();
                                //Main.player[Projectile.owner].ApplyDamageToNPC(target, Projectile.damage * 2, Projectile.knockBack, Main.player[Projectile.owner].direction, false); ;
                                //target.StrikeNPC(Projectile.damage * 2, Projectile.knockBack, 0);
                                extraHit = 30;
                            }
                            if (Main.netMode != 1)
                            NetMessage.SendData(23, -1, -1, null, target.whoAmI);
                        }

                        /*Vector2 v = Projectile.Center - target.Center;
                        v = Vector2.Normalize(v) * 15;
                        Vector2 center = target.Center;
                        target.Center += v;
                        if (!target.noTileCollide)
                        {
                            if (Collision.SolidCollision(target.position, target.width, target.height))
                            {
                                target.Center = center;
                            }
                        }
                        if (center != target.Center && Main.netMode != 1)
                        {
                            NetMessage.SendData(23, -1, -1, null, target.whoAmI);
                        }*/

                    }
                }
            }
            if (Projectile.alpha < 50) Projectile.Kill();
            TryTransform();
            base.AI();
        }
        public override bool? CanCutTiles()
        {   
            return true;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] > 1) modifiers.FinalDamage = (modifiers.FinalDamage * Projectile.ai[0]);
            if (Projectile.knockBack != (float)伊蕾娜.HitType.HitByWind) modifiers.FinalDamage = (modifiers.FinalDamage * 1.5f);
            base.ModifyHitNPC(target, ref modifiers);
        }


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (resetHit <= 0 && Projectile.ai[0] <= 1)
            {
                Projectile.velocity *= 0.8f;
                //Projectile.alpha = (int)(Projectile.alpha*0.8f);
                resetHit = 15;
                Projectile.netUpdate = true;
            }
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

        void TryTransform(int type =-1)//风魔法可以被环境转化（水，岩浆，雨，雪），也可以命中转化（潮湿，燃烧，冻结或有冻结值）
        {
            if (transForm) return;
            //水属性相关
            if(type == -1)//环境转化
            {
                if (!Projectile.lavaWet&&Projectile.wet )
                {
                    if(!Collision.LavaCollision(Projectile.Center, Projectile.width, Projectile.height))
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
            if(Projectile.owner == Main.myPlayer)
            {
                if (Main.SceneMetrics.SnowTileCount > 3000 || Main.snowDust > 30)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

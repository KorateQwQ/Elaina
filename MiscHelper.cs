global using Microsoft.Xna.Framework;
global using Terraria;
global using Terraria.Localization;
global using 伊蕾娜.Projectiles;
global using 伊蕾娜.System;
global using static 伊蕾娜.MiscHelper;
using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using 伊蕾娜.Items;

namespace 伊蕾娜
{
    public static class MiscHelper
    {
        private const string Local = "Mods.伊蕾娜.";
        public static ElainaModplayer EMP(this Player player) => player.GetModPlayer<ElainaModplayer>();
        public static EXPmodplayer EXP(this Player player) => player.GetModPlayer<EXPmodplayer>();
        public static string GTV(string key, params string[] args) => Language.GetTextValue(Local + key, args);
        public static bool IfTarget(NPC target, Player player)
        {
            if (!(target.CountsAsACritter && player.dontHurtCritters) && !target.dontTakeDamage && target.active && !target.immortal && !target.friendly && !target.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
            {
                return true;
            }
            return false;
        }
        public static int 尝试锁定鼠标位置敌人()
        {
            int target = -1;
            float distance = 0;
            foreach (NPC npc in Main.npc)
            {
                if (IfTarget(npc, Main.player[Main.myPlayer]))
                {
                    if ((npc.position - Main.MouseWorld).Length() < 200 && (distance == 0 || ((npc.position - Main.MouseWorld).Length() < distance)))
                    {
                        distance = (npc.position - Main.MouseWorld).Length();
                        {
                            target = npc.whoAmI;
                        }
                    }
                }
            }
            return target;
        }
        public static Vector2 WandCenter(this Player player) =>player.MountedCenter+ new Vector2(1*player.direction, 0).RotatedBy(player.itemRotation)*35f;
        public static Vector2[] SmoothStep(Vector2[] vecs, int extraLength)//平滑处理，增加标记的坐标点
        {
            int l = vecs.Length;
            extraLength += l;

            Vector2[] scVecs = new Vector2[extraLength];
            for (int n = 0; n < extraLength; n++)
            {
                float t = n / (float)extraLength;
                float k = (l - 1) * t;
                int i = (int)k;
                float vk = k % 1;
                if (i == 0)
                {
                    scVecs[n] = Vector2.CatmullRom(2 * vecs[0] - vecs[1], vecs[0], vecs[1], vecs[2], vk);
                }
                else if (i == l - 2)
                {
                    scVecs[n] = Vector2.CatmullRom(vecs[l - 3], vecs[l - 2], vecs[l - 1], 2 * vecs[l - 1] - vecs[l - 2], vk);
                }
                else
                {
                    scVecs[n] = Vector2.CatmullRom(vecs[i - 1], vecs[i], vecs[i + 1], vecs[i + 2], vk);
                }
            }
            return scVecs;
        }
        public static int MagicDamage(this Player player, float dmg) => (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(dmg);

        public static bool Channel(this Player player) => player.HeldItem.type == ModContent.ItemType<ElainaWand>() && player.itemAnimation > 0;

        /// <summary>
        /// 自动调整魔杖位置至鼠标位置,如果传入位置则使用传入的位置（联机用，或者需要根据弹幕位置时，如延迟激光）。返回值为魔杖位置到玩家位置的向量,用于弹幕调整自身位置
        /// </summary>
        /// <param name="player"></param>
        public static Vector2 ControlWand(this Projectile projectile,Vector2? mousePosition = null,bool towardToMouse = true)
        {
            mousePosition ??= Main.MouseWorld;
            Player player = Main.player[projectile.owner];

            Vector2 toward = (mousePosition - player.MountedCenter).Value;

            toward.SafeNormalize(Vector2.One);
            

            player.itemRotation = (float)Math.Atan2(toward.ToRotation().ToRotationVector2().Y * player.direction, 
                toward.ToRotation().ToRotationVector2().X * player.direction);;//武器朝向
            //player.itemLocation = toward * 5;
            
            if(towardToMouse) player.direction = toward.X < 0 ? -1 : 1;//玩家朝向根据鼠标

            return toward;
        }
        
        public static Vector2 ControlWandByHeldProj(this Projectile projectile, bool towardToMouse = true)
        {
            Player player = Main.player[projectile.owner];

            Vector2 toward = new Vector2(1, 0).RotatedBy(projectile.rotation);
            
            if(towardToMouse) player.direction = toward.X < 0 ? -1 : 1;//玩家朝向根据鼠标

            toward.SafeNormalize(Vector2.One);
            

            player.itemRotation = (float)Math.Atan2(toward.ToRotation().ToRotationVector2().Y * player.direction, 
                toward.ToRotation().ToRotationVector2().X * player.direction);;//武器朝向
            //player.itemLocation = toward * 5;
            
            return toward;
        }
    }
}

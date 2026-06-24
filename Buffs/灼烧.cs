using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Projectiles.Flame;

namespace 伊蕾娜.Buffs
{

    public class 灼烧 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("灼烧");
            // Description.SetDefault("tmd燃起来了");
            Main.buffNoSave[Type] = false;
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.pvpBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (Collision.WetCollision(npc.Center, 10, 10) || npc.wet || npc.HasBuff(BuffID.Wet))
            {
                if (npc.HasBuff(BuffID.Wet)) npc.buffTime[npc.FindBuffIndex(BuffID.Wet)] -= 300;
                npc.buffTime[buffIndex] -= 600;
            }
            //
            if (Main.raining)
            {
                foreach (var rain in Main.rain)
                {
                    if (rain.active)
                    {
                        if ((rain.position - npc.position).Length() < 25)
                        {
                            npc.buffTime[buffIndex] -= 600;
                        }
                    }
                }
            }
            if (Main.GameUpdateCount % 10 == 0)
            {
                if (!npc.immortal)
                {
                    if (npc.realLife >= 0)
                    {
                        Main.npc[npc.realLife].life -= 5;
                        Main.npc[npc.realLife].checkDead();
                        Main.npc[npc.realLife].CheckActive();

                    }
                    else if (npc.life > 0)
                    {
                        npc.life -= 5;
                        npc.checkDead();
                        npc.CheckActive();
                        //Main.NewText(damage + " " + 斩杀伤害);

                    }
                }
                CombatText.NewText(npc.getRect(), CombatText.DamagedHostile, 5, false, true);
                Vector2 随机矩形 = new(Main.rand.NextFloat(-18, 18), Main.rand.NextFloat(-18, 18));
                //ModContent.GetInstance<drawFlameModplayer>().flameui.创造火焰(随机矩形 + npc.Center);
                Projectile.NewProjectile(null, 随机矩形 + npc.Center, Vector2.Zero, ModContent.ProjectileType<火焰燃烧>(), 0, (float)伊蕾娜.HitType.HitByFlame, Main.myPlayer, Main.rand.Next(2, 8));

            }
        }
    }
}

using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.System
{
    public class EXPmodplayer : ModPlayer
    {
        int basicLV = 0;
        int extraLV = 0;
        int LV = 0;
        public static float DamageScale = 1;
        public override void ResetEffects()
        {

            extraLV = 0;
            //Player.GetCritChance(DamageClass.Magic) += 100;
            base.ResetEffects();
        }

        public override void OnEnterWorld()
        {
            base.OnEnterWorld();
        }
        public void Reset()
        {
            basicLV = 0;
            extraLV = 0;
            LV = 0;
        }
        public override void FrameEffects()
        {
            //Main.NewText(Main.SceneMetrics.SnowTileCount + " " + SceneMetrics.SnowTileMax+" "+Main.snowDust);
            //Main.NewText(Main.bottomWorld / 16+" "+Player.position.Y/16);

            if (NPC.downedBoss1 && basicLV < 1)
                basicLV = 1;
            if (NPC.downedBoss2 && basicLV < 2)
                basicLV = 2;
            if (NPC.downedBoss3 && basicLV < 3)
                basicLV = 3;
            if (Main.hardMode && basicLV < 4)
                basicLV = 4;
            if (NPC.downedMechBossAny && basicLV < 5)
                basicLV = 5;
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3 && basicLV < 6)
                basicLV = 6;
            if (NPC.downedPlantBoss && basicLV < 7)
                basicLV = 7;
            if (NPC.downedGolemBoss && basicLV < 8)
                basicLV = 8;
            if (NPC.downedTowers && basicLV < 9)
                basicLV = 9;
            if (NPC.downedMoonlord && basicLV < 10)
                basicLV = 10;
            
            LV = basicLV + extraLV;
            //LV = basicLV = 10;
            //Main.NewText(NPC.downedMoonlord);
            /*if (Main.HoverItem.type > 0)
            {
                Main.NewText(Main.mouseText + " " + Main.mouseTextColor);
            }*/
            //Main.NewText(Main.mouseText+" "+Main.mouseTextColor);
            base.FrameEffects();
        }
        public int GetLv() => LV;
        public int GetBasicLv() => LV;
        public void SetExtraLv(int extralv)
        {
            extraLV += extralv;
        }
        public int GetMaxWater() => (int)MathHelper.Lerp(510f, 12000f, LV/ 10f);
        public int 吸收间隔() => (int)MathHelper.Lerp(20f, 2f, LV / 10f) < 2 ? 2 : (int)MathHelper.Lerp(20f, 2f, LV / 10f);
        public float GetMagicMissleDamage() => (int)MathHelper.Lerp(20f, 250f * DamageScale, LV * LV / 100f);

        public int GetMana(float i) => (int)(i * 5 * Player.manaCost);

        public float GetFlameDamage()
        {
            return (int)MathHelper.Lerp(15f, 350f * DamageScale, LV * LV / 100f);

        }
        public float GetIceDamage()
        {
            return (int)MathHelper.Lerp(50f, 700f * DamageScale, LV * LV / 100f) * Player.GetTotalDamage(DamageClass.Magic).Additive;
        }
        public float GetWindDamage()
        {
            float result = (int)MathHelper.Lerp(40f, 280f * DamageScale, (LV - 5) / 5f);
            if (result < 40) result = 40;
            result *= Player.GetTotalDamage(DamageClass.Magic).Additive;
            return result;
        }
        public float 五重飞弹间隔()
        {
            return 60 * MathHelper.Lerp(2f, 0.3f, GetLv() / 10f);
        }
        public float 火球间隔()
        {
            return  MathHelper.Lerp(90, 20, GetLv() / 10f);
        }

    }
}

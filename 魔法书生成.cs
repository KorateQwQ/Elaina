using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using 伊蕾娜.Items.魔法书;
using 伊蕾娜.Projectiles;

namespace 伊蕾娜
{
    public class 魔法书生成 : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            base.KillTile(i, j, type, ref fail, ref effectOnly, ref noItem);
        }
        public override void DropCritterChance(int i, int j, int type, ref int wormChance, ref int grassHopperChance, ref int jungleGrubChance)
        {

            base.DropCritterChance(i, j, type, ref wormChance, ref grassHopperChance, ref jungleGrubChance);
        }
        public override void Drop(int i, int j, int type)/* tModPorter Suggestion: Use CanDrop to decide if items can drop, use this method to drop additional items. See documentation. */
        {
            if (type == 50)
            {
                bool haslearnWater = true;

                //Console.WriteLine("iflearn?"+haslearnWater);
                if (haslearnWater)
                {
                    if (Main.rand.Next(0, 100) > 99)
                    {
                        Item.NewItem(null, new Rectangle(i * 16, j * 16, 50, 50), ModContent.ItemType<水魔法书>(), 1);
                    }
                }
                else
                {
                    if (Main.raining)
                    {
                        if (Main.rand.Next(0, 100) > 50)
                        {
                            Item.NewItem(null, new Rectangle(i * 16, j * 16, 50, 50), ModContent.ItemType<水魔法书>(), 1);
                        }
                    }
                    else if (Main.rand.Next(0, 100) > 95)
                    {
                        Item.NewItem(null, new Rectangle(i * 16, j * 16, 50, 50), ModContent.ItemType<水魔法书>(), 1);
                    }

                }
            }
        }
    }
}

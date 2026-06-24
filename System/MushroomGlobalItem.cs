using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace 伊蕾娜
{

    public partial class ElainaModplayer
    {
        public class MushroomGlobalItem : GlobalItem
        {
            public override bool InstancePerEntity => true;
            public int mushroom = 0;

            public override void UseItemFrame(Item item, Player player)
            {
                if (item.useStyle == ItemUseStyleID.Swing)//item.axe>0&&
                {
                    if (player.direction > 0)
                    {

                        //if (player.itemRotation < -1.5)
                        {
                            //Main.NewText(player.itemRotation + 2);
                            player.itemRotation = MathHelper.Lerp(-1.3f, 1.2f, (player.itemRotation + 2) / 3.31f);
                            //Main.NewText(item.useStyle + " " + player.itemRotation + player.itemLocation);

                        }
                        if (item.pick > 0)
                        {
                            if (player.itemRotation < -1)
                                player.itemRotation = -1f;
                            //
                        }
                    }
                    else
                    {
                        //Main.NewText(player.itemRotation);
                        player.itemRotation = MathHelper.Lerp(-1.6f, 1.3f, (player.itemRotation + 2) / 4.05f);
                        //Main.NewText("2+"+player.itemRotation);
                    }

                }
                base.UseItemFrame(item, player);
            }
            public override bool? UseItem(Item item, Player player)
            {

                return null;
            }
            public override void GetHealLife(Item item, Player player, bool quickHeal, ref int healValue)
            {
                mushroom--;
                base.GetHealLife(item, player, quickHeal, ref healValue);
            }
            public override bool ConsumeItem(Item item, Player player)
            {

                if (item.type == 5 && player.GetModPlayer<ElainaModplayer>().Elaina && mushroom <= 0)
                {
                    mushroom = 120;
                    if (Language.ActiveCulture.Name == "en-US")
                    {
                        switch (Main.rand.Next(0, 3))
                        {
                            case 0:
                                Main.NewText("It's awful...");
                                break;
                            case 1:
                                Main.NewText("Is this part of the test?");
                                break;
                            case 2:
                                Main.NewText("Let's give it to Saya...");
                                break;
                        }
                    }
                    else
                    {

                        switch (Main.rand.Next(0, 3))
                        {
                            case 0:
                                Main.NewText("真难吃...");
                                break;
                            case 1:
                                Main.NewText("这也是考验的一环吗?");
                                break;
                            case 2:
                                Main.NewText("还是留给沙耶吧..");
                                break;
                        }
                    }

                    player.AddBuff(20, 600);
                }
                return true;
            }
        }

    }
}

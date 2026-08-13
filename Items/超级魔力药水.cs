using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace 伊蕾娜.Items
{
    public class 究极魔力药水 : ModItem//物品名
    {

        public override void SetStaticDefaults()
        {
            /*if (Language.ActiveCulture.Name == "en-US")
            {
                DisplayName.SetDefault("Ultimate Magic Potion");
                Tooltip.SetDefault("Immaculate Magic Potion");
            }
            else
            {
                DisplayName.SetDefault("究极魔力药水");
                Tooltip.SetDefault("完美无瑕的魔力药水");
            }*/
            // DisplayName.SetDefault("Ultimate Magic Potion");
            //DisplayName.AddTranslation(7, "究极魔力药水");
            // Tooltip.SetDefault("Immaculate Magic Potion");
            //Tooltip.AddTranslation(7, "完美无瑕的魔力药水");
            //DisplayName.SetDefault("究极魔力药水");
        }

        public override void SetDefaults()
        {
            Item.noUseGraphic = true;//不使用贴图
                                     //item.damage = 0;//伤害
            Item.consumable = true;
            Item.healMana = 1000;
            Item.width = 34;
            Item.height = 34;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = 5;
            Item.noMelee = true;
            Item.holdStyle = 0;
            //item.knockBack = 6;//击退力
            Item.value = Item.sellPrice(0, 1, 0, 0);//价格
            Item.expert = true;
            Item.rare = -12;//稀有度
                            //
                            //Item.makeNPC = ModContent.NPCType<gg_Head>();
            Item.UseSound = SoundID.Item3;//物品声音
            Item.autoReuse = false;//连点效果
            Item.maxStack = 30;//最大堆叠数量
        }



        public override bool CanUseItem(Player player)
        {
            //NPC.NewNPC(null,player.position.X,player.position.Y, ModContent.NPCType<gg_Head>(),)
            //player.statMana -= 990;
            return base.CanUseItem(player);
        }

        public override void GetHealMana(Player player, bool quickHeal, ref int healValue)
        {
            healValue = 1000;
        }
        public override bool? UseItem(Player player)
        {
            player.statMana -= 970;
            return base.UseItem(player);
        }
        public override void OnConsumeItem(Player player)
        {
            base.OnConsumeItem(player);
        }

    }
}
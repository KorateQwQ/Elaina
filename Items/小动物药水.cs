using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using System.Collections;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.Items
{
    public class 小动物药水 : ModItem
    {
        static ArrayList AllCritters = new();

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Critter Potion");
            //DisplayName.AddTranslation(7, "小动物药水");
            // Tooltip.SetDefault("Become a critter");
            //Tooltip.AddTranslation(7, "变成一只小动物");
            //DisplayName.SetDefault("究极魔力药水");
        }
        public override void SetDefaults()
        {
            Item.noUseGraphic = true;//不使用贴图
                                     //item.damage = 0;//伤害
            Item.consumable = true;

            Item.shoot = ModContent.ProjectileType<Projectiles.小动物药水射弹>();
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.shootSpeed = 13;
            Item.width = 34;
            Item.height = 34;
            Item.noMelee = true;
            Item.holdStyle = 0;
            //item.knockBack = 6;//击退力
            Item.value = Item.sellPrice(0, 0, 1, 0);//价格
            Item.expert = true;
            Item.rare = -12;//稀有度

            Item.useStyle = 1;        
            Item.UseSound = SoundID.Item1;//物品声音
            Item.maxStack = 30;//最大堆叠数量
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile proj = Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<Projectiles.小动物药水射弹>(), 
            1,0, player.whoAmI, 随机生成小动物());
            proj.netUpdate = true;
            return false;
        }
        static public int 随机生成小动物()
        {   //style7 被动地面ai：    
            //速度： 1, 8.6, NPCID.Bunny,NPCID.PartyBunny, NPCID.BunnySlimed,NPCID.GoldBunny,NPCID.TownBunny,NPCID.BunnyXmas,NPCID.ExplosiveBunny??,
            //速度： 1.56,8.6 NPCID.Squirrel NPCID.SquirrelGold NPCID.SquirrelRed
            //速度： 2, 7.2 NPCID.Mouse,NPCID.GoldMouse NPCID.Rat
            //style7 被动两栖ai：
            //NPCID.Frog NPCID.GoldFrog水中x10瞬间，地面1，6
            //NPCID.TurtleJungle,NPCID.SeaTurtle,NPCID.Turtle 水中2，地面0.5，6
            //NPCID.Goldfish NPCID.GoldfishWalker
            //NPCID.TruffleWorm
            //style16 游泳ai
            //NPCID.Goldfish
            //NPCID.Dolphin
            //
            //style7 被动地面ai：    
            AllCritters.Add(NPCID.Bunny);
            AllCritters.Add(NPCID.PartyBunny);
            AllCritters.Add(NPCID.BunnySlimed);
            AllCritters.Add(NPCID.GoldBunny);
            AllCritters.Add(NPCID.TownBunny);
            AllCritters.Add(NPCID.BunnyXmas);
            AllCritters.Add(NPCID.ExplosiveBunny);
            AllCritters.Add(NPCID.Squirrel);
            AllCritters.Add(NPCID.SquirrelGold);
            AllCritters.Add(NPCID.SquirrelRed);
            AllCritters.Add(NPCID.Mouse);
            AllCritters.Add(NPCID.GoldMouse);
            AllCritters.Add(NPCID.Rat);
            AllCritters.Add(NPCID.TownCat);
            AllCritters.Add(NPCID.TownDog);

            //style7 被动两栖ai：
            AllCritters.Add(NPCID.Frog);
            AllCritters.Add(NPCID.GoldFrog);
            AllCritters.Add(NPCID.TurtleJungle);
            AllCritters.Add(NPCID.SeaTurtle);
            AllCritters.Add(NPCID.Turtle);
            AllCritters.Add(NPCID.GoldfishWalker);
            //style7 被动三栖ai:
            AllCritters.Add(NPCID.Duck);
            AllCritters.Add(NPCID.DuckWhite);
            AllCritters.Add(NPCID.Seagull);
            //style16 游泳ai
            AllCritters.Add(NPCID.Goldfish);
            AllCritters.Add(NPCID.Dolphin);
            //飞行ai
            //NPCID.Bird
            AllCritters.Add(NPCID.Bird);
            AllCritters.Add(NPCID.GoldBird);
            AllCritters.Add(NPCID.BirdBlue);
            AllCritters.Add(NPCID.BirdRed);
            AllCritters.Add(NPCID.LadyBug);
            AllCritters.Add(NPCID.GoldLadyBug);
            AllCritters.Add(NPCID.Lavafly);
            AllCritters.Add(NPCID.LightningBug);
            AllCritters.Add(NPCID.Butterfly);
            AllCritters.Add(NPCID.HellButterfly);
            AllCritters.Add(NPCID.EmpressButterfly);
            AllCritters.Add(NPCID.GoldButterfly);

            int length = AllCritters.Count;
            var array = AllCritters.ToArray();
            //result[Main.rand.Next(0,length)]
            string result = (array[Main.rand.Next(0, length)]).ToString();
            int r = 0;
            int.TryParse(result,out r);
            return r;
        }
        public override bool CanUseItem(Player player)
        {
            
            Item.holdStyle = 0;
            Item.UseSound = SoundID.Item1;
            //NPC.NewNPC(null,player.position.X,player.position.Y, ModContent.NPCType<gg_Head>(),)
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, 
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            base.PostDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Item.rare = ItemRarityID.Quest;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            base.PostDrawInWorld(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI);
        }
    }
}

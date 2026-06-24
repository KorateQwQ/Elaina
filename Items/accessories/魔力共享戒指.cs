using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.ID;
using ReLogic.Content;
using System.IO;
using System;

namespace 伊蕾娜.Items.accessories
{
    public class 魔力共享戒指 : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.vanity = true;
            if (Language.ActiveCulture.Name == "en-US")
            {
                // DisplayName.SetDefault("Magic Sharing Ring");
                /* Tooltip.SetDefault("Share mana with players who also have this item, the maximum magic power will be increased by 20\n" +
                    " and the mana usage rate will be reduced by 20%, the mana sharing effect will also take effect in the fashion position."); */
            }
            else
            {
                // DisplayName.SetDefault("魔力共享戒指");
                // Tooltip.SetDefault("共享魔力,最大魔力增加20,且魔力使用率减少20%,共享魔力效果在时装位也会生效");
            }

        }
        public override void SetDefaults()
        {
            Item.manaIncrease = 20;
            Item.width = 40;
            Item.height = 70;
            Item.accessory = true; // Makes this item an accessory.
            Item.hasVanityEffects = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 1); // Sets the item sell price to one gold coin.
            Item.manaIncrease = 20;

        }
        public override void UpdateVanity(Player player)
        {
            //player.manaCost -= 1f;
            if (player.manaCost - 0.2 > 0)
            {
                player.manaCost -= 0.2f;
            }
            player.GetModPlayer<蓝量共享>().ring = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statManaMax2 += 20;
            if (player.manaCost - 0.2 > 0)
            {
                player.manaCost -= 0.2f;
            }
            player.GetModPlayer<蓝量共享>().ring = true;
        }
        public override void AddRecipes()
        {
            
            Condition NearElaina = new("RecipeConditions.NearElaina", ()=> Main.LocalPlayer.GetModPlayer<ElainaModplayer>().Elaina);
            Recipe recipe = CreateRecipe();
            //recipe.AddRecipeGroup(RecipeGroupID.Wood, 1);//任意木头1
            recipe.AddIngredient(ItemID.HallowedBar, 10);
            recipe.AddIngredient(ItemID.ManaRegenerationBand);
            recipe.AddCondition(NearElaina);
            recipe.Register();


        }

    }

    public class 蓝量共享 : ModPlayer
    {
        public int proj = -1;
        public int SaveMana = 1000;
        public int 连接id = -1;
        public bool ring = false;
        public int cd = 0;
        public bool 客机成功扣蓝 = false;
        public bool 客机蓝量不足 = false;
        public bool 蓝量不足 = false;
        public override void ResetEffects()
        {
            ring = false;
            base.ResetEffects();
        }
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (Main.netMode == NetmodeID.SinglePlayer && Main.mouseLeft && Main.mouseLeftRelease)
            {
                /*Main.NewText(proj == null);

                if (proj == null)
                {
                    Main.NewText("生成1");
                    proj = Projectile.NewProjectileDirect(null, Player.Center, Vector2.Zero, ModContent.ProjectileType<魔力连接>(), 0, 0, Player.whoAmI, SaveMana);
                }
                else if (!proj.active)
                {
                    Main.NewText("生成2");

                    proj = Projectile.NewProjectileDirect(null, Player.Center, Vector2.Zero, ModContent.ProjectileType<魔力连接>(), 0, 0, Player.whoAmI, SaveMana);
                }*/

            }
            if (Main.myPlayer == Player.whoAmI && drawInfo.shadow == 0)
            {
                if (Player.statMana > 200 && 蓝量不足)
                {
                    foreach (var p in Main.player)
                    {
                        if (p != null && p.active && p.GetModPlayer<蓝量共享>().客机蓝量不足)
                        {
                            同步(Player.whoAmI, 连接id, 3, false, 0);
                        }
                    }
                }
                if (cd > 0) cd--;
                if (ring)//戴着戒指且没有连接的玩家,则试图寻找带着相同戒指的玩家
                {
                    if (连接id < 0)
                    {
                        foreach (var target in Main.ActivePlayers)
                        {
                            if (!target.dead && target.whoAmI != Main.myPlayer && target.GetModPlayer<蓝量共享>().ring)
                            {
                                连接id = target.whoAmI;
                            }
                        }
                    }
                    else
                    {
                        if (!Main.player[连接id].GetModPlayer<蓝量共享>().ring)//检测到跟你连接的玩家摘掉了戒指
                        {
                            连接id = -1;
                        }
                    }
                }
                if (!ring) 连接id = -1;
            }

            base.DrawEffects(drawInfo, ref r, ref g, ref b, ref a, ref fullBright);
        }
        public override void OnMissingMana(Item item, int neededMana)//没蓝的时候先执行这个在继续判断要不要使用魔力药水
        {

            //Main.NewText(Player.statMana + "Needcost " + neededMana+"玩家1蓝量为 " + Main.player[1].statMana);
            if (Main.myPlayer == Player.whoAmI && 连接id >= 0 && cd <= 0)//只有缺蓝的主机可以触发
            {
                if (Main.player[连接id].active && !Main.player[连接id].dead && Main.player[连接id].CheckMana(neededMana, true))
                {
                    float distance = (Main.player[连接id].Center - Player.Center).Length();
                    if (distance < 1500)
                    {
                        Main.player[连接id].manaFlower = true;
                        //Main.NewText("当前储存蓝量= " + SaveMana);
                        Player.statMana += neededMana;
                        同步(Player.whoAmI, 连接id, 1, false, neededMana);
                        if (proj == -1)
                        {
                            //Main.NewText("制造1 ");
                            proj = Projectile.NewProjectile(null, Player.Center, Vector2.Zero, ModContent.ProjectileType<魔力连接>(), 连接id, 0, Player.whoAmI, SaveMana);
                        }
                        else if (!Main.projectile[proj].active)
                        {
                            //Main.NewText("制造2 ");
                            proj = Projectile.NewProjectile(null, Player.Center, Vector2.Zero, ModContent.ProjectileType<魔力连接>(), 连接id, 0, Player.whoAmI, SaveMana);

                        }
                        else if (Main.projectile[proj].timeLeft < 120)
                        {
                            Main.projectile[proj].Kill();
                            proj = Projectile.NewProjectile(null, Player.Center, Vector2.Zero, ModContent.ProjectileType<魔力连接>(), 连接id, 0, Player.whoAmI, SaveMana);
                        }
                        else
                        {
                        }
                    }
                }
            }

            base.OnMissingMana(item, neededMana);
        }
        public void 同步(int playerId, int targetId, int type, bool 蓝量不足, int 试图扣蓝)
        {   //1主机告诉客机要扣篮//2客机告诉主机没蓝了//3客机告诉主机有蓝
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)伊蕾娜.MessageType.魔力共享);
            packet.Write(playerId);
            packet.Write(targetId);
            packet.Write(type);//1主机告诉客机要扣篮//2客机告诉主机没蓝了//3客机告诉主机有蓝
            packet.Write(蓝量不足);
            packet.Write(试图扣蓝);
            packet.Send(-1, playerId);
        }
    }
    public class 魔力连接 : ModProjectile
    {
        Effect DefaultEffect;
        Asset<Texture2D> texture;
        Asset<Texture2D> texture3;
        Asset<Texture2D> MainColor;
        Asset<Texture2D> MainShape;
        Asset<Texture2D> MaskColor;
        Projectile proj;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);

        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("魔力连接");
        }
        public override void OnKill(int timeLeft)
        {
            var player = Main.player[Projectile.owner];
            var target = Main.player[Projectile.damage];
            var p = player.GetModPlayer<蓝量共享>();
            Projectile.ai[1] = 4;
            base.OnKill(timeLeft);
        }
        public override void AI()
        {   //damage 为需要获取蓝量的playerId,ai0为需要给她传输的蓝量,ai1区别类型,0为索要蓝量1为传输
            //                        target.GetModPlayer<蓝量共享>().SaveMana += (int)Projectile.ai[0];
            /*if (Projectile.damage == 0)
            {
                var player1 = Main.player[Projectile.owner];
                Projectile.position = (Main.MouseWorld + player1.Center)/2;
                Projectile.rotation = (Main.MouseWorld-player1.Center).ToRotation();
                if(Main.mouseLeft)Projectile.timeLeft=300;
            }
            else*/
            {
                var player = Main.player[Projectile.owner];
                var target = Main.player[Projectile.damage];
                var p = player.GetModPlayer<蓝量共享>();
            }
            base.AI();
        }
        public void 同步(int playerId, int ignoreId, bool 蓝量不足, bool 成功扣蓝)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)伊蕾娜.MessageType.魔力共享);
            packet.Write(playerId);
            packet.Write(蓝量不足);
            packet.Write(成功扣蓝);
            packet.Send(-1, ignoreId);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            //if (Projectile.owner == Main.myPlayer)
            Vector2 playerpos = Main.player[Projectile.owner].MountedCenter;
            Vector2 targetpos = Main.player[Projectile.damage].MountedCenter;
            var normalDir = Vector2.Normalize(playerpos - targetpos);
            normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));
            float width = 2;
            float 渐变 = 60 - (float)Main.GameUpdateCount % 120;
            float 渐变率 = MathHelper.Lerp(1, 1.2f, (float)Math.Abs(渐变) / 60);
            width *= 渐变率;
            float alpha = 1;
            if (Projectile.timeLeft < 120)
            {
                alpha = MathHelper.Lerp(0, 1, Projectile.timeLeft / 120f);
            }
            {
                if (texture == null) texture = Mod.Assets.Request<Texture2D>("Projectiles/MagicIce/冰施法");
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;
                Vector2 origin = new(139, 127);
                Rectangle sourceRectangle = new(0, 0, 550, 250);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
                Main.spriteBatch.Draw(texture.Value, targetpos - Main.screenPosition, sourceRectangle, Color.Red * alpha, Projectile.rotation, origin, 渐变率, SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(texture.Value, playerpos - Main.screenPosition, sourceRectangle, Color.Red * alpha, Projectile.rotation, origin, 渐变率, SpriteEffects.None, 0f);
                List<CustomVertexInfo> bars = new();
                List<CustomVertexInfo> triangleList = new();
                //连接四个顶点
                var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;
                bars.Add(new CustomVertexInfo(playerpos + normalDir * width * trans.M11, Color.White, new Vector3(0, 1, alpha)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                bars.Add(new CustomVertexInfo(playerpos + normalDir * -width * trans.M11, Color.White, new Vector3(0, 0, alpha)));

                bars.Add(new CustomVertexInfo(targetpos + normalDir * width * trans.M11, Color.White, new Vector3(1, 1, alpha)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                bars.Add(new CustomVertexInfo(targetpos + normalDir * -width * trans.M11, Color.White, new Vector3(1, 0, alpha)));
                if (bars.Count > 2)
                {

                    // 按照顺序连接三角形
                    triangleList.Add(bars[0]);

                    var vertex = new CustomVertexInfo(playerpos, Color.White,
                        new Vector3(0, 0.5f, 1));
                    triangleList.Add(bars[1]);
                    triangleList.Add(vertex);
                    for (int i = 0; i < bars.Count - 2; i += 2)
                    {
                        triangleList.Add(bars[i]);
                        triangleList.Add(bars[i + 2]);
                        triangleList.Add(bars[i + 1]);

                        triangleList.Add(bars[i + 1]);
                        triangleList.Add(bars[i + 2]);
                        triangleList.Add(bars[i + 3]);
                    }


                    // 干掉注释掉就可以只显示三角形栅格
                    /*RasterizerState rasterizerState = new RasterizerState();
					rasterizerState.CullMode = CullMode.None;
					rasterizerState.FillMode = FillMode.WireFrame;
					Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/

                    var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
                    var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) * Main.Transform;

                    // 把变换和所需信息丢给shader

                    //
                    DefaultEffect.Parameters["uTransform"].SetValue(model * projection);
                    DefaultEffect.Parameters["uTime"].SetValue(-(float)Main.GameUpdateCount * 0.03f);

                    Main.graphics.GraphicsDevice.Textures[0] = MainColor.Value;
                    Main.graphics.GraphicsDevice.Textures[1] = MainShape.Value;
                    Main.graphics.GraphicsDevice.Textures[2] = MaskColor.Value;

                    Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.PointWrap;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.PointWrap;
                    //Main.graphics.GraphicsDevice.Textures[0] = Main.magicPixel;
                    //Main.graphics.GraphicsDevice.Textures[1] = Main.magicPixel;
                    //Main.graphics.GraphicsDevice.Textures[2] = Main.magicPixel;

                    DefaultEffect.CurrentTechnique.Passes[0].Apply();


                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);
                    Main.graphics.GraphicsDevice.RasterizerState = originalState;
                }
                bars.Add(new CustomVertexInfo(playerpos + normalDir * width * 3 * trans.M11, Color.White, new Vector3(0, 1, alpha / 3)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                bars.Add(new CustomVertexInfo(playerpos + normalDir * -width * 3 * trans.M11, Color.White, new Vector3(0, 0, alpha / 3)));

                bars.Add(new CustomVertexInfo(targetpos + normalDir * width * 3 * trans.M11, Color.White, new Vector3(1, 1, alpha / 3)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                bars.Add(new CustomVertexInfo(targetpos + normalDir * -width * 3 * trans.M11, Color.White, new Vector3(1, 0, alpha / 3)));
                if (bars.Count > 2)
                {

                    // 按照顺序连接三角形
                    triangleList.Add(bars[0]);

                    var vertex = new CustomVertexInfo(playerpos, Color.White,
                        new Vector3(0, 0.5f, 1));
                    triangleList.Add(bars[1]);
                    triangleList.Add(vertex);
                    for (int i = 0; i < bars.Count - 2; i += 2)
                    {
                        triangleList.Add(bars[i]);
                        triangleList.Add(bars[i + 2]);
                        triangleList.Add(bars[i + 1]);

                        triangleList.Add(bars[i + 1]);
                        triangleList.Add(bars[i + 2]);
                        triangleList.Add(bars[i + 3]);
                    }


                    // 干掉注释掉就可以只显示三角形栅格
                    /*RasterizerState rasterizerState = new RasterizerState();
					rasterizerState.CullMode = CullMode.None;
					rasterizerState.FillMode = FillMode.WireFrame;
					Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/

                    var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
                    var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) * Main.Transform;

                    // 把变换和所需信息丢给shader

                    //
                    DefaultEffect.Parameters["uTransform"].SetValue(model * projection);
                    DefaultEffect.Parameters["uTime"].SetValue(-(float)Main.GameUpdateCount * 0.03f);

                    Main.graphics.GraphicsDevice.Textures[0] = MainColor.Value;
                    Main.graphics.GraphicsDevice.Textures[1] = MainShape.Value;
                    Main.graphics.GraphicsDevice.Textures[2] = MaskColor.Value;

                    Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.PointWrap;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.PointWrap;
                    //Main.graphics.GraphicsDevice.Textures[0] = Main.magicPixel;
                    //Main.graphics.GraphicsDevice.Textures[1] = Main.magicPixel;
                    //Main.graphics.GraphicsDevice.Textures[2] = Main.magicPixel;

                    DefaultEffect.CurrentTechnique.Passes[0].Apply();


                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);

                    Main.graphics.GraphicsDevice.RasterizerState = originalState;



                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }
        public override void SetDefaults()
        {
            if (DefaultEffect == null) DefaultEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/tail").Value;

            if (MainColor == null) MainColor = Mod.Assets.Request<Texture2D>("Projectiles/魔力连接");
            if (MainShape == null) MainShape = Mod.Assets.Request<Texture2D>("Projectiles/渐变2");
            if (MaskColor == null) MaskColor = Mod.Assets.Request<Texture2D>("Projectiles/渐变2");
            Projectile.penetrate = -1; // 穿透数量
            Projectile.tileCollide = true;//瓷砖碰撞
            Projectile.friendly = false;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.damage = 0;
            Projectile.timeLeft = 180;
            //Projectile.CritChance = (int)Main.player[Projectile.owner].GetTotalCritChance(DamageClass.Magic);

            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;

        }


        private struct CustomVertexInfo : IVertexType
        {
            private static VertexDeclaration _vertexDeclaration = new(
            [
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            ]);
            public Vector2 Position;
            public Color Color;
            public Vector3 TexCoord;

            public CustomVertexInfo(Vector2 position, Color color, Vector3 texCoord)
            {
                Position = position;
                Color = color;
                TexCoord = texCoord;
            }

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    return _vertexDeclaration;
                }
            }
        }
    }
}

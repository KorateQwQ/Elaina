using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Terraria.Audio;
using Terraria.GameContent.Achievements;
namespace 伊蕾娜.炼金
{
    public class 爆炸 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.ignoreWater = true;//无视水
            Projectile.friendly = true;//可以攻击敌人	
            Projectile.hostile = false;
            Projectile.penetrate = -1; // 穿透数量
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.timeLeft = 5;
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.scale = 1f;
            Projectile.damage = 50;
            //Projectile.CritChance = (int)Main.player[Projectile.owner].GetTotalCritChance(DamageClass.Magic);

        }
        public override void AI()
        {
            Projectile.hostile = true;
            base.AI();
        }
        public override bool CanHitPlayer(Player target)
        {
            return true;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {

            var player = Main.player[Projectile.owner].GetModPlayer<炼金modplayer>();
            float extraHitBox = MathHelper.Lerp(1, 10, Projectile.ai[0] / 30f);
            float point = 0f;
            Vector2 length = Vector2.One* Projectile.width* extraHitBox;
            float width =  Projectile.width * extraHitBox;

            bool result = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center- length / 2, Projectile.Center + length/2, width, ref point);
            return result;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            var player = Main.player[Projectile.owner].GetModPlayer<炼金modplayer>();

            modifiers.FinalDamage += (int)Projectile.ai[1];
            int lifemax = target.lifeMax;
            if (target.realLife >= 0) lifemax = Main.npc[target.realLife].lifeMax;
            modifiers.FinalDamage += (int)(lifemax * (Projectile.ai[0] / 100f));
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62, Projectile.position);//62
            var player = Main.player[Projectile.owner].GetModPlayer<炼金modplayer>();
            float extraHitBox = MathHelper.Lerp(1, 10, Projectile.ai[0] / 30f);
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 100 * extraHitBox / 2f; i++)
                {
                    Vector2 v = new(Main.rand.NextFloatDirection() * 40f, Main.rand.NextFloatDirection() * 40f);
                    v = Vector2.Normalize(v) * 30 * extraHitBox;
                    Dust d = Dust.NewDustDirect(Projectile.position, 40, 40, 127,
                    -v.X, -v.Y, 255, Color.White, 5f);
                    d.noGravity = true;

                }
            }

            int explosionRadius = 3;
            int minTileX = (int)(Projectile.Center.X / 16f - explosionRadius);
            int maxTileX = (int)(Projectile.Center.X / 16f + explosionRadius);
            int minTileY = (int)(Projectile.Center.Y / 16f - explosionRadius);
            int maxTileY = (int)(Projectile.Center.Y / 16f + explosionRadius);
            /*
            int minTileX = (int)(projectile.Center.X / 16f - (float)explosionRadius);
            int maxTileX = (int)(projectile.Center.X / 16f + (float)explosionRadius);
            int minTileY = (int)(projectile.Center.Y / 16f - (float)explosionRadius);
            int maxTileY = (int)(projectile.Center.Y / 16f + (float)explosionRadius);
            */
            if (minTileX < 0)
            {
                minTileX = 0;
            }
            if (maxTileX > Main.maxTilesX)
            {
                maxTileX = Main.maxTilesX;
            }
            if (minTileY < 0)
            {
                minTileY = 0;
            }
            if (maxTileY > Main.maxTilesY)
            {
                maxTileY = Main.maxTilesY;
            }
            bool canKillWalls = false;
            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    float diffX = Math.Abs(x - Projectile.Center.X / 16f);
                    float diffY = Math.Abs(y - Projectile.Center.Y / 16f);
                    double distance = Math.Sqrt((double)(diffX * diffX + diffY * diffY));
                    if (distance < explosionRadius && Main.tile[x, y] != null && Main.tile[x, y].WallType == 0)
                    {
                        canKillWalls = true;
                        break;
                    }
                }
            }
            AchievementsHelper.CurrentlyMining = true;

            for (int i = minTileX; i <= maxTileX; i++)
            {
                for (int j = minTileY; j <= maxTileY; j++)
                {
                    float diffX = Math.Abs(i - Projectile.Center.X / 16f);
                    float diffY = Math.Abs(j - Projectile.Center.Y / 16f);
                    double distanceToTile = Math.Sqrt((double)(diffX * diffX + diffY * diffY));

                    if (distanceToTile < explosionRadius)
                    {
                        bool canKillTile = true;
                        if (Main.tile[i, j] != null && Main.tile[i, j].HasTile)
                        {
                            canKillTile = true;
                            if (Main.tile[i, j].TileType == 26 || Main.tile[i, j].TileType == 107 || Main.tile[i, j].TileType == 108 || Main.tile[i, j].TileType == 111 || Main.tile[i, j].TileType == 226 || Main.tile[i, j].TileType == 237 || Main.tile[i, j].TileType == 221 || Main.tile[i, j].TileType == 222 || Main.tile[i, j].TileType == 223 || Main.tile[i, j].TileType == 211 || Main.tile[i, j].TileType == 404)
                            {
                                canKillTile = false;
                            }
                            if (!Main.hardMode && Main.tile[i, j].TileType == 58)
                            {
                                canKillTile = false;
                            }
                            if (!TileLoader.CanExplode(i, j))
                            {
                                canKillTile = false;
                            }

                            if (TileID.Sets.BasicChest[Main.tile[i, j].TileType])
                            {
                                Tile tile = Main.tile[i, j];
                                if (!Chest.CanDestroyChest(i - tile.TileFrameX / 18 % 2, j - tile.TileFrameY / 18)) canKillTile = false;
                                if (Main.netMode == NetmodeID.MultiplayerClient) canKillTile = false;
                                //Main.NewText(Chest.CanDestroyChest(i - tile.TileFrameX/ 18 % 2, j - tile.TileFrameY / 18));
                            }
                            if (canKillTile)
                            {
                                WorldGen.KillTile(i, j, false, false, false);
                                if (!Main.tile[i, j].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                                {
                                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j, 0f, 0, 0, 0);
                                }
                            }
                        }
                        if (canKillTile)
                        {
                            for (int x = i - 1; x <= i + 1; x++)
                            {
                                for (int y = j - 1; y <= j + 1; y++)
                                {
                                    if (Main.tile[x, y] != null && Main.tile[x, y].WallType > 0 && canKillWalls && WallLoader.CanExplode(x, y, Main.tile[x, y].WallType))
                                    {
                                        WorldGen.KillWall(x, y, false);
                                        if (Main.tile[x, y].WallType == 0 && Main.netMode != NetmodeID.SinglePlayer)
                                        {
                                            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 2, x, y, 0f, 0, 0, 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            player.炸锅物品总伤害 = 0;
            player.炸锅物品总稀有度 = 0;
            base.OnKill(timeLeft);
        }
    }
}

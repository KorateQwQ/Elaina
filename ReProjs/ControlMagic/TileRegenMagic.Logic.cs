using System;
using System.Collections.Generic;
using System.IO;
using KL.Extensions;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ObjectData;
using 伊蕾娜.Config;
using 伊蕾娜.Dusts;

namespace 伊蕾娜.ReProjs.ControlMagic;

public partial class TileRegenMagic
{
    class TileRegenMagic_GlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        public bool ElainaProtectItem = false;

        public int ProtectTime = 0;

        public Vector2 TilePosBeforeBreak = new Vector2(-1);

        public bool TryBackToOriginTile = false;

        private int TraceTime = 0;

        public int WallToRegen = 0;

        //如果是大锅类型的物品，placestyle可以决定其分支。否则会固定生成烹饪锅
        public int placeStyle = 0;

        public short Tileslope = 0;

        public int TileToRegen = -1;

        public short TileFrameX = -1;
        
        public short TileFrameY = -1;

        public short bottomTileFrameX = -1;

        public short bottomTileFrameY = -1;

        //多格物块的变种，可以决定左右朝向等。
        public int PlaceAlternate = -1;

        //物块还原回去时，最初的速度朝向。物块物品会先随机飞出去，再朝着它被破坏的位置追击。
        public Vector2 InitBackSpeedDir = Vector2.Zero;

        //物块飞出去时的角度偏移，避免走直线。
        public float InitRotOffset = 0;


        public override void Load()
        {
            On_Item.CanCombineStackInWorld += On_ItemOnCanCombineStackInWorld;
            On_Item.NewItem_Inner += On_ItemOnNewItem_Inner;
            base.Load();
        }

        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
            ref float rotation,
            ref float scale, int whoAmI)
        {
            if (item is { IsAir: false } && item.GetGlobalItem<TileRegenMagic_GlobalItem>().ElainaProtectItem)
            {

                EndBeginDraw(1,1);
                BloomEffectByGivenColor(TextureAssets.Item[item.type].Size());
                for (int i = 0; i < 20; i++)
                {
                    DrawItemInWorld(item, item.CenterForDraw(),  Color.Lerp(new Color(255,120,180) * 0.95f, Color.Black, i / 20f),new Vector2(scale*(1.2f+i*0.07f)),rotation);
                }
                
                EndBeginDraw();
                DrawItemInWorld(item, item.CenterForDraw(),  lightColor,new Vector2(scale));

                return false;
            }

            return base.PreDrawInWorld(item, spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
        }

        public void MoveToOriginTile(Item item)
        {
            if (Main.GameUpdateCount % 3 == 0)
            {
                Color c = Main.hslToRgb(Main.rand.NextFloat(0, 1f), 1, 0.7f);
                Dust d = Dust.NewDustDirect(item.Center + Main.rand.NextVector2Circular(10, 10), 0, 0,
                    ModContent.DustType<starDust>(),
                    0, 0, 255, c, Main.rand.NextFloat(0.3f, 0.4f));
                d.noGravity = true;
            } 

            item.Center += item.GetGlobalItem<TileRegenMagic_GlobalItem>().InitBackSpeedDir;
            if (TraceTime++ < 30)
            {
                item.velocity = Vector2.Zero;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().InitBackSpeedDir = item
                    .GetGlobalItem<TileRegenMagic_GlobalItem>().InitBackSpeedDir
                    .RotatedBy(item.GetGlobalItem<TileRegenMagic_GlobalItem>().InitRotOffset);
                //Main.NewText(item.GetGlobalItem<TileRegenMagic_GlobalItem>().InitRotOffset);
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().InitBackSpeedDir *= 0.92f;
            }
            else
            {
                item.velocity = item.GetGlobalItem<TileRegenMagic_GlobalItem>().InitBackSpeedDir;
                Vector2 targetPos = TilePosBeforeBreak * 16; //TilePosBeforeBreak*16
                item.TraceTargetPosition(targetPos, 20, 0.2f);
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().InitBackSpeedDir = item.velocity;
                item.velocity = Vector2.Zero;

                if ((item.Center - targetPos).Length() < 20)
                {
                    item.GetGlobalItem<TileRegenMagic_GlobalItem>().TryBackToOriginTile = false;
                    int playerID = Main.myPlayer;

                    if (Main.netMode is 0 or 2&&!Main.tile[(int)TilePosBeforeBreak.X, (int)TilePosBeforeBreak.Y].HasTile)
                    {
                        //Main.NewText("Try Place Tile: " + item.createTile +" place Style: "+ item.placeStyle+" TargetPos: "+ TilePosBeforeBreak);
                        int tileForPlace = TileToRegen >= 0 ? TileToRegen : item.createTile;
                        
                        if (WorldGen.PlaceTile((int)TilePosBeforeBreak.X, (int)TilePosBeforeBreak.Y, tileForPlace,
                                false, true, playerID, item.placeStyle))
                        {
                            //Main.NewText("Place Tile Success");
                            Main.tile[(int)TilePosBeforeBreak.X,(int)TilePosBeforeBreak.Y].TileFrameX = TileFrameX;
                            Main.tile[(int)TilePosBeforeBreak.X,(int)TilePosBeforeBreak.Y].TileFrameY = TileFrameY;
                            //Main.tile[(int)TilePosBeforeBreak.X,(int)TilePosBeforeBreak.Y].frame

                            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, (int)TilePosBeforeBreak.X,
                                (int)TilePosBeforeBreak.Y, tileForPlace,
                                 item.placeStyle); //1增加0删除ij位置161砖块类型messagebuffer，最后一位placestyle可不加
                        }
                    }

                    if (Main.tile[(int)TilePosBeforeBreak.X, (int)TilePosBeforeBreak.Y].HasTile)
                    {
                        Main.NewText("重复位置");
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        Color c = Main.hslToRgb(Main.rand.NextFloat(0, 1f), 1, 0.7f);
                        Vector2 velocity = Main.rand.NextVector2Circular(10, 10);
                        Dust d = Dust.NewDustDirect(item.Center + Main.rand.NextVector2Circular(5, 5), 0, 0,
                            ModContent.DustType<starDust>(),
                            velocity.X, velocity.Y, 255, c, Main.rand.NextFloat(0.45f, 0.7f));
                        d.noGravity = true;
                    }
                    item.TurnToAir();
                }
            }
        }

        private int On_ItemOnNewItem_Inner(On_Item.orig_NewItem_Inner orig, IEntitySource source, int x, int y,
            int width, int height, Item itemToClone, int type, int stack, bool noBroadcast, int pfix, bool noGrabDelay,
            bool reverseLookup)
        {
            int itemIndex = orig.Invoke(source, x, y, width, height, itemToClone, type, stack, noBroadcast, pfix,
                noGrabDelay, reverseLookup);
            Item item = Main.item[itemIndex];
            item.whoAmI = itemIndex;
            
            x /= 16;
            y /= 16;
            Tile tile = Framing.GetTileSafely(x, y);


            //Log("Break TIle type: "+type);
            if (tile!=null)
            {
                if (source is EntitySource_TileBreak )//&& (item.createTile>=0||item.createWall>0)
                {
                    //Main.NewText("Is valid Tile! Try Protect");
                    foreach (var player in Main.player)
                    {
                        //足够靠近伊蕾娜时，此物品被保护
                        if (player is { active: true } && (player.Center - item.Center).Length() < 2000)
                        {
                            var p = player.GetModPlayer<ElainaModplayer>();
                            if (p.Elaina)
                            {
                                int spawnTileType = item.createTile;
                                int spawnWallType = tile.WallType;
                                short spawnTileFrameX = -1;
                                short spawnTileFrameY = -1;
                                short spawnBottomTileFrameX = -1;
                                short spawnBottomTileFrameY = -1;
                                int alternate = -1;
                                SlopeType spawnTileSlope = 0;
                                if (tile.HasTile)
                                {
                                    spawnTileType = tile.TileType;
                                    spawnTileFrameX = tile.TileFrameX;
                                    spawnTileFrameY = tile.TileFrameY;
                                    Tile bottomTile = Main.tile[x, y  + 1];

                                    if (bottomTile.HasTile)
                                    {
                                        spawnBottomTileFrameX = bottomTile.TileFrameX;
                                        spawnBottomTileFrameY = bottomTile.TileFrameY;
                                    }
                                    spawnTileSlope = tile.Slope;
                                }

                                FindCorrectSpawnPosOfTile(spawnTileType, item.placeStyle,ref alternate,ref x, ref y);
                                //Log("ProtectItemByElaina: "+type);

                                item.GetGlobalItem<TileRegenMagic_GlobalItem>().ProtectItemByElaina(item, x, y,spawnTileType,spawnWallType,(short)spawnTileSlope,alternate,
                                    spawnTileFrameX,spawnTileFrameY,spawnBottomTileFrameX,spawnBottomTileFrameY);

                            }
                        }
                    }
                }
            }
            

            return itemIndex;
        }

        void FindCorrectSpawnPosOfTile(int tileType,int placeStyle,ref int Alternate, ref int x, ref int y)
        {
            if (TileGengenMagic_TileSystem.TileOriginPos.ContainsKey(new Vector2(x, y)))
            {
                //Main.NewText("Find Multi Tile's Origin Tile Position: " + TileGengenMagic_TileSystem.TileOriginPos[new Vector2(x,y)].OriginX+" "+ TileGengenMagic_TileSystem.TileOriginPos[new Vector2(x,y)].OriginY+" type: " +TileGengenMagic_TileSystem.TileOriginPos[new Vector2(x,y)].TileType );
                TileGengenMagic_TileSystem.TileOriginPosition OriginPosition =
                    TileGengenMagic_TileSystem.TileOriginPos[new Vector2(x, y)];
                x = OriginPosition.OriginX;
                y = OriginPosition.OriginY;
                Alternate = OriginPosition.Alternate;
            }
        }

        public void ProtectItemByElaina(Item item, int x, int y,int TileToRegen = -1,int WallToRegen = 0, short Tileslope = 0, int Alternates = -1,short TileFrameX = -1,short TileFrameY = -1,short bottomTileFrameX = -1,short bottomTileFrameY = -1)
        {
            if (ServerOrLocalMode())
            {
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().ElainaProtectItem = true;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().ProtectTime = 600;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().TilePosBeforeBreak = new Vector2(x, y);
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileToRegen =
                    TileToRegen >= 0 ? TileToRegen : item.createTile;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().WallToRegen = WallToRegen;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().placeStyle = item.placeStyle;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().Tileslope = Tileslope;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().PlaceAlternate = Alternates;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameX = TileFrameX;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameY = TileFrameY;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameX = bottomTileFrameX;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameY = bottomTileFrameY;
                
                NetMessage.SendData(MessageID.SyncItem,-1,-1,null,item.whoAmI);
                //Log($"Protect {item} Item ID:{item.whoAmI}");
                //item.noGrabDelay = 60;
            }
        }

        public void RemoveProtectItemByElaina(Item item)
        {
            if (Main.netMode is 0 or 1)
            {
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().ElainaProtectItem = false;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().ProtectTime = 0;
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().TilePosBeforeBreak = new Vector2(-1);
                item.NetUpdate();
            }
        }

        private bool On_ItemOnCanCombineStackInWorld(On_Item.orig_CanCombineStackInWorld orig, Item self)
        {
            if (self.GetGlobalItem<TileRegenMagic_GlobalItem>().ElainaProtectItem) return false;
            return orig.Invoke(self);
        }


        /*public override void NetSend(Item item, BinaryWriter writer)
        {
            //Log("NetSend: " + ProtectTime+" item: "+item);

            /*writer.Write(ElainaProtectItem);
            writer.Write(ProtectTime);
            writer.WriteVector2(TilePosBeforeBreak);
            //writer.Write(TryBackToOriginTile);
            writer.Write(TileToRegen);
            writer.Write(WallToRegen);
            //writer.WriteVector2(InitBackSpeedDir);
            //writer.Write(InitRotOffset);
            
            writer.Write(Tileslope);
            writer.Write(TileFrameX);
            writer.Write(TileFrameY);
            writer.Write(bottomTileFrameX);
            writer.Write(bottomTileFrameY);
            writer.Write(placeStyle);
            writer.Write(PlaceAlternate);#1#
            base.NetSend(item, writer);
        }*/

        /*public override void NetReceive(Item item, BinaryReader reader)
        {
            /*ElainaProtectItem = reader.ReadBoolean();
            ProtectTime = reader.ReadInt32();
            TilePosBeforeBreak = reader.ReadVector2();
            //TryBackToOriginTile = reader.ReadBoolean();
            TileToRegen = reader.ReadInt32();
            WallToRegen = reader.ReadInt32();
            //InitBackSpeedDir = reader.ReadVector2();
            //InitRotOffset = reader.ReadSingle();

            Tileslope = reader.ReadInt16();
            TileFrameX = reader.ReadInt16();
            TileFrameY = reader.ReadInt16();
            bottomTileFrameX = reader.ReadInt16();
            bottomTileFrameY = reader.ReadInt16();
            placeStyle = reader.ReadInt32();
            PlaceAlternate = reader.ReadInt32();#1#

            //Log("ProtectTime: " + ProtectTime + " itemID: " + item.whoAmI+" backTarget: "+ TilePosBeforeBreak);


            base.NetReceive(item, reader);
        }*/

        public override bool CanPickup(Item item, Player player)
        {
            //if (item.GetGlobalItem<TileRegenMagic_GlobalItem>().ElainaProtectItem) return false;
            return base.CanPickup(item, player);
        }

        public override void UpdateInventory(Item item, Player player)
        {
            item.GetGlobalItem<TileRegenMagic_GlobalItem>().ProtectTime = 0;
            item.GetGlobalItem<TileRegenMagic_GlobalItem>().ElainaProtectItem = false;

            base.UpdateInventory(item, player);
        }

        public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
        {
            if (ElainaProtectItem)
            {
                //Console.WriteLine(" whoAmI: "+Main.myPlayer +"Item ID: "+item.whoAmI+ "Update Item name: "+ item.Name +"ElainaProtectItem: "+ ElainaProtectItem+ " Regen Position: "+ TilePosBeforeBreak);
                if (ProtectTime-- <= 0)
                {
                    ElainaProtectItem = false;
                    ProtectTime = 0;
                }


                if (TryBackToOriginTile)
                {
                    MoveToOriginTile(item);
                }
            }

            base.Update(item, ref gravity, ref maxFallSpeed);
        }

        public override void OnSpawn(Item item, IEntitySource source)
        {
            base.OnSpawn(item, source);
        }

        class TileRegenMagic_ModPlayer : ModPlayer
        {
            public override void ResetEffects()
            {
                base.ResetEffects();
            }

            public override void Load()
            {
                On_WorldGen.PlaceTile += On_WorldGenOnPlaceTile;
                base.Load();
            }

            private bool On_WorldGenOnPlaceTile(On_WorldGen.orig_PlaceTile orig, int i, int j, int type, bool mute, bool forced, int plr, int style)
            {
                return orig(i, j, type, mute, forced, plr, style);
            }
            

            public override void FrameEffects()
            {
                /*if (IsRightClick()&& Main.myPlayer == Player.whoAmI)
                {
                    /*
                    int x = (int)(Main.MouseWorld.X / 16);
                    int y = (int)(Main.MouseWorld.Y / 16);
                    Main.NewText("x: "+x+" y: "+y+" "+ Main.tile[x,y]);
                    #1#

                    /*int tileType = Main.tile[x, y].TileType;
                    TileObjectData tileData = TileObjectData.GetTileData(tileType,0);
            
                    if (tileData != null)
                    {
                        for (int i = 0; i < tileData.Width; i++)
                        {
                            Tile tile = Framing.GetTileSafely(x-1, y);
                            if (tile.HasTile && tile.TileType == tileType)
                            {
                                x -= 1;
                            }
                        }
                        for (int i = 0; i < tileData.Height; i++)
                        {
                            Tile tile = Framing.GetTileSafely(x, y-1);
                            if (tile.HasTile && tile.TileType == tileType)
                            {
                                y -= 1;
                            }
                        }
                
                        x += tileData.Origin.X;
                        y += tileData.Origin.Y;
                        Main.NewText("Tile place position = " + x + "  " + y + " " + " Origin: "+ tileData.Origin);
                    }#1#
                    
                    /*int x = (int)(Main.MouseWorld.X / 16);
                    int y = (int)(Main.MouseWorld.Y / 16);

                    bool canPlace = TileObject.CanPlace(x,y , (ushort)79, 0, 1, out TileObject tileObject, onlyCheck: false);

                    if (canPlace)
                    {
                        TileObject.Place(tileObject);
                        if (Main.netMode == 1 && !Main.tileContainer[tileObject.type] && tileObject.type != 423)
                            NetMessage.SendObjectPlacement(-1, x, y, tileObject.type, tileObject.style, tileObject.alternate, tileObject.random, Player.direction);
                    }#1#
                    

                }
                if (IsLeftClick()&& Main.myPlayer == Player.whoAmI)
                {
                    /*int x = (int)(Main.MouseWorld.X / 16);
                    int y = (int)(Main.MouseWorld.Y / 16);
                    Main.NewText(Main.tile[x,y]+" TileFrameNumber: " +Main.tile[x,y].TileFrameNumber);
                    Main.tile[x, y].TileFrameX = 0;
                    Main.tile[x, y+1].TileFrameX = 44;
                    Main.tile[x, y+2].TileFrameX = 0;#1#

                }
                if (Main.netMode!=2&& KeyBind.蓄力.JustPressed && Main.myPlayer == Player.whoAmI)
                {

                    //Main.NewText(x+" "+y);
                    //WorldGen.PlaceTile(x, y, 79, false, true, Main.myPlayer, 0);

                    Projectile.NewProjectile(null, Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<TileRegenMagic>(), 0, 0, Main.myPlayer);
                }*/

                base.FrameEffects();
            }

            public override bool OnPickup(Item item)
            {
                //if (item.GetGlobalItem<TileRegenMagic_GlobalItem>().ElainaProtectItem)
                // item.GetGlobalItem<TileRegenMagic_GlobalItem>().RemoveProtectItemByElaina(item);

                return base.OnPickup(item);
            }
        }

        class TileRegenMagic_ModTile : GlobalTile
        {
            public override void Load()
            {
                On_WorldGen.PlaceObject += On_WorldGenOnPlaceObject;
                base.Load();
            }

            private bool On_WorldGenOnPlaceObject(On_WorldGen.orig_PlaceObject orig, int x, int y, int type, bool mute, int style, int alternate, int random, int direction)
            {
                if (!TileObject.CanPlace(x, y, type, style, direction, out var objectData))
                {
                    return false;
                }

                objectData.random = random;
                objectData.alternate = alternate;
                if (TileObject.Place(objectData)) {
                    WorldGen.SquareTileFrame(x, y);
                    if (!mute)
                    {
                        SoundEngine.PlaySound(SoundID.Dig, new Vector2(x * 16, y * 16));
                    }
                }

                return true;
            }

            public override void KillTile(int x, int y, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
            {
                int tileType = type;
                int style =0;
                int alternate = -1;
                TileObjectData tileData = TileObjectData.GetTileData(Main.tile[x,y]);
                TileObjectData.GetTileInfo(Main.tile[x, y],ref style,ref alternate);
                
                if (tileData != null&&!fail)
                {
                    //Main.NewText($"Kill tile position {x}, {y}");
                    int modifyX = x;
                    int modifyY = y;
                    
                    for (int i = 0; i < tileData.Width; i++)
                    {
                        Tile tile = Framing.GetTileSafely(modifyX - 1, modifyY);

                        if (tile.HasTile && tile.TileType == tileType)
                        {
                            modifyX -= 1;
                        }
                    }

                    for (int i = 0; i < tileData.Height; i++)
                    {
                        Tile tile = Framing.GetTileSafely(modifyX, modifyY - 1);
                        if (tile.HasTile && tile.TileType == tileType)
                        {
                            modifyY -= 1;
                        }
                    }

                    //说明对于一个多格物块，这个是它的第一个被破坏的物块
                    bool FullComble = true;
                    for (int i = 0; i < tileData.Width; i++)
                    {
                        for (int j = 0; j < tileData.Height; j++)
                        {
                            Tile tile = Framing.GetTileSafely(modifyX+i, modifyY+j);
                            if (tile.TileType != type) FullComble = false;
                        }
                    }

                    //此处为放置原点，但是不需要这个
                    /*modifyX += tileData.Origin.X;
                    modifyY += tileData.Origin.Y;*/
                    if (FullComble)
                    {
                        for (int i = 0; i < tileData.Width; i++)
                        {
                            for (int j = 0; j < tileData.Height; j++)
                            {
                                TileGengenMagic_TileSystem.TileOriginPos[new Vector2(modifyX+i, modifyY + j)] = new TileGengenMagic_TileSystem.TileOriginPosition(modifyX+i, modifyY + j,modifyX, modifyY, type,alternate);
                                //Main.NewText($"Help modify: {modifyX+i}, {modifyY + j}");
                            }
                        }
                        //Main.NewText($"KillTile Type: {type} origin: " + x+" "+y +" Modify: "+modifyX+tileData.Origin.X+" "+modifyY+tileData.Origin.Y);
                        //Log($"KillTile Type: {type} origin: " + x+" "+y +" Modify: "+ (modifyX+tileData.Origin.X)+" "+(modifyY+tileData.Origin.Y));
                    }

                }

                base.KillTile(x, y, type, ref fail, ref effectOnly, ref noItem);
            }

            public override void Drop(int i, int j, int type)
            {
                base.Drop(i, j, type);
            }
        }

        public class TileGengenMagic_TileSystem : ModSystem
        {
            public struct TileOriginPosition
            {
                public int TileType = -1;
                public int x = -1;
                public int y = -1;
                public int OriginX = -1;
                public int OriginY = -1;
                public int Alternate = -1;

                public TileOriginPosition(int x,int y, int originX,int originY, int tileType,int alternate)
                {
                    this.x = x;
                    this.y = y;
                    OriginX = originX;
                    OriginY = originY;
                    TileType = tileType;
                    Alternate = alternate;
                }
            }
            public static Dictionary<Vector2, TileOriginPosition> TileOriginPos = new Dictionary<Vector2, TileOriginPosition>();
            
        }
    }
}
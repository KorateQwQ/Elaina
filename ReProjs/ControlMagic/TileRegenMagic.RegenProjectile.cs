using System.Collections.Generic;
using System.IO;
using System.Linq;
using KL.Extensions;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace 伊蕾娜.ReProjs.ControlMagic;

public partial class TileRegenMagic : KLProjectile
{
    private List<Vector2> needToResetFramePos = new List<Vector2>();
    private bool canEnd = false;

    public override void SetDefaults()
    {
        Projectile.tileCollide = false;
        Projectile.friendly = false;
        Projectile.timeLeft = 3;
        
        base.SetDefaults();
    }

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
    }

    public override void AI()
    {
        if(canEnd)return;
        List<Item> itemList = new List<Item>();

        foreach (Item item in Main.item)
        {
            if (item is { active: true, IsAir: false } &&item.GetGlobalItem<TileRegenMagic_GlobalItem>()!=null&& item.GetGlobalItem<TileRegenMagic_GlobalItem>().ProtectTime > 0)
            {
                itemList.Add(item);
            }
        }
        //Main.NewText("Projectile time: "+Projectile.timeLeft);

        //itemList.Sort((a, b) => b.GetGlobalItem<TileRegenMagic_GlobalItem>().TilePosBeforeBreak.Y.CompareTo(a.GetGlobalItem<TileRegenMagic_GlobalItem>().TilePosBeforeBreak.Y));
        itemList = itemList
            .OrderByDescending(item => item.GetGlobalItem<TileRegenMagic_GlobalItem>().TilePosBeforeBreak.Y).ToList();
        foreach (var item in itemList)
        {
            //Main.NewText(item.Name);
            //Main.NewText(item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameX+" "+item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameY+" "+item.GetGlobalItem<TileRegenMagic_GlobalItem>().TilePosBeforeBreak+" ");
            Vector2 TilePosBeforeBreak = item.GetGlobalItem<TileRegenMagic_GlobalItem>().TilePosBeforeBreak;
            int tileForPlace = item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileToRegen >= 0
                ? item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileToRegen
                : item.createTile;
            int wallForPlace = item.GetGlobalItem<TileRegenMagic_GlobalItem>().WallToRegen;
            int placeStyle = item.GetGlobalItem<TileRegenMagic_GlobalItem>().placeStyle;
            short tileFrameX = item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameX;
            short tileFrameY = item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameY;
            int Alternate = item.GetGlobalItem<TileRegenMagic_GlobalItem>().PlaceAlternate;
            int x = (int)TilePosBeforeBreak.X;
            int y = (int)TilePosBeforeBreak.Y;

            if (wallForPlace > 0)
            {
                WorldGen.PlaceWall(x, y, wallForPlace);
                item.TurnToAir();
            }
            else if (item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameX >= 0 &&
                     item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameY >= 0)
            {
                //设置物块类型
                Main.tile[(int)TilePosBeforeBreak.X, (int)TilePosBeforeBreak.Y].TileType = (ushort)tileForPlace;

                //改变tileFrame
                if (tileFrameX >= 0) Main.tile[x, y].TileFrameX = tileFrameX;
                if (tileFrameY >= 0) Main.tile[x, y].TileFrameY = tileFrameY;

                Tile bottomTile = Framing.GetTileSafely(x, y + 1);
                if (bottomTile.HasTile)
                {
                    if (item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameX >= 0)
                        bottomTile.TileFrameX = item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameX;
                    if (item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameY >= 0)
                        bottomTile.TileFrameY = item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameY;
                }

                //改变半砖类型
                WorldGen.SlopeTile((int)TilePosBeforeBreak.X, (int)TilePosBeforeBreak.Y,
                    item.GetGlobalItem<TileRegenMagic_GlobalItem>().Tileslope);

                //强制激活
                Main.tile[(int)TilePosBeforeBreak.X, (int)TilePosBeforeBreak.Y].ForceChangeActice(true);
            }
            else
            {
                bool canTurnToAir = false;
                RegenTile(ref canTurnToAir, TilePosBeforeBreak, tileForPlace, placeStyle, Alternate, tileFrameX,
                    tileFrameY);
                if (canTurnToAir)
                {
                    item.TurnToAir();
                }
            }
        }

        foreach (var item in itemList)
        {
            if (!item.IsAir)
            {
                Vector2 TilePosBeforeBreak = item.GetGlobalItem<TileRegenMagic_GlobalItem>().TilePosBeforeBreak;
                int tileForPlace = item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileToRegen >= 0
                    ? item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileToRegen
                    : item.createTile;
                int placeStyle = item.GetGlobalItem<TileRegenMagic_GlobalItem>().placeStyle;

                int x = (int)TilePosBeforeBreak.X;
                int y = (int)TilePosBeforeBreak.Y;

                if (item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameX >= 0 &&
                    item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameY >= 0)
                {
                    //设置物块类型
                    Main.tile[x, y].TileType = (ushort)tileForPlace;

                    //改变tileFrame
                    Main.tile[x, y].TileFrameX = item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameX;
                    Main.tile[x, y].TileFrameY = item.GetGlobalItem<TileRegenMagic_GlobalItem>().TileFrameY;
                    Tile bottomTile = Framing.GetTileSafely(x, y + 1);
                    if (bottomTile.HasTile)
                    {
                        if (item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameX >= 0)
                            bottomTile.TileFrameX =
                                item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameX;
                        if (item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameY >= 0)
                            bottomTile.TileFrameY =
                                item.GetGlobalItem<TileRegenMagic_GlobalItem>().bottomTileFrameY;
                    }

                    //我就不发包，自动多端同步？
                    //NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1/*placeTile*/, x, y,tileForPlace, placeStyle);
                    //NetMessage.SendTileSquare(Main.myPlayer, x, y, 2, 2);
                    item.TurnToAir();
                }

                //同步附近物块帧
                needToResetFramePos.Add(new Vector2(x, y));
            }
        }

        canEnd = true;
        foreach (var item in itemList)
        {
            if (!item.IsAir)
            {
                canEnd = false;
                break;
            }
        }
        base.AI();
    }

    void RegenTile(ref bool canTurnToAir, Vector2 TilePosBeforeBreak, int tileForPlace, int placeStyle, int Alternate,
        short FrameX,
        short FrameY)
    {
        //Log($"RegenPosition: {TilePosBeforeBreak} tileType: {tileForPlace} placeStyle: {placeStyle} ");
        //Main.NewText($"RegenPosition: {TilePosBeforeBreak} tileType: {tileForPlace} placeStyle: {placeStyle} ");

        if (true /*Main.myPlayer == Projectile.owner*/)
        {
            int x = (int)TilePosBeforeBreak.X;
            int y = (int)TilePosBeforeBreak.Y;
            TileObjectData tileData = TileObjectData.GetTileData(tileForPlace, placeStyle);
            //TileObject tileObject = default(TileObject);
            int direction = 0;

            if (tileData == null) return;

            bool canPlace = TileObject.CanPlace(x + tileData.Origin.X, y + tileData.Origin.Y, (ushort)tileForPlace,
                placeStyle, direction, out TileObject tileObject, onlyCheck: false);
            
            tileObject.alternate = Alternate;

            bool placeSuccess = TileObject.Place(tileObject);
            if ( /*canPlace && */placeSuccess)
            {
                canTurnToAir = true;
                needToResetFramePos.Add(new Vector2(x + tileData.Origin.X, y + tileData.Origin.Y));
                TileObjectData.CallPostPlacementPlayerHook(x + tileData.Origin.X, y + tileData.Origin.Y, (ushort)tileForPlace, placeStyle, direction, tileObject.alternate, tileObject);

                //WorldGen.SquareTileFrame(x + tileData.Origin.X, y + tileData.Origin.Y);
                //TileObjectData.CallPostPlacementPlayerHook(x+tileData.Origin.X, y+tileData.Origin.Y, tileObject.type, tileObject.style,direction, tileObject.alternate, tileObject);

                /*if (Main.netMode == 1)
                {
                    NetMessage.SendObjectPlacement(-1, x + tileData.Origin.X, y + tileData.Origin.Y, tileObject.type,
                        tileObject.style, tileObject.alternate, tileObject.random, direction);
                }*/
            }

            /*Log("Projectile place tile alternate: "+Alternate);
            if (WorldGen.PlaceObject(x + tileData.Origin.X, y + tileData.Origin.Y, tileObject.type, false, placeStyle,
                    Alternate, tileObject.random)&&Main.netMode==NetmodeID.Server)
            {
                NetMessage.SendObjectPlacement(Main.myPlayer, x+tileData.Origin.X, y+tileData.Origin.Y, tileObject.type, tileObject.style, Alternate, tileObject.random, direction);
            }*/
        }

        /*//改变tileFrame
        if(FrameX>=0)Main.tile[x,y].TileFrameX = FrameX;
        if(FrameY>=0) Main.tile[x,y].TileFrameY = FrameY;*/

        //Main.NewText(FrameX+" "+FrameY);
        //WorldGen.SquareTileFrame(x,y,false);

        //NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1/*placeTile*/, x, y,tileForPlace, placeStyle);
    }


    public override void OnKill(int timeLeft)
    {
        foreach (Item item in Main.item)
        {
            if (item is { active: true, IsAir: false } &&
                item.GetGlobalItem<TileRegenMagic_GlobalItem>().ProtectTime > 0)
            {
                item.TurnToAir();
            }
        }

        foreach (Vector2 pos in needToResetFramePos)
        {
            WorldGen.SquareTileFrame((int)pos.X, (int)pos.Y);
        }
        /*if (Main.myPlayer == Projectile.owner && Projectile.ai[0] == 0)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<TileRegenMagic>(), 0, 0, Main.myPlayer, 1);
        }*/

        base.OnKill(timeLeft);
    }
}
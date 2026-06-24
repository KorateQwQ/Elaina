using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using System;
using Terraria.ID;

namespace 伊蕾娜.炼金
{
    public class 大锅Tile : GlobalTile
    {
        public override void MouseOver(int i, int j, int type)
        {
            if (Main.netMode != 2)
            {
                Tile tile = Main.tile[i, j];
                var player = Main.LocalPlayer;
                var p = player.GetModPlayer<炼金modplayer>();
                player.noThrow = 2;//不许鼠标丢掉物品
                player.cursorItemIconEnabled = true;
                if (type == TileID.CookingPots && tile.TileFrameX > 18)
                {
                    //p.使用提示 = true;
                    p.mouseOnSpot = true;
                }
                else
                {
                    p.使用提示 = false;
                    p.炼制提示 = false;
                }
            }

            base.MouseOver(i, j, type);
        }
        public override void MouseOverFar(int i, int j, int type)
        {

            base.MouseOverFar(i, j, type);
        }
        public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
        {

            return base.PreDraw(i, j, type, spriteBatch);
        }
        public override void DrawEffects(int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile tile = Main.tile[i, j];
            var player = Main.LocalPlayer;

            var p = player.GetModPlayer<炼金modplayer>();

            if (type == TileID.CookingPots && tile.TileFrameX > 18)
            {

            }
            else
            {
                p.使用提示 = false;
                p.炼制提示 = false;
            }
            if (Main.netMode != 2)
            {
                if (p.炸锅倒计时 > 0 && (p.TheSpotPosition == new Vector2(i, j) ||
                    p.TheSpotPosition == new Vector2(i - 1, j)
                    || p.TheSpotPosition == new Vector2(i, j - 1)
                    || p.TheSpotPosition == new Vector2(i - 1, j - 1)) && p.TheSpotUsingNow.TileType == type)//
                {
                    drawData.tileLight = Color.Lerp(Color.Red, Color.White, Math.Abs(Main.GameUpdateCount % 60 - 30f) / 30f);
                }
            }

            base.DrawEffects(i, j, type, spriteBatch, ref drawData);
        }
        public override void RightClick(int i, int j, int type)
        {
            //if (Main.netMode != 2)
            {
                Tile tile = Main.tile[i, j];
                int left = i;
                int top = j;
                var player = Main.LocalPlayer;
                var p = player.GetModPlayer<炼金modplayer>();

                //Main.NewText(ModContent.GetInstance<modifyScreen>().startModify);

                if (type == TileID.CookingPots && tile.TileFrameX > 18 && ModContent.GetInstance<modifyScreen>().startModify <= 1 && ModContent.GetInstance<modifyScreen>().endModify == 0 && (p.使用提示 || p.炼制提示))
                {

                    if (tile.TileFrameX % 36 != 0)
                    {
                        left--;
                    }

                    if (tile.TileFrameY != 0)
                    {
                        top--;
                    }
                    p.TheSpotUsingNow = Main.tile[left, top];
                    p.TheSpotPosition = new Vector2(left, top);
                    player.position = new Vector2(left * 16 - 8, top * 16 - 10);
                    player.CloseSign();
                    player.SetTalkNPC(-1);
                    player.tileInteractionHappened = true;
                    if (p.使用提示)
                    {
                        p.使用锅炉();

                    }
                    else if (p.炼制提示)
                    {
                        p.使用锅炉();
                    }

                    Main.mouseRightRelease = false;
                    Main.npcChatCornerItem = 0;
                    Main.playerInventory = true;//打开玩家背包

                }
            }
            
            base.RightClick(i, j, type);
        }
        
        public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch)
        {   
            base.PostDraw(i, j, type, spriteBatch);
        }
        public override void SetStaticDefaults()
        {   

            base.SetStaticDefaults();
        }
    }
}

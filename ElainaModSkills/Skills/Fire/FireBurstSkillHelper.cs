using System.Diagnostics.CodeAnalysis;
using KL.Extensions;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;
using NotImplementedException = System.NotImplementedException;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

public class FireBurstSkillHelper
{
    /// <summary>
    /// 从目标位置往下检测到第一个实体物块，如果未检测到
    /// </summary>
    /// <param name="validPosition"></param>
    /// <param name="checkDistance"></param>
    /// <returns></returns>
    public static bool GetValidPositionForFireBurst(ref Vector2 validPosition,float checkDistance = 2000f)
    {
        float y = validPosition.Y;
        float x = validPosition.X;
        for (int i = 0; i < checkDistance; i += 16)
        {
            Tile tile = Framing.GetTileSafely(new Vector2(x, y+i));
            if (IsSolidBrick(tile))
            {
                //Main.NewText(tile.IsSnowBiomeTile());
                validPosition.Y = y + i;
                return true;
            }
        }
        return false;
    }
    
    //检测是否为实心砖块
    public static bool IsSolidBrick(Tile tile)
    {
        if(tile == null)return false;
        if (tile.HasTile)
        {
            return Main.tileSolid[tile.TileType];
        }

        if (tile.LiquidAmount > 0)
        {
            return tile.LiquidType == LiquidID.Lava;
        }
        return false;
    }

    class FireLightSystem : ModSystem
    {
        public override void Load()
        { 
            On_TileDrawing.DrawTiles_GetLightOverride += OnDrawTiles_GetLightOverride;
            base.Load();
        }
        private Color OnDrawTiles_GetLightOverride(On_TileDrawing.orig_DrawTiles_GetLightOverride orig, TileDrawing self, int j, int i, Tile tileCache, ushort typeCache, short tileFrameX, short tileFrameY, Color tileLight)
        {
            Color result = orig(self, j, i, tileCache, typeCache, tileFrameX, tileFrameY, tileLight);
            if (result == Color.Black) result = new Color(1, 1, 1, 255);
            return result;
        }
        
        
    }
}
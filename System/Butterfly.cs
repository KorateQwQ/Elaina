using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System
{
    public class Butterfly : GlobalNPC
    {
        public override void AI(NPC npc)
        {
            if (npc.type == NPCID.Butterfly || npc.type == NPCID.HellButterfly && !npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
            {
                //if (Main.player[Main.myPlayer].GetModPlayer<ElainaModplayer>().Elaina)
                Player target = null;
                for (int i = 0; i < 5; i++)
                {
                    var p = Main.player[i];
                    float distance = Vector2.Distance(p.Center, npc.Center);
                    //npc.CountsAsACritter
                    if (p != null && p.active && p.GetModPlayer<ElainaModplayer>().Elaina && distance < 1000 && distance > 300)
                    {
                        target = p;
                        Vector2 v = Vector2.Normalize(p.Center - npc.Center) * 15;
                        npc.noTileCollide = true;
                        npc.velocity = (npc.velocity + v) / 15f;
                        float veffect = MathHelper.Lerp(1, 15, distance / 1200);
                        //npc.rotation = npc.velocity.ToRotation();
                        npc.velocity = Vector2.Normalize(npc.velocity) * veffect;
                        //Main.NewText(v+" "+npc.velocity);
                    }
                    else if (target == null)
                    {
                        npc.noTileCollide = false;
                    }
                }
            }
        }
    }
}

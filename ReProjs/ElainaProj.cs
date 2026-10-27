using System.Threading.Channels;
using Terraria.Audio;
using Terraria.ModLoader;
using 伊蕾娜.Items;

namespace 伊蕾娜.ReProjs
{
    public abstract class ElainaProj : ModProjectile
    {
        public static readonly int wand = ModContent.ItemType<ElainaWand>();
        public Player Owner => Main.player[Projectile.owner];
        public ElainaModplayer EMP => Owner.GetModPlayer<ElainaModplayer>();
        public EXPmodplayer EXP => Owner.GetModPlayer<EXPmodplayer>();
        public bool Channel => Owner.Channel();

        public bool LeftJustPress => Main.mouseLeft && Main.mouseLeftRelease;

        public void FrameCounter(int frame)
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > frame)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
        }
        public Color Pink => new Color(255, 160, 239, 255);
        public Color Wind => new Color(107, 250, 232, 255);
        public Color Water => new Color(30, 116, 224, 100);
        public Color Fire => new Color(255, 107, 42, 155);
        public Color Ice => new Color(178, 234, 255, 100);
        public Color Lightning => new Color(107, 250, 232, 255);

    }
}

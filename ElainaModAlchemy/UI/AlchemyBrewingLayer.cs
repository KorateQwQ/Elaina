using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Gameplay;

namespace 伊蕾娜.ElainaModAlchemy.UI;

public sealed class AlchemyBrewingLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Torso, PlayerDrawLayers.OffhandAcc);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
        !drawInfo.headOnlyRender && !drawInfo.drawPlayer.invis && !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetModPlayer<AlchemyBrewingPlayer>().Active;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        var player = drawInfo.drawPlayer.GetModPlayer<AlchemyBrewingPlayer>();
        var texture = Mod.Assets.Request<Texture2D>(AlchemyBrewAnimation.TexturePath(player.IsBrewing)).Value;
        var ground = AlchemyBrewAnimation.Ground(drawInfo.Position - Main.screenPosition,
            drawInfo.drawPlayer.width, drawInfo.drawPlayer.height, drawInfo.drawPlayer.HeightOffsetVisual);
        drawInfo.DrawDataCache.Add(new DrawData(texture,
            AlchemyBrewAnimation.BodyPosition(ground, player.IsBrewing),
            AlchemyBrewAnimation.Frame(player.IsBrewing, player.AnimationFrame), drawInfo.colorArmorBody,
            0, Vector2.Zero, 1f, SpriteEffects.None) { shader = drawInfo.drawPlayer.cBody });
    }
}

public sealed class AlchemyStoolLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.PortableStool, PlayerDrawLayers.ElectrifiedDebuffBack);
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
        !drawInfo.headOnlyRender && !drawInfo.drawPlayer.invis && !drawInfo.drawPlayer.dead &&
        drawInfo.drawPlayer.GetModPlayer<AlchemyBrewingPlayer>().IsBrewing;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        var texture = Terraria.GameContent.TextureAssets.Extra[102].Value;
        var ground = AlchemyBrewAnimation.Ground(drawInfo.Position - Main.screenPosition,
            drawInfo.drawPlayer.width, drawInfo.drawPlayer.height, drawInfo.drawPlayer.HeightOffsetVisual);
        drawInfo.DrawDataCache.Add(new DrawData(texture, AlchemyBrewAnimation.StoolPosition(ground),
            new Rectangle(0, 0, texture.Width, texture.Height), drawInfo.colorArmorLegs,
            0, Vector2.Zero, AlchemyBrewAnimation.StoolScale, SpriteEffects.None) { shader = drawInfo.cPortableStool });
    }
}

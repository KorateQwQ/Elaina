using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;

namespace 伊蕾娜.ElainaActions;

public class ArmDrawingFixer : ILoadable
{
    private static Vector2 FrontArmDrawOffset = new(0f, 0f);
    private static Vector2 FrontArmRotationCenterOffset = new Vector2(1,-2);
    private static Vector2 BackArmDrawOffset = new(0f, 0f);
    private static Vector2 BackArmRotationCenterOffset = new Vector2(-1, -2);
    private static readonly int[] FrontArmBodyFrameYOffset = new int[20]
    {
        0, // 0
        0, // 1
        0, // 2
        0, // 3
        0, // 4
        0, // 5
        0, // 6
        0, // 7
        0, // 8
        0, // 9
        -1, // 10
        0, // 11
        0, // 12
        -1, // 13
        0, // 14
        0, // 15
        -1, // 16
        0, // 17
        0, // 18
        -1, // 19
    };
    private static readonly int[] BackArmBodyFrameYOffset = new int[20]
    {
        0, // 0
        0, // 1
        0, // 2
        0, // 3
        0, // 4
        0, // 5
        0, // 6
        0, // 7
        0, // 8
        0, // 9
        -1, // 10
        0, // 11
        0, // 12
        -1, // 13
        0, // 14
        0, // 15
        -1, // 16
        0, // 17
        0, // 18
        -1, // 19
    };

    public void Load(Mod mod)
    {
        On_PlayerDrawLayers.DrawPlayer_28_ArmOverItem += On_PlayerDrawLayersOnDrawPlayer_28_ArmOverItem;
        On_PlayerDrawLayers.DrawPlayer_28_ArmOverItemComposite += On_PlayerDrawLayersOnDrawPlayer_28_ArmOverItemComposite;
        On_PlayerDrawLayers.DrawPlayer_12_SkinComposite_BackArmShirt += On_PlayerDrawLayersOnDrawPlayer_12_SkinComposite_BackArmShirt;
    }

    public void Unload()
    {
        On_PlayerDrawLayers.DrawPlayer_28_ArmOverItem -= On_PlayerDrawLayersOnDrawPlayer_28_ArmOverItem;
        On_PlayerDrawLayers.DrawPlayer_28_ArmOverItemComposite -= On_PlayerDrawLayersOnDrawPlayer_28_ArmOverItemComposite;
        On_PlayerDrawLayers.DrawPlayer_12_SkinComposite_BackArmShirt -= On_PlayerDrawLayersOnDrawPlayer_12_SkinComposite_BackArmShirt;
    }

    public static Vector2 GetFrontArmTotalOffset(Player player)
    {
        if (!ShouldFix(player))
        {
            return Vector2.Zero;
        }

        SpriteEffects playerEffect = GetPlayerEffect(player);
        return ApplySpriteEffects(UpdateFrontArmDrawOffset(player), playerEffect)
            + ApplySpriteEffects(FrontArmRotationCenterOffset, playerEffect);
    }

    public static Vector2 GetBackArmTotalOffset(Player player)
    {
        if (!ShouldFix(player))
        {
            return Vector2.Zero;
        }

        SpriteEffects playerEffect = GetPlayerEffect(player);
        return ApplySpriteEffects(UpdateBackArmDrawOffset(player), playerEffect)
            + ApplySpriteEffects(BackArmRotationCenterOffset, playerEffect);
    }

    public static (Vector2 FrontArmOffset, Vector2 BackArmOffset) GetArmTotalOffsets(Player player)
    {
        if (!ShouldFix(player))
        {
            return (Vector2.Zero, Vector2.Zero);
        }

        SpriteEffects playerEffect = GetPlayerEffect(player);
        return (
            ApplySpriteEffects(UpdateFrontArmDrawOffset(player), playerEffect) + ApplySpriteEffects(FrontArmRotationCenterOffset, playerEffect),
            ApplySpriteEffects(UpdateBackArmDrawOffset(player), playerEffect) + ApplySpriteEffects(BackArmRotationCenterOffset, playerEffect));
    }

    private void On_PlayerDrawLayersOnDrawPlayer_28_ArmOverItem(On_PlayerDrawLayers.orig_DrawPlayer_28_ArmOverItem orig, ref PlayerDrawSet drawinfo)
    {
        if (!ShouldFix(drawinfo)||drawinfo.drawPlayer.compositeFrontArm.enabled)
        {
            orig.Invoke(ref drawinfo);
            return;
        }

        Vector2 frontArmDrawOffset = UpdateFrontArmDrawOffset(drawinfo);
        Vector2 drawOffset = ApplySpriteEffects(frontArmDrawOffset, drawinfo.playerEffect);
        Vector2 rotationCenterOffset = ApplySpriteEffects(FrontArmRotationCenterOffset, drawinfo.playerEffect);
        Vector2 oldBodyPosition = drawinfo.drawPlayer.bodyPosition;
        Vector2 oldBodyVect = drawinfo.bodyVect;

        drawinfo.drawPlayer.bodyPosition += drawOffset + rotationCenterOffset;
        drawinfo.bodyVect += rotationCenterOffset;

        try
        {
            orig.Invoke(ref drawinfo);
        }
        finally
        {
            drawinfo.drawPlayer.bodyPosition = oldBodyPosition;
            drawinfo.bodyVect = oldBodyVect;
        }
    }

    private void On_PlayerDrawLayersOnDrawPlayer_28_ArmOverItemComposite(On_PlayerDrawLayers.orig_DrawPlayer_28_ArmOverItemComposite orig, ref PlayerDrawSet drawinfo)
    {
        Vector2 frontArmDrawOffset = UpdateFrontArmDrawOffset(drawinfo);
        int drawDataStartIndex = drawinfo.DrawDataCache.Count;

        orig.Invoke(ref drawinfo);

        if (!ShouldFix(drawinfo) || !drawinfo.drawPlayer.compositeFrontArm.enabled)
        {
            return;
        }

        OffsetCompositeFrontArmDrawData(
            ref drawinfo,
            drawDataStartIndex,
            ApplySpriteEffects(frontArmDrawOffset, drawinfo.playerEffect),
            ApplySpriteEffects(FrontArmRotationCenterOffset, drawinfo.playerEffect));
    }

    private void On_PlayerDrawLayersOnDrawPlayer_12_SkinComposite_BackArmShirt(On_PlayerDrawLayers.orig_DrawPlayer_12_SkinComposite_BackArmShirt orig, ref PlayerDrawSet drawinfo)
    {
        Vector2 backArmDrawOffset = UpdateBackArmDrawOffset(drawinfo);
        int drawDataStartIndex = drawinfo.DrawDataCache.Count;

        orig.Invoke(ref drawinfo);

        if (!ShouldFix(drawinfo) || !drawinfo.drawPlayer.compositeBackArm.enabled)
        {
            return;
        }

        OffsetCompositeBackArmDrawData(
            ref drawinfo,
            drawDataStartIndex,
            ApplySpriteEffects(backArmDrawOffset, drawinfo.playerEffect),
            ApplySpriteEffects(BackArmRotationCenterOffset, drawinfo.playerEffect));
    }

    private static Vector2 UpdateFrontArmDrawOffset(PlayerDrawSet drawinfo)
    {
        Vector2 frontArmDrawOffset = FrontArmDrawOffset;
        int bodyFrameIndex = drawinfo.drawPlayer.bodyFrame.Y / drawinfo.drawPlayer.bodyFrame.Height;
        if (bodyFrameIndex >= 7 && bodyFrameIndex < FrontArmBodyFrameYOffset.Length)
        {
            frontArmDrawOffset.X += 3;
            Vector2 vanillaDrawOffset = Main.OffsetsPlayerHeadgear[bodyFrameIndex];
            vanillaDrawOffset.Y -= 2f;
            frontArmDrawOffset -= vanillaDrawOffset;
            frontArmDrawOffset.Y += FrontArmBodyFrameYOffset[bodyFrameIndex];
            //rintText(FrontArmBodyFrameYOffset[bodyFrameIndex]);
        }

        return frontArmDrawOffset;
    }

    private static Vector2 UpdateFrontArmDrawOffset(Player player)
    {
        Vector2 frontArmDrawOffset = FrontArmDrawOffset;
        int bodyFrameIndex = player.bodyFrame.Y / player.bodyFrame.Height;
        if (bodyFrameIndex >= 7 && bodyFrameIndex < FrontArmBodyFrameYOffset.Length)
        {
            frontArmDrawOffset.X += 3;
            Vector2 vanillaDrawOffset = Main.OffsetsPlayerHeadgear[bodyFrameIndex];
            vanillaDrawOffset.Y -= 2f;
            frontArmDrawOffset -= vanillaDrawOffset;
            frontArmDrawOffset.Y += FrontArmBodyFrameYOffset[bodyFrameIndex];
        }

        return frontArmDrawOffset;
    }

    private static Vector2 UpdateBackArmDrawOffset(PlayerDrawSet drawinfo)
    {
        Vector2 backArmDrawOffset = BackArmDrawOffset;

        int bodyFrameIndex = drawinfo.drawPlayer.bodyFrame.Y / drawinfo.drawPlayer.bodyFrame.Height;
        if (bodyFrameIndex >= 7 && bodyFrameIndex < BackArmBodyFrameYOffset.Length)
        {
            backArmDrawOffset.X += 3;
            Vector2 vanillaDrawOffset = Main.OffsetsPlayerHeadgear[bodyFrameIndex];
            vanillaDrawOffset.Y -= 2f;
            backArmDrawOffset -= vanillaDrawOffset;
            backArmDrawOffset.Y += BackArmBodyFrameYOffset[bodyFrameIndex];
        }

        return backArmDrawOffset;
    }

    private static Vector2 UpdateBackArmDrawOffset(Player player)
    {
        Vector2 backArmDrawOffset = BackArmDrawOffset;

        int bodyFrameIndex = player.bodyFrame.Y / player.bodyFrame.Height;
        if (bodyFrameIndex >= 7 && bodyFrameIndex < BackArmBodyFrameYOffset.Length)
        {
            backArmDrawOffset.X += 3;
            Vector2 vanillaDrawOffset = Main.OffsetsPlayerHeadgear[bodyFrameIndex];
            vanillaDrawOffset.Y -= 2f;
            backArmDrawOffset -= vanillaDrawOffset;
            backArmDrawOffset.Y += BackArmBodyFrameYOffset[bodyFrameIndex];
        }

        return backArmDrawOffset;
    }

    private static void OffsetCompositeFrontArmDrawData(ref PlayerDrawSet drawinfo, int drawDataStartIndex, Vector2 drawOffset, Vector2 rotationCenterOffset)
    {
        for (int i = drawDataStartIndex; i < drawinfo.DrawDataCache.Count; i++)
        {
            DrawData drawData = drawinfo.DrawDataCache[i];
            if (!drawData.sourceRect.HasValue || !drawData.sourceRect.Value.Equals(drawinfo.compFrontArmFrame))
            {
                continue;
            }

            drawData.position += drawOffset + rotationCenterOffset;
            drawData.origin += rotationCenterOffset;
            drawinfo.DrawDataCache[i] = drawData;
        }
    }

    private static void OffsetCompositeBackArmDrawData(ref PlayerDrawSet drawinfo, int drawDataStartIndex, Vector2 drawOffset, Vector2 rotationCenterOffset)
    {
        for (int i = drawDataStartIndex; i < drawinfo.DrawDataCache.Count; i++)
        {
            DrawData drawData = drawinfo.DrawDataCache[i];
            if (!drawData.sourceRect.HasValue || !drawData.sourceRect.Value.Equals(drawinfo.compBackArmFrame))
            {
                continue;
            }

            drawData.position += drawOffset + rotationCenterOffset;
            drawData.origin += rotationCenterOffset;
            drawinfo.DrawDataCache[i] = drawData;
        }
    }

    private static bool ShouldFix(PlayerDrawSet drawinfo)
    {
        return drawinfo.drawPlayer.GetModPlayer<ElainaModplayer>().Elaina;
    }

    private static bool ShouldFix(Player player)
    {
        return player.GetModPlayer<ElainaModplayer>().Elaina;
    }

    private static SpriteEffects GetPlayerEffect(Player player)
    {
        SpriteEffects playerEffect = SpriteEffects.None;
        if (player.direction == -1)
        {
            playerEffect |= SpriteEffects.FlipHorizontally;
        }

        if (player.gravDir == -1f)
        {
            playerEffect |= SpriteEffects.FlipVertically;
        }

        return playerEffect;
    }

    private static Vector2 ApplySpriteEffects(Vector2 offset, SpriteEffects playerEffect)
    {
        if (playerEffect.HasFlag(SpriteEffects.FlipHorizontally))
        {
            offset.X *= -1f;
        }

        if (playerEffect.HasFlag(SpriteEffects.FlipVertically))
        {
            offset.Y *= -1f;
        }

        return offset;
    }
}
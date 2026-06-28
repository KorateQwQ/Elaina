using System;
using KL.Drawing;
using KL.Extensions;
using KL.Utils;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaActions;

public static class ElainaTrailVisualStyle
{
    private static Texture2D defaultTrailTexture;
    private static Texture2D defaultStarLineTexture;
    private static Texture2D defaultStarCrossTexture;

    public static Color DefaultWandTrailColor = new(255, 160, 239, 0);
    public static Color DefaultSlashTrailColor = new(255, 100, 255, 0);
    public static Color DefaultStarColor = new(255, 100, 255, 0);
    public static float DefaultTrailMaxWidth = 5f;
    public static float DefaultTrailEndWidth = 0f;
    public static float DefaultTrailFadeInEnd = 0.2f;
    public static float DefaultTrailFadeOutStart = 0.8f;
    public static float DefaultStarFadeInEnd = 0.2f;
    public static float DefaultStarFadeOutStart = 0.8f;
    public static float DefaultStarBlinkMin = 0.5f;
    public static float DefaultStarBlinkMax = 1f;
    public static float DefaultStarBlinkCycle = 60f;
    public static Vector2 DefaultStarCrossScale = new(0.05f, 0.03f);
    public static Vector2 DefaultStarLineScale = new(0.2f, 0.2f);
    public static Vector2 DefaultStarLineScale2 = new(0.1f, 0.2f);
    public static float DefaultStarLineRotation = MathHelper.PiOver2;

    public static Texture2D GetTrailTexture(Texture2D texture = null)
    {
        if (texture != null)
        {
            return texture;
        }

        defaultTrailTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LightTrail", AssetRequestMode.ImmediateLoad).Value;
        return defaultTrailTexture;
    }

    public static Texture2D GetStarLineTexture(Texture2D texture = null)
    {
        if (texture != null)
        {
            return texture;
        }

        defaultStarLineTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLine", AssetRequestMode.ImmediateLoad).Value;
        return defaultStarLineTexture;
    }

    public static Texture2D GetStarCrossTexture(Texture2D texture = null)
    {
        if (texture != null)
        {
            return texture;
        }

        defaultStarCrossTexture ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/T_StartFlare001", AssetRequestMode.ImmediateLoad).Value;
        return defaultStarCrossTexture;
    }

    public static float GetActionAlpha(float actionProgress, float? fadeInEnd = null, float? fadeOutStart = null)
    {
        float fadeIn = fadeInEnd ?? DefaultTrailFadeInEnd;
        float fadeOut = fadeOutStart ?? DefaultTrailFadeOutStart;
        float alpha = 1f;
        if (fadeIn > 0f && actionProgress < fadeIn)
        {
            alpha = KLMathF.ClampLerp(0f, 1f, actionProgress / fadeIn);
        }

        if (fadeOut < 1f && actionProgress > fadeOut)
        {
            alpha = KLMathF.ClampLerp(1f, 0f, (actionProgress - fadeOut) / (1f - fadeOut));
        }

        return alpha;
    }

    public static void DrawTrail(Vector2[] points, Vector2 attachPoint, float attachRotation, float actionProgress,
        Color trailColor, float trailMaxWidth, float trailEndWidth, float? fadeInEnd = null, float? fadeOutStart = null,
        Texture2D texture = null, bool debugPoint = false)
    {
        texture = GetTrailTexture(texture);
        if (texture == null || points == null || points.Length < 2)
        {
            return;
        }

        float totalAlpha = GetActionAlpha(actionProgress, fadeInEnd, fadeOutStart);
        TrailEffect(texture, points, trailColor * totalAlpha, trailColor * 0f, trailMaxWidth, trailEndWidth,
            attachPoint: attachPoint, attachRotation: attachRotation, debugPoint: debugPoint);
    }

    public static void DrawWandTrail(Vector2[] points, float actionProgress)
    {
        DrawTrail(points, Vector2.Zero, 0f, actionProgress, DefaultWandTrailColor, DefaultTrailMaxWidth,
            DefaultTrailEndWidth, DefaultTrailFadeInEnd, DefaultTrailFadeOutStart);
    }

    public static void DrawSlashTrail(Vector2[] points, Vector2 attachPoint, float attachRotation, float actionProgress,
        bool debugPoint = false)
    {
        DrawTrail(points, attachPoint, attachRotation, actionProgress, DefaultSlashTrailColor, DefaultTrailMaxWidth,
            DefaultTrailEndWidth, DefaultTrailFadeInEnd, DefaultTrailFadeOutStart, debugPoint: debugPoint);
    }

    public static void DrawDefaultStar(ref PlayerDrawSet drawInfo, Vector2 starCenter, float actionProgress)
    {
        DrawStar(ref drawInfo, starCenter, actionProgress, DefaultStarColor, DefaultStarFadeInEnd,
            DefaultStarFadeOutStart, DefaultStarBlinkMin, DefaultStarBlinkMax, DefaultStarBlinkCycle,
            DefaultStarCrossScale, DefaultStarLineScale, DefaultStarLineScale2, DefaultStarLineRotation);
    }

    public static void DrawStar(ref PlayerDrawSet drawInfo, Vector2 starCenter, float actionProgress,
        Color starColor, float? fadeInEnd = null, float? fadeOutStart = null,
        float? blinkMin = null, float? blinkMax = null, float? blinkCycle = null,
        Vector2? crossScale = null, Vector2? lineScale = null, Vector2? lineScale2 = null,
        float? lineRotation = null, Texture2D crossTexture = null, Texture2D lineTexture = null)
    {
        crossTexture = GetStarCrossTexture(crossTexture);
        lineTexture = GetStarLineTexture(lineTexture);
        if (crossTexture == null || lineTexture == null)
        {
            return;
        }

        crossScale ??= DefaultStarCrossScale;
        lineScale ??= DefaultStarLineScale;
        lineScale2 ??= DefaultStarLineScale2;

        float totalAlpha = GetActionAlpha(actionProgress, fadeInEnd, fadeOutStart);
        float blinkAlpha = PingPongWave(blinkMin ?? DefaultStarBlinkMin, blinkMax ?? DefaultStarBlinkMax,
            blinkCycle ?? DefaultStarBlinkCycle, Main.timeForVisualEffects, PingPongWaveType.SmoothStep);
        Color color = starColor * totalAlpha * blinkAlpha;

        drawInfo.DrawDataCache.Add(new DrawData(
            crossTexture,
            starCenter - Main.screenPosition,
            null,
            color,
            0f,
            crossTexture.Origin(),
            crossScale.Value,
            SpriteEffects.None,
            0)
        {
            ignorePlayerRotation = true
        });

        drawInfo.DrawDataCache.Add(new DrawData(
            lineTexture,
            starCenter - Main.screenPosition,
            null,
            color * 0.5f,
            0f,
            lineTexture.Origin(),
            lineScale.Value,
            SpriteEffects.None,
            0)
        {
            ignorePlayerRotation = true
        });

        drawInfo.DrawDataCache.Add(new DrawData(
            lineTexture,
            starCenter - Main.screenPosition,
            null,
            color * 0.5f,
            lineRotation ?? DefaultStarLineRotation,
            lineTexture.Origin(),
            lineScale2.Value,
            SpriteEffects.None,
            0)
        {
            ignorePlayerRotation = true
        });
    }
}

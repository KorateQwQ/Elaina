using System;
using System.Collections.Generic;
using KL.Projectiles;
using KL.Drawing;
using KL.Utils;
namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

internal sealed class ShardbindTrail
{
    TrailManager.TrailHandle managedTrail;
    (Texture2D Texture, float TimeOffset, float WidthScale) lastTrailState;
    bool foreground;
    LayerDrawRequestSystem.DrawTargetLayer Layer => foreground ? LayerDrawRequestSystem.DrawTargetLayer.OverPlayers : LayerDrawRequestSystem.DrawTargetLayer.BehindNPCsAndTiles;
    LayerDrawRequestSystem.DrawTiming Timing => foreground ? LayerDrawRequestSystem.DrawTiming.Before : LayerDrawRequestSystem.DrawTiming.After;
    bool released;

    public void Advance(Vector2 tail, bool inForeground, float perspective, int time, bool arrived, bool sampleOnly)
    {
        if (arrived)
        {
            // 晚到的网络状态只重建历史，不重新发射已结束的拖尾或命中效果。
            if (sampleOnly) { released = true; return; }
            if (OldCenter == null || OldCenter.Length < 2)
                throw new InvalidOperationException("命中时缺少冰锥术飞行拖尾。");
            ConnectTrailToBreakPoint(tail);
            released = true;
            return;
        }
        if (released) return;
        foreground = inForeground;
        TrailSampleCount = 50;
        TrailMaxLength = 500;
        RecordDistanceLimitedTrail(tail);
        if (time == 24 && OldCenter.Length != 1)
            throw new InvalidOperationException("发射起点混入了后撤轨迹。");
        if (time < 25 || !ValidTrailArray()) return;
        lastTrailState = (AssetManager.GetTexture("KL.Effects.Tex.Trail.streak_801_4r"),
            -(float)time % 1200 / 40, perspective);
        if (sampleOnly) return;
        TrailManager.CreateOrUpdateTrail(ref managedTrail, OldCenter, TrailLifetime, lastTrailState,
            DrawManagedTrail, layer: Layer, timing: Timing, bloom: foreground);
    }

    Vector2[] OldCenter;
    readonly List<Vector2> distanceLimitedTrailPositions = new();
    int TrailSampleCount;
    float TrailMaxLength;
    private static float TrailWidth => 5f;
    const int TrailLifetime = 10, BreakFadeTime = 4;
    private bool ValidTrailArray() => OldCenter is { Length: >= 2 };
    private void RecordDistanceLimitedTrail(Vector2 center)
    {
        if (distanceLimitedTrailPositions.Count == 0 ||
            Vector2.DistanceSquared(distanceLimitedTrailPositions[0], center) > 0.01f)
        {
            distanceLimitedTrailPositions.Insert(0, center);
        }

        float remainingLength = TrailMaxLength;
        for (int i = 1; i < distanceLimitedTrailPositions.Count; i++)
        {
            float segmentLength = Vector2.Distance(distanceLimitedTrailPositions[i - 1], distanceLimitedTrailPositions[i]);
            if (segmentLength <= remainingLength)
            {
                remainingLength -= segmentLength;
                continue;
            }

            float segmentProgress = segmentLength <= 0f ? 0f : remainingLength / segmentLength;
            distanceLimitedTrailPositions[i] = Vector2.Lerp(
                distanceLimitedTrailPositions[i - 1], distanceLimitedTrailPositions[i], segmentProgress);
            distanceLimitedTrailPositions.RemoveRange(i + 1, distanceLimitedTrailPositions.Count - i - 1);
            break;
        }

        float pathLength = 0f;
        for (int i = 1; i < distanceLimitedTrailPositions.Count; i++)
            pathLength += Vector2.Distance(distanceLimitedTrailPositions[i - 1], distanceLimitedTrailPositions[i]);

        int outputCount = pathLength <= 0.01f ? 1 : Math.Max(1, TrailSampleCount);
        OldCenter = new Vector2[outputCount];
        for (int sample = 0; sample < outputCount; sample++)
        {
            float targetDistance = outputCount == 1 ? 0f : pathLength * sample / (outputCount - 1f);
            float traversed = 0f;
            OldCenter[sample] = distanceLimitedTrailPositions[0];
            for (int i = 1; i < distanceLimitedTrailPositions.Count; i++)
            {
                float segmentLength = Vector2.Distance(distanceLimitedTrailPositions[i - 1], distanceLimitedTrailPositions[i]);
                if (traversed + segmentLength >= targetDistance)
                {
                    float segmentProgress = segmentLength <= 0f ? 0f : (targetDistance - traversed) / segmentLength;
                    OldCenter[sample] = Vector2.Lerp(distanceLimitedTrailPositions[i - 1], distanceLimitedTrailPositions[i], segmentProgress);
                    break;
                }
                traversed += segmentLength;
                OldCenter[sample] = distanceLimitedTrailPositions[i];
            }
        }
    }
    private static void DrawManagedTrail((Texture2D Texture, float TimeOffset, float WidthScale) state, Vector2[] points, float remainingRatio)
    {
        DrawTrail(state, points, 1f);
    }
    private static void DrawTrail((Texture2D Texture, float TimeOffset, float WidthScale) state, Vector2[] points, float fade)
    {
        if (points.Length < 2 || fade <= 0f)
            return;

        TrailEffect(state.Texture, points, new Vector4(new Color(200,220,255,255).ToVector3()*2,fade),  new Vector4(new Color(200,220,255,255).ToVector3()*1,fade),
            endAlpha:1,maxWidth:TrailWidth * fade * state.WidthScale,endWidth:0,blendState:2,debugPoint:false,imageScale:new Vector2(0.5f,1),
            uTime:new Vector2(state.TimeOffset,0));
    }
    private static void DrawReleasedTrail((Texture2D Texture, float TimeOffset, float WidthScale) state, Vector2[] points, float remainingRatio)
    {
        float age = (1f - remainingRatio) * TrailLifetime;
        float fade = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(1f - age / BreakFadeTime, 0f, 1f));
        DrawTrail(state, points, fade);
    }
    private static void DrawBreakConnection((Texture2D Texture, float TimeOffset, float WidthScale) state, Vector2[] points, float remainingRatio)
    {
        DrawTrail(state, points, MathHelper.SmoothStep(0f, 1f, remainingRatio));
    }
    private void ConnectTrailToBreakPoint(Vector2 impactTail)
    {
        Vector2 head = OldCenter[0];
        Vector2 delta = impactTail - head;
        float distance = delta.Length();
        TrailManager.CreateOrUpdateTrail(ref managedTrail, OldCenter, TrailLifetime,
            lastTrailState, DrawReleasedTrail,
            layer: Layer, timing: Timing, bloom: foreground);

        if (distance < 0.5f)
            return;

        Vector2 tangent = OldCenter[0] - OldCenter[1];
        if (tangent.LengthSquared() < 0.001f || Vector2.Dot(tangent, delta) <= 0f)
            tangent = delta;
        tangent = Vector2.Normalize(tangent) * distance;
        var connection = new Vector2[12];
        for (int i = 0; i < connection.Length; i++)
            connection[i] = Vector2.Hermite(head, tangent, impactTail, delta, i / (float)(connection.Length - 1));

        // 只连接到命中当帧的尾尖，保留之前各帧的实际轨迹。
        TrailManager.TrailHandle connectionHandle = null;
        TrailManager.CreateOrUpdateTrail(ref connectionHandle, connection, BreakFadeTime,
            lastTrailState, DrawBreakConnection, layer: Layer, timing: Timing,
            bloom: foreground, shrinkPathOnExpire: false);
    }
}

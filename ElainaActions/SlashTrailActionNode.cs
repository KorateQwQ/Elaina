using System;
using System.Collections.Generic;
using KL.ActionsSystem;
using KL.Extensions;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaActions;

public class SlashTrailActionNode : ElainaNode
{
    public class ProgressKey
    {
        public float Progress;
        public float Value;

        public ProgressKey()
        {
        }

        /// <summary>
        /// 节点的进度映射，value超过1时拖尾可以进行回收，
        /// </summary>
        /// <param name="progress">动</param>
        /// <param name="value"></param>
        public ProgressKey(float progress, float value)
        {
            Progress = progress;
            Value = value;
        }
    }

    public class SlashTrailSegment
    {
        public float StartProgress;
        public float EndProgress = 1f;
        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public Vector2 ControlPointA;
        public Vector2 ControlPointB;
        public float TrailLength = 0.8f;
        public float MaxProgress = 1f;
        /// <summary>
        /// 回收拖尾的时间，value超过1的部分会用于回收拖尾，如果EndRecoverDuration为0则代表瞬间回收拖尾。
        /// </summary>
        public float EndRecoverDuration = 1f;
        public bool MirrorByOwnerDirection = true;
        public List<ProgressKey> ProgressMap = new();

        public bool Contains(float progress)
        {
            return progress >= StartProgress && progress <= EndProgress;
        }

        public float GetLocalProgress(float progress)
        {
            float duration = EndProgress - StartProgress;
            if (duration <= 0f)
            {
                return 1f;
            }

            float localProgress = MathHelper.Clamp((progress - StartProgress) / duration, 0f, 1f);
            if (ProgressMap.Count == 0)
            {
                return localProgress * MaxProgress;
            }

            if (localProgress <= ProgressMap[0].Progress)
            {
                return ProgressMap[0].Value;
            }

            for (int i = 1; i < ProgressMap.Count; i++)
            {
                ProgressKey previous = ProgressMap[i - 1];
                ProgressKey current = ProgressMap[i];
                if (localProgress > current.Progress)
                {
                    continue;
                }

                float keyDuration = current.Progress - previous.Progress;
                if (keyDuration <= 0f)
                {
                    return current.Value;
                }

                float keyProgress = (localProgress - previous.Progress) / keyDuration;
                return MathHelper.Lerp(previous.Value, current.Value, keyProgress);
            }

            return ProgressMap[^1].Value;
        }
    }

    public int PointCount = 50;
    public ActionDrawLayerType DrawLayer = ActionDrawLayerType.OverPlayer;
    public bool DrawStar = true;
    public bool DebugPoint = false;

    public SlashTrailSegment[] Segments;
    private Vector2[] slashTrailPositions = Array.Empty<Vector2>();
    private Vector2[] slashTrailDrawPositions = Array.Empty<Vector2>();

    public SlashTrailActionNode(int startFrame, int endFrame, params SlashTrailSegment[] segments) : base(startFrame, endFrame)
    {
        Segments = segments is { Length: > 0 } ? segments : CreateDefaultSegments();
    }

    public SlashTrailActionNode SetSegments(params SlashTrailSegment[] newSegments)
    {
        if (newSegments is { Length: > 0 })
        {
            Segments = newSegments;
        }

        return this;
    }

    public override void Draw(ActionModPlayer actionPlayer, ref PlayerDrawSet drawInfo, int actionFrame, float nodeProgress, ActionDrawLayerType drawLayerType)
    {
        if (!IsTargetLayer(drawLayerType, DrawLayer))
        {
            return;
        }

        SlashTrailSegment segment = GetCurrentSegment(nodeProgress);
        Player owner = GetOwner(actionPlayer);
        EnsureTrailArrays();
        BuildSegmentTrail(owner, segment, nodeProgress);

        ElainaTrailVisualStyle.DrawSlashTrail(GetSlashTrailDrawPositions(owner), owner.MountedCenter,
            owner.fullRotation, nodeProgress, DebugPoint);

        RestoreActionDrawBatch();
        if (DrawStar)
        {
            ElainaTrailVisualStyle.DrawDefaultStar(ref drawInfo, GetStarCenter(owner).Floor(), nodeProgress);
        }
    }

    private SlashTrailSegment GetCurrentSegment(float nodeProgress)
    {
        if (Segments == null || Segments.Length == 0)
        {
            Segments = CreateDefaultSegments();
        }

        for (int i = 0; i < Segments.Length; i++)
        {
            if (Segments[i].Contains(nodeProgress))
            {
                return Segments[i];
            }
        }

        return nodeProgress < Segments[0].StartProgress ? Segments[0] : Segments[^1];
    }

    private void EnsureTrailArrays()
    {
        int pointCount = Math.Max(2, PointCount);
        if (slashTrailPositions.Length == pointCount)
        {
            return;
        }

        slashTrailPositions = new Vector2[pointCount];
        slashTrailDrawPositions = new Vector2[pointCount];
    }

    private void BuildSegmentTrail(Player owner, SlashTrailSegment segment, float nodeProgress)
    {
        float segmentProgress = segment.GetLocalProgress(nodeProgress);
        float endProgress = 0f;
        if (segmentProgress > 1f)
        {
            endProgress = segmentProgress - 1f;
            if (segment.EndRecoverDuration > 0f)
            {
                endProgress /= segment.EndRecoverDuration;
            }

            endProgress = MathHelper.Clamp(endProgress, 0f, 1f);
            segmentProgress = 1f;
        }

        float headProgress = MathHelper.Clamp(BezierEase(segmentProgress), 0f, 1f);
        float trailLength = segment.TrailLength * (1f - endProgress);

        Vector2 startPosition = ApplyDirection(owner, segment, segment.StartPosition);
        Vector2 endPosition = ApplyDirection(owner, segment, segment.EndPosition);
        Vector2 pointA = ApplyDirection(owner, segment, segment.ControlPointA);
        Vector2 pointB = ApplyDirection(owner, segment, segment.ControlPointB);

        for (int i = 0; i < slashTrailPositions.Length; i++)
        {
            float trailProgress = i / (slashTrailPositions.Length - 1f);
            float pointProgress = MathHelper.Clamp(headProgress - trailLength * trailProgress, 0f, 1f);
            slashTrailPositions[i] = GetBezierPoint(pointProgress, startPosition, endPosition, pointA, pointB);
        }

        if (DebugPoint)
        {
            DrawDebugPoint(owner, startPosition, endPosition, pointA, pointB);
        }
    }

    private static Vector2 ApplyDirection(Player owner, SlashTrailSegment segment, Vector2 position)
    {
        if (segment.MirrorByOwnerDirection)
        {
            position.X *= owner.direction;
        }

        return position;
    }

    private static float BezierEase(float progress)
    {
        return progress * progress * (3f - 2f * progress);
    }

    private static Vector2 GetBezierPoint(float progress, Vector2 startPosition, Vector2 endPosition,
        Vector2 controlPointA, Vector2 controlPointB)
    {
        float remainingProgress = 1f - progress;
        return remainingProgress * remainingProgress * remainingProgress * startPosition
            + 3f * remainingProgress * remainingProgress * progress * controlPointA
            + 3f * remainingProgress * progress * progress * controlPointB
            + progress * progress * progress * endPosition;
    }

    private Vector2[] GetSlashTrailDrawPositions(Player owner)
    {
        for (int i = 0; i < slashTrailPositions.Length; i++)
        {
            slashTrailDrawPositions[i] = GetLocalDrawOffset(owner, slashTrailPositions[i]);
        }

        return slashTrailDrawPositions;
    }

    private Vector2 GetStarCenter(Player owner)
    {
        if (slashTrailPositions.Length == 0)
        {
            return GetWandCenter(owner);
        }

        return GetMountedLocalDrawPosition(owner, slashTrailPositions[0]);
    }

    private void DrawDebugPoint(Player owner, Vector2 startPosition, Vector2 endPosition, Vector2 pointA, Vector2 pointB)
    {
        Vector2[] previewPoints = new Vector2[slashTrailPositions.Length];
        for (int i = 0; i < previewPoints.Length; i++)
        {
            float previewProgress = i / (previewPoints.Length - 1f);
            previewPoints[i] = GetLocalDrawOffset(owner, GetBezierPoint(previewProgress, startPosition, endPosition, pointA, pointB));
        }

        for (int i = 0; i < previewPoints.Length; i++)
        {
            Vector2 normalDir = i < previewPoints.Length - 1
                ? previewPoints[i] - previewPoints[i + 1]
                : previewPoints[i - 1] - previewPoints[i];
            normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                owner.MountedCenter + previewPoints[i] + normalDir - Main.screenPosition,
                new Rectangle(0, 0, 1, 1), Color.Red, 0f, new Vector2(0.5f, 0.5f), 1f, SpriteEffects.None, 0f);
        }
    }

    public static SlashTrailSegment[] CreateDefaultSegments()
    {
        return new[]
        {
            new SlashTrailSegment
            {
                StartProgress = 0f,
                EndProgress = 1f,
                StartPosition = new Vector2(0f, 0f),
                EndPosition = new Vector2(40f, 0f),
                ControlPointA = new Vector2(13f, 0f),
                ControlPointB = new Vector2(27f, 0f),
                TrailLength = 0.8f,
                MaxProgress = 1f,
                EndRecoverDuration = 1f
            }
        };
    }
}

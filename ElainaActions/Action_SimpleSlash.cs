using System;
using KL.ActionsSystem;
using KL.Extensions;
using KL.Utils;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaActions;

public class Action_SimpleSlash : ElainaAction
{
    public override bool UseItemTime => true;

    protected readonly Vector2[] slashTrailPositions;
    protected readonly Vector2[] slashTrailDrawPositions;
    protected override bool DrawWandTrail => false;

    protected override Vector2 StarCenter
    {
        get
        {
            if (slashTrailPositions.Length > 0)
            {
                return GetPlayerLocalDrawOffset(slashTrailPositions[0]).RotatedBy(owner.fullRotation)+owner.MountedCenter;
            }
            return base.StarCenter;
        }
    }

    public Action_SimpleSlash() : base(45)
    {
        slashTrailPositions = new Vector2[50];
        slashTrailDrawPositions = new Vector2[slashTrailPositions.Length];

        AddNode(new ArmActionNode(
                0,
                15,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Full,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                0f,
                -1.8f))
            .AddNode(new ArmActionNode(
                15,
                25,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                Player.CompositeArmStretchAmount.Quarter,
                -1.8f,
                -2.0f))
            .AddNode(new ArmActionNode(
                25,
                35,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Quarter,
                Player.CompositeArmStretchAmount.Full,
                -2.0f,
                1.0f))
            .AddNode(new ArmActionNode(
                35,
                45,
                ActionArmType.Front,
                Player.CompositeArmStretchAmount.Full,
                Player.CompositeArmStretchAmount.Full,
                1.0f,
                0.0f))
            /*.AddNode(new ArmActionNode(
                0,
                30,
                ActionArmType.Back,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                Player.CompositeArmStretchAmount.ThreeQuarters,
                0.3f,
                0.3f))*/;
    }

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        Array.Clear(slashTrailPositions);
        Array.Clear(slashTrailDrawPositions);
        base.OnStart(actionPlayer);
    }

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        base.Update(actionPlayer, actionFrame, actionProgress);
    }

    public override void Draw(ActionModPlayer actionPlayer, ref PlayerDrawSet drawInfo, int actionFrame, float actionProgress, ActionDrawLayerType drawLayerType)
    {
        if (drawLayerType != ActionDrawLayerType.OverPlayer)
        {
            base.Draw(actionPlayer, ref drawInfo, actionFrame, actionProgress, drawLayerType);
            return;
        }

        float totalAlpha = 1;
        if(actionProgress<0.2f) totalAlpha = KLMathF.ClampLerp(0f, 1f, actionProgress / 0.2f);
        if(actionProgress>0.8f) totalAlpha = 1-KLMathF.ClampLerp(0f, 1f, (actionProgress - 0.8f) / 0.2f);
        

        if (actionProgress > 0.65f)
        {

            float progress = (actionProgress - 0.65f) * 5.5f;
            progress = MathHelper.Clamp(progress, 0f, 10f);
            BuildSlashTrail(progress);
            //DrawInWorld(Cross,WandCenter,color: new Color(255,100,255,0),scale: new Vector2(0.2f));
        
            TrailEffect(trailTex, GetSlashTrailDrawPositions(), new Color(255,100,255,0)*totalAlpha, new Color(255,100,255,0) * 0, 5, 0,
                attachPoint: actionPlayer.Player.MountedCenter,attachRotation:owner.fullRotation, debugPoint: false);
        }
        else
        {
            //这么写是为了先快后慢动作。
            float progress = actionProgress / 0.4f;
            if(actionProgress>0.3)progress = 0.75f+ (actionProgress-0.3f) /0.7f;
            
            progress = MathHelper.Clamp(progress, 0f, 5f);
            BuildStartSlashTrail(progress);
            
            TrailEffect(trailTex, GetSlashTrailDrawPositions(), new Color(255,100,255,0)*totalAlpha, new Color(255,100,255,0) * 0, 5, 0,
                attachPoint: actionPlayer.Player.MountedCenter, attachRotation:owner.fullRotation,debugPoint: false);
        }
        
        EndBeginDraw(0, 1);
        base.Draw(actionPlayer, ref drawInfo, actionFrame, actionProgress, drawLayerType);

    }

    private void BuildSlashTrail(float actionProgress)
    {
        //当progress大于1时做一个拖尾回收效果
        float endProgress = 0;
        if (actionProgress > 1)
        {
            endProgress = actionProgress - 1;
            endProgress = MathHelper.Clamp(endProgress, 0f, 1f);
            actionProgress = 1;
        }
        
        float headProgress = MathHelper.Clamp(BezierEase(actionProgress), 0f, 1f);

        float trailLength = 0.8f*(1-endProgress);
        Vector2 startPosition = new Vector2(44f * owner.direction, -12f);
        Vector2 endPosition = new Vector2(-42f * owner.direction, 18f);
        Vector2 pointA = new Vector2(28f * owner.direction, 32f);
        Vector2 pointB = new Vector2(-34f * owner.direction, 52f);

        for (int i = 0; i < slashTrailPositions.Length; i++)
        {
            float trailProgress = i / (slashTrailPositions.Length - 1f);
            float pointProgress = MathHelper.Clamp(headProgress - trailLength * trailProgress, 0f, 1f);
            slashTrailPositions[i] = GetBezierPoint(pointProgress, startPosition, endPosition, pointA, pointB);
        }

        bool debugPoint = false;
        if (debugPoint)
        {
            DrawDebugPoint(startPosition, endPosition, pointA, pointB);
        }
    }
    
    private void BuildStartSlashTrail(float actionProgress)
    {
        //当progress大于1时做一个拖尾回收效果
        float endProgress = 0;
        if (actionProgress > 1)
        {
            endProgress = actionProgress - 1;
            endProgress = KLMathF.ClampLerp(0f, 1f, endProgress / 0.2f);
            actionProgress = 1;
        }
        float headProgress = MathHelper.Clamp(BezierEase(actionProgress), 0f, 1f);
        float trailLength = 0.82f *(1-endProgress);
        Vector2 startPosition = new Vector2(-14f * owner.direction, 35f);
        Vector2 endPosition = new Vector2(44f * owner.direction, -12f);
        Vector2 pointA = new Vector2(-8f * owner.direction, 48f);
        Vector2 pointB = new Vector2(34f * owner.direction, 32f);

        for (int i = 0; i < slashTrailPositions.Length; i++)
        {
            float trailProgress = i / (slashTrailPositions.Length - 1f);
            float pointProgress = MathHelper.Clamp(headProgress - trailLength * trailProgress, 0f, 1f);
            slashTrailPositions[i] = GetBezierPoint(pointProgress, startPosition, endPosition, pointA, pointB);
        }

        bool debugPoint = false;
        if (debugPoint)
        {
            DrawDebugPoint(startPosition, endPosition, pointA, pointB);
        }
    }
    
    private Vector2[] GetSlashTrailDrawPositions()
    {
        for (int i = 0; i < slashTrailPositions.Length; i++)
        {
            slashTrailDrawPositions[i] = GetPlayerLocalDrawOffset(slashTrailPositions[i]);
        }

        return slashTrailDrawPositions;
    }

    void DrawDebugPoint(Vector2 startPosition, Vector2 endPosition, Vector2 pointA, Vector2 pointB)
    {
        Vector2[] previewPoints = new Vector2[slashTrailPositions.Length];
        for (int i = 0; i < previewPoints.Length; i++)
        {
            float previewProgress = i / (previewPoints.Length - 1f);
            previewPoints[i] = GetPlayerLocalDrawOffset(GetBezierPoint(previewProgress, startPosition, endPosition, pointA, pointB));
        }

        for (int i = 0; i < previewPoints.Length; i++)
        {
            Vector2 normalDir = i < previewPoints.Length - 1
                ? previewPoints[i] - previewPoints[i + 1]
                : previewPoints[i - 1] - previewPoints[i];
            normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                owner.MountedCenter + previewPoints[i] + normalDir  - Main.screenPosition,
                new Rectangle(0, 0, 1, 1), Color.Red, 0f, new Vector2(0.5f, 0.5f), 1f, SpriteEffects.None, 0f);
        }
    }
}
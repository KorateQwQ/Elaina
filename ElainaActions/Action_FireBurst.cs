namespace 伊蕾娜.ElainaActions;

public sealed class Action_FireBurst : Action_SimpleShoot
{
    protected override void RequestActionStarBloom(float actionProgress)
    {
        ElainaTrailVisualStyle.RequestBloomStar(StarCenter.Floor(), actionProgress,
            ElainaTrailVisualStyle.FireWandColor);
    }

    protected override void DrawWandTrailVisual(Vector2[] points, float actionProgress)
    {
        ElainaTrailVisualStyle.DrawTrail(points, Vector2.Zero, 0f, actionProgress,
            ElainaTrailVisualStyle.FireWandColor, ElainaTrailVisualStyle.DefaultTrailMaxWidth,
            ElainaTrailVisualStyle.DefaultTrailEndWidth, ElainaTrailVisualStyle.DefaultTrailFadeInEnd,
            ElainaTrailVisualStyle.DefaultTrailFadeOutStart);
    }
}

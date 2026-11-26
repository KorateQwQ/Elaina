using System;
using System.Collections.Generic;
using KL.SkillSystem;
using KL.ActionsSystem;
using 伊蕾娜.ElainaActions;
using Terraria.UI;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>
/// 冰凌锁在鼠标附近优先锁定 Boss，同类优先锁定最大生命值高的敌人，
/// 并在目标身上绘制「寒霜菱形框」准星（Effects/Content/IceReticle.fx）。
/// </summary>
public class IceConeTargetSystem : ModSystem
{
    // 鼠标到敌人碰撞箱的最大锁定距离（世界像素）
    private const float LockRange = 400f;
    // 已锁定的目标放宽保持距离，避免在边界来回闪烁
    private const float KeepRangeMultiplier = 1.15f;

    private static int targetIndex = -1;
    private static readonly ConstellationUIClock visualClock = new();
    private static ReticlePresentation hover;
    private static bool VisualsActive => !Main.dedServ && !Main.gameMenu && Main.hasFocus && !Main.hideUI && !Main.gamePaused;

    internal static ReticlePresentation TakeHover(NPC target)
    {
        if (!VisualsActive || hover == null || hover.Tracking.Opacity < .001f) return null;
        ReticlePresentation captured = hover;
        hover = null;
        captured.Advance(0, target.Center, ShardbindReticle.Radius(target));
        captured.Confirm();
        return captured;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        // 即使 F11 隐藏后没有绘制回调，也立即断开时钟采样。
        if (!VisualsActive) visualClock.Stop();
    }

    internal static double SampleVisualDelta() => visualClock.Sample(VisualsActive);

    internal static ReticlePresentation AdvanceHover(NPC hovered, double delta)
    {
        if (hovered != null && hover == null)
            hover = new ReticlePresentation(hovered.Center, ShardbindReticle.Radius(hovered));
        if (hover == null) return null;
        hover.Advance(delta, hovered?.Center, hovered == null ? hover.Tracking.Radius : ShardbindReticle.Radius(hovered));
        if (hovered == null && hover.Tracking.Opacity < .001f) hover = null;
        return hover;
    }

    /// <summary>独立冰凌锁或冰锥快捷锁定期间的准星目标；未追踪时返回 null。</summary>
    public static NPC LockedTarget => CanTrack && targetIndex >= 0 && IsValidTarget(Main.npc[targetIndex]) ? Main.npc[targetIndex] : null;

    /// <summary>优先沿用准星锁定，否则按同样规则即时查找一次。</summary>
    public static NPC ResolveTarget() => CanTrack ? LockedTarget ?? FindTarget(Main.MouseWorld, -1) : null;

    public static NPC FindTarget(Vector2 mouseWorld, int currentIndex)
    {
        NPC best = null;
        float bestDistance = float.MaxValue;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc))
                continue;

            bool isCurrent = npc.whoAmI == currentIndex;
            float distance = DistanceToHitbox(mouseWorld, npc);
            if (distance > (isCurrent ? LockRange * KeepRangeMultiplier : LockRange))
                continue;

            // 优先级相同才比较碰撞箱距离，完全相同时保留已有锁定。
            if (best == null || (npc.boss != best.boss ? npc.boss
                : npc.lifeMax != best.lifeMax ? npc.lifeMax > best.lifeMax
                : distance < bestDistance || distance == bestDistance && isCurrent))
            {
                best = npc;
                bestDistance = distance;
            }
        }

        return best;
    }

    public static bool IsValidTarget(NPC npc) => npc.active && npc.lifeMax > 5 && npc.CanBeChasedBy();

    private static float DistanceToHitbox(Vector2 point, NPC npc)
    {
        Rectangle hitbox = npc.Hitbox;
        Vector2 closest = Vector2.Clamp(point, hitbox.TopLeft(), hitbox.BottomRight());
        return Vector2.Distance(point, closest);
    }

    private static Action_IceCone QuickCastAction => Main.LocalPlayer.GetModPlayer<ActionModPlayer>().CurrentAnimAction as Action_IceCone;

    private static bool CanTrack => !Main.dedServ && !Main.gameMenu
        && Main.LocalPlayer is { active: true, dead: false, ghost: false }
        && Main.LocalPlayer.HeldItem.ModItem is ElainaWand
        && (ShardbindSelected() || QuickCastAction is { CanReleaseQuickCast: true, QuickCastTarget: not null });

    private static bool ShardbindSelected()
    {
        List<Skill> skills = ElainaSkillModPlayer.GetActiveSkill;
        int index = ElainaSkillModPlayer.CurrentSkillIndex;
        return index >= 0 && index < skills.Count && skills[index]?.ModSkill is ShardbindSkill;
    }

    public override void OnWorldUnload()
    {
        ResetTracking();
        hover = null;
        visualClock.Stop();
    }

    private static void ResetTracking()
    {
        targetIndex = -1;
    }


    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;

        if (!CanTrack)
        {
            ResetTracking();
            return;
        }

        NPC target = ShardbindSelected() ? FindTarget(Main.MouseWorld, targetIndex) : QuickCastAction?.QuickCastTarget;
        targetIndex = target?.whoAmI ?? -1;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Interface Logic 1");
        layers.Insert(Math.Max(index, 0), new LegacyGameInterfaceLayer(
            "伊蕾娜: Ice Cone Reticle",
            () =>
            {
                DrawReticle();
                return true;
            },
            InterfaceScaleType.Game));
    }

    private static void DrawReticle()
    {
        if (ElainaActions.Action_IceCone.PreviewSpawnRange && !Main.dedServ && !Main.gameMenu
            && Main.LocalPlayer is { active: true, dead: false, ghost: false }
            && Main.LocalPlayer.HeldItem.ModItem is ElainaWand)
        {
            List<Skill> previewSkills = ElainaSkillModPlayer.GetActiveSkill;
            int previewIndex = ElainaSkillModPlayer.CurrentSkillIndex;
            if (previewIndex >= 0 && previewIndex < previewSkills.Count
                && previewSkills[previewIndex]?.ModSkill is IceConeSkill)
            {
                Vector2 center = Main.LocalPlayer.MountedCenter;
                ElainaActions.Action_IceCone.DrawSpawnRange(center, (Main.MouseWorld - center).ToRotation());
            }
        }

        double delta = SampleVisualDelta();
        if (!VisualsActive) return;
        NPC hovered = LockedTarget;
        bool hoverConfirmed = false;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not ShardbindProj controller) continue;
            NPC target = controller.LockedTarget;
            if (target != null && target == hovered && projectile.owner == Main.myPlayer) hoverConfirmed = true;
            if (controller.ElapsedTicks >= ShardbindSequence.LaunchTick) continue;
            controller.ReticlePresentation ??= new ReticlePresentation(projectile.Center, controller.ReticleRadius, confirmed: true);
            ReticlePresentation presentation = controller.ReticlePresentation;
            presentation.Advance(delta, projectile.Center, target == null ? controller.ReticleRadius : ShardbindReticle.Radius(target));
            ShardbindReticle.Draw(presentation, controller.ElapsedTicks);
        }
        if (hoverConfirmed) hovered = null;
        ReticlePresentation hovering = AdvanceHover(hovered, delta);
        if (hovering != null) ShardbindReticle.Draw(hovering);
    }
}

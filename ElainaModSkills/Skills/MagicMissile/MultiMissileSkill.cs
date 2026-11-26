using System;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Localization;
using static 伊蕾娜.Localization.UIText;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

[SkillUIInfo(State = 2, Pixels = 200)]
public class MultiMissileSkill : ElainaSkill
{
    internal const int MissileCount = 5;
    private static float Radius => 120f;
    private bool quickCasting;

    public override Type[] PrerequisiteSkills => [typeof(MagicMissileSkill)];
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireSkillLevel<MagicMissileSkill>(5));

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        int current = GetConfiguredDamage(skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(skillLevel: nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }

    internal static bool TryGetEquipped(Player player, out MultiMissileSkill skill)
    {
        var skills = player.GetModPlayer<ElainaSkillModPlayer>();
        return skills.TryGetUnlockedModSkill(out skill) && skill.Skill != null && skills.ActiveSkill.Contains(skill.Skill);
    }

    internal void QuickCast()
    {
        quickCasting = true;
        Skill.UseSkill(Player.GetSource_Misc(nameof(MultiMissileSkill)), Player.GetModPlayer<ElainaSkillModPlayer>().Attributes);
        quickCasting = false;
    }

    public override bool CanUseSkill()
    {
        var actions = Player.GetModPlayer<ActionModPlayer>();
        if (Player.whoAmI != Main.myPlayer || Stack <= 0 || !Player.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled
            || actions.HasAction && !(quickCasting && actions.CurrentAnimAction is Action_MagicMissile { CanQuickCast: true }))
            return false;

        // 一次需要整组槽位。上轮仍在等候目标时不能插入半轮，也不会消耗资源。
        int spawnerType = ModContent.ProjectileType<MagicMissleSpawner>();
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == Player.whoAmI && projectile.type == spawnerType && projectile.ai[0] != 2)
                return false;
        return base.CanUseSkill();
    }

    protected override bool OnCast(IEntitySource source)
    {
        if (!CanUseSkill()) return false;
        var action = new Action_MultiMissile(GetConfiguredDamage(), quickCasting);
        float rotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
        if (!Player.GetModPlayer<ActionModPlayer>().StartAction(action, interruptCurrentAction: quickCasting, rotation: rotation))
            return false;
        Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost);
        return base.OnCast(source);
    }

    internal static Vector2 GetSlotPosition(Player player, int slotIndex)
    {
        float angle = MathHelper.Lerp(0, -MathHelper.Pi, slotIndex / (float)(MissileCount - 1));
        return player.VisualCenter() + angle.ToRotationVector2() * Radius;
    }

    protected override object[] SkillDescriptionArgs => [MissileCount, SkillValueText.Damage(GetConfiguredDamage(), SkillValueKind.Magic)];
}

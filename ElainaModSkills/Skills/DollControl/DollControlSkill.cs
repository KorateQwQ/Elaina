using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 技能轮盘选中时召唤玩偶并打开指令盘，不切换当前术式。独立标记键由 <see cref="DollCommandPlayer"/> 处理。
/// 玩偶持续存在，直到“收回玩偶”指令、主人死亡或离开世界。
/// </summary>
[SkillUIInfo(State = 2, Pixels = 400)]
public class DollControlSkill : ElainaSkill
{
    public const int TeamSize = DollControlProgression.TeamSize;
    private int appliedLevel;
    public override int MaxLevel => Math.Min(DollControlProgression.MaximumLevel, base.MaxLevel);
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost,
            RequireConfiguredCharacterLevelCap(nameof(DollControlSkill), 10),
            RequireConfiguredCharacterLevel(nameof(DollControlSkill), 6));

    public override SkillUnlockCondition GetConstellationUpgradeCondition(int nextLevel) => SkillUnlockCondition.BySkillPoint(DollControlProgression.UpgradeCost);

    protected override object[] SkillDescriptionArgs =>
        [DollControlProgression.DollCount(Level), DollControlProgression.UpgradeCost, MarkKeyLabel];

    // 标记键名随绑定实时变化；未绑定时以红字提醒玩家去设置。
    private static string MarkKeyLabel => DollCommandPlayer.MarkKeyName is { } key
        ? $"[c/FFD27A:{key}]"
        : $"[c/E28096:{Language.GetTextValue("Mods.伊蕾娜.UI.DollMarkUnbound")}]";

    private static string CountLabel => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.DollStats.Count");
    private static string SpeedLabel => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.DollStats.ActionSpeed");
    public override (string Label, string Value, string Unit)[] GetConstellationStats(int level) =>
        [(CountLabel, DollControlProgression.DollCount(level).ToString(), ""),
         (SpeedLabel, DollControlProgression.ActionSpeedBonusPercent(level).ToString(), "%")];

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel) =>
        [(SpeedLabel, DollControlProgression.ActionSpeedBonusPercent(nextLevel - 1).ToString(),
            DollControlProgression.ActionSpeedBonusPercent(nextLevel).ToString(), "%")];

    public override void Initialize()
    {
        base.Initialize();
        SetSummonCost();
    }

    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        // 旧占位技能存档中有 60 秒的默认冷却。
        SetSummonCost();
    }

    private void SetSummonCost()
    {
        MagicPointCost = 20f;
        MaxCD = 1f;
    }

    public override void UpdateEquips(Player player)
    {
        base.UpdateEquips(player);
        if (Main.netMode == NetmodeID.Server || player.whoAmI != Main.myPlayer || !player.active || player.dead)
            return;
        int level = DollControlProgression.ClampLevel(Level);
        if (appliedLevel == level) return;
        // 已召出的整队随升级立即成长；弹幕池不足时保留任务，之后重试补齐。
        if (RefreshTeam(player, level)) appliedLevel = level;
    }

    private static bool RefreshTeam(Player player, int level)
    {
        int count = DollControlProgression.DollCount(level), present = 0, free = 0;
        bool hasTeam = false;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (!projectile.active) { free++; continue; }
            if (projectile.owner != player.whoAmI || projectile.ModProjectile is not DollPetProjectile { IsDismissing: false } doll)
                continue;
            hasTeam = true;
            if (doll.FormationSlot >= count) { doll.Dismiss(); continue; }
            present |= 1 << doll.FormationSlot;
            doll.SetSkillLevel(level);
        }
        if (!hasTeam) return true;
        int missing = 0;
        for (int slot = 0; slot < count; slot++) if ((present & (1 << slot)) == 0) missing++;
        if (free < missing) return false;
        for (int slot = 0; slot < count; slot++)
            if ((present & (1 << slot)) == 0)
                SpawnDoll(player, slot, level, player.GetSource_Misc(nameof(DollControlSkill)));
        return true;
    }

    private static void SpawnDoll(Player player, int slot, int level, IEntitySource source)
    {
        int type = slot switch
        {
            0 => ModContent.ProjectileType<KumaPetProjectile>(),
            1 => ModContent.ProjectileType<SecondDollProjectile>(),
            2 => ModContent.ProjectileType<ThirdDollProjectile>(),
            _ => ModContent.ProjectileType<FourthDollProjectile>()
        };
        int index = Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, 0, 0f, player.whoAmI);
        if (index < 0 || index >= Main.maxProjectiles || Main.projectile[index].ModProjectile is not DollPetProjectile summoned)
            return;
        summoned.SetSkillLevel(level);
        summoned.RecallToOwner(player);
    }

    public override bool CanUseSkill()
    {
        if (Main.netMode == NetmodeID.Server || !Player.active || Player.dead)
            return false;

        if (DollPetProjectile.FindOwned(Player) != null) return base.CanUseSkill();
        int free = 0;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (!Main.projectile[i].active && ++free >= DollControlProgression.DollCount(Level)) return base.CanUseSkill();
        }
        return false;
    }

    /// <summary>技能轮盘直接执行召唤并打开指令盘，不改动当前攻击术式。</summary>
    internal void OpenCommands()
    {
        if (DollPetProjectile.FindOwned(Player) == null)
        {
            var skills = ElainaSkillModPlayer.GetActiveSkill;
            for (int i = 0; i < skills.Count; i++)
                if (ReferenceEquals(skills[i]?.ModSkill, this))
                {
                    Player.GetModPlayer<ElainaSkillModPlayer>().UseSkill(i, Player.GetSource_Misc(nameof(DollControlSkill)));
                    break;
                }
        }
        Player.GetModPlayer<DollCommandPlayer>().OpenCommandWheel();
    }

    // 兼容旧的术式选择入口；指令操作已独立于魔杖左键。
    public override bool BeginWandUse() => DollPetProjectile.FindOwned(Player) == null;

    public override bool PreUseSkill(IEntitySource source = null)
    {
        // 技能属于本地玩家；只有生成端支付魔力并创建弹幕，其他端使用原版同步。
        if (!CanUseSkill() || DollPetProjectile.FindOwned(Player) != null ||
            !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        int level = DollControlProgression.ClampLevel(Level);
        for (int slot = 0; slot < DollControlProgression.DollCount(level); slot++)
            SpawnDoll(Player, slot, level, source ?? Player.GetSource_Misc(nameof(DollControlSkill)));
        appliedLevel = level;
        // 收集物归整队共有，重新召唤时只交还一次。
        var first = DollPetProjectile.FindOwned(Player);
        if (first != null)
            Player.GetModPlayer<DollCommandPlayer>().DeliverLoot(first.Projectile.Center, first.Projectile.GetSource_FromThis());
        return base.PreUseSkill(source);
    }
}

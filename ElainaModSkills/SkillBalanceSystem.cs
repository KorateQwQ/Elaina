using System;
using System.Text;
using KL.Utils;
using Terraria;
using Terraria.ModLoader;
#if DEBUG
using System.IO;
using Microsoft.Xna.Framework;
#endif

namespace 伊蕾娜.ElainaModSkills;

/// <summary>本地技能定价；仅 Debug 热读取源码 JSON，Release 只读取 .tmod 内的配置，不同步配置。</summary>
public sealed class SkillBalanceSystem : ModSystem
{
    private static SkillBalanceConfig defaults = SkillBalanceConfig.Fallback;
    private static SkillBalanceFileStore store = new(defaults);
    public static SkillBalanceConfig Current => store.Current;
#if DEBUG
    private long nextPoll;
    private string reportedError;
    public string SourcePath => string.IsNullOrWhiteSpace(Mod.SourceFolder)
        ? null
        : Path.Combine(Mod.SourceFolder, SkillBalanceConfig.ResourcePath.Replace('/', Path.DirectorySeparatorChar));
#endif

    public override void Load()
    {
        defaults = SkillBalanceConfig.Fallback;
        if (Mod.FileExists(SkillBalanceConfig.ResourcePath))
        {
            string json = Encoding.UTF8.GetString(Mod.GetFileBytes(SkillBalanceConfig.ResourcePath));
            if (SkillBalanceConfig.TryParse(json, out SkillBalanceConfig packed, out string error)) defaults = packed;
            else Mod.Logger.Warn("打包技能配置无效，使用安全默认值：" + error);
        }
        store = new(defaults);
#if DEBUG
        Poll(false, true);
#endif
    }

    public override void Unload()
    {
        defaults = SkillBalanceConfig.Fallback;
        store = new(defaults);
#if DEBUG
        nextPoll = 0;
        reportedError = null;
#endif
    }

#if DEBUG
    public override void UpdateUI(GameTime gameTime) => Poll(true);
    public override void PostUpdateEverything() => Poll(true);

    private void Poll(bool notify, bool force = false)
    {
        long now = Environment.TickCount64;
        if (!force && now < nextPoll) return;
        nextPoll = now + 500;
        // 只读取已有的本机源码配置；联机也不接收或发送其他机器的配置。
        if (string.IsNullOrEmpty(SourcePath) || !File.Exists(SourcePath)) return;
        bool changed = store.Refresh(SourcePath);
        if (store.LastError != null)
        {
            if (reportedError != store.LastError)
            {
                reportedError = store.LastError;
                Mod.Logger.Warn($"技能配置读取失败，继续使用上次成功配置：{SourcePath}；{reportedError}");
            }
            return;
        }
        reportedError = null;
        if (!changed) return;
        Mod.Logger.Info("技能配置已重载：" + SourcePath);
        if (notify && !Main.gameMenu && !Main.dedServ)
            Main.NewText("伊蕾娜：技能配置已重载，新参数将在下一次施法使用。", 195, 160, 255);
    }

#endif

    public static bool TryGetSkill(string id, out SkillBalanceEntry entry) => Current.Skills.TryGetValue(id, out entry);

    private static double GetCharacterBaselineDps(int characterLevel)
    {
        if (characterLevel <= 0) return 0;
        // KL 曲线每 5 级一个节点；用双精度插值，避免 float 误差导致整数伤害少 1。
        double state = characterLevel / 5d;
        int lower = (int)Math.Floor(state);
        double start = KLDpsHelper.GetLevelDps(lower * 5);
        double end = KLDpsHelper.GetLevelDps((lower + 1) * 5);
        return start + (end - start) * (state - lower);
    }

    private static double GetBaselineDps(SkillBalanceConfig config, SkillBalanceEntry entry, int skillLevel) =>
        GetCharacterBaselineDps(config.GetSkillCharacterLevel(entry, skillLevel));

    /// <summary>按技能实际等级映射伤害节点，不含装备、暴击、命中、防御或资源限制。</summary>
    public static float GetBaseDps(int skillLevel, string id)
    {
        SkillBalanceConfig config = Current;
        return config.Skills.TryGetValue(id, out SkillBalanceEntry entry)
            ? (float)config.GetSkillDps(GetBaselineDps(config, entry, skillLevel), entry) : 0;
    }

    /// <summary>按技能实际等级映射的单发基础伤害；默认按配置发数分摊。</summary>
    public static int GetBaseDamage(int skillLevel, string id, int? hitCount = null)
    {
        SkillBalanceConfig config = Current;
        return config.Skills.TryGetValue(id, out SkillBalanceEntry entry)
            ? SkillBalanceConfig.ToHitDamage(config.GetSkillDamage(GetBaselineDps(config, entry, skillLevel), entry), hitCount ?? entry.HitCount) : 0;
    }

    /// <summary>按技能实际等级一次读取同一份配置；玩家只提供装备增伤，不再提供伤害基准等级。</summary>
    public static bool TryGetPlayerBalance(Player player, string id, int skillLevel, DamageClass damageClass,
        out SkillBalanceResult result, int? hitCount = null)
    {
        result = null;
        SkillBalanceConfig config = Current;
        if (skillLevel <= 0 || !config.Skills.TryGetValue(id, out SkillBalanceEntry entry)) return false;
        int hits = hitCount ?? entry.HitCount;
        if (hits < 1) throw new ArgumentOutOfRangeException(nameof(hitCount));
        int characterLevel = config.GetSkillCharacterLevel(entry, skillLevel);
        double baseline = GetCharacterBaselineDps(characterLevel);
        double baseDamage = config.GetSkillDamage(baseline, entry);
        double damage = player.GetTotalDamage(damageClass).ApplyTo((float)baseDamage);
        result = new(entry, config.GetSkillDps(baseline, entry), baseDamage, damage, hits,
            SkillBalanceConfig.ToHitDamage(damage, hits), skillLevel, characterLevel);
        return true;
    }

    public static int GetPlayerDamage(Player player, string id, int skillLevel, DamageClass damageClass, int? hitCount = null) =>
        TryGetPlayerBalance(player, id, skillLevel, damageClass, out SkillBalanceResult result, hitCount) ? result.DamagePerHit : 0;
}

/// <summary>本次定价快照；DamagePerHit 可直接传给弹幕，不可再次应用装备增伤或再次除以发数。</summary>
public sealed record SkillBalanceResult(SkillBalanceEntry Entry, double BaseDps, double BaseCastDamage,
    double CastDamage, int HitCount, int DamagePerHit, int SkillLevel, int CharacterLevel);

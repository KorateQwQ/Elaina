using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using 伊蕾娜.System;

namespace 伊蕾娜.ElainaModSkills.Loadout;

/// <summary>把 SkillBalance.json 中的阶段推荐配装穿到玩家身上；只在单人模式下直接改装备与属性。</summary>
public static class LoadoutApplier
{
    private const int FirstAccessory = 3, AccessorySlots = 7;

    /// <summary>Progress 为已击败的最高 Boss 序号（月总 18），等级上限 5×(Progress+1)，即网页的第 Progress+1 阶段。</summary>
    public static int CurrentStage(Player player) =>
        Math.Clamp((int)MathF.Floor(player.GetModPlayer<ElainaStatePlayer>().Progress) + 1, 1, SkillBalanceConfig.LoadoutStages);

    public static bool CanApply(Player player, out string reason)
    {
        reason = Main.netMode != NetmodeID.SinglePlayer ? LocalizeUI("SinglePlayerLoadout")
            : !player.GetModPlayer<ElainaModplayer>().Elaina ? LocalizeUI("ElainaLoadout")
            : null;
        return reason == null;
    }

    public static IEnumerable<(int Type, int Prefix)> Accessories(SkillLoadout loadout)
    {
        var seen = new HashSet<int>();
        foreach (var entry in loadout.Items.Take(loadout.Slots))
            if (LoadoutItems.TryGetAccessory(entry.Id, out int type) && seen.Add(type))
                yield return (type, LoadoutItems.Prefixes.GetValueOrDefault(entry.Prefix));
    }

    public static int[] Armor(SkillLoadout loadout) => LoadoutItems.Armors.GetValueOrDefault(loadout.Armor) ?? [0, 0, 0];

    /// <summary>调试换装：删除当前功能护甲和饰品，按预设槽序创建新物品；空槽清空，不读取或写入背包。</summary>
    public static string Apply(Player player, SkillLoadout loadout)
    {
        int[] armor = Armor(loadout);
        var slots = Enumerable.Range(FirstAccessory, AccessorySlots).Where(player.IsItemSlotUnlockedAndUsable).ToArray();
        // 包括当前锁定的功能饰品槽，避免高阶段装备残留；时装、染料、背包和其他配装栏不属于本次换装。
        for (int slot = 0; slot < FirstAccessory + AccessorySlots; slot++)
            player.armor[slot] = new Item();

        int equipped = 0;
        for (int slot = 0; slot < 3; slot++)
            if (armor[slot] > 0) { player.armor[slot] = Create(armor[slot], 0); equipped++; }

        int missing = 0;
        var seen = new HashSet<int>();
        for (int i = 0; i < Math.Min(loadout.Slots, loadout.Items.Count); i++)
        {
            var entry = loadout.Items[i];
            if (!LoadoutItems.TryGetAccessory(entry.Id, out int type) || !seen.Add(type)) continue;
            if (i >= slots.Length) { missing++; continue; }
            player.armor[slots[i]] = Create(type, LoadoutItems.Prefixes.GetValueOrDefault(entry.Prefix));
            equipped++;
        }
        var notes = new List<string> { LocalizeUI("GearChanged", equipped) };
        notes.AddRange(SetStats(player, loadout));
        if (missing > 0) notes.Add(LocalizeUI("MissingSlots", missing));
        return string.Join(" · ", notes);
    }

    private static Item Create(int type, int prefix)
    {
        var item = new Item(type);
        if (prefix > 0) item.Prefix(prefix);
        return item;
    }

    public static int BaseMana(Player player) => 20 + player.ConsumedManaCrystals * 20;
    public static int BaseLife(Player player) => 100 + player.ConsumedLifeCrystals * 20 + player.ConsumedLifeFruit * 5;

    /// <summary>魔力水晶、生命水晶与生命果的使用数量直接设为推荐值，可升可降（便于回到开局 100 血等早期状态）。</summary>
    private static IEnumerable<string> SetStats(Player player, SkillLoadout loadout)
    {
        int mana = BaseMana(player);
        player.ConsumedManaCrystals = Math.Clamp((loadout.BaseMana - 20) / 20, 0, 9);
        if (BaseMana(player) != mana) yield return LocalizeUI("ManaChanged", mana, BaseMana(player));

        int life = BaseLife(player);
        player.ConsumedLifeCrystals = Math.Clamp((Math.Min(loadout.MaxLife, 400) - 100) / 20, 0, 15);
        player.ConsumedLifeFruit = Math.Clamp((loadout.MaxLife - 400) / 5, 0, 20);
        if (BaseLife(player) == life) yield break;
        player.statLife = Math.Min(player.statLife, BaseLife(player));
        yield return LocalizeUI("LifeChanged", life, BaseLife(player));
    }
}

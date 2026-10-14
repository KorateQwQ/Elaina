using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Items.accessories;
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

    /// <summary>护甲为 0 的部位、utility/empty 槽保留原装备；被换下的装备放回背包，背包满时掉落在脚下。</summary>
    public static string Apply(Player player, SkillLoadout loadout)
    {
        var displaced = new List<Item>();
        int changed = 0;
        int[] armor = Armor(loadout);
        for (int slot = 0; slot < 3; slot++)
            if (armor[slot] > 0 && Equip(player, slot, armor[slot], 0, displaced)) changed++;

        var slots = Enumerable.Range(FirstAccessory, AccessorySlots).Where(player.IsItemSlotUnlockedAndUsable).ToList();
        var pending = new List<(int Type, int Prefix)>();
        foreach (var (type, prefix) in Accessories(loadout))
        {
            int owned = slots.FirstOrDefault(slot => player.armor[slot].type == type, -1);
            if (owned < 0) { pending.Add((type, prefix)); continue; }
            if (Equip(player, owned, type, prefix, displaced)) changed++;
            slots.Remove(owned);
        }
        // 先占空槽，再换普通饰品，最后才动翅膀、鞋子、气球这类机动饰品；伊蕾娜的帽子始终保留。
        var free = slots.Where(slot => player.armor[slot].type != ModContent.ItemType<ElainaHat>())
            .OrderBy(slot => ReplaceCost(player.armor[slot])).ToList();
        int missing = 0;
        foreach (var (type, prefix) in pending)
        {
            if (free.Count == 0) { missing++; continue; }
            if (Equip(player, free[0], type, prefix, displaced)) changed++;
            free.RemoveAt(0);
        }

        bool dropped = false;
        foreach (var item in displaced)
        {
            var left = player.GetItem(player.whoAmI, item, GetItemSettings.InventoryUIToInventorySettings);
            if (left.IsAir) continue;
            player.QuickSpawnItem(player.GetSource_Misc("ElainaLoadout"), left, left.stack);
            dropped = true;
        }

        var notes = new List<string> { changed > 0 ? LocalizeUI("GearChanged", changed) : LocalizeUI("SameEquipment") };
        notes.AddRange(SetStats(player, loadout));
        if (missing > 0) notes.Add(LocalizeUI("MissingSlots", missing));
        if (dropped) notes.Add(LocalizeUI("DroppedEquipment"));
        return string.Join(" · ", notes);
    }

    private static int ReplaceCost(Item item) =>
        item.IsAir ? 0 : item.wingSlot > 0 || item.shoeSlot > 0 || item.balloonSlot > 0 ? 2 : 1;

    /// <summary>槽位已是同类物品时只在前缀不同才重铸；否则优先从背包取用，没有再新建。</summary>
    private static bool Equip(Player player, int slot, int type, int prefix, List<Item> displaced)
    {
        Item current = player.armor[slot];
        if (current.type == type)
        {
            if (prefix == 0 || current.prefix == prefix) return false;
            player.armor[slot] = Create(type, prefix, current.favorited);
            return true;
        }
        Item next = Take(player, type, prefix);
        if (!current.IsAir) displaced.Add(current);
        player.armor[slot] = next;
        return true;
    }

    private static Item Take(Player player, int type, int prefix)
    {
        int found = -1;
        for (int i = 0; i < Main.InventoryItemSlotsCount; i++)
        {
            Item item = player.inventory[i];
            if (item.type != type) continue;
            if (prefix == 0 || item.prefix == prefix) { found = i; break; }
            if (found < 0) found = i;
        }
        if (found < 0) return Create(type, prefix, false);
        Item taken = player.inventory[found];
        player.inventory[found] = new Item();
        // 背包里的同类物品前缀不对时就地重铸，不额外复制一件；护甲与饰品都不可堆叠。
        return prefix == 0 || taken.prefix == prefix ? taken : Create(type, prefix, taken.favorited);
    }

    private static Item Create(int type, int prefix, bool favorited)
    {
        var item = new Item(type);
        if (prefix > 0) item.Prefix(prefix);
        item.favorited = favorited;
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

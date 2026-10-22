using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Buffs;
using 伊蕾娜.ElainaModAlchemy.item.Foods;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>按无限增益的接收者解析蜂蜜面包，保留 ImproveGame 的门槛、来源和开关。</summary>
public sealed class ImproveGameFoodCompatibility : ModSystem
{
    public override void PostSetupContent()
    {
        if (Main.dedServ || !ModLoader.TryGetMod("ImproveGame", out var improveGame))
            return;

        // AddPotion 只能注册固定 ID，不能表达角色差异；原方法还会按物品类型缓存结果。
        var provider = improveGame.Code.GetType("ImproveGame.Common.GlobalItems.ApplyBuffItem");
        // 1.8.2 安装包及新版源码使用复数名称，旧版使用单数名称。
        var method = provider?.GetMethod("GetItemBuffTypes", BindingFlags.Public | BindingFlags.Static, [typeof(Item)])
            ?? provider?.GetMethod("GetItemBuffType", BindingFlags.Public | BindingFlags.Static, [typeof(Item)]);
        if (method == null || method.ReturnType != typeof(List<int>))
        {
            Mod.Logger.Warn($"ImproveGame {improveGame.Version}: no supported GetItemBuffTypes/GetItemBuffType API; Honey Bread infinite-buff compatibility was not installed.");
            return;
        }

        MonoModHooks.Add(method, ResolveFoodBuffs);
        Mod.Logger.Info($"Honey Bread infinite-buff compatibility installed for ImproveGame {improveGame.Version}: {method.Name}.");
    }

    private static List<int> ResolveFoodBuffs(Func<Item, List<int>> original, Item item)
    {
        var buffs = original(item);
        // 空结果代表未达到堆叠门槛、功能关闭或被忽略，不能自行启用无限增益。
        if (item.type != ModContent.ItemType<HoneyBread>() || buffs.Count == 0)
            return buffs;

        // ImproveGame 对背包、药水袋、储存管理器和队友共享统一应用到 LocalPlayer。
        // 不修改物品字段或它的全局缓存，避免持有者身份影响接收者。
        return Main.LocalPlayer.GetModPlayer<ElainaModplayer>().Elaina
            ? [BuffID.WellFed2, ModContent.BuffType<塔の西>()]
            : [BuffID.WellFed];
    }
}

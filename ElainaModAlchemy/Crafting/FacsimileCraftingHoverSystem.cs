using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

/// <summary>记录真正生成悬停物品的配方，不以选中项或产物类型猜测配方。</summary>
public sealed class FacsimileCraftingHoverSystem : ModSystem
{
    private static Recipe hoveredRecipe;
    private static Item hoveredItem;

    public override void Load()
    {
        IL_Main.DrawInventory += PatchCraftingHover;
        IL_Main.HoverOverCraftingItemButton += PatchCraftingHover;
    }

    public override void Unload()
    {
        IL_Main.DrawInventory -= PatchCraftingHover;
        IL_Main.HoverOverCraftingItemButton -= PatchCraftingHover;
        hoveredRecipe = null;
        hoveredItem = null;
    }

    private static void PatchCraftingHover(ILContext il)
    {
        var cursor = new ILCursor(il);
        // 两种合成列表都会先克隆 Recipe.createItem，再通过 MouseHover(22)
        // 再克隆一次。保留栈上的 Recipe，等最终 HoverItem 生成后才绑定引用。
        if (!cursor.TryGotoNext(
            i => i.MatchLdfld<Recipe>(nameof(Recipe.createItem)),
            i => i.MatchCallvirt<Item>(nameof(Item.Clone)),
            i => i.MatchStsfld<Main>(nameof(Main.HoverItem)),
            i => i.MatchLdcI4(ItemSlot.Context.CraftingMaterial),
            i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.MouseHover))))
            throw new InvalidOperationException($"Ashen Facsimile Dust: crafting hover was not found in {il.Method.Name}.");

        cursor.Emit(OpCodes.Dup);
        cursor.Index += 5;
        cursor.EmitDelegate<Action<Recipe>>(CaptureHover);
    }

    internal static void CaptureHover(Recipe recipe)
    {
        hoveredRecipe = recipe;
        hoveredItem = Main.HoverItem;
    }

    internal static bool TryGetHoveredRecipe(Item item, out Recipe recipe)
    {
        recipe = null;
        if (!Main.playerInventory || !Main.craftingHide || !Main.guideItem.IsAir ||
            item.tooltipContext != ItemSlot.Context.CraftingMaterial ||
            !ReferenceEquals(item, Main.HoverItem) || !ReferenceEquals(item, hoveredItem) ||
            hoveredRecipe == null || item.type != hoveredRecipe.createItem.type)
            return false;
        recipe = hoveredRecipe;
        return true;
    }
}

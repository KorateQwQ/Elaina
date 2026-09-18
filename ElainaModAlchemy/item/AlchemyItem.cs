using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>全新炼金系统的产物基类。具体产物统一放在本目录或其子目录中。</summary>
public abstract class AlchemyItem : ModItem
{
    /// <summary>
    /// 此物品的炼金配方；null 表示尚未配置配方，不能制作。
    /// 包含模组物品 ID 时，应在内容加载完成后创建配方，
    /// 例如在属性中延迟初始化，避免在静态初始化时调用 ModContent.ItemType。
    /// </summary>
    public virtual AlchemyRecipe AlchemyRecipe => null;

    /// <summary>
    /// 当前玩家是否已解锁配方。未来 UI 以此决定显示真实物品还是问号；
    /// 未解锁的配方不可制作。解锁条件及其状态来源由具体物品决定。
    /// </summary>
    public abstract bool IsRecipeUnlocked(Player player);

    /// <summary>
    /// 可选的配方获取提示；null 表示没有提示。
    /// 子类可使用 this.GetLocalization("RecipeUnlockHint") 提供本地化文本。
    /// </summary>
    public virtual LocalizedText RecipeUnlockHint => null;

    /// <summary>
    /// 统一检查配方解锁与材料数量，不消耗材料、不生成产物。
    /// 未来制作入口须在执行制作前调用此方法。
    /// </summary>
    public bool CanCraft(Player player, IReadOnlyList<Item> materials)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(materials);
        return IsRecipeUnlocked(player) && AlchemyRecipe is { } recipe && recipe.HasIngredients(materials);
    }
}

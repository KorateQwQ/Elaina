using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item.Foods;

/// <summary>炖菜：食用后获得酒足饭饱。</summary>
public sealed class BeefStew : AlchemyFood
{
    public override string EntryId => "stew";
    protected override int FoodBuffType => BuffID.WellFed3;
}

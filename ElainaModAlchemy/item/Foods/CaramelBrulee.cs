using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item.Foods;

/// <summary>焦糖布蕾：食用后获得吃得好。</summary>
public sealed class CaramelBrulee : AlchemyFood
{
    public override string EntryId => "brulee";
    protected override int FoodBuffType => BuffID.WellFed;
}

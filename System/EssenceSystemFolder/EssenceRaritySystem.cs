using KL.Utils;
using Terraria.GameContent.UI;
using Terraria.ID;
using 伊蕾娜.Items;

namespace 伊蕾娜.System.EssenceSystemFolder;

public class EssenceRaritySystem : ModSystem
{
    public static int?  ItemMaxRarity = 0;
    public static float MaxBossState = 0;

    public override void Load()
    {
        On_ItemRarity.Initialize += On_ItemRarityOnInitialize;
        base.Load();
    }

    private void On_ItemRarityOnInitialize(On_ItemRarity.orig_Initialize orig)
    {
        orig.Invoke();
        /*if (RarityLoader.GetRarity(ItemRarityID.Count + RarityLoader.RarityCount - 1) is { } rarity)
        {
            
        }*/
    }

    public override void PostAddRecipes()
    {
        ItemMaxRarity = RarityLoader.GetRarity(RarityLoader.RarityCount-1)?.Type;
        MaxBossState = KLGameStateManager.GetWorldMaxBossValue();
        base.PostAddRecipes();
    }

    public override void PostUpdateProjectiles()
    {
        //获取所有物品的最大稀有度
        //PrintText(Main.LocalPlayer.HeldItem.rare);
        //PrintText($"MaxRarityInSystem: {ItemMaxRarity} ");
        //PrintText($"MaxBossStateInSystem: {MaxBossState}");
        base.PostUpdateProjectiles();
    }
}
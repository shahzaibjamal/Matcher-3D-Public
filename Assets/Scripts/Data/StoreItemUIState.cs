using System;
using System.Collections.Generic;

[Serializable]
public class StoreItemUIState
{
    public string ItemID;
    public string Name;
    public StoreItemCategory Category;
    public StorePurchaseType PurchaseType; // "IAP" or "Gold"
    public StoreCurrencyType CurrencyType;

    public bool IsVisible;
    public int DisplayCost;

    // The list of rewards that will be passed to Inventory.AddRewards
    public List<RewardData> ProcessedRewards;

    public int DisplayQuantity => ProcessedRewards.Count > 0 ? ProcessedRewards[0].Amount : 0;
}
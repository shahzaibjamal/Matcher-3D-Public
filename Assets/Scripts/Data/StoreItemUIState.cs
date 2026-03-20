using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoreItemUIState
{
    public string ItemID;
    public string Name;
    public StoreItemCategory Category;
    public StorePurchaseType PurchaseType; // "IAP" or "Gold"
    public StoreCurrencyType CurrencyType;

    public bool IsVisible;
    public float DisplayCost;
    public string GetFormattedCost()
    {
        if (PurchaseType == StorePurchaseType.IAP)
        {
            // "f2" ensures two decimal places (0.99)
            return $"${DisplayCost:f2}";
        }

        // For Gold, we usually want whole numbers
        return Mathf.FloorToInt(DisplayCost).ToString();
    }
    // The list of rewards that will be passed to Inventory.AddRewards
    public List<RewardData> ProcessedRewards;

    public int DisplayQuantity => ProcessedRewards.Count > 0 ? ProcessedRewards[0].Amount : 0;
}
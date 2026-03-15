using System;
using System.Collections.Generic;

[Serializable]
public class StoreItemData
{
    public string Id;
    public string Name;
    public StoreItemCategory Category; // "Currency", "Bundle", "PowerUp"
    public StorePurchaseType PurchaseType; // "IAP" or "Gold"
    public float Cost;
    public StoreCurrencyType CurrencyType; // "USD" or "Gold"

    // Parallel arrays based on your sheet design
    public List<RewardData> Rewards;
    public string Description;
}

public enum StoreItemCategory
{
    Currency,
    Bundle,
    PowerUp,
    Replenish
}
public enum StorePurchaseType
{
    IAP,
    GoldPurchase,
}
public enum StoreCurrencyType
{
    USD,
    Gold,
}
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PurchaseManager : MonoBehaviour
{
    public static PurchaseManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary> Gets items for the shop, filtered by your enum category. </summary>
    public List<StoreItemUIState> GetStoreFront(StoreItemCategory? filterCategory = null)
    {
        return DataManager.Instance.Metadata.StoreItems
            .Select(ProcessItemState)
            .Where(state => state.IsVisible && (filterCategory == null || state.Category == filterCategory))
            .ToList();
    }

    private StoreItemUIState ProcessItemState(StoreItemData data)
    {
        var save = GameManager.Instance.SaveData;
        var state = new StoreItemUIState
        {
            ItemID = data.Id,
            IsVisible = true,
            Name = data.Name,
            Category = data.Category, // Match your sheet string to enum
            PurchaseType = data.PurchaseType,
            CurrencyType = data.CurrencyType,
            ProcessedRewards = new List<RewardData>()
        };

        foreach (var r in data.Rewards)
        {
            RewardData newReward = new RewardData { RewardType = r.RewardType, Amount = r.Amount };

            if (r.RewardType == RewardType.Heart)
            {
                int missingLives = save.MaxLives - save.CurrentLives;
                if (missingLives <= 0)
                {
                    state.IsVisible = false;
                    return state;
                }

                // Pro-rate based on enum logic
                float unitCost = data.Cost / r.Amount;
                state.DisplayCost = Mathf.CeilToInt(unitCost * missingLives);
                newReward.Amount = missingLives;
            }
            else
            {
                state.DisplayCost = (int)data.Cost;
            }

            state.ProcessedRewards.Add(newReward);
        }

        return state;
    }

    public void PurchaseItem(string itemID)
    {
        StoreItemData data = DataManager.Instance.GetStoreItemByID(itemID);
        if (data == null) return;

        StoreItemUIState currentState = ProcessItemState(data);
        var save = GameManager.Instance.SaveData;

        // Check if the cost is Gold or Real Money using your enum
        if (currentState.CurrencyType == StoreCurrencyType.Gold)
        {
            // Use the gold update method you provided
            if (save.Inventory.TryUpdateGoldAmount(-currentState.DisplayCost))
            {
                // Grant rewards using the strictly RewardData-only method
                save.Inventory.AddRewards(currentState.ProcessedRewards);

                GameSaveData.OnLivesChanged?.Invoke();
                Debug.Log($"Purchased {data.Name} for {currentState.DisplayCost} Gold.");
            }
        }
        else if (currentState.CurrencyType == StoreCurrencyType.USD)
        {
            // Trigger IAP Logic here
            Debug.Log($"Initiating Real Money Transaction: {currentState.DisplayCost} USD");
            // OnSuccess: save.Inventory.AddRewards(currentState.ProcessedRewards);
        }
    }
}
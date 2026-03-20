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
    public Dictionary<StoreItemCategory, List<StoreItemUIState>> GetGroupedStoreFront()
    {
        // Gets all visible items and groups them by their Enum Category
        return GetStoreFront()
            .GroupBy(item => item.Category)
            .ToDictionary(group => group.Key, group => group.ToList());
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
                int missingLives = DataManager.Instance.Metadata.Settings.MaxLives - save.CurrentLives;
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

    public void PurchaseItem(string itemID, System.Action<bool> onComplete = null)
    {
        StoreItemData data = DataManager.Instance.GetStoreItemByID(itemID);
        if (data == null) { onComplete?.Invoke(false); return; }

        StoreItemUIState currentState = ProcessItemState(data);
        var save = GameManager.Instance.SaveData;

        // --- GOLD PURCHASE ---
        if (currentState.CurrencyType == StoreCurrencyType.Gold)
        {
            if (save.Inventory.TryUpdateGoldAmount(-(int)currentState.DisplayCost))
            {
                FulfillRewards(currentState.ProcessedRewards);
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("Insufficient Gold.");
                onComplete?.Invoke(false);
            }
        }
        // --- USD (IAP) PURCHASE ---
        else if (currentState.CurrencyType == StoreCurrencyType.USD)
        {
            // Connectivity Check
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogError("No Internet Connection. Cannot initiate IAP.");
                // You could trigger a "No Internet" Popup here
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log($"Initiating Real Money Transaction: {currentState.DisplayCost} USD");

            IAPManager.Instance.BuyProductID(itemID, (success) =>
            {
                if (success)
                {
                    // The only place USD rewards are granted
                    FulfillRewards(currentState.ProcessedRewards);
                    onComplete?.Invoke(true);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            });
        }
    }

    /// <summary>
    /// The Single Authority for granting items. 
    /// Called by both Gold and IAP paths.
    /// </summary>
    private void FulfillRewards(List<RewardData> rewards)
    {
        var save = GameManager.Instance.SaveData;

        // 1. Logic: Update the Inventory
        save.Inventory.AddRewards(rewards);

        // 2. Visuals: Show the reward popups via your RewardManager
        RewardManager.Instance.AddRewardsToQueue(rewards);
        Scheduler.Instance.ExecuteAfterDelay(0.5f, () => RewardManager.Instance.CheckAndShowNext());

        // 3. UI Events: Notify listeners (like Heart bars)
        GameEvents.OnLivesChanged?.Invoke();
        SoundController.Instance.PlaySoundEffect("cash");

        Debug.Log("Rewards fulfilled and UI notified.");
    }
}
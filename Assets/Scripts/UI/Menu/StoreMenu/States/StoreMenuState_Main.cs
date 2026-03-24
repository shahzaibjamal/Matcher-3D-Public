using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StoreMenuBaseState_Main : StoreMenuBaseState
{
    private List<StoreItemView> _activeItemViews = new List<StoreItemView>();

    // Direct references to the Replenish UI elements
    private GameObject _replenishLabel;
    private GameObject _replenishGrid;

    private int _lastLifeCount;
    private float _updateTimer;
    private const float UPDATE_INTERVAL = 0.5f; // Check lives twice per second

    public StoreMenuBaseState_Main(StoreMenuController controller) : base(controller) { }

    public override void Enter()
    {
        base.Enter();
        _lastLifeCount = GameManager.Instance.SaveData.CurrentLives;

        Scheduler.Instance.SubscribeUpdate(OnUpdate);
        UIAnimations.ToonIn(View.canvasGroup, View.Root, null);

        LoadStoreItems();
    }

    public override void Exit()
    {
        base.Exit();
        Scheduler.Instance.UnsubscribeUpdate(OnUpdate);
        _activeItemViews.Clear();
    }

    private void OnUpdate(float dt)
    {
        _updateTimer += dt;
        if (_updateTimer < UPDATE_INTERVAL) return;
        _updateTimer = 0;

        int maxLives = DataManager.Instance.Metadata.Settings.MaxLives;
        int currentLives = GameManager.Instance.SaveData.CurrentLives;

        // If life count changed (timer or purchase)
        if (currentLives != _lastLifeCount)
        {
            _lastLifeCount = currentLives;

            // If lives are now full, remove the replenish section immediately
            if (currentLives >= maxLives && _replenishGrid != null)
            {
                RemoveReplenishSection();
            }
            else
            {
                RefreshStoreItems();
            }
        }
    }

    public void LoadStoreItems()
    {
        // 1. Clear current UI and list
        foreach (Transform child in View.StoreItemsContainer) GameObject.Destroy(child.gameObject);
        _activeItemViews.Clear();
        _replenishLabel = null;
        _replenishGrid = null;

        var groupedStore = PurchaseManager.Instance.GetGroupedStoreFront();
        int maxLives = DataManager.Instance.Metadata.Settings.MaxLives;
        int currentLives = GameManager.Instance.SaveData.CurrentLives;

        // 2. Iterate through categories and spawn directly
        foreach (StoreItemCategory category in System.Enum.GetValues(typeof(StoreItemCategory)))
        {
            if (!groupedStore.ContainsKey(category)) continue;

            // Skip hearts if already at max
            if (category == StoreItemCategory.Replenish && currentLives >= maxLives) continue;

            // Spawn Label directly into Container
            GameObject label = GameObject.Instantiate(View.CategoryLabelPrefab, View.StoreItemsContainer);
            label.GetComponentInChildren<TMPro.TMP_Text>().text = category.ToString();

            // Spawn Grid directly into Container
            GameObject grid = GameObject.Instantiate(View.GridContainerPrefab, View.StoreItemsContainer);

            // Track specifically if it's the Replenish category
            if (category == StoreItemCategory.Replenish)
            {
                _replenishLabel = label;
                _replenishGrid = grid;
            }

            // Spawn Item Cards
            foreach (var itemState in groupedStore[category])
            {
                GameObject card = GameObject.Instantiate(View.ItemCardPrefab, grid.transform);
                StoreItemView itemView = card.GetComponent<StoreItemView>();
                itemView.Setup(itemState, OnPurchaseCallback);
                _activeItemViews.Add(itemView);
            }
        }
    }

    private void RemoveReplenishSection()
    {
        if (_replenishLabel != null) GameObject.Destroy(_replenishLabel);
        if (_replenishGrid != null) GameObject.Destroy(_replenishGrid);

        _replenishLabel = null;
        _replenishGrid = null;

        // Clean up internal list of view references
        _activeItemViews.RemoveAll(item => item == null);

        Debug.Log("Lives full: Replenish section removed from Store.");
    }

    private void OnPurchaseCallback(bool success)
    {
        if (success)
        {
            int maxLives = DataManager.Instance.Metadata.Settings.MaxLives;
            if (GameManager.Instance.SaveData.CurrentLives >= maxLives)
                RemoveReplenishSection();
            else
                RefreshStoreItems();
        }
    }

    private void RefreshStoreItems()
    {
        var save = GameManager.Instance.SaveData;
        int maxLives = DataManager.Instance.Metadata.Settings.MaxLives;
        int currentLives = save.CurrentLives;

        // 1. Get the fresh grouped data from PurchaseManager 
        // This ensures ProcessItemState runs again with the NEW life count
        var freshGroupedStore = PurchaseManager.Instance.GetGroupedStoreFront();

        // 2. Iterate through our active views
        for (int i = _activeItemViews.Count - 1; i >= 0; i--)
        {
            var itemView = _activeItemViews[i];
            if (itemView == null) continue;

            // Try to find the matching fresh state for this specific item
            StoreItemUIState freshState = null;
            foreach (var group in freshGroupedStore.Values)
            {
                freshState = group.FirstOrDefault(s => s.ItemID == itemView.State.ItemID);
                if (freshState != null) break;
            }

            // 3. Logic: If the item is no longer visible (e.g., hearts full)
            if (freshState == null || !freshState.IsVisible)
            {
                GameObject.Destroy(itemView.gameObject);
                _activeItemViews.RemoveAt(i);
            }
            else
            {
                // 4. Update the card with the pro-rated cost and reward amount
                itemView.Setup(freshState, OnPurchaseCallback);
                itemView.RefreshUI();
            }
        }

        // 5. Cleanup: If the Replenish category is now empty, kill the label and grid
        CheckAndCleanupEmptyCategories();
    }

    private void CheckAndCleanupEmptyCategories()
    {
        // If we have no more "Replenish" items in our active list, kill the headers
        bool hasReplenish = _activeItemViews.Any(v => v != null && v.State.Category == StoreItemCategory.Replenish);

        if (!hasReplenish && _replenishGrid != null)
        {
            GameObject.Destroy(_replenishLabel);
            GameObject.Destroy(_replenishGrid);
            _replenishLabel = null;
            _replenishGrid = null;
        }
    }
}
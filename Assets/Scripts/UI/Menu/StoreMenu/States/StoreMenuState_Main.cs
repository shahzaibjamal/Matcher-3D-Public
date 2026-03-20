using System.Collections.Generic;
using UnityEngine;

public class StoreMenuBaseState_Main : StoreMenuBaseState
{
    // Store references to the active views to refresh them later
    private List<StoreItemView> _activeItemViews = new List<StoreItemView>();

    public StoreMenuBaseState_Main(StoreMenuController controller) : base(controller) { }

    public override void Enter()
    {
        base.Enter();
        UIAnimations.ToonIn(View.canvasGroup, View.Root, null);
        LoadStoreItems();
    }

    public override void Exit()
    {
        base.Exit();
        // Clear references on exit to prevent memory leaks
        _activeItemViews.Clear();
    }

    public void LoadStoreItems()
    {
        // 1. Clear current UI and references
        foreach (Transform child in View.StoreItemsContainer) GameObject.Destroy(child.gameObject);
        _activeItemViews.Clear();

        // 2. Get Grouped Data
        var groupedStore = PurchaseManager.Instance.GetGroupedStoreFront();

        // 3. Iterate through categories
        foreach (StoreItemCategory category in System.Enum.GetValues(typeof(StoreItemCategory)))
        {
            if (!groupedStore.ContainsKey(category)) continue;

            // Spawn Category Label
            GameObject label = GameObject.Instantiate(View.CategoryLabelPrefab, View.StoreItemsContainer);
            label.GetComponentInChildren<TMPro.TMP_Text>().text = category.ToString();

            // Spawn Grid Container
            GameObject grid = GameObject.Instantiate(View.GridContainerPrefab, View.StoreItemsContainer);

            // Spawn items into that grid
            foreach (var itemState in groupedStore[category])
            {
                GameObject card = GameObject.Instantiate(View.ItemCardPrefab, grid.transform);
                StoreItemView itemView = card.GetComponent<StoreItemView>();

                // Initialize and track the view
                itemView.Setup(itemState, OnPurchaseCallback);
                _activeItemViews.Add(itemView);
            }
        }
    }

    private void OnPurchaseCallback(bool success)
    {
        if (success)
        {
            Debug.Log("Purchase Successful! Refreshing Store UI...");
            RefreshStoreItems();
        }
    }

    private void RefreshStoreItems()
    {
        // Instead of Re-Instantiating everything, we just tell existing cards to update their visuals
        // This is much better for performance and prevents layout "flicker"
        foreach (var itemView in _activeItemViews)
        {
            if (itemView != null)
            {
                itemView.RefreshUI();
            }
        }
    }
}
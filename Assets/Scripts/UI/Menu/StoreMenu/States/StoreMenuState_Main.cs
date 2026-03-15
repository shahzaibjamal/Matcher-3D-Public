using UnityEngine;

public class StoreMenuBaseState_Main : StoreMenuBaseState
{
    public StoreMenuBaseState_Main(StoreMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        UIAnimations.ToonIn(View.canvasGroup, View.Root, null);
        LoadStoreItems();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public void LoadStoreItems()
    {
        // 1. Clear current UI
        foreach (Transform child in View.StoreItemsContainer) GameObject.Destroy(child.gameObject);

        // 2. Get Grouped Data
        var groupedStore = PurchaseManager.Instance.GetGroupedStoreFront();

        // 3. Iterate through categories (Order them as you like)
        foreach (StoreItemCategory category in System.Enum.GetValues(typeof(StoreItemCategory)))
        {
            if (!groupedStore.ContainsKey(category)) continue;

            // Spawn the Label (e.g., "Replenish", "Gold Purchases")
            GameObject label = GameObject.Instantiate(View.CategoryLabelPrefab, View.StoreItemsContainer);
            label.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = category.ToString();

            // Spawn a Grid Container for the 2-column layout
            GameObject grid = GameObject.Instantiate(View.GridContainerPrefab, View.StoreItemsContainer);

            // Spawn items into that grid
            foreach (var itemState in groupedStore[category])
            {
                GameObject card = GameObject.Instantiate(View.ItemCardPrefab, grid.transform);
                card.GetComponent<StoreItemView>().Setup(itemState);
            }
        }
    }
}

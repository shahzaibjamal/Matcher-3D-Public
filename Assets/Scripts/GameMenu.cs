using UnityEngine;
using System.Collections.Generic;

public class GameMenu : MonoBehaviour
{
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private Transform itemViewParent;
    [SerializeField] private ItemView itemViewPrefab;

    private Dictionary<string, ItemView> itemViews = new Dictionary<string, ItemView>();

    void Start()
    {
        // slotManager.OnLevelReady += InitializeUI;
        // slotManager.OnMatchResolved += HandleMatchResolved;
    }

    private void InitializeUI(LevelData level)
    {
        foreach (Transform child in itemViewParent)
            Destroy(child.gameObject);

        itemViews.Clear();

        foreach (string uid in level.itemsToCollect)
        {
            ItemData item = Metadata.Instance.itemDatabase.GetItemByUID(uid);
            if (item == null) continue;

            int count = level.itemsToSpawn.Find(e => e.itemUID == uid)?.count ?? 0;
            ItemView view = Instantiate(itemViewPrefab, itemViewParent);
            view.SetItem(item, count);
            itemViews[uid] = view;
        }
    }

    private void HandleMatchResolved(ItemData matchedItem)
    {
        if (itemViews.TryGetValue(matchedItem.UID, out ItemView view))
        {
            view.UpdateCount(-3);

            if (view.CurrentCount <= 0)
            {
                Destroy(view.gameObject);
                itemViews.Remove(matchedItem.UID);

                if (itemViews.Count == 0)
                {
                    // slotManager.NotifyLevelComplete();
                }
            }
        }
    }
}

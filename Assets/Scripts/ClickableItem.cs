using UnityEngine;

public class ClickableItem : MonoBehaviour
{
    public ItemData itemData; // assigned by Spawner

    void OnMouseDown()
    {
        if (itemData == null) return;

        // Occupy slot immediately, animate this item into slot
        SlotManager.Instance.AnimateItemToSlot(itemData, transform);
    }
}

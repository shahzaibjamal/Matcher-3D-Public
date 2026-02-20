using System;
using UnityEngine;

public class ClickableItem : MonoBehaviour
{
    public ItemData ItemData;
    public Action<ItemData, Transform> OnItemClicked;

    void OnMouseDown()
    {
        if (ItemData == null) return;

        OnItemClicked?.Invoke(ItemData, transform);
    }

    void OnDestroy()
    {
        OnItemClicked = null;
    }
}

using System;
using UnityEngine;

public class ClickableItem : MonoBehaviour
{
    [HideInInspector]
    public ItemData ItemData;
    public Collider Collider;
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

using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableItem : MonoBehaviour
{
    [HideInInspector]
    public ItemData ItemData;
    public Collider Collider;
    public Rigidbody Rigidbody;
    public Action<ItemData, Transform> OnItemClicked;

    void OnMouseDown()
    {
        if (ItemData == null) return;

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; // Stop here, the click was on UI
        }

        OnItemClicked?.Invoke(ItemData, transform);
    }

    void OnDestroy()
    {
        OnItemClicked = null;
    }
}

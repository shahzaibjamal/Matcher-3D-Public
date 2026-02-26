using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableItem : MonoBehaviour, IClickable
{
    [HideInInspector]
    public ItemData ItemData;
    public Collider Collider;
    public Rigidbody Rigidbody;
    public Action<ItemData, Transform> OnItemClicked;

    void OnMouseDown()
    {
        if (GameManager.Instance.UseRaycast)
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; // Stop here, the click was on UI
        }

        HandleLogic();
    }

    void OnDestroy()
    {
        OnItemClicked = null;
    }

    public void OnHandleClick(RaycastHit hitInfo)
    {
        if (!GameManager.Instance.UseRaycast)
        {
            return;
        }
        HandleLogic();
    }

    private void HandleLogic()
    {
        if (ItemData == null) return;

        OnItemClicked?.Invoke(ItemData, transform);
    }
}

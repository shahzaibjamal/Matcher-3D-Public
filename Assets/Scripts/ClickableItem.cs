using System;
using DG.Tweening;
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

    public void Highlight(bool isHinted)
    {
        int targetLayer = isHinted ? LayerMask.NameToLayer("Hint") : LayerMask.NameToLayer("Default");

        // Apply to parent and all children recursively
        SetLayerRecursive(gameObject, targetLayer);

        if (isHinted)
        {
            // Add the Scale Pulse (Motion is key to catching the eye)
            transform.DOScale(Vector3.one * 1.2f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetId("HintLoop");
        }
        else
        {
            // Reset
            DOTween.Kill("HintLoop");
            transform.DOScale(Vector3.one, 0.2f);
        }
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }
}

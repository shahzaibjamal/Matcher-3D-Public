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

    private int _hintLayer = -1;
    private int _defaultLayer = -1;
    [Header("Settings")]
    public float liftAmount = 0.2f;
    public float animationSpeed = 0.15f;

    private Vector3 _originalLocalPos;
    private bool _isInitialized = false;

    void Awake()
    {
        _hintLayer = LayerMask.NameToLayer("Hint");
        _defaultLayer = LayerMask.NameToLayer("Default");
    }

    void OnDestroy()
    {
        OnItemClicked = null;
    }
    private void EnsureInit()
    {
        if (_isInitialized) return;
        _originalLocalPos = transform.localPosition;
        _isInitialized = true;
    }

    public void OnHandleClick(RaycastHit hitInfo)
    {
        HandleLogic();
    }

    private void HandleLogic()
    {
        if (ItemData == null) return;

        // Apply to parent and all children recursively
        SetLayerRecursive(gameObject, _defaultLayer);

        OnItemClicked?.Invoke(ItemData, transform);
    }

    public void Highlight(bool isHinted)
    {
        int targetLayer = isHinted ? _hintLayer : _defaultLayer;

        // Apply to parent and all children recursively
        SetLayerRecursive(gameObject, targetLayer);

        if (isHinted)
        {
            // Add the Scale Pulse (Motion is key to catching the eye)
            transform.DOScale(Vector3.one * 1.05f, 0.5f)
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
    public void OnPointerDown(RaycastHit hitInfo)
    {
        if (Collider != null && !Collider.enabled) return;
        EnsureInit();
        Highlight(true);

        // Kill any existing "Drop" animation before starting "Lift"
        transform.DOKill();
        transform.DOLocalMove(_originalLocalPos + Vector3.up * liftAmount, 0.1f).SetEase(Ease.OutCubic).SetId("HoverTween"); ;
    }

    public void OnPointerUp()
    {
        if (Collider != null && !Collider.enabled) return;

        Highlight(false);

        // Smoothly return home
        transform.DOKill();
        transform.DOLocalMove(_originalLocalPos, 0.15f).SetEase(Ease.OutQuad).SetId("HoverTween"); ;
    }

}

using System;
using DG.Tweening;
using UnityEngine;

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

    // Static gatekeeper shared by all instances
    private static bool _isTrayFillable = true;

    void Awake()
    {
        _hintLayer = LayerMask.NameToLayer("Hint");
        _defaultLayer = LayerMask.NameToLayer("Default");
    }

    private void OnEnable()
    {
        GameEvents.OnSlotsFillableEvent += HandleFillableStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnSlotsFillableEvent -= HandleFillableStateChanged;
    }

    private void HandleFillableStateChanged(bool fillable)
    {
        _isTrayFillable = fillable;
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
        // If tray is full, do NOT process the click logic (sending to tray)
        if (!_isTrayFillable)
        {
            // --- IMPLEMENT SFX FOR DENIAL HERE ---
            // SoundController.Instance.Play("DenyClick");
            return;
        }

        HandleLogic();
    }

    private void HandleLogic()
    {
        if (ItemData == null) return;
        SetLayerRecursive(gameObject, _defaultLayer);
        OnItemClicked?.Invoke(ItemData, transform);
    }

    public void Highlight(bool isHinted)
    {
        int targetLayer = isHinted ? _hintLayer : _defaultLayer;
        SetLayerRecursive(gameObject, targetLayer);

        if (isHinted)
        {
            transform.DOScale(Vector3.one * 1.05f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetId("HintLoop");
        }
        else
        {
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

        transform.DOKill();

        if (_isTrayFillable)
        {
            // Normal Lift
            Highlight(true);
            transform.DOLocalMove(_originalLocalPos + Vector3.up * liftAmount, 0.1f)
                .SetEase(Ease.OutCubic)
                .SetId("HoverTween");
        }
        else
        {
            // Denial "Wobble" or "Heavy Lift"
            // We lift it much less and add a little shake to show it's stuck
            transform.DOLocalMove(_originalLocalPos + Vector3.up * (liftAmount * 0.3f), 0.05f)
                .SetEase(Ease.OutBounce)
                .OnComplete(() => transform.DOShakeRotation(0.2f, 5f, 10, 90));

            // --- IMPLEMENT VFX FOR DENIAL HERE ---
        }
    }

    public void OnPointerUp()
    {
        if (Collider != null && !Collider.enabled) return;

        Highlight(false);

        // Smoothly return home
        transform.DOKill();
        transform.DOLocalMove(_originalLocalPos, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetId("HoverTween");
    }
}
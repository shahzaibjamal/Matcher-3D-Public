using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ClickableItem : MonoBehaviour, IClickable
{
    [HideInInspector]
    public ItemData ItemData;

    [HideInInspector]
    public Rigidbody Rigidbody;

    public Action<ItemData, Transform> OnItemClicked;
    public bool IsUpright;
    public Vector3 Rotation;
    public Vector3 Position;

    [SerializeField] private Collider[] _colliders; // Changed from single Collider to Array

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
        if (Rigidbody == null)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }
        // Fallback: If for some reason the array is empty, fetch them now
        if (_colliders == null || _colliders.Length == 0)
        {
            _colliders = GetComponentsInChildren<Collider>();
        }
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
        transform.DOKill();
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
        if (IsActiveOpeningTutorial())
        {
            if (IsFTUETarget())
            {
                HandleLogic();
            }
            return;
        }
        // If tray is full, do NOT process the click logic (sending to tray)
        if (!_isTrayFillable)
        {
            SoundController.Instance.PlaySoundEffect("Deny");
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
            transform.DOKill();
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
        if (!IsInteractable()) return;
        EnsureInit();

        transform.DOKill();

        if (_isTrayFillable)
        {
            // Normal Lift
            Highlight(true);
            transform.DOLocalMove(transform.localPosition + Vector3.up * liftAmount, 0.1f)
                .SetEase(Ease.OutCubic)
                .SetId("HoverTween");
        }
        else
        {
            // Denial "Wobble" or "Heavy Lift"
            // We lift it much less and add a little shake to show it's stuck
            transform.DOLocalMove(transform.localPosition + Vector3.up * (liftAmount * 0.3f), 0.05f)
                .SetEase(Ease.OutBounce)
                .OnComplete(() => transform.DOShakeRotation(0.2f, 5f, 10, 90));

            // --- IMPLEMENT VFX FOR DENIAL HERE ---
        }
    }

    public void OnPointerUp()
    {
        if (!IsInteractable()) return;

        Highlight(false);

        // Smoothly return home
        transform.DOKill();
        transform.DOLocalMove(transform.localPosition, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetId("HoverTween");
    }

    public void SetCollidersEnabled(bool isEnabled)
    {
        if (_colliders == null || _colliders.Length == 0) return;

        foreach (var col in _colliders)
        {
            if (col != null) col.enabled = isEnabled;
        }
    }

    private bool IsActiveOpeningTutorial()
    {
        return FTUEManager.Instance != null &&
            FTUEManager.Instance.IsTutorialActive() &&
            !FTUEManager.Instance.IsSequenceCompleted("Opening");
    }

    private bool IsFTUETarget()
    {
        return IsActiveOpeningTutorial() &&
            TryGetComponent<FTUETarget>(out var target) &&
            FTUEManager.Instance.IsCurrentTarget(target.TargetID);
    }

    /// <summary>
    /// Helper to check if the item is currently interactable
    /// </summary>
    private bool IsInteractable()
    {
        bool hasColliders = _colliders != null && _colliders.Length > 0 && _colliders[0].enabled;
        if (!hasColliders) return false;

        // If we are in the Opening tutorial, ONLY the target is interactable
        if (IsActiveOpeningTutorial())
        {
            return IsFTUETarget();
        }

        // Otherwise, allow normal interaction
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 1. Rigidbody Safety Check
        if (Rigidbody == null)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        // 2. Collider Safety Check & Pre-fetch
        var foundColliders = GetComponentsInChildren<Collider>(true);

        // Check if we need to refresh the main array
        bool needsUpdate = _colliders == null || _colliders.Length != foundColliders.Length;

        if (!needsUpdate && _colliders != null)
        {
            foreach (var col in _colliders)
            {
                if (col == null)
                {
                    needsUpdate = true;
                    break;
                }
            }
        }

        if (needsUpdate)
        {
            _colliders = foundColliders;

            // 3. Update ColliderLinks for deep hierarchy support
            foreach (var col in _colliders)
            {
                if (col == null) continue;

                // Ensure the child has a Link component pointing back to this script
                if (!col.TryGetComponent<ClickableLink>(out var link))
                {
                    link = col.gameObject.AddComponent<ClickableLink>();
                }

                link.ParentScript = this;
                UnityEditor.EditorUtility.SetDirty(col.gameObject);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"<color=cyan>ClickableItem:</color> Updated {foundColliders.Length} colliders and links for {gameObject.name}");
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (_colliders == null || _colliders.Length == 0) return;

        foreach (var col in _colliders)
        {
            if (col == null || !col.enabled) continue;

            // Set the color for the primitive outlines
            Gizmos.color = Color.green;

            if (col is BoxCollider box)
            {
                // Align the gizmo with the box's local rotation and scale
                Gizmos.matrix = col.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);

                // Add a faint fill so you can see the volume
                Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                // Spheres are drawn relative to their world position and radius
                Gizmos.matrix = Matrix4x4.identity;
                Vector3 worldCenter = col.transform.TransformPoint(sphere.center);
                float worldRadius = sphere.radius * Mathf.Max(col.transform.lossyScale.x, col.transform.lossyScale.y, col.transform.lossyScale.z);

                Gizmos.DrawWireSphere(worldCenter, worldRadius);
                Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
                Gizmos.DrawSphere(worldCenter, worldRadius);
            }
            else if (col is CapsuleCollider capsule)
            {
                // Capsules are tricky to draw with standard Gizmos, 
                // so we represent them with a WireCube for click-area visualization
                Gizmos.matrix = col.transform.localToWorldMatrix;
                Vector3 size = Vector3.one;
                // Adjust size based on capsule direction (0=X, 1=Y, 2=Z)
                if (capsule.direction == 0) size = new Vector3(capsule.height, capsule.radius * 2, capsule.radius * 2);
                else if (capsule.direction == 1) size = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2);
                else size = new Vector3(capsule.radius * 2, capsule.radius * 2, capsule.height);

                Gizmos.DrawWireCube(capsule.center, size);
                Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
                Gizmos.DrawCube(capsule.center, size);
            }

            // Reset matrix for the next iteration
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
#endif
}
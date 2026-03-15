using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Physics Settings")]
    [SerializeField] private LayerMask clickLayer;
    [SerializeField] private float maxDistance = 100f;

    [Header("Detection Strategy")]
    [Tooltip("If true, looks for IClickable directly on the collider's GameObject. If false, uses ClickableLink or searches parents.")]
    [SerializeField] private bool directClickableOnly = true;

    [Header("Threshold Settings")]
    [SerializeField] private float holdThreshold = 0.15f;
    [SerializeField] private float moveThreshold = 10f;

    private Camera _mainCam;
    private IClickable _currentHoveredTarget;

    // State Tracking
    private Vector2 _startMousePos;
    private float _holdTimer;
    private bool _isSweepModeActive;

    private Dictionary<KeyCode, Action> _keyCallbacks = new Dictionary<KeyCode, Action>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        _mainCam = Camera.main;
        Scheduler.Instance.SubscribeUpdate(OnUpdate);
    }

    void OnUpdate(float dt)
    {
        if (PerformanceDebugMenu.IsMouseOverMenu) return;

        HandleKeyboardInput();

        if (Input.GetMouseButtonDown(0))
        {
            HandlePointerDown();
        }
        else if (Input.GetMouseButton(0))
        {
            HandlePointerHeld(dt);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HandlePointerUp();
        }
    }

    private void HandlePointerDown()
    {
        if (IsPointerOverUI()) return;

        _startMousePos = Input.mousePosition;
        _holdTimer = 0;
        _isSweepModeActive = false;

        if (TryGetClickable(out _, out IClickable clickable))
        {
            _currentHoveredTarget = clickable;
        }
    }

    private void HandlePointerHeld(float dt)
    {
        _holdTimer += dt;
        float distMoved = Vector2.Distance(_startMousePos, Input.mousePosition);

        if (!_isSweepModeActive && (_holdTimer > holdThreshold || distMoved > moveThreshold))
        {
            _isSweepModeActive = true;
            if (_currentHoveredTarget != null)
            {
                TryGetClickable(out RaycastHit hit, out _);
                _currentHoveredTarget.OnPointerDown(hit);
            }
        }

        if (_isSweepModeActive)
        {
            if (IsPointerOverUI())
            {
                ClearCurrentHover();
                return;
            }

            if (TryGetClickable(out RaycastHit hit, out IClickable newTarget))
            {
                if (newTarget != _currentHoveredTarget)
                {
                    _currentHoveredTarget?.OnPointerUp();
                    _currentHoveredTarget = newTarget;
                    _currentHoveredTarget.OnPointerDown(hit);
                }
            }
            else
            {
                ClearCurrentHover();
            }
        }
    }

    private void HandlePointerUp()
    {
        if (_currentHoveredTarget != null)
        {
            MonoBehaviour targetBehaviour = _currentHoveredTarget as MonoBehaviour;

            if (!_isSweepModeActive)
            {
                _currentHoveredTarget.OnHandleClick(default);
                CheckFTUE(targetBehaviour);
            }
            else
            {
                if (TryGetClickable(out RaycastHit hit, out IClickable finalTarget))
                {
                    if (finalTarget == _currentHoveredTarget)
                    {
                        _currentHoveredTarget.OnHandleClick(hit);
                        CheckFTUE(targetBehaviour);
                    }
                }
            }

            _currentHoveredTarget.OnPointerUp();
            _currentHoveredTarget = null;
        }

        _isSweepModeActive = false;
    }

    private void CheckFTUE(MonoBehaviour target)
    {
        if (target != null && target.TryGetComponent<FTUETarget>(out var ftue))
        {
            ftue.OnObjectClicked();
        }
    }

    private void ClearCurrentHover()
    {
        if (_currentHoveredTarget != null)
        {
            _currentHoveredTarget.OnPointerUp();
            _currentHoveredTarget = null;
        }
    }

    private bool TryGetClickable(out RaycastHit hit, out IClickable clickable)
    {
        clickable = null;
        Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, maxDistance, clickLayer))
        {
            if (directClickableOnly)
            {
                // Toggle ON: Direct approach (Collider is on the same object as the script)
                clickable = hit.collider.GetComponent<IClickable>();
            }
            else
            {
                // Toggle OFF: Link/Recursive approach
                var link = hit.collider.GetComponent<ClickableLink>();
                if (link != null)
                {
                    clickable = link.ParentScript;
                }
                else
                {
                    clickable = hit.collider.GetComponentInParent<IClickable>();
                }
            }

            return clickable != null;
        }
        return false;
    }

    // --- Standard Boilerplate ---
    private void HandleKeyboardInput()
    {
        foreach (var keyEntry in _keyCallbacks)
            if (Input.GetKeyDown(keyEntry.Key)) keyEntry.Value?.Invoke();
    }

    public void RegisterKey(KeyCode key, Action callback)
    {
        if (!_keyCallbacks.ContainsKey(key)) _keyCallbacks[key] = null;
        _keyCallbacks[key] += callback;
    }

    public void UnregisterKey(KeyCode key, Action callback)
    {
        if (_keyCallbacks.ContainsKey(key)) _keyCallbacks[key] -= callback;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        for (int i = 0; i < Input.touchCount; ++i)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }
        return false;
    }

    void OnDestroy()
    {
        if (Scheduler.Instance != null) Scheduler.Instance.UnsubscribeUpdate(OnUpdate);
    }
}
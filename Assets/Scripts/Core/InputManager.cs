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

        // Just identify the potential first target, but don't lift yet
        if (TryGetClickable(out _, out IClickable clickable))
        {
            _currentHoveredTarget = clickable;
        }
    }

    private void HandlePointerHeld(float dt)
    {
        _holdTimer += dt;
        float distMoved = Vector2.Distance(_startMousePos, Input.mousePosition);

        // 1. Determine if we have transitioned from a "Tap" to a "Sweep"
        if (!_isSweepModeActive && (_holdTimer > holdThreshold || distMoved > moveThreshold))
        {
            _isSweepModeActive = true;
            // Lift the item we are currently over when the threshold breaks
            if (_currentHoveredTarget != null)
            {
                TryGetClickable(out RaycastHit hit, out _);
                _currentHoveredTarget.OnPointerDown(hit);
            }
        }

        // 2. If in Sweep Mode, update the hovered target every frame
        if (_isSweepModeActive)
        {
            if (IsPointerOverUI())
            {
                ClearCurrentHover();
                return;
            }

            if (TryGetClickable(out RaycastHit hit, out IClickable newTarget))
            {
                // If we moved onto a new object while sweeping
                if (newTarget != _currentHoveredTarget)
                {
                    _currentHoveredTarget?.OnPointerUp(); // Drop old
                    _currentHoveredTarget = newTarget;
                    _currentHoveredTarget.OnPointerDown(hit); // Lift new
                }
            }
            else
            {
                // Over empty space
                ClearCurrentHover();
            }
        }
    }

    private void HandlePointerUp()
    {
        if (_currentHoveredTarget != null)
        {
            // Case A: Fast Tap (threshold never reached)
            if (!_isSweepModeActive)
            {
                _currentHoveredTarget.OnHandleClick(default);
            }
            // Case B: Released during a Sweep
            else
            {
                // Final confirmation: are we still over it?
                if (TryGetClickable(out RaycastHit hit, out IClickable finalTarget))
                {
                    if (finalTarget == _currentHoveredTarget)
                        _currentHoveredTarget.OnHandleClick(hit);
                }
            }

            _currentHoveredTarget.OnPointerUp();
            _currentHoveredTarget = null;
        }

        _isSweepModeActive = false;
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
            clickable = hit.collider.GetComponent<IClickable>();
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

        // Desktop
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        // Mobile
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
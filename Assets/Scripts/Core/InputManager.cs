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
    private Camera _mainCam;

    // Dictionary to store key subscriptions: KeyCode -> Action
    private Dictionary<KeyCode, Action> _keyCallbacks = new Dictionary<KeyCode, Action>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        _mainCam = Camera.main;

        Scheduler.Instance.SubscribeUpdate(OnUpdate);
    }

    // --- Keyboard Subscription API ---
    public void RegisterKey(KeyCode key, Action callback)
    {
        if (!_keyCallbacks.ContainsKey(key)) _keyCallbacks[key] = null;
        _keyCallbacks[key] += callback;
    }

    public void UnregisterKey(KeyCode key, Action callback)
    {
        if (_keyCallbacks.ContainsKey(key)) _keyCallbacks[key] -= callback;
    }

    void OnUpdate(float dt)
    {
        // 1. Handle Mouse/Raycast Input
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        // 2. Handle Registered Key Input
        // We only iterate through the keys that actually have subscribers
        foreach (var keyEntry in _keyCallbacks)
        {
            if (Input.GetKeyDown(keyEntry.Key))
            {
                keyEntry.Value?.Invoke();
            }
        }
    }

    private void HandleMouseClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickLayer))
        {
            // Direct notification via Interface
            IClickable clickable = hit.collider.GetComponent<IClickable>();
            clickable?.OnHandleClick(hit);
        }
    }

    void OnDestroy()
    {
        // Always clean up manual subscriptions
        if (Scheduler.Instance != null)
        {
            Scheduler.Instance.UnsubscribeUpdate(OnUpdate);
        }
    }
}
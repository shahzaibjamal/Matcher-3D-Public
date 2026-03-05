using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea = Rect.zero;
    private Vector2 _lastScreenSize = Vector2.zero;
    private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        // Only refresh if the safe area or screen orientation changes
        if (_lastSafeArea != Screen.safeArea ||
            _lastScreenSize.x != Screen.width ||
            _lastScreenSize.y != Screen.height ||
            _lastOrientation != Screen.orientation)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        Rect safeArea = Screen.safeArea;

        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        _lastOrientation = Screen.orientation;

        // Convert safe area rectangle from pixels to normalized anchors (0.0 to 1.0)
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Apply to RectTransform
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;

        // Reset offsets to ensure it stretches to the new anchors
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
    }
}
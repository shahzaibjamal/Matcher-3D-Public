using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class HeaderStretchSafeArea : MonoBehaviour
{
    private RectTransform _rectTransform;
    private RectTransform _headerParent;
    private float _initialHeaderHeight;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        // Assuming the parent is the "Header Panel" that needs to grow
        _headerParent = transform.parent.GetComponent<RectTransform>();
        _initialHeaderHeight = _headerParent.sizeDelta.y;

        ApplyPadding();
    }

    void Update()
    {
        // Check for safe area changes (orientation swaps)
        if (Screen.safeArea.y != _lastYOffset) ApplyPadding();
    }

    private float _lastYOffset;

    private void ApplyPadding()
    {
        Rect safeArea = Screen.safeArea;

        // Calculate the notch depth in pixels
        float notchPixels = Screen.height - safeArea.yMax;

        // Convert pixels to Canvas units
        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas.GetComponent<RectTransform>().rect.height / Screen.height;
        float notchUIUnits = notchPixels * scaleFactor;

        _lastYOffset = safeArea.y;

        // 1. Increase the Height of the Header Background
        // This makes the header "taller" to cover the notch
        _headerParent.sizeDelta = new Vector2(_headerParent.sizeDelta.x, _initialHeaderHeight + notchUIUnits);

        // 2. Push the Content down within that taller header
        // offsetMax.y = -notchUIUnits pushes the top of the content down
        _rectTransform.offsetMax = new Vector2(_rectTransform.offsetMax.x, -notchUIUnits);
        _rectTransform.offsetMin = Vector2.zero; // Keep bottom anchored to the bottom of header
    }
}
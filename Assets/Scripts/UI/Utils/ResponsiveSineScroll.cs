using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class ResponsiveSineScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Orientation")]
    public bool isVertical = true;

    [Header("Wave Settings")]
    public float amplitude = 100f;
    public float frequency = 1.0f;

    [Header("Snapping (Interval)")]
    public bool useSnapping = true;
    public float interval = 250f;    // The "Height" or "Width" of one item + spacing
    public float snapSpeed = 12f;
    public float velocityThreshold = 150f;

    private ScrollRect scrollRect;
    private RectTransform content;
    private RectTransform viewport;
    private bool isDragging;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;
        viewport = scrollRect.viewport;
    }

    public void OnBeginDrag(PointerEventData eventData) => isDragging = true;
    public void OnEndDrag(PointerEventData eventData) => isDragging = false;

    void LateUpdate()
    {
        UpdateWavePositions();

        if (useSnapping && !isDragging)
        {
            HandleIntervalSnapping();
        }
    }

    private void UpdateWavePositions()
    {
        // Viewport size in world units for normalization
        float viewportSize = (isVertical ? viewport.rect.height : viewport.rect.width) * viewport.lossyScale.y;
        Vector3 viewportCenterWorld = viewport.position;

        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform container = content.GetChild(i) as RectTransform;
            if (container.childCount == 0) continue;

            RectTransform visual = container.GetChild(0) as RectTransform;

            // Distance from center in World Space
            float worldDelta = isVertical ?
                (visual.position.y - viewportCenterWorld.y) :
                (visual.position.x - viewportCenterWorld.x);

            // Normalize distance (-0.5 to 0.5)
            float normalizedDist = worldDelta / viewportSize;

            // Apply Sine wave
            float offset = Mathf.Sin(normalizedDist * frequency * Mathf.PI) * amplitude;

            visual.localPosition = isVertical ? new Vector2(offset, 0) : new Vector2(0, offset);
        }
    }

    private void HandleIntervalSnapping()
    {
        float velocity = isVertical ? scrollRect.velocity.y : scrollRect.velocity.x;

        if (Mathf.Abs(velocity) < velocityThreshold)
        {
            scrollRect.velocity = Vector2.zero;

            // 1. Get current anchored position
            Vector2 currentPos = content.anchoredPosition;

            // 2. Math: Round to nearest interval
            // We use a negative for Vertical because scrolling "down" increases Y in UI space
            float targetValue;
            if (isVertical)
            {
                targetValue = Mathf.Round(currentPos.y / interval) * interval;
                currentPos.y = Mathf.Lerp(currentPos.y, targetValue, Time.deltaTime * snapSpeed);
            }
            else
            {
                targetValue = Mathf.Round(currentPos.x / interval) * interval;
                currentPos.x = Mathf.Lerp(currentPos.x, targetValue, Time.deltaTime * snapSpeed);
            }

            // 3. Apply smoothed position
            content.anchoredPosition = currentPos;
        }
    }
}
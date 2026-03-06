using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class SmartToggle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public RectTransform background;
    public RectTransform handle;
    public Image backgroundImage;

    [Header("Colors")]
    public Color onColor = new Color(0.2f, 0.8f, 0.4f);
    public Color offColor = new Color(0.5f, 0.5f, 0.5f);
    public float padding = 4f;

    [Header("Animation")]
    public float animationDuration = 0.22f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Default State")]
    [SerializeField] private bool defaultOn = false;

    public event Action<bool> OnValueChanged;

    private bool isOn;
    private bool animating;
    private float animTime;
    private float animStartX;
    private float animTargetX;
    private float minX, maxX;
    private bool isDragging;
    private Vector2 pointerDownScreenPos;
    private const float TapMoveThreshold = 8f;

    void Awake()
    {
        if (!backgroundImage && background) backgroundImage = background.GetComponent<Image>();
        RecalculateGeometry();
        isOn = defaultOn;
        ApplyVisualInstant();
    }

    void Start()
    {
    }

    void OnRectTransformDimensionsChange()
    {
        RecalculateGeometry();
        ApplyVisualInstant();
    }

    private void RecalculateGeometry()
    {
        if (!background || !handle) return;
        float halfTrack = (background.rect.width - handle.rect.width) * 0.5f;
        minX = -halfTrack + padding;
        maxX = halfTrack - padding;
    }

    public void SetIsOn(bool value, bool animate = true)
    {
        if (isOn == value && !animate) return;

        isOn = value;

        if (animate)
        {
            animating = true;
            animTime = 0f;
            animStartX = handle.anchoredPosition.x;
            animTargetX = isOn ? maxX : minX;
        }
        else
        {
            ApplyVisualInstant();
        }

        OnValueChanged?.Invoke(isOn);
    }

    public void Toggle() => SetIsOn(!isOn, true);

    public bool IsOn() => isOn;

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownScreenPos = eventData.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        animating = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out var local))
            return;

        float x = Mathf.Clamp(local.x, minX, maxX);
        handle.anchoredPosition = new Vector2(x, handle.anchoredPosition.y);
        ApplyBackgroundColor(PositionToNormalized(x));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        bool targetOn = PositionToNormalized(handle.anchoredPosition.x) >= 0.5f;
        SetIsOn(targetOn, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            if ((eventData.position - pointerDownScreenPos).sqrMagnitude <= TapMoveThreshold * TapMoveThreshold)
            {
                Toggle();
            }
        }
    }

    void Update()
    {
        if (animating)
        {
            animTime += Time.deltaTime / animationDuration;
            float t = Mathf.Clamp01(animTime);
            float eased = ease.Evaluate(t);

            float x = Mathf.Lerp(animStartX, animTargetX, eased);
            handle.anchoredPosition = new Vector2(x, handle.anchoredPosition.y);
            ApplyBackgroundColor(PositionToNormalized(x));

            if (t >= 1f) animating = false;
        }
    }

    private float PositionToNormalized(float x) => Mathf.InverseLerp(minX, maxX, x);

    private void ApplyBackgroundColor(float t)
    {
        if (backgroundImage)
            backgroundImage.color = Color.Lerp(offColor, onColor, t);
    }

    private void ApplyVisualInstant()
    {
        float x = isOn ? maxX : minX;
        handle.anchoredPosition = new Vector2(x, handle.anchoredPosition.y);
        ApplyBackgroundColor(isOn ? 1f : 0f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!backgroundImage && background) backgroundImage = background.GetComponent<Image>();
        RecalculateGeometry();

        // In the editor, reflect defaultOn immediately
        isOn = defaultOn;
        ApplyVisualInstant();
    }
#endif
}

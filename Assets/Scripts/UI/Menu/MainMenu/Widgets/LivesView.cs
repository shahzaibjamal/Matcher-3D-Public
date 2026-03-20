using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LivesView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _mainFillImage;    // The actual colored bar
    [SerializeField] private Image _ghostFillImage;   // The "delayed" white/transparent bar
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private Button _addMoreButton;
    [SerializeField] private RectTransform _barContainer;

    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private float _ghostDelay = 0.2f;
    [SerializeField] private Vector3 _punchScale = new Vector3(0.15f, 0.15f, 0);

    [Header("Range Settings")]
    [Tooltip("The fill amount where the bar actually starts (e.g., 0.15 if the icon covers the first 15%)")]
    [Range(0f, 0.5f)][SerializeField] private float _fillOffset = 0.1f;
    [Tooltip("The fill amount where the bar ends (usually 1.0)")]
    [Range(0.5f, 1f)][SerializeField] private float _maxFillLimit = 1f;

    private Tween _fillTween;
    private Tween _ghostTween;

    private void OnEnable()
    {
        // Subscribe to the global life change event
        GameEvents.OnLivesChanged += RefreshUI;
        RefreshUI(true); // Snap to value immediately on screen load
    }

    private void OnDisable()
    {
        GameEvents.OnLivesChanged -= RefreshUI;
    }

    private void Start()
    {
        if (_addMoreButton != null)
            _addMoreButton.onClick.AddListener(OnAddMoreClicked);
    }

    public void RefreshUI() => RefreshUI(false);

    public void RefreshUI(bool immediate)
    {
        var save = GameManager.Instance.SaveData;
        float targetFill = save.MaxLives > 0 ? (float)save.CurrentLives / save.MaxLives : 0;

        // Update the number text immediately
        _amountText.text = save.CurrentLives.ToString();

        float rawPercent = save.MaxLives > 0 ? (float)save.CurrentLives / save.MaxLives : 0;

        // 2. Remap the value: NewValue = Offset + (Percent * (Max - Offset))
        // This ensures 0 lives = _fillOffset and Max lives = _maxFillLimit
        float remappedFill = _fillOffset + (rawPercent * (_maxFillLimit - _fillOffset));

        _amountText.text = save.CurrentLives.ToString();
        if (immediate)
        {
            _mainFillImage.fillAmount = remappedFill;
            if (_ghostFillImage != null) _ghostFillImage.fillAmount = targetFill;
            return;
        }

        // 1. Animate Main Fill
        _fillTween?.Kill();
        _fillTween = _mainFillImage.DOFillAmount(remappedFill, _animationDuration)
            .SetEase(Ease.OutQuad);

        // 2. Animate Ghost Fill (The trailing effect)
        if (_ghostFillImage != null)
        {
            _ghostTween?.Kill();
            // Ghost bar waits slightly then catches up
            _ghostTween = _ghostFillImage.DOFillAmount(targetFill, _animationDuration)
                .SetDelay(_ghostDelay)
                .SetEase(Ease.OutCubic);
        }

        // 3. The "Juice" Punch
        // Only punch if the value actually changed (prevents punch on Init)
        _barContainer.DOKill();
        _barContainer.localScale = Vector3.one;
        _barContainer.DOPunchScale(_punchScale, 0.3f, 10, 1f);
    }

    private void OnAddMoreClicked()
    {
        // Tactile button feedback
        _addMoreButton.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), 0.2f);

        Debug.Log("Opening Lives/Store Menu...");
        MenuManager.Instance.OpenMenu<StoreMenuView, StoreMenuController, StoreMenuData>(Menus.Type.Store);
    }
}
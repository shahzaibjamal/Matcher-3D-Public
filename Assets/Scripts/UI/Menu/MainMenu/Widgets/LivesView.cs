using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class LivesView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _mainFillImage;
    [SerializeField] private Image _ghostFillImage;
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private TMP_Text _timerText; // Add this to your Prefab!
    [SerializeField] private Button _addMoreButton;
    [SerializeField] private RectTransform _barContainer;

    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private float _ghostDelay = 0.2f;
    [SerializeField] private Vector3 _punchScale = new Vector3(0.15f, 0.15f, 0);

    [Header("Range Settings")]
    [Range(0.0f, 0.7f)][SerializeField] private float _fillOffset = 0.1f;
    [Range(0.5f, 1f)][SerializeField] private float _maxFillLimit = 1f;

    private Tween _fillTween;
    private Tween _ghostTween;
    private int _lastInternalLifeCount = -1;

    private void OnEnable()
    {
        // Still listen to events for immediate refills (purchases/rewards)
        GameEvents.OnLivesChanged += HandleLifeChangeEvent;
        RefreshUI(true);
    }

    private void OnDisable()
    {
        GameEvents.OnLivesChanged -= HandleLifeChangeEvent;
    }

    private void Start()
    {
        if (_addMoreButton != null)
            _addMoreButton.onClick.AddListener(OnAddMoreClicked);
    }

    // This is called by your MenuState's Scheduler every 0.2s
    public void RefreshUI() => RefreshUI(false);

    private void HandleLifeChangeEvent() => RefreshUI(false);

    public void RefreshUI(bool immediate)
    {
        var save = GameManager.Instance.SaveData;
        int maxLives = DataManager.Instance.Metadata.Settings.MaxLives;
        int currentLives = save.CurrentLives; // Accessing property triggers UpdateLivesLogic

        // 1. Update Timer Text with 1s Offset
        UpdateTimerDisplay(save, maxLives);

        // 2. Only run Bar Animations if the actual life count changed
        // This prevents the bar from "punching" every 0.2s when the scheduler runs
        if (currentLives != _lastInternalLifeCount)
        {
            AnimateBar(currentLives, maxLives, immediate);
            _lastInternalLifeCount = currentLives;
        }
    }

    private void UpdateTimerDisplay(GameSaveData save, int maxLives)
    {
        if (_timerText == null) return;

        if (save.CurrentLives >= maxLives)
        {
            _timerText.text = "FULL"; // Or use Localization
            return;
        }

        if (!string.IsNullOrEmpty(save.LastLifeLostTime) && DateTime.TryParse(save.LastLifeLostTime, out DateTime lastLost))
        {
            int secondsPerLife = DataManager.Instance.Metadata.Settings.SecondsToRecover;
            TimeSpan diff = lastLost.AddSeconds(secondsPerLife) - DateTime.Now;

            // Visual Offset: Show 00:01 until the very moment the life is added
            double displaySeconds = Math.Max(0, diff.TotalSeconds + 1);
            TimeSpan t = TimeSpan.FromSeconds(displaySeconds);

            _timerText.text = string.Format("{0:D2}:{1:D2}", (int)t.TotalMinutes, t.Seconds);

            // Subtle color warning
            _timerText.color = displaySeconds < 20 ? Color.red : Color.white;
        }
    }

    private void AnimateBar(int currentLives, int maxLives, bool immediate)
    {
        float rawPercent = maxLives > 0 ? (float)currentLives / maxLives : 0;
        float remappedFill = _fillOffset + (rawPercent * (_maxFillLimit - _fillOffset));

        _amountText.text = currentLives.ToString();

        if (immediate)
        {
            _mainFillImage.fillAmount = remappedFill;
            if (_ghostFillImage != null) _ghostFillImage.fillAmount = rawPercent;
            return;
        }

        // Main Fill
        _fillTween?.Kill();
        _fillTween = _mainFillImage.DOFillAmount(remappedFill, _animationDuration).SetEase(Ease.OutQuad);

        // Ghost Fill
        if (_ghostFillImage != null)
        {
            _ghostTween?.Kill();
            _ghostTween = _ghostFillImage.DOFillAmount(rawPercent, _animationDuration)
                .SetDelay(_ghostDelay)
                .SetEase(Ease.OutCubic);
        }

        // Juice Punch (Now only happens when life count actually changes!)
        _barContainer.DOKill(true);
        _barContainer.DOPunchScale(_punchScale, 0.3f, 10, 1f);
    }

    private void OnAddMoreClicked()
    {
        _addMoreButton.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), 0.2f);
        MenuManager.Instance.OpenMenu<StoreMenuView, StoreMenuController, StoreMenuData>(Menus.Type.Store);
    }
}
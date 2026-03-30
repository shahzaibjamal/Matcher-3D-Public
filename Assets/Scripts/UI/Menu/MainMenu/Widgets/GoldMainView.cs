using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class GoldMainView : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text GoldText;
    public Image GoldIcon;
    public RectTransform Container; // The main parent of this HUD element
    public Button AddMoreButton;  //store
    public bool AnimateGold = true;

    private int _displayedAmount = 0;
    private Tween _countTween;

    public UIShimmerEffect UIShimmerEffect;

    public void Init(int amount, bool showAddMore = false)
    {
        UpdateAmount(amount);
        AddMoreButton.gameObject.SetActive(showAddMore);
    }

    void Awake()
    {
        UIShimmerEffect.Play();
        GameEvents.OnGoldUpdatedEvent += OnGoldUpdate;
        AddMoreButton.onClick.AddListener(OnAddMoreButtonClicked);
    }

    private void OnAddMoreButtonClicked()
    {
        MenuManager.Instance.OpenMenu<StoreMenuView, StoreMenuController, StoreMenuData>(Menus.Type.Store);
    }
    private void OnGoldUpdate(int amount)
    {
        if (AnimateGold)
            PlayCollectAnimation(amount);
        else
            UpdateAmount(amount);
    }

    private void UpdateAmount(int initialAmount)
    {
        _displayedAmount = initialAmount;
        GoldText.text = initialAmount.ToString("N0");
    }

    /// <summary>
    /// Animates the HUD total with a counting effect and a scale "pulse".
    /// </summary>
    public void PlayCollectAnimation(int targetAmount, float duration = 0.5f)
    {
        // 1. Scale Pulse (Juice)
        if (Container != null)
            Container.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), 0.3f, 10, 1f);

        GoldIcon.transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.4f);

        // 2. Numerical Counting Animation
        _countTween?.Kill();

        // Track the last value we played a sound for
        int lastSoundValue = _displayedAmount;

        // Determine how often to play the sound (e.g., every 5 gold or every 1 unit)
        // For large amounts, use a bigger step so it doesn't overwhelm the audio engine
        int totalDiff = Mathf.Abs(targetAmount - _displayedAmount);
        int soundStep = totalDiff > 100 ? 10 : 1;

        _countTween = DOTween.To(() => _displayedAmount, x =>
        {
            _displayedAmount = x;
            GoldText.text = _displayedAmount.ToString("N0");

            // Only play sound if the value has changed by our 'step'
            if (Mathf.Abs(_displayedAmount - lastSoundValue) >= soundStep)
            {
                lastSoundValue = _displayedAmount;
                SoundController.Instance.PlaySoundEffect("coin_collect");
            }
        }, targetAmount, duration)
        .SetEase(Ease.OutQuad);

    }

    // Returns the world position of the icon for the fly animation target
    public Vector3 GetTargetPosition() => GoldIcon.transform.position;

    void OnDestroy()
    {
        GoldIcon.transform.DOKill();
        Container.DOKill();
        _countTween.Kill();
        GameEvents.OnGoldUpdatedEvent -= OnGoldUpdate;
        AddMoreButton.onClick.RemoveListener(OnAddMoreButtonClicked);
    }
}
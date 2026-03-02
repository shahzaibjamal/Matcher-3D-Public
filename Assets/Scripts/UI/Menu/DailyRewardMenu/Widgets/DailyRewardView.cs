using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using TS.LocalizationSystem;

public class DailyRewardView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimedOverlay;
    [SerializeField] private CanvasGroup _canvasGroup;

    private DailyRewardData _data;
    private Action<DailyRewardData> _onClaimRequested;

    // Receives the sprite resolved by the Window
    public void Setup(DailyRewardData data, Sprite icon, bool isClaimed, bool isReady, Action<DailyRewardData> claimCallback)
    {
        _data = data;
        _onClaimRequested = claimCallback;
        _canvasGroup.alpha = isReady ? 1.0f : 0.6f;

        iconImage.sprite = icon;
        amountText.text = string.Format(LocaleManager.Localize(LocalizationKeys.reward_amount), data.Amount);
        dayText.text = string.Format(LocaleManager.Localize(LocalizationKeys.day), data.Day);

        claimedOverlay.SetActive(isClaimed);
        claimButton.interactable = !isClaimed && isReady;

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(HandleClick);
    }
    private void HandleClick()
    {
        _onClaimRequested?.Invoke(_data);
        SetClaimed();
    }

    public void SetClaimed()
    {
        claimedOverlay.SetActive(true);
        claimButton.interactable = false;
    }

}
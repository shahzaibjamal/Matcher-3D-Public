using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class GoldRewardView : MonoBehaviour
{
    [Header("HUD (Always Visible)")]
    public TMP_Text HudGoldText;
    public Image HudGoldIcon;
    private int _currentHudAmount = 0;

    [Header("Reward Elements (The Pop-ups)")]
    public TMP_Text RewardAmountText;
    public Image RewardIcon;

    [Header("Settings")]
    public float PopDuration = 0.5f;
    public float CountDuration = 1.0f;
    public float FlyToHudDuration = 0.6f;

    public void ShowReward(int rewardAmount, float delay, Action onComplete = null)
    {
        // 1. Reset Reward Elements
        RewardAmountText.gameObject.SetActive(true);
        RewardIcon.gameObject.SetActive(true);

        RewardAmountText.transform.localScale = Vector3.zero;
        RewardIcon.transform.localScale = Vector3.zero;
        RewardAmountText.text = $"+{rewardAmount}";

        // Store original positions to reset them later
        Vector3 originalTextPos = RewardAmountText.transform.position;
        Vector3 originalIconPos = RewardIcon.transform.position;

        Sequence seq = DOTween.Sequence();

        // STEP A: Pop the Reward Elements in
        seq.SetDelay(delay);
        seq.Append(RewardIcon.transform.DOScale(1.2f, PopDuration).SetEase(Ease.OutBack));
        seq.Join(RewardAmountText.transform.DOScale(1.0f, PopDuration).SetEase(Ease.OutBack));

        // STEP B: Brief pause to let the player see the amount
        seq.AppendInterval(0.5f);

        // STEP C: Fly to HUD
        // We move them toward the HUD's world position
        seq.Append(RewardIcon.transform.DOMove(HudGoldIcon.transform.position, FlyToHudDuration).SetEase(Ease.InBack));
        seq.Join(RewardAmountText.transform.DOMove(HudGoldText.transform.position, FlyToHudDuration).SetEase(Ease.InBack));

        // Shrink them as they approach the HUD
        seq.Join(RewardIcon.transform.DOScale(0.5f, FlyToHudDuration));
        seq.Join(RewardAmountText.transform.DOScale(0.5f, FlyToHudDuration));

        // STEP D: Impact & Update HUD
        seq.OnComplete(() =>
        {
            // Hide the reward clones
            RewardIcon.gameObject.SetActive(false);
            RewardAmountText.gameObject.SetActive(false);

            // Reset their positions for the next time ShowReward is called
            RewardIcon.transform.position = originalIconPos;
            RewardAmountText.transform.position = originalTextPos;

            // HUD Juice: Punch the HUD icon when the gold "hits" it
            HudGoldIcon.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f);

            // Animate the HUD total increasing
            int startAmount = _currentHudAmount;
            _currentHudAmount += rewardAmount;

            DOTween.To(() => startAmount, x =>
            {
                startAmount = x;
                HudGoldText.text = startAmount.ToString("N0");
            }, _currentHudAmount, 0.5f).SetEase(Ease.OutQuad);

            onComplete?.Invoke();
        });
    }

    // Call this at Start or after loading Save Data
    public void SetInitialHudAmount(int amount)
    {
        _currentHudAmount = amount;
        HudGoldText.text = amount.ToString("N0");
        RewardAmountText.gameObject.SetActive(false);
        RewardIcon.gameObject.SetActive(false);

    }
}
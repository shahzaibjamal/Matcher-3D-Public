using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class GoldRewardView : MonoBehaviour
{
    [Header("Target HUD")]
    public GoldMainView TargetHUD;

    [Header("Reward UI Elements")]
    public RectTransform RewardContainer; // Parent of text and icon
    public TMP_Text RewardAmountText;
    public Image RewardIcon;

    [Header("Animation Settings")]
    public float PopDuration = 0.5f;
    public float FlyDuration = 0.7f;
    public Ease FlyEase = Ease.InBack;

    private Vector3 _startPosition;

    private void Awake()
    {
        _startPosition = RewardContainer.position;
        RewardContainer.gameObject.SetActive(false);
    }

    public void Initialize(int rewardAmount)
    {
        RewardAmountText.text = $"+{rewardAmount}";
        RewardContainer.gameObject.SetActive(true);
    }

    /// <summary>
    /// Pops a reward amount on screen, then flies it to the HUD.
    /// </summary>
    /// <param name="rewardAmount">The amount to show in the popup (+100)</param>
    /// <param name="newTotal">The final total the HUD should reach after the flight</param>
    public void ShowReward(int rewardAmount, int newTotal, float initialDelay = 0f, Action onComplete = null)
    {
        // Setup
        RewardContainer.position = _startPosition;
        RewardContainer.localScale = Vector3.zero;
        RewardAmountText.text = $"+{rewardAmount}";
        RewardContainer.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // 1. Initial Pop In
        seq.AppendInterval(initialDelay);
        seq.Append(RewardContainer.DOScale(1.2f, PopDuration).SetEase(Ease.OutBack));

        // 2. Hover for a moment
        seq.AppendInterval(0.3f);

        // 3. Fly to HUD
        // We use GetTargetPosition() from the HUD to find where the icon is currently
        seq.Append(RewardContainer.DOMove(TargetHUD.GetTargetPosition(), FlyDuration).SetEase(FlyEase));

        // Shrink slightly as it enters the HUD
        seq.Join(RewardContainer.DOScale(0.6f, FlyDuration).SetEase(Ease.InQuad));

        // 4. On Arrival
        seq.OnComplete(() =>
        {
            RewardContainer.gameObject.SetActive(false);

            // Trigger the HUD's internal juice and count animation
            TargetHUD.PlayCollectAnimation(newTotal);

            onComplete?.Invoke();
        });
    }
}
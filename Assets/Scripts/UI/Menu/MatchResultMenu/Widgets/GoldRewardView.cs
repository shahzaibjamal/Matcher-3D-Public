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
        // 1. Reset
        // RewardContainer.localScale = Vector3.zero;
        // Set local position to zero (the center of the GoldRewardView)
        RewardContainer.localPosition = Vector3.zero;
        RewardContainer.gameObject.SetActive(true);

        // 2. Calculate the local vector from the container to the HUD
        // InverseTransformPoint converts the HUD's world position into a local position 
        // relative to the GoldRewardView's space.
        Vector3 worldTarget = TargetHUD.GetTargetPosition();
        Vector3 localTarget = RewardContainer.parent.InverseTransformPoint(worldTarget);

        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(initialDelay);
        seq.Append(RewardContainer.DOScale(1.2f, PopDuration).SetEase(Ease.OutBack));
        seq.AppendInterval(0.3f);

        // 3. USE DOLocalMove
        // This ignores the world-space "tug of war" and moves relative to the parent
        seq.Append(RewardContainer.DOLocalMove(localTarget, FlyDuration).SetEase(FlyEase));
        seq.Join(RewardContainer.DOScale(0.5f, FlyDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            RewardContainer.gameObject.SetActive(false);
            TargetHUD.PlayCollectAnimation(newTotal);
            onComplete?.Invoke();
        });
    }
}
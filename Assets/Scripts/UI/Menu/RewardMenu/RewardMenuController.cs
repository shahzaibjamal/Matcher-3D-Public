using DG.Tweening;
using TS.LocalizationSystem;
using UnityEngine;

public class RewardMenuController : MenuController<RewardMenuView, RewardMenuData>
{
    public override void OnEnter()
    {
        SetState(new RewardMenuBaseState_Main(this));
        Setup(Data.RewardData);
        View.FullscreenButton.onClick.AddListener(OnClickClaim);

    }
    public override void OnExit()
    {
        // Important: Clean up the infinite float and rotation!
        View.RewardContainer.DOKill();
        // View.ShineImage.transform.DOKill();
        View.FullscreenButton.onClick.RemoveListener(OnClickClaim);
        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    public void Setup(RewardData data)
    {
        View.IconImage.sprite = View.RewardIconMapper.GetIcon(data.RewardType);
        View.AmountText.text = string.Format(LocaleManager.Localize(LocalizationKeys.reward_amount), data.Amount);

        // --- 1. Reset States ---
        View.canvasGroup.alpha = 0;
        View.RewardContainer.localScale = Vector3.zero;
        View.GodRaysTransform.localScale = Vector3.zero;
        View.GodRaysTransform.localRotation = Quaternion.identity;
        View.FullscreenButton.interactable = false;

        // --- 2. The Main Sequence ---
        Sequence s = DOTween.Sequence().SetUpdate(true); // SetUpdate(true) ensures it plays even if game is paused

        // A. Fade in background
        s.Append(View.canvasGroup.DOFade(1, 0.3f));

        // B. God Rays Entrance (Scale up + Start Rotation)
        s.Join(View.GodRaysTransform.DOScale(1.2f, 0.6f).SetEase(Ease.OutBack));
        View.GodRaysTransform.DORotate(new Vector3(0, 0, 360), 8f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1);

        // C. The Icon "Pop"
        s.Append(View.RewardContainer.DOScale(1.1f, 0.4f).SetEase(Ease.OutBack));

        // D. The "Impact" Moment
        s.AppendCallback(() =>
        {
            // Subtle haptic if on mobile
            Handheld.Vibrate();

            View.FullscreenButton.interactable = true;

            // Add a "Pulse" to the rays so they feel alive
            View.GodRaysTransform.DOScale(1.0f, 1.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

            // Add a gentle float to the reward
            View.RewardContainer.DOLocalMoveY(20f, 2f).SetRelative().SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

            View.ConfettiParticles.Play();
        });

        // E. Punch the amount text so the player notices HOW MUCH they got
        s.Append(View.AmountText.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 5, 1));
    }

    public void OnClickClaim() // Linked to a full-screen button
    {
        // Exit Animation
        View.RewardContainer.DOScale(0, 0.3f).SetEase(Ease.InBack);
        View.canvasGroup.DOFade(0, 0.3f).OnComplete(() =>
        {
            FinishReward();
        });
    }

    public void FinishReward()
    {
        // Close the menu through your MenuManager
        MenuManager.Instance.GoBack();

        // Trigger the RewardManager to show the next one
        Data.Callback?.Invoke();
    }
}
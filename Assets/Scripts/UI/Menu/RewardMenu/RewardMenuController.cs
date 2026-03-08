using DG.Tweening;
using TS.LocalizationSystem;
using UnityEngine;

public class RewardMenuController : MenuController<RewardMenuView, RewardMenuData>
{
    private Sequence _sequence;
    public override void OnEnter()
    {
        SetState(new RewardMenuBaseState_Main(this));
        Setup(Data.RewardData);
        View.FullscreenButton.onClick.AddListener(OnClickClaim);

    }
    public override void OnExit()
    {
        View.RewardContainer.DOKill();
        View.FullscreenButton.onClick.RemoveListener(OnClickClaim);
        _sequence?.Kill();
        View.RewardContainer.DOKill();
        View.GodRaysTransform.DOKill();
        View.AmountText.transform.DOKill();
        base.OnExit();
        Data.Callback?.Invoke();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    public void Setup(RewardData data)
    {
        // --- 0. Data Mapping ---
        View.IconImage.sprite = View.RewardIconMapper.GetIcon(data.RewardType);
        View.AmountText.text = string.Format(LocaleManager.Localize(LocalizationKeys.reward_amount), data.Amount);

        // --- 1. Reset States ---
        // Kill any surviving tweens from the previous time this menu opened
        View.RewardContainer.DOKill();
        View.GodRaysTransform.DOKill();
        View.AmountText.transform.DOKill();

        // Reset values AFTER killing tweens
        View.canvasGroup.alpha = 0;
        View.RewardContainer.localScale = Vector3.zero;
        View.RewardContainer.localPosition = Vector3.zero; // Reset to center
        View.RewardContainer.localRotation = Quaternion.identity;

        View.GodRaysTransform.localScale = Vector3.zero;
        View.GodRaysTransform.localRotation = Quaternion.identity;
        View.FullscreenButton.interactable = false;
        SoundController.instance.PlaySoundEffect("reward");

        // --- 2. The Sequence ---
        _sequence = DOTween.Sequence().SetUpdate(true);

        _sequence.Append(View.canvasGroup.DOFade(1, 0.3f));

        // Join the God Rays entrance
        _sequence.Join(View.GodRaysTransform.DOScale(1.2f, 0.6f).SetEase(Ease.OutBack));

        // C. The Icon "Pop"
        _sequence.Append(View.RewardContainer.DOScale(1.1f, 0.4f).SetEase(Ease.OutBack));
        // D. The "Impact" Moment & Looping Logic
        _sequence.AppendCallback(() =>
        {
            View.FullscreenButton.interactable = true;
            View.RewardContainer.DOKill();

            // --- THE JETSONS HOVER (Clean Restart) ---

            // 1. High-Frequency Y-Jitter
            View.RewardContainer.DOLocalMoveY(50f, 0.5f)
                .SetRelative(true)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);

            // 2. The Saucer Sway (Tilt)
            View.RewardContainer.DORotate(new Vector3(0, 0, 5f), 0.8f)
                .SetEase(Ease.InOutQuad)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);

            // 3. THE GOD RAYS (Anti-Jitter Logic)
            // Instead of starting from 0, we use RotateMode.LocalAxisAdd 
            // to ensure it just keeps spinning relative to its current state.
            View.GodRaysTransform.DOLocalRotate(new Vector3(0, 0, 360f), 5f, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetUpdate(true);
        });

        _sequence.Append(View.AmountText.transform.DOPunchScale(Vector3.one * 0.25f, 0.5f, 8, 1));
    }
    public void OnClickClaim() // Linked to a full-screen button
    {
        Debug.LogError("OnClickclaim");
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
    }

}
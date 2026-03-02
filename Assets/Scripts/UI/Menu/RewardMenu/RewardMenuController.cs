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

        // Initial State for Animation
        View.canvasGroup.alpha = 0;
        View.RewardContainer.localScale = Vector3.zero;

        // Entrance Animation
        Sequence s = DOTween.Sequence();
        s.Append(View.canvasGroup.DOFade(1, 0.3f));
        s.Join(View.RewardContainer.DOScale(1, 0.5f).SetEase(Ease.OutBack));
        s.Append(View.RewardContainer.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5, 1));
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
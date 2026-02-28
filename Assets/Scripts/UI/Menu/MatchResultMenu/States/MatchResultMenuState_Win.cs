using DG.Tweening;
using TS.LocalizationSystem;
using UnityEngine;

public class MatchResultMenuBaseState_Win : MatchResultMenuBaseState
{
    public MatchResultMenuBaseState_Win(MatchResultMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (Data.Level > 10 || true)
            View.GoldMulitplierButton.gameObject.SetActive(true);
        View.Result.text = LocaleManager.Localize(LocalizationKeys.result_win);
        View.Status.gameObject.SetActive(true);

        if (Data.MatchRate > 0.9f)
        {
            View.Status.text = LocaleManager.Localize(LocalizationKeys.status_perfect);
        }
        else if (Data.MatchRate > 0.70f)
        {
            View.Status.text = LocaleManager.Localize(LocalizationKeys.status_amazing);

        }
        else
        {
            View.Status.text = LocaleManager.Localize(LocalizationKeys.status_good);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    protected override void OnMenuOpenAnimationComplete()
    {
        base.OnMenuOpenAnimationComplete();
        int score = Data.MatchRate > 0.9f ? 3 : Data.MatchRate > 0.7f ? 2 : 1;
        AnimateAllStars(score);
    }

    public void AnimateAllStars(int score)
    {
        float delay = 0f;
        for (int i = 0; i < View.StarViews.Length; i++)
        {
            delay = i * View.StarsApearDelay;
            if (i < score)
            {
                // Stagger by 0.3 seconds each: 0.0s, 0.3s, 0.6s
                View.StarViews[i].Show(delay);
            }
            else
            {
                View.StarViews[i].ResetView();
            }
        }

        // 1. Kill any existing rotation to prevent stacking
        View.GodRays.SetActive(true);
        View.GodRays.transform.DOKill();

        // 2. Start the endless rotation
        View.GodRays.transform.DORotate(new Vector3(0, 0, 360), 10f, RotateMode.FastBeyond360)
            .SetDelay(delay)            // Wait before starting
            .SetEase(Ease.Linear)      // Constant speed (essential for loops)
            .SetLoops(-1, LoopType.Incremental); // -1 means infinite    

        int total = Data.GoldAmount + GameManager.Instance.SaveData.Inventory.Gold;
        View.GoldRewardView.ShowReward(Data.GoldAmount, total, delay);
        GameManager.Instance.SaveData.Inventory.TryUpdateGoldAmount(Data.GoldAmount);
        View.TextAnimation.PlayReveal();
    }
}

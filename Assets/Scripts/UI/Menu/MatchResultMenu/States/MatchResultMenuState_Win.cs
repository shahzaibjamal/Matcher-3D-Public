using System;
using DG.Tweening;
using TS.LocalizationSystem;
using Unity.VisualScripting;
using UnityEngine;

public class MatchResultMenuBaseState_Win : MatchResultMenuBaseState
{
    public MatchResultMenuBaseState_Win(MatchResultMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (Data.LevelData.Number > 10 || true)
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
        Data.Score = score;
        AnimateAllStars(score);
    }

    private void AnimateAllStars(int score)
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

        View.TextAnimation.PlayReveal();
    }

    private void PlayGoldAnimation(int goldAmount, int totalAmount, float delay, Action onComplete = null)
    {
        View.GoldRewardView.ShowReward(goldAmount, totalAmount, delay, onComplete);
        GameManager.Instance.SaveData.Inventory.TryUpdateGoldAmount(goldAmount);
    }

    public override void OnContinueButtonClicked()
    {
        base.OnContinueButtonClicked();

        int goldAmount = 0;
        foreach (var rewardData in Data.LevelData.Rewards)
        {
            if (rewardData.RewardType == RewardType.Gold)
            {
                goldAmount = rewardData.Amount;
            }

        }
        int total = goldAmount + GameManager.Instance.SaveData.Inventory.Gold;

        PlayGoldAnimation(goldAmount, total, 0f, OnGoldAnimationCompleted);

    }

    public override void OnGoldMultiplierButtonClicked()
    {
        base.OnGoldMultiplierButtonClicked();

        // show video ad 
        // And then callback and continue
        // Add ad multipler constant
        int goldAmount = 0;
        foreach (var rewardData in Data.LevelData.Rewards)
        {
            if (rewardData.RewardType == RewardType.Gold)
            {
                goldAmount = rewardData.Amount;
            }

        }
        int total = 3 * goldAmount + GameManager.Instance.SaveData.Inventory.Gold;

        PlayGoldAnimation(3 * goldAmount, total, 0f, OnGoldAnimationCompleted);
    }

    private void OnGoldAnimationCompleted()
    {
        Scheduler.Instance.ExecuteAfterDelay(1.5f, Controller.GoToMainMenu);
    }
}

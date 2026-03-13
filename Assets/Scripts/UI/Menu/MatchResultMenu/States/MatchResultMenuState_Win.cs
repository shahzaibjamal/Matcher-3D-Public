using System;
using System.Collections.Generic;
using System.Linq;
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

        View.GoldMulitplierButton.gameObject.SetActive(Data.LevelData.Number > AdManager.Instance.rewardedUnlockLevel);
        View.Result.text = LocaleManager.Localize(LocalizationKeys.result_win);
        View.Status.gameObject.SetActive(false);

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
        View.GodRays.transform.DOKill();
        base.Exit();
    }

    protected override void OnMenuOpenAnimationComplete()
    {
        base.OnMenuOpenAnimationComplete();
        int score = Data.MatchRate > 0.9f ? 3 : Data.MatchRate > 0.7f ? 2 : 1;
        Data.Score = score;
        SoundController.Instance.PlaySoundEffect("level_complete");
        GameManager.Instance.Vibrate(Haptics.HapticTypes.Success);
        AnimateAllStars(score);
    }

    private void AnimateAllStars(int score)
    {
        float delay = 0f;
        float starsAppearDelay = 0.3f;
        Scheduler.Instance.ExecuteAfterDelay(starsAppearDelay, () =>
        {
            SoundController.Instance.PlaySoundEffect("star");

        });


        for (int i = 0; i < View.StarViews.Length; i++)
        {
            delay = i * starsAppearDelay; // stars appear delay
            View.StarViews[i].Show(delay);
        }

        // 1. Kill any existing rotation to prevent stacking
        View.GodRays.SetActive(true);
        View.GodRays.transform.DOKill();

        // 2. Start the endless rotation
        View.GodRays.transform.DORotate(new Vector3(0, 0, 360), 10f, RotateMode.FastBeyond360)
            .SetDelay(delay)            // Wait before starting
            .SetEase(Ease.Linear)      // Constant speed (essential for loops)
            .SetLoops(-1, LoopType.Incremental); // -1 means infinite    

        View.Status.gameObject.SetActive(true);
        View.TextAnimation.PlayReveal();

        Scheduler.Instance.ExecuteAfterDelay(1.0f + delay, DisplayRewards);
        Scheduler.Instance.ExecuteAfterDelay(1.3f + delay, () =>
        {
            View.ConfettiLeft.Play();
            View.ConfettiRight.Play();
            View.ConfettiTop.Play();
            SoundController.Instance.PlaySoundEffect("confetti");
            View.ContinueButton.interactable = true;
            View.GoldMulitplierButton.interactable = true;
        });
    }

    private void PlayGoldAnimation(int goldAmount, int totalAmount, float delay, Action onComplete = null)
    {
        View.GoldRewardView.ShowReward(goldAmount, totalAmount, delay, onComplete);
        GameManager.Instance.SaveData.Inventory.TryUpdateGoldAmount(goldAmount);
    }

    public override void OnContinueButtonClicked()
    {
        base.OnContinueButtonClicked();
        Data.Rewards = Data.LevelData.Rewards;

        OnGoldAnimationCompleted();
    }

    public void DisplayRewards()
    {
        // 1. Reset Visuals
        View.RewardsCanvasGroup.alpha = 0f;
        View.RewardsCanvasGroup.transform.localScale = Vector3.one * 0.7f;
        View.RewardsCanvasGroup.gameObject.SetActive(true);

        int goldAmount = 0;
        RewardData nonGoldReward = null;

        // 2. Identify Rewards from Data
        foreach (var rewardData in Data.LevelData.Rewards)
        {
            if (rewardData.RewardType == RewardType.Gold)
                goldAmount += rewardData.Amount;
            else
                nonGoldReward = rewardData;
        }
        // 3. Update Gold View
        bool hasGold = goldAmount > 0;
        View.GoldRewardView.gameObject.SetActive(hasGold);
        if (hasGold) View.GoldRewardView.Initialize(goldAmount);

        // 4. Update Single "Other" Reward View
        bool hasOther = nonGoldReward != null;
        View.RewardView.gameObject.SetActive(hasOther);

        string currentLevelID = GameManager.Instance.SaveData.CurrentLevelID;
        if (hasOther && Data.LevelData.Id == currentLevelID)
        {
            Sprite icon = View.RewardIconMapper.GetIcon(nonGoldReward.RewardType);
            View.RewardView.Initialize(icon, nonGoldReward.Amount);
        }

        // 5. Entrance Animation (Pop and Fade)
        Sequence seq = DOTween.Sequence();
        seq.Append(View.RewardsCanvasGroup.DOFade(1f, 0.3f));
        seq.Join(View.RewardsCanvasGroup.transform.DOScale(1.1f, 0.4f).SetEase(Ease.OutBack));
        seq.Append(View.RewardsCanvasGroup.transform.DOScale(1f, 0.15f));
    }

    public override void OnGoldMultiplierButtonClicked()
    {
        base.OnGoldMultiplierButtonClicked();

        // show video ad 
        // And then callback and continue
        // Add ad multipler constant

        AdManager.Instance.ShowRewarded(OnRewardAdComplete, OnRewardAdFailed);
    }

    private void OnRewardAdFailed()
    {
        MenuManager.Instance.OpenMenu<GenericPopupMenuView, GenericPopupMenuController, GenericPopupMenuData>(Menus.Type.GenericPopup, new GenericPopupMenuData
        (
            LocalizationKeys.no,
            LocalizationKeys.no_ads_message,
            LocalizationKeys.ok
        ));
    }

    private void OnRewardAdComplete()
    {
        // 1. Create a fresh list to hold the multiplied rewards
        List<RewardData> bonusRewards = new List<RewardData>();
        int goldAmountForAnimation = 0;
        int finalTotalGold = GameManager.Instance.SaveData.Inventory.Gold;

        // 2. Clone and Modify
        foreach (var originalReward in Data.LevelData.Rewards)
        {
            // Create a new instance so we don't mess with the original LevelData
            RewardData newReward = new RewardData();
            newReward.RewardType = originalReward.RewardType;

            if (originalReward.RewardType == RewardType.Gold)
            {
                // Apply the 3x multiplier
                newReward.Amount = originalReward.Amount * 3;
                goldAmountForAnimation = newReward.Amount;
                finalTotalGold += newReward.Amount;
            }
            else
            {
                // Keep other rewards (Gems, Items, etc.) as they were
                newReward.Amount = originalReward.Amount;
            }

            bonusRewards.Add(newReward);
        }

        // 3. Assign the new list to your Data container
        Data.Rewards = bonusRewards;

        View.GoldRewardView.Initialize(goldAmountForAnimation);
        // 4. Trigger the UI/Animation
        // We pass the 3x amount and the calculated final total
        // PlayGoldAnimation(goldAmountForAnimation, finalTotalGold, 0f, OnGoldAnimationCompleted);
        OnGoldAnimationCompleted();
    }

    private void OnGoldAnimationCompleted()
    {
        UpdateRewardManager();
        Scheduler.Instance.ExecuteAfterDelay(1.5f, Controller.GoToNextLevel);
    }

    private void UpdateRewardManager()
    {
        string currentLevelID = GameManager.Instance.SaveData.CurrentLevelID;

        if (Data.LevelData.Id == currentLevelID)
        {
            // Full Reward: Current level matches, add everything
            GameManager.Instance.SaveData.Inventory.AddRewards(Data.Rewards);
            RewardManager.Instance.AddRewardsToQueue(Data.Rewards);
        }
        else
        {
            // Partial Reward: Level already completed/doesn't match, filter for Gold only
            var goldRewards = new List<RewardData>(); // Replace 'Reward' with your actual class name if different

            foreach (var reward in Data.Rewards)
            {
                if (reward.RewardType == RewardType.Gold)
                {
                    goldRewards.Add(reward);
                }
            }

            if (goldRewards.Count > 0)
            {
                GameManager.Instance.SaveData.Inventory.AddRewards(goldRewards);
                RewardManager.Instance.AddRewardsToQueue(goldRewards);
            }
        }
    }
}

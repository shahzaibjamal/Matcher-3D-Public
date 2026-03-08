using System;
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    private Queue<RewardData> _pendingRewards = new Queue<RewardData>();
    private bool _isShowingPopup = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Init()
    {
        LoadFromSave();
    }

    private void LoadFromSave()
    {
        var saved = GameManager.Instance.SaveData.SavedPendingRewards;
        if (saved != null && saved.Count > 0)
        {
            foreach (var reward in saved) _pendingRewards.Enqueue(reward);
        }
    }
    public void AddRewardToQueue(RewardData reward)
    {
        if (reward == null) return;
        AddRewardsToQueue(new List<RewardData> { reward });
    }

    public void AddRewardsToQueue(List<RewardData> rewards)
    {
        if (rewards == null || rewards.Count == 0) return;

        foreach (var reward in rewards)
        {
            if (reward == null) continue;

            _pendingRewards.Enqueue(reward);

            // Keep the save data in sync with the runtime queue
            GameManager.Instance.SaveData.SavedPendingRewards.Add(reward);
        }

        // Save once after the entire batch is processed to avoid IO overhead
        GameManager.Instance.SaveGame();
    }
    public void CheckAndShowNext(Action onAllRewardsClaimed = null)
    {
        // If we are already showing or the queue is empty, trigger the final callback
        if (_isShowingPopup) return;

        if (_pendingRewards.Count == 0)
        {
            onAllRewardsClaimed?.Invoke();
            return;
        }

        _isShowingPopup = true;
        RewardData nextReward = _pendingRewards.Dequeue();

        // Sync save data
        GameManager.Instance.SaveData.SavedPendingRewards.Remove(nextReward);
        GameManager.Instance.SaveGame();

        MenuManager.Instance.OpenMenu<RewardMenuView, RewardMenuController, RewardMenuData>(
            Menus.Type.Reward,
            new RewardMenuData(nextReward, () =>
            {
                _isShowingPopup = false;
                // Pass the callback forward to the next check
                CheckAndShowNext(onAllRewardsClaimed);
            })
        );
    }
}
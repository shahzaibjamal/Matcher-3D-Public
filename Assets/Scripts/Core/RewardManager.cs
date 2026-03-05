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

    public void AddRewardToQueue(List<RewardData> rewards)
    {
        if (rewards == null) return;

        foreach (var reward in rewards)
        {
            _pendingRewards.Enqueue(reward);
            // Add to save data immediately so it's persistent
            GameManager.Instance.SaveData.SavedPendingRewards.Add(reward);
        }
        GameManager.Instance.SaveGame(); // Save progress
    }

    public void CheckAndShowNext()
    {
        if (_isShowingPopup || _pendingRewards.Count == 0) return;

        _isShowingPopup = true;
        RewardData nextReward = _pendingRewards.Dequeue();

        // Remove from persistent save data since it's now being shown
        GameManager.Instance.SaveData.SavedPendingRewards.Remove(nextReward);
        GameManager.Instance.SaveGame();

        MenuManager.Instance.OpenMenu<RewardMenuView, RewardMenuController, RewardMenuData>(
            Menus.Type.Reward,
            new RewardMenuData(nextReward, () =>
            {
                _isShowingPopup = false;
                CheckAndShowNext();
            })
        );
    }
}
using UnityEngine;
using System;
using System.Collections.Generic;

public class RewardManager : MonoBehaviour
{
    private static RewardManager _instance;
    public static RewardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Look for existing instance in the scene
                _instance = FindFirstObjectByType<RewardManager>();

                // If none exists, create a new persistent GameObject
                if (_instance == null)
                {
                    GameObject go = new GameObject("RewardManager");
                    _instance = go.AddComponent<RewardManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private Queue<RewardData> _pendingRewards = new Queue<RewardData>();
    private bool _isShowingPopup = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds a reward to the visual queue. Call this after updating Inventory.
    /// </summary>
    public void AddRewardToQueue(RewardData reward)
    {
        _pendingRewards.Enqueue(reward);
    }

    public void AddRewardToQueue(List<RewardData> rewards)
    {
        if (rewards == null) return;

        foreach (var reward in rewards)
        {
            _pendingRewards.Enqueue(reward);
        }
    }
    /// <summary>
    /// Checks if there are rewards to show. Call this when arriving at the Main Menu.
    /// </summary>
    public void CheckAndShowNext()
    {
        if (_isShowingPopup || _pendingRewards.Count == 0) return;

        _isShowingPopup = true;
        RewardData nextReward = _pendingRewards.Dequeue();

        // Open the menu using your specific architecture
        MenuManager.Instance.OpenMenu<RewardMenuView, RewardMenuController, RewardMenuData>(
            Menus.Type.Reward,
            new RewardMenuData(nextReward, () =>
            {
                _isShowingPopup = false;
                // Recursively check for the next reward in the queue
                CheckAndShowNext();
            })
        );
    }

    // Optional: Check if any rewards are waiting (for UI badges)
    public bool HasPendingRewards => _pendingRewards.Count > 0;
}
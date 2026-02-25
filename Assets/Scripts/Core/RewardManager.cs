using UnityEngine;
using System.Collections.Generic;

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    private Queue<RewardData> _pendingRewards = new Queue<RewardData>();
    private bool _isShowingPopup = false;

    private void Awake() => Instance = this;

    public void AddRewardToQueue(RewardData reward) => _pendingRewards.Enqueue(reward);

    public void CheckAndShowNext()
    {
        // Don't show if we are already showing one, or if the queue is empty
        if (_isShowingPopup || _pendingRewards.Count == 0) return;

        _isShowingPopup = true;
        RewardData nextReward = _pendingRewards.Dequeue();

        // Pass the data and a completion callback to the Menu Manager
        // 'onExit' will trigger ShowNext again
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
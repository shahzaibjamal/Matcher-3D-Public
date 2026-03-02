using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class DailyRewardsWindow : MonoBehaviour
{
    [SerializeField] private DailyRewardView[] _rewardViews;
    [SerializeField] private RewardIconMapper _iconMapper; // Reference to your SO
    private Action<RewardData> _onRewardClaimedCallback;
    private List<DailyRewardData> _currentRewards;

    public void Initialize(List<DailyRewardData> inputRewards, Action<RewardData> claimCallback)
    {
        _currentRewards = inputRewards;
        _onRewardClaimedCallback = claimCallback;
        RefreshUI();
    }

    private void RefreshUI()
    {
        int daysSinceSignUp = GetDaysSinceSignUp();
        var saveData = GameManager.Instance.SaveData;

        for (int i = 0; i < _rewardViews.Length; i++)
        {
            if (i >= _currentRewards.Count) break;

            DailyRewardData data = _currentRewards[i];
            bool claimed = saveData.ClaimedDailyRewards.Contains(data.Day);
            bool ready = daysSinceSignUp >= data.Day;

            // Resolve sprite here from the SO
            Sprite rewardIcon = _iconMapper.GetIcon(data.RewardType);

            // Pass the sprite directly to the view
            _rewardViews[i].Setup(data, rewardIcon, claimed, ready, OnItemClicked);
        }
    }

    private void OnItemClicked(DailyRewardData data)
    {
        var saveData = GameManager.Instance.SaveData;

        if (!saveData.ClaimedDailyRewards.Contains(data.Day))
        {
            saveData.ClaimedDailyRewards.Add(data.Day);

            // Fire external callback
            _onRewardClaimedCallback?.Invoke(data);

            // Refresh the whole UI to show the new 'Claimed' states
            RefreshUI();
        }
    }

    private int GetDaysSinceSignUp()
    {
        // Assuming sign_up_time is also stored in GameSaveData now
        DateTime signTime = GameManager.Instance.SaveData.SignUpDate;
        return (DateTime.Now - signTime).Days + 1; // +1 if Day 1 starts immediately
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Simulate Next Day")]
    private static void SimulateNextDay()
    {
        var saveData = GameManager.Instance.SaveData;
        saveData.SignUpDate = saveData.SignUpDate.AddDays(-1); // Simulate one more day passed

        Debug.Log($"Simulated next day. New SignUpDate: {saveData.SignUpDate}");
    }
#endif
}

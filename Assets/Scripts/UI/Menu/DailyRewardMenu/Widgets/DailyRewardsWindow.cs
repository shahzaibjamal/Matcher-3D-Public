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
            // 1. Hide views that don't have data (e.g. if you have 7 UI slots but only 5 days of data)
            if (i >= _currentRewards.Count)
            {
                _rewardViews[i].gameObject.SetActive(false);
                continue;
            }

            _rewardViews[i].gameObject.SetActive(true);
            DailyRewardData data = _currentRewards[i];

            // 2. Determine status
            bool claimed = saveData.ClaimedDailyRewards.Contains(data.Day);

            // Ready if the day has passed AND it hasn't been claimed yet
            bool ready = daysSinceSignUp >= data.Day && !claimed;

            // 3. Pass the IconMapper (SO) and data to the view
            // The View will now loop through data.Rewards and use _iconMapper to find sprites
            _rewardViews[i].Setup(data, _iconMapper, claimed, ready, OnItemClicked);
        }
    }

    private void OnItemClicked(DailyRewardData data)
    {
        var saveData = GameManager.Instance.SaveData;

        if (!saveData.ClaimedDailyRewards.Contains(data.Day))
        {
            saveData.ClaimedDailyRewards.Add(data.Day);

            // Fire external callback
            // _onRewardClaimedCallback?.Invoke(data);

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

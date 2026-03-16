using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using TS.LocalizationSystem;

public class DailyRewardView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimedOverlay;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _bgDim;

    [Header("Rewards Container")]
    [SerializeField] private RectTransform rewardContainer; // Where icons are spawned
    [SerializeField] private RewardView rewardPrefab; // The small RewardView prefab

    private DailyRewardData _data;
    private Action<DailyRewardData> _onClaimRequested;
    private List<RewardView> _spawnedRewards = new List<RewardView>();

    // Note: We no longer need a single 'icon' passed in, 
    // because we will resolve icons for each reward inside the loop.
    public void Setup(DailyRewardData data, RewardIconMapper iconMapper, bool isClaimed, bool isReady, Action<DailyRewardData> claimCallback)
    {
        _data = data;
        _onClaimRequested = claimCallback;

        // Visual state based on availability
        _bgDim.gameObject.SetActive(!(isReady || isClaimed));
        dayText.text = string.Format(LocaleManager.Localize(LocalizationKeys.day), data.Day);

        claimedOverlay.SetActive(isClaimed);
        claimButton.interactable = !isClaimed && isReady;

        // 1. Clear previous rewards (important for pooling/refreshing)
        ClearContainer();

        // 2. Spawn and Initialize a RewardView for every reward in the list
        foreach (var reward in data.Rewards)
        {
            RewardView rv = Instantiate(rewardPrefab, rewardContainer);

            // Resolve the specific icon for this reward type
            Sprite icon = iconMapper.GetIcon(reward.RewardType);

            rv.Initialize(icon, reward.Amount);
            _spawnedRewards.Add(rv);
        }

        // 3. Set up button
        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(HandleClick);
    }

    private void ClearContainer()
    {
        foreach (var rv in _spawnedRewards)
        {
            if (rv != null) Destroy(rv.gameObject);
        }
        _spawnedRewards.Clear();
    }

    private void HandleClick()
    {
        _onClaimRequested?.Invoke(_data);
        SetClaimed();
    }

    public void SetClaimed()
    {
        claimedOverlay.SetActive(true);
        claimButton.interactable = false;
        _canvasGroup.alpha = 1.0f;
    }
}
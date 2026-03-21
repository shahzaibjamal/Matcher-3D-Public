using System.Collections.Generic;
using TS.LocalizationSystem;
using UnityEngine;

public class LevelDetailMenuMenuBaseState_Main : LevelDetailMenuMenuBaseState
{
    private List<GameObject> _rewardViews = new List<GameObject>();

    public LevelDetailMenuMenuBaseState_Main(LevelDetailMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        View.DetailPanel.SetActive(true);
        View.StartButton.gameObject.SetActive(true);
        View.StartButton.onClick.AddListener(OnStartButtonClicked);
        View.LevelText.text = string.Format(LocaleManager.Localize(LocalizationKeys.title_level), Data.LevelData.Number);
        View.OkButtonText.text = LocaleManager.Localize(LocalizationKeys.start_game);

        ResetUI();
    }

    public override void Exit()
    {
        base.Exit();
        View.StartButton.onClick.RemoveListener(OnStartButtonClicked);
        CleanUp();
        View.DetailPanel.SetActive(false);
    }

    public override void ResetUI()
    {
        base.ResetUI();
        CleanUp();
        LoadRewards();
    }

    private void CleanUp()
    {
        foreach (var rewardsGo in _rewardViews)
        {
            GameObject.Destroy(rewardsGo);
        }
    }

    private void LoadRewards()
    {
        string currentLevelID = GameManager.Instance.SaveData.CurrentLevelID;
        bool isRewardUnclaimed = Data.LevelData.Id == currentLevelID;
        foreach (var rewardData in Data.LevelData.Rewards)
        {
            if (isRewardUnclaimed || rewardData.RewardType == RewardType.Gold)
            {
                var go = GameObject.Instantiate(View.RewardViewPrefab, View.RewardsContainer);
                _rewardViews.Add(go);
                if (go.TryGetComponent<RewardView>(out var rewardView))
                {
                    Sprite icon = View.IconMapper.GetIcon(rewardData.RewardType);

                    rewardView.Initialize(icon, rewardData.Amount);
                }
            }
        }
    }
    private void OnStartButtonClicked()
    {
        // MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
        // {
        //     OnLoadingComplete = OnLoadingComplete
        // });
        OnLoadingComplete();
    }

    private void OnLoadingComplete()
    {
        GameEvents.OnGameInitializedEvent?.Invoke(Data.LevelData.Id);

        MenuManager.Instance.OpenMenu<GameMenuView, GameMenuController, GameMenuData>(Menus.Type.Game, new GameMenuData
        {
            levelId = Data.LevelData.Id
        });
    }
}

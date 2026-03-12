using System.Collections.Generic;
using TS.LocalizationSystem;
using UnityEngine;

public class LevelDetailMenuController : MenuController<LevelDetailMenuView, LevelDetailMenuData>
{
    private List<GameObject> _rewardViews = new List<GameObject>();
    public override void OnEnter()
    {
        SetState(new LevelDetailMenuMenuBaseState_Main(this));

        View.StartButton.onClick.AddListener(OnStartButtonClicked);
        View.LevelText.text = string.Format(LocaleManager.Localize(LocalizationKeys.title_level), Data.LevelData.Number);
        LoadRewards();
    }
    public override void OnExit()
    {
        View.StartButton.onClick.RemoveListener(OnStartButtonClicked);
        CleanUp();
        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
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
        MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
        {
            OnLoadingComplete = OnLoadingComplete
        });
    }

    private void OnLoadingComplete()
    {
        GameEvents.OnGameInitializedEvent?.Invoke(Data.LevelData.Id);

        MenuManager.Instance.OpenMenu<GameMenuView, GameMenuController, GameMenuData>(Menus.Type.Game, new GameMenuData
        {
            levelId = Data.LevelData.Id
        });
    }

    public override void HandleBackInput()
    {
        base.HandleBackInput();
    }
}
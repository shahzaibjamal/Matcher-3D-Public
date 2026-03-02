using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MatchResultMenuController : MenuController<MatchResultMenuView, MatchResultMenuData>
{
    public override void OnEnter()
    {
        if (Data.IsWin)
        {
            SetState(new MatchResultMenuBaseState_Win(this));
        }
        else
        {
            SetState(new MatchResultMenuBaseState_Lose(this));
        }
        View.ContinueButton.onClick.AddListener(OnContinueButtonClicked);
        View.GoldMulitplierButton.onClick.AddListener(OnGoldMultiplierButtonClicked);
        RewardManager.Instance.AddRewardToQueue(Data.LevelData.Rewards);
    }

    public override void OnExit()
    {
        View.Root.alpha = 0.0f;
        View.GodRays.SetActive(false);
        View.ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
        View.GoldMulitplierButton.onClick.RemoveListener(OnGoldMultiplierButtonClicked);

        base.OnExit();
    }


    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    void OnContinueButtonClicked()
    {
        (CurrentState as MatchResultMenuBaseState).OnContinueButtonClicked();

    }

    public void GoToMainMenu()
    {
        MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
        {
            OnLoadingComplete = OnLoadingComplete
        });
        GameEvents.OnLevelCompleteEvent?.Invoke(Data.IsWin, Data.LevelData.levelUID, Data.Score, Data.Score);
    }

    private void OnLoadingComplete()
    {
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Settings);
    }
    void OnGoldMultiplierButtonClicked()
    {
        (CurrentState as MatchResultMenuBaseState).OnGoldMultiplierButtonClicked();
        // MenuManager.Instance.GoBack();
        // GameEvents.OnCleanSweepTrayEvent?.Invoke();
    }
    public override void HandleBackInput()
    {

    }
}
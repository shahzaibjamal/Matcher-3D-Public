using System;
using TS.LocalizationSystem;
using Unity.VisualScripting;


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
        View.TitleLevelNumber.text = String.Format(LocaleManager.Localize(LocalizationKeys.title_level), Data.LevelData.Number);
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
        View.ContinueButton.interactable = false;
        View.GoldMulitplierButton.interactable = false;
    }

    public void GoToNextLevel()
    {
        string currentLevelID = GameManager.Instance.SaveData.CurrentLevelID;
        GameEvents.OnLevelCompleteEvent?.Invoke(Data.IsWin, Data.LevelData.Id, Data.Score, Data.Score);
        if (Data.LevelData.Id == currentLevelID)
        {
            LoadNextLevel();
        }
        else
        {
            LoadLevelSelect();
        }
    }

    private void LoadNextLevel()
    {
        string currentLevelID = GameManager.Instance.SaveData.CurrentLevelID;

        GameEvents.OnGameInitializedEvent?.Invoke(currentLevelID);
        MenuManager.Instance.OpenMenu<GameMenuView, GameMenuController, GameMenuData>(Menus.Type.Game, new GameMenuData
        {
            levelId = currentLevelID
        });
    }
    private void LoadLevelSelect()
    {
        MenuManager.Instance.OpenMenu<LevelSelectMenuView, LevelSelectMenuController, LevelSelectMenuData>(Menus.Type.LevelSelect);
    }

    void OnGoldMultiplierButtonClicked()
    {
        (CurrentState as MatchResultMenuBaseState).OnGoldMultiplierButtonClicked();
        View.ContinueButton.interactable = false;
        View.GoldMulitplierButton.interactable = false;
    }

    public override void HandleBackInput()
    {

    }
}
using TS.LocalizationSystem;

public class LoseLifeMenuBaseState_Restart : LoseLifeMenuBaseState
{
    public LoseLifeMenuBaseState_Restart(LoseLifeMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        if (GameManager.Instance.SaveData.CurrentLives <= 1)
        {
            View.MessageText.text = LocaleManager.Localize(LocalizationKeys.no_restart_message);
            View.LeaveText.text = LocaleManager.Localize(LocalizationKeys.leave);
            View.LeaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }
        else
        {
            View.LeaveText.text = LocaleManager.Localize(LocalizationKeys.restart);
            View.MessageText.text = LocaleManager.Localize(LocalizationKeys.restart_message);
            View.LeaveButton.onClick.AddListener(OnRestartButtonClicked);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    private void OnRestartButtonClicked()
    {
        UIAnimations.ToonOut(View.canvasGroup, View.Root, () =>
        {
            GameEvents.OnGameQuitEvent?.Invoke();
            MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
            {
                OnLoadingComplete = () =>
                {
                    GameEvents.OnLevelRestartEvent?.Invoke();
                    MenuManager.Instance.OpenMenu<GameMenuView, GameMenuController, GameMenuData>(Menus.Type.Game);
                }
            });
        });
    }
}

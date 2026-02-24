public class PauseMenuController : MenuController<PauseMenuView, PauseMenuData>
{
    public override void OnEnter()
    {
        SetState(new PauseMenuBaseState(this));

        View.ResumeButton.onClick.AddListener(OnResumeButtonClicked);
        View.RestartButton.onClick.AddListener(OnRestarButton);
        View.HomeButton.onClick.AddListener(OnHomeButtonClicked);
        View.CloseButton.onClick.AddListener(OnResumeButtonClicked);
    }
    public override void OnExit()
    {
        View.ResumeButton.onClick.RemoveListener(OnResumeButtonClicked);
        View.RestartButton.onClick.RemoveListener(OnRestarButton);
        View.HomeButton.onClick.RemoveListener(OnHomeButtonClicked);
        View.CloseButton.onClick.RemoveListener(OnResumeButtonClicked);
        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    private void OnHomeButtonClicked()
    {
        GameEvents.OnGameInitializedEvent?.Invoke();
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Main, new MainMenuData());
    }
    private void OnResumeButtonClicked()
    {
        MenuManager.Instance.GoBack();
    }

    private void OnRestarButton()
    {
        Spawner.SpawnGameSystems();
        OnResumeButtonClicked();
    }
}
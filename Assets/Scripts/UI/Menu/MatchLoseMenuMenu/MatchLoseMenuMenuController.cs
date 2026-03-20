public class MatchLoseMenuMenuController : MenuController<MatchLoseMenuMenuView, MatchLoseMenuMenuData>
{
    public override void OnEnter()
    {
        SetState(new MatchLoseMenuMenuBaseState(this));
        View.ClearButton.onClick.AddListener(OnClearButtonClick);
        View.QuitButton.onClick.AddListener(OnQuitButtonClick);
    }
    public override void OnExit()
    {
        View.ClearButton.onClick.RemoveListener(OnClearButtonClick);
        View.QuitButton.onClick.RemoveListener(OnQuitButtonClick);
        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    public override void HandleBackInput()
    {
    }
    private void OnClearButtonClick()
    {
        MenuManager.Instance.GoBack();
        GameEvents.OnCleanSweepTrayEvent?.Invoke();
    }
    private void OnQuitButtonClick()
    {
        GameManager.Instance.SaveData.UseLife();
        MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
        {
            OnLoadingComplete = OnLoadingComplete
        });
    }

    private void OnLoadingComplete()
    {
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Main);
    }
}
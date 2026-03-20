public class MatchLoseMenuController : MenuController<MatchLoseMenuView, MatchLoseMenuData>
{
    public override void OnEnter()
    {
        SetState(new MatchLoseMenuBaseState(this));
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
        MenuManager.Instance.OpenMenu<LoseLifeMenuView, LoseLifeMenuController, LoseLifeMenuData>(Menus.Type.LoseLife, new LoseLifeMenuData
        {
            isRestart = false
        });
    }
}
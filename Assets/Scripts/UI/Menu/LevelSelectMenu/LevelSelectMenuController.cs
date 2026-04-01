public class LevelSelectMenuController : MenuController<LevelSelectMenuView, LevelSelectMenuData>
{
    public override void OnEnter()
    {
        SetState(new LevelSelectMenuBaseState_Main(this));
        View.CloseButtn.onClick.AddListener(HandleBackInput);

        MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
        {
            Delay = -1,
            OnLoadingComplete = InitialFocus,
            LoadingTask = View.InfiniteMapManager.InitializeMapAsync
        });
    }
    public override void OnExit()
    {
        View.CloseButtn.onClick.RemoveListener(HandleBackInput);
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
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Main);
    }
    private void InitialFocus()
    {
        string currentLevel = GameManager.Instance.SaveData.CurrentLevelID;
        Scheduler.Instance.ExecuteAfterDelay(0.3f, () => View.InfiniteMapManager.FocusOnLevel(DataManager.Instance.GetLevelByID(currentLevel).Number)); ;
        SoundController.Instance.PlaySoundEffect("level_screen_load");
    }
}
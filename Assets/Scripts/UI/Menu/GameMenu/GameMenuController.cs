public class GameMenuController : MenuController<GameMenuView, GameMenuData>
{

    public override void OnEnter()
    {
        SetState(new GameMenuBaseState_Main(this));
    }
    public override void OnExit()
    {
        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
        CurrentState.Enter();
    }
    public override void HandleBackInput()
    {
        OpenPauseMenu();
    }

    public void OpenPauseMenu()
    {
        MenuManager.Instance.OpenMenu<PauseMenuView, PauseMenuController, PauseMenuData>(Menus.Type.Pause);
    }
}
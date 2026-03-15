public class LoseLifeMenuController : MenuController<LoseLifeMenuView, LoseLifeMenuData>
{
    public override void OnEnter()
    {
        SetState(Data.isRestart ? new LoseLifeMenuBaseState_Restart(this) : new LoseLifeMenuBaseState_Quit(this));
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
    }

    public override void HandleBackInput()
    {
        base.HandleBackInput();
    }
}
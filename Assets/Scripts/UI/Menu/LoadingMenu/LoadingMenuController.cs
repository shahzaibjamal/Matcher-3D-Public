public class LoadingMenuController : MenuController<LoadingMenuView, LoadingMenuData>
{
    public override void OnEnter()
    {
        SetState(new LoadingMenuBaseState_Main(this));
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
}
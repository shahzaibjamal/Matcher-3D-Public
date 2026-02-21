public class DebugMenuController : MenuController<DebugMenuView, DebugMenuData>
{
    public override void OnEnter()
    {
        SetState(new DebugMenuBaseState_Main(this));
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
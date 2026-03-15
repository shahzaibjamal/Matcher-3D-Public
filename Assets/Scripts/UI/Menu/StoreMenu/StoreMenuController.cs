public class StoreMenuController : MenuController<StoreMenuView, StoreMenuData>
{
    public override void OnEnter()
    {
        SetState(new StoreMenuBaseState_Main(this));
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
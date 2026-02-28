public class GenericPopupMenuController : MenuController<GenericPopupMenuView, GenericPopupMenuData>
{
    public override void OnEnter()
    {
        SetState(new GenericPopupMenuBaseState(this));
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
    }
}
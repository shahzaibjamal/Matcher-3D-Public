public class DailyRewardMenuController : MenuController<DailyRewardMenuView, DailyRewardMenuData>
{
    public override void OnEnter()
    {
        SetState(new DailyRewardMenuBaseState_Main(this));
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
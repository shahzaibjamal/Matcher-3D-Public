public class RewardMenuController : MenuController<RewardMenuView, RewardMenuData>
{
    public override void OnEnter()
    {
        SetState(new RewardMenuBaseState_Main(this));
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
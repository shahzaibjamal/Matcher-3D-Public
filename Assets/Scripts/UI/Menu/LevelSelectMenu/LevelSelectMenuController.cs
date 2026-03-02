public class LevelSelectMenuController : MenuController<LevelSelectMenuView, LevelSelectMenuData>
{
    public override void OnEnter()
    {
        SetState(new LevelSelectMenuBaseState_Main(this));
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
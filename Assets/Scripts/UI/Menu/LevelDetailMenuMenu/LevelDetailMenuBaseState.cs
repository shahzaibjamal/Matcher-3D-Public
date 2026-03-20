using Unity.VisualScripting;

public class LevelDetailMenuMenuBaseState : MenuBaseState<LevelDetailMenuController, LevelDetailMenuView, LevelDetailMenuData>
{
    public LevelDetailMenuMenuBaseState(LevelDetailMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        View.DetailPanel.SetActive(false);
        View.NoLivesPanel.SetActive(false);
    }


    public override void Exit()
    {
    }

    public virtual void ResetUI()
    {

    }
}
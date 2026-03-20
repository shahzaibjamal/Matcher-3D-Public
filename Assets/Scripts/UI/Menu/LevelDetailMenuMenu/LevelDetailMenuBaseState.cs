using Unity.VisualScripting;

public class LevelDetailMenuMenuBaseState : MenuBaseState<LevelDetailMenuController, LevelDetailMenuView, LevelDetailMenuData>
{
    public LevelDetailMenuMenuBaseState(LevelDetailMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        View.StartButton.gameObject.SetActive(false);
        View.ShopButton.gameObject.SetActive(false);
        View.OkButton.gameObject.SetActive(false);
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
public class MatchResultMenuBaseState_Lose : MatchResultMenuBaseState
{
    public MatchResultMenuBaseState_Lose(MatchResultMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        View.GoldMulitplierButton.gameObject.SetActive(false);
        View.Status.gameObject.SetActive(false);
    }

    public override void Exit()
    {
        base.Exit();
    }
}

using TS.LocalizationSystem;

public class MatchResultMenuBaseState_Lose : MatchResultMenuBaseState
{
    public MatchResultMenuBaseState_Lose(MatchResultMenuController controller) : base(controller)
    {
    }
    /*************************************************************/


    //                       NOT BEING USED                       //


    /*************************************************************/

    public override void Enter()
    {
        base.Enter();
        View.GoldMulitplierButton.gameObject.SetActive(false);
        View.Status.gameObject.SetActive(false);
        View.Result.text = LocaleManager.Localize(LocalizationKeys.result_lose);

        Scheduler.Instance.ExecuteAfterDelay(3.2f, () =>
        {
            MenuManager.Instance.GoBack();
            GameEvents.OnCleanSweepTrayEvent?.Invoke();
        });
    }

    public override void Exit()
    {
        base.Exit();
    }
}

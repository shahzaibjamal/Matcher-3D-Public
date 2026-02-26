using TS.LocalizationSystem;

public class MatchResultMenuBaseState_Win : MatchResultMenuBaseState
{
    public MatchResultMenuBaseState_Win(MatchResultMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (Data.Level > 10)
            View.GoldMulitplierButton.gameObject.SetActive(true);
        View.Result.text = LocaleManager.Localize(LocalizationKeys.result_win);

        if (Data.MatchRate > 90)
        {
            View.Status.text = LocaleManager.Localize(LocalizationKeys.status_perfect);
        }
        else if (Data.MatchRate > 70)
        {
            View.Status.text = LocaleManager.Localize(LocalizationKeys.status_amazing);

        }
        else
        {
            View.Status.text = LocaleManager.Localize(LocalizationKeys.status_good);
        }

    }

    public override void Exit()
    {
        base.Exit();
    }
}

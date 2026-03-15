using TS.LocalizationSystem;

public class LoseLifeMenuBaseState_Quit : LoseLifeMenuBaseState
{
    public LoseLifeMenuBaseState_Quit(LoseLifeMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        View.MessageText.text = LocaleManager.Localize(LocalizationKeys.leave_level_message);
        View.LeaveText.text = LocaleManager.Localize(LocalizationKeys.leave);
        View.CancelText.text = LocaleManager.Localize(LocalizationKeys.cancel);
        View.LeaveButton.onClick.AddListener(OnLeaveButtonClicked);
    }

    public override void Exit()
    {
        base.Exit();
    }
}

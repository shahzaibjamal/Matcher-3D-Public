public class MainMenuBaseState_Main : MainMenuBaseState
{
    public MainMenuBaseState_Main(MainMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        View.StartButton.onClick.AddListener(OnStartButtonClicked);
    }

    public override void Exit()
    {
        base.Exit();
    }

    private void OnStartButtonClicked()
    {
        Controller.StartButtonClicked();
    }
}

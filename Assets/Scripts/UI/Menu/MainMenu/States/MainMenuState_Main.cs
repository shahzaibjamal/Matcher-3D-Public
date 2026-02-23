public class MainMenuBaseState_Main : MainMenuBaseState
{
    public MainMenuBaseState_Main(MainMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        View.StartButton.onClick.AddListener(OnStartButtonClicked);
        View.DebugButton.onClick.AddListener(OnDebugButtonClicked);
    }

    public override void Exit()
    {
        base.Exit();
    }

    private void OnStartButtonClicked()
    {
        Controller.StartButtonClicked();
    }
    private void OnDebugButtonClicked()
    {
        MenuManager.Instance.OpenMenu<DebugMenuView, DebugMenuController, DebugMenuData>(Menus.Type.Debug, new DebugMenuData());
    }
}


using TS.LocalizationSystem;
using UnityEngine;

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
        View.SettingsButton.onClick.AddListener(OnSettingsButtonClicked);
    }

    public override void Exit()
    {
        View.StartButton.onClick.RemoveListener(OnStartButtonClicked);
        View.DebugButton.onClick.RemoveListener(OnDebugButtonClicked);
        View.SettingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        base.Exit();
    }

    private void OnStartButtonClicked()
    {
        Controller.StartButtonClicked();
    }
    private void OnSettingsButtonClicked()
    {
        MenuManager.Instance.OpenMenu<SettingsMenuView, SettingsMenuController, SettingsMenuData>(Menus.Type.Settings, new SettingsMenuData());
    }
    private void OnDebugButtonClicked()
    {
        MenuManager.Instance.OpenMenu<DebugMenuView, DebugMenuController, DebugMenuData>(Menus.Type.Debug, new DebugMenuData());
    }
}

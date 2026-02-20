using System;

public class MainMenuController : MenuController<MainMenuView, MainMenuData>
{
    public static event Action OnStartButtonClicked;
    public override void OnEnter()
    {
        SetState(new MainMenuBaseState_Main(this));

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

    public void StartButtonClicked()
    {
        OnStartButtonClicked?.Invoke();
    }
}
using System;
using UnityEngine;

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
        Debug.LogError("StartButton Clicked " + OnStartButtonClicked.GetInvocationList().Length);

        MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
        {
            OnLoadingComplete = OnLoadingComplete
        });
    }

    private void OnLoadingComplete()
    {
        OnStartButtonClicked?.Invoke();
        MenuManager.Instance.OpenMenu<GameMenuView, GameMenuController, GameMenuData>(Menus.Type.Game);
    }
}
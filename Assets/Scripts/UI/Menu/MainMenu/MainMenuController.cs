using System;
using TS.LocalizationSystem;
using UnityEngine;

public class MainMenuController : MenuController<MainMenuView, MainMenuData>
{
    public static event Action OnStartButtonClicked;
    public override void OnEnter()
    {
        SetState(new MainMenuBaseState_Main(this));
        SoundController.instance.PlayBGM("bg", 1.0f);
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

    public override void HandleBackInput()
    {
        MenuManager.Instance.OpenMenu<GenericPopupMenuView, GenericPopupMenuController, GenericPopupMenuData>(
            Menus.Type.GenericPopup,
            new GenericPopupMenuData(
                LocalizationKeys.exit,
                LocalizationKeys.game_quit,
                LocalizationKeys.confirm,
                () => Application.Quit(),
                LocalizationKeys.cancel
            )
        );
    }
    public void StartButtonClicked()
    {
        if (!GameManager.Instance.CanLoadNextLevel())
        {
            MenuManager.Instance.OpenMenu<GenericPopupMenuView, GenericPopupMenuController, GenericPopupMenuData>(
                        Menus.Type.GenericPopup,
                        new GenericPopupMenuData(
                            LocalizationKeys.exit,
                            LocalizationKeys.levels_no_more,
                            LocalizationKeys.confirm
                        )
                    );
            return;
        }

        MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
        {
            OnLoadingComplete = OnLoadingComplete
        });
    }

    private void OnLoadingComplete()
    {
        OnStartButtonClicked?.Invoke();
        MenuManager.Instance.OpenMenu<GameMenuView, GameMenuController, GameMenuData>(Menus.Type.Game, new GameMenuData());
    }
}
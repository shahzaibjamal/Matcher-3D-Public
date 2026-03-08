using System;
using TS.LocalizationSystem;
using UnityEngine;

public class MainMenuController : MenuController<MainMenuView, MainMenuData>
{
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

        MenuManager.Instance.OpenMenu<LevelDetailMenuView, LevelDetailMenuController, LevelDetailMenuData>(Menus.Type.LevelDetail, new LevelDetailMenuData
        {
            LevelData = LevelManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID)
        });
    }
}
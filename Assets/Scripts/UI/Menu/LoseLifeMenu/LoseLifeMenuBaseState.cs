using TS.LocalizationSystem;
using Unity.VisualScripting;

public class LoseLifeMenuBaseState : MenuBaseState<LoseLifeMenuController, LoseLifeMenuView, LoseLifeMenuData>
{
    public LoseLifeMenuBaseState(LoseLifeMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        View.LeaveText.text = LocaleManager.Localize(LocalizationKeys.leave);
        View.CancelText.text = LocaleManager.Localize(LocalizationKeys.cancel);
        View.CancelButton.onClick.AddListener(Controller.HandleBackInput);

        UIAnimations.ToonIn(View.canvasGroup, View.Root, null);
    }


    public override void Exit()
    {
        View.CancelButton.onClick.RemoveListener(Controller.HandleBackInput);
        View.LeaveButton.onClick.RemoveAllListeners();
    }

    protected void OnLeaveButtonClicked()
    {
        GameManager.Instance.SaveData.UseLife();
        UIAnimations.ToonOut(View.canvasGroup, View.Root, () =>
        {
            GameEvents.OnGameQuitEvent?.Invoke();
            MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Main);

            // MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
            // {
            //     OnLoadingComplete = OnLoadingComplete
            // });
        });
    }

    // private void OnLoadingComplete()
    // {
    //     MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Main);
    // }

}
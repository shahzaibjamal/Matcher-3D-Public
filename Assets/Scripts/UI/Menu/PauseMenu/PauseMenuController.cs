using UnityEngine;

public class PauseMenuController : MenuController<PauseMenuView, PauseMenuData>
{
    public override void OnEnter()
    {
        SetState(new PauseMenuBaseState(this));

        View.ResumeButton.onClick.AddListener(OnResumeButtonClicked);
        View.RestartButton.onClick.AddListener(OnRestartButtonClicked);
        View.BGButton.onClick.AddListener(OnResumeButtonClicked);
        View.HomeButton.onClick.AddListener(OnHomeButtonClicked);
        View.SettingsButton.onClick.AddListener(OnSettingsButtonClicked);

        UIAnimations.ToonIn(View.GetComponent<CanvasGroup>(), View.Root, null);
    }
    public override void OnExit()
    {
        View.ResumeButton.onClick.RemoveListener(OnResumeButtonClicked);
        View.RestartButton.onClick.RemoveListener(OnRestartButtonClicked);
        View.HomeButton.onClick.RemoveListener(OnHomeButtonClicked);
        View.BGButton.onClick.RemoveListener(OnResumeButtonClicked);
        View.SettingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    private void OnHomeButtonClicked()
    {
        UIAnimations.ToonOut(View.GetComponent<CanvasGroup>(), View.Root, () =>
        {
            GameEvents.OnGameQuitEvent?.Invoke();
        });
        MenuManager.Instance.OpenMenu<LoadingMenuView, LoadingMenuController, LoadingMenuData>(Menus.Type.Loading, new LoadingMenuData
        {
            OnLoadingComplete = OnLoadingComplete
        });
    }
    private void OnLoadingComplete()
    {
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Main);
    }
    private void OnResumeButtonClicked()
    {
        UIAnimations.ToonOut(View.GetComponent<CanvasGroup>(), View.Root, () =>
        {
            MenuManager.Instance.GoBack();
        });
    }

    private void OnRestartButtonClicked()
    {
        GameEvents.OnLevelRestartEvent?.Invoke();
        OnResumeButtonClicked();
    }
    private void OnSettingsButtonClicked()
    {
        MenuManager.Instance.OpenMenu<SettingsMenuView, SettingsMenuController, SettingsMenuData>(Menus.Type.Settings);
    }
}
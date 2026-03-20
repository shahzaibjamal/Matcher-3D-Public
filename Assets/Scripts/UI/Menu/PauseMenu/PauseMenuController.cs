using DG.Tweening;
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

        View.RestartButton.gameObject.SetActive(GameManager.Instance.SaveData.CurrentLives > 1);
        UIAnimations.ToonIn(View.canvasGroup, View.Root, null);
    }
    public override void OnExit()
    {
        View.ResumeButton.onClick.RemoveListener(OnResumeButtonClicked);
        View.RestartButton.onClick.RemoveListener(OnRestartButtonClicked);
        View.HomeButton.onClick.RemoveListener(OnHomeButtonClicked);
        View.BGButton.onClick.RemoveListener(OnResumeButtonClicked);
        View.SettingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        View.canvasGroup.transform.DOKill();
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
        MenuManager.Instance.OpenMenu<LoseLifeMenuView, LoseLifeMenuController, LoseLifeMenuData>(Menus.Type.LoseLife, new LoseLifeMenuData
        {
            isRestart = false
        });
    }

    private void OnResumeButtonClicked()
    {
        UIAnimations.ToonOut(View.canvasGroup, View.Root, () =>
        {
            HandleBackInput();
        });
    }

    private void OnRestartButtonClicked()
    {
        MenuManager.Instance.OpenMenu<LoseLifeMenuView, LoseLifeMenuController, LoseLifeMenuData>(Menus.Type.LoseLife, new LoseLifeMenuData
        {
            isRestart = true
        });
    }

    private void OnSettingsButtonClicked()
    {
        MenuManager.Instance.OpenMenu<SettingsMenuView, SettingsMenuController, SettingsMenuData>(Menus.Type.Settings, new SettingsMenuData());
    }
}
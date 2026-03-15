
using UnityEngine;

public class SettingsMenuController : MenuController<SettingsMenuView, SettingsMenuData>
{
    public override void OnEnter()
    {
        SetState(new SettingsMenuBaseState_Main(this));
        View.CloseButton.onClick.AddListener(OnCloseButtonClicked);
        UIAnimations.ToonIn(View.canvasGroup, View.Root, null);
    }
    public override void OnExit()
    {
        base.OnExit();
        View.CloseButton.onClick.RemoveListener(OnCloseButtonClicked);
        GameManager.Instance.SaveGame();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    private void OnCloseButtonClicked()
    {
        Data.CurrentContainer = (RectTransform)View.MainView.transform;
        SetState(new SettingsMenuBaseState_Main(this));
        SoundController.Instance.PlaySoundEffect("btn");
    }
}
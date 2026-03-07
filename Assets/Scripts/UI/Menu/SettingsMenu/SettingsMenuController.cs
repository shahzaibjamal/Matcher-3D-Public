
public class SettingsMenuController : MenuController<SettingsMenuView, SettingsMenuData>
{
    public override void OnEnter()
    {
        SetState(new SettingsMenuBaseState_Main(this));
        View.CloseButton.onClick.AddListener(OnCloseButtonClicked);
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
        SetState(new SettingsMenuBaseState_Main(this));
        SoundController.instance.PlaySoundEffect("btn");
    }
}
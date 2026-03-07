using Unity.VisualScripting;

public class SettingsMenuBaseState : MenuBaseState<SettingsMenuController, SettingsMenuView, SettingsMenuData>
{
    public SettingsMenuBaseState(SettingsMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        View.MainView.gameObject.SetActive(false);
        View.LanguageContainer.gameObject.SetActive(false);
        View.PrivacyContainer.gameObject.SetActive(false);
        View.TermsContainer.gameObject.SetActive(false);
        View.CloseButton.gameObject.SetActive(false);
    }


    public override void Exit()
    {
    }
}
using TS.LocalizationSystem;
using Unity.VisualScripting;

public class GenericPopupMenuBaseState : MenuBaseState<GenericPopupMenuController, GenericPopupMenuView, GenericPopupMenuData>
{
    public GenericPopupMenuBaseState(GenericPopupMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        // 1. Set text content
        View.TitleText.text = LocaleManager.Localize(Data.Title);
        View.MessageText.text = LocaleManager.Localize(Data.Message);
        View.ConfirmButtonText.text = LocaleManager.Localize(Data.ConfirmText);

        // 2. Setup Buttons
        View.ConfirmButton.onClick.AddListener(OnConfirmButtonClicked);

        // 3. Handle Optional Cancel Button
        if (Data.IsTwoButton)
        {
            View.CancelButton.gameObject.SetActive(true);
            View.CancelButtonText.text = LocaleManager.Localize(Data.CancelText);
            View.CancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
        else
        {
            View.CancelButton.gameObject.SetActive(false);
        }

    }


    public override void Exit()
    {
        View.ConfirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        View.CancelButton.onClick.RemoveListener(OnCancelButtonClicked);
    }

    private void OnConfirmButtonClicked()
    {
        Data.OnConfirm?.Invoke();
        MenuManager.Instance.GoBack();
    }
    private void OnCancelButtonClicked()
    {
        Data.OnCancel?.Invoke();
        MenuManager.Instance.GoBack();
    }
}
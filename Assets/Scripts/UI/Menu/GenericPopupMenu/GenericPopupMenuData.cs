using System;

public class GenericPopupMenuData : MenuData
{
    public string TitleKey;
    public string MessageKey;

    // Button Labels
    public string ConfirmTextKey;
    public string CancelTextKey;

    // Callbacks
    public Action OnConfirm;
    public Action OnCancel;

    // Logic helper
    public bool IsTwoButton => !string.IsNullOrEmpty(CancelTextKey) || OnCancel != null;
    public GenericPopupMenuData() { }

    public GenericPopupMenuData(
        string titleKey,
        string messageKey,
        string confirmTextKey = null,
        Action onConfirm = null,
        string cancelTextKey = null, // Leave null for 1-button popup
        Action onCancel = null)
    {
        TitleKey = titleKey;
        MessageKey = messageKey;
        ConfirmTextKey = confirmTextKey;
        OnConfirm = onConfirm;
        CancelTextKey = cancelTextKey;
        OnCancel = onCancel;
    }
}
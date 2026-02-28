using System;

public class GenericPopupMenuData : MenuData
{
    public string Title;
    public string Message;

    // Button Labels
    public string ConfirmText;
    public string CancelText;

    // Callbacks
    public Action OnConfirm;
    public Action OnCancel;

    // Logic helper
    public bool IsTwoButton => !string.IsNullOrEmpty(CancelText) || OnCancel != null;

    public GenericPopupMenuData(
        string title,
        string message,
        string confirmTextKey = null,
        Action onConfirm = null,
        string cancelTextKey = null, // Leave null for 1-button popup
        Action onCancel = null)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmTextKey;
        OnConfirm = onConfirm;
        CancelText = cancelTextKey;
        OnCancel = onCancel;
    }
}
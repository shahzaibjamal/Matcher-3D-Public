using UnityEngine;

public enum PointerDirection { None, Top, Bottom, Left, Right }

[System.Serializable]
public struct FTUEStep
{
    public string TargetID;
    public string RequiredEvent;
    [TextArea] public string Message;
    public bool RequireClick;

    [Header("Visual Toggles")]
    public bool ShowCutout;      // If false, the screen stays dark/solid
    public bool ShowHand;        // If false, the pointer is hidden

    public float CustomSize;
    public PointerDirection HandDirection;
}
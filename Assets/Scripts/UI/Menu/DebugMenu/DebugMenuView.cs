using TMPro;
using UnityEngine.UI;

public class DebugMenuView : MenuView
{
    public TMP_Text LevelIdText;

    public SmartToggle DebugToggle;
    public SmartToggle GraphicsToggle;
    public Button NextLevelButton;
    public Button PrevLevelButton;
    public Button ResetButton;
    public Button BackButton;
    public Button PowerUpButton;
    public Slider ScaleSlider;
    public TMP_Text ScaleCurrentText;
    public TMP_Text ScaleMinText;
    public TMP_Text ScaleMaxText;
}
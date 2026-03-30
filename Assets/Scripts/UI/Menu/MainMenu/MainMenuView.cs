using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : MenuView
{
    public Button StartButton;
    public Button SettingsButton;
    public Button LevelButton;
    public Button DebugButton;

    public GoldMainView GoldMainView;
    public LivesView LivesView;

    public Image FadeOutImage;

    public Button GiftButton;
    public Button DailyRewardButton;
    public Button DailySpinButton;
    public Button StoreButton;

    public UIShimmerEffect StoreShimmer;
    public UIShimmerEffect RewardShimmer;

    public GameObject LevelPanel;
    public Image LevelPanelFlashImage;
    public TMP_Text LevelNumber;
}
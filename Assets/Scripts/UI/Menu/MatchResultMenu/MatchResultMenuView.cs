using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchResultMenuView : MenuView
{
    public CanvasGroup Root;
    public Button ContinueButton;
    public Button GoldMulitplierButton;
    public GameObject GodRays;

    public TMP_Text Result;
    public TMP_Text Status;

    public TextAnimations TextAnimation;
    public StarView[] StarViews; // Assign your 3 stars here

    public float StarsApearDelay = 0.3f;
    public int StarCount = 3;

    public TMP_Text LevelNumber;
    public GoldRewardView GoldRewardView;
}
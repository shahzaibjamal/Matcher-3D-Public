using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchResultMenuView : MenuView
{
    public CanvasGroup Root;
    public CanvasGroup BGCanvasGroup;
    public Button ContinueButton;
    public Button GoldMulitplierButton;
    public GameObject GodRays;

    public TMP_Text TitleLevelNumber;
    public TMP_Text Result;
    public TMP_Text Status;

    public TextAnimations TextAnimation;
    public StarView[] StarViews; // Assign your 3 stars here

    public float StarsApearDelay = 0.3f;

    public TMP_Text LevelNumber;
    public GoldRewardView GoldRewardView;
    public GoldMainView GoldMainView;
    public RewardView RewardView;

    public RewardIconMapper RewardIconMapper;

    public CanvasGroup RewardsCanvasGroup;
    public ParticleSystem ConfettiRight;
    public ParticleSystem ConfettiLeft;
    public ParticleSystem ConfettiTop;
}
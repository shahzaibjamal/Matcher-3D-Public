using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelDetailMenuView : MenuView
{
    public TMP_Text OkButtonText;

    public RectTransform Root;
    [Header("Level Detail")]
    public GameObject DetailPanel;
    public TMP_Text LevelText;
    public Transform RewardsContainer;
    public RewardIconMapper IconMapper;
    public GameObject RewardViewPrefab;
    public Button StartButton;

    [Header("No Lives")]
    public GameObject NoLivesPanel;
    public Button OkButton;
    public Button ShopButton;
    public TMP_Text shopButtonText;
    public TMP_Text NoLivesText;
    public TMP_Text TimerText;

}
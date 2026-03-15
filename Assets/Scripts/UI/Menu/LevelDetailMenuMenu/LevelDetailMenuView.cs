using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelDetailMenuView : MenuView
{
    public RectTransform Root;
    public TMP_Text LevelText;
    public Transform RewardsContainer;
    public RewardIconMapper IconMapper;
    public GameObject RewardViewPrefab;
    public Button StartButton;
}
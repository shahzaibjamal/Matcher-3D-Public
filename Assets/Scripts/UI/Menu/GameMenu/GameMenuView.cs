using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameMenuView : MenuView
{
    public Transform ItemViewParent;
    public ItemView ItemViewPrefab;
    public TrayView TrayView;

    public Button PauseButton;
    public TMP_Text LevelId;
    public GoldMainView GoldMainView;
    public PowerUpButton PowerUpPrefab;

    private List<PowerUpButton> _activeButtons = new List<PowerUpButton>();

    public Transform PowerUpContainer;

    [Header("Data Configuration")]
    public PowerUpVisualDatabase PowerUpVisualDatabase;

    public BroomSweeper BroomSweeper;

}
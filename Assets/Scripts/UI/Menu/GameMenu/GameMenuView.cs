using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMenuView : MenuView
{
    public Transform ItemViewParent;
    public ItemView ItemViewPrefab;
    public TrayView TrayView;

    public Button PauseButton;

    // public Button UndoButton;
    // public Button ShakeButton;
    // public Button HintButton;
    // public Button MagnetButton;

    public PowerUpButton PowerUpPrefab;

    private List<PowerUpButton> _activeButtons = new List<PowerUpButton>();

    public Transform PowerUpContainer;

    [Header("Data Configuration")]
    public PowerUpVisualDatabase PowerUpVisualDatabase;

}
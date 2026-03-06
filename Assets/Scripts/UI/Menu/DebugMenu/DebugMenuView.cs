using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenuView : MenuView
{
    public GameData GameData;
    public TMP_InputField LevelIdInput;
    public Transform ItemsParent;
    public GameObject ItemRowPrefab;
    public Button LoadButton;
    public Button SaveButton;
    public Button BackButton;
    public Button PowerUpButton;
}
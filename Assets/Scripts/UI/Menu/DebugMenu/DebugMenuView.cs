using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenuView : MenuView
{
    public LevelDatabase LevelDatabase;
    public TMP_InputField LevelUidInput;
    public Transform ItemsParent;
    public GameObject ItemRowPrefab;
    public Button LoadButton;
    public Button SaveButton;
    public Button BackButton;
}
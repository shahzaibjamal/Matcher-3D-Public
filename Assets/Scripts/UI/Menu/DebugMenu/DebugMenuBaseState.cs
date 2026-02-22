using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenuBaseState : MenuBaseState<DebugMenuController, DebugMenuView, DebugMenuData>
{
    private LevelData _currentLevel;
    public DebugMenuBaseState(DebugMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        View.LoadButton.onClick.AddListener(OnLoadButtonClicked);
        View.LevelUidInput.onSubmit.AddListener((input) => { OnLoadButtonClicked(); });
        View.BackButton.onClick.AddListener(() =>
        {
            MenuManager.Instance.GoBack();
        });
        LoadGameData();
    }


    public override void Exit()
    {
    }

    private void OnLoadButtonClicked()
    {
        LoadLevel();
    }

    public void LoadLevel()
    {
        string uid = View.LevelUidInput.text;
        _currentLevel = View.LevelDatabase.GetLevelByUID(uid);

        foreach (Transform child in View.ItemsParent)
            GameObject.Destroy(child.gameObject);

        if (_currentLevel == null)
        {
            Debug.LogWarning("No level found with UID: " + uid);
            return;
        }

        foreach (var entry in _currentLevel.itemsToSpawn)
        {
            var debugItemRow = CreateDebugItemRow(entry.itemUID, entry.count);
            if (debugItemRow != null)
            {
                debugItemRow.Quantity.onEndEdit.AddListener(val =>
                {
                    if (int.TryParse(val, out int newCount))
                        entry.count = newCount;
                });
            }
        }
    }

    private void LoadGameData()
    {
        CreateDebugItemRow("LeapDuration", View.GameData.LeapDuration);
        CreateDebugItemRow("MergeDuration", View.GameData.MergeDuration);
        CreateDebugItemRow("FlightUpDuration", View.GameData.FlightUpDuration);
        CreateDebugItemRow("FlightToTrayDuration", View.GameData.FlightToTrayDuration);

    }

    private DebugItemRow CreateDebugItemRow(string uid, float quantity)
    {
        GameObject row = GameObject.Instantiate(View.ItemRowPrefab, View.ItemsParent);
        var debugItemRow = row.GetComponent<DebugItemRow>();

        if (debugItemRow != null)
        {
            debugItemRow.Uid.text = uid;
            debugItemRow.Quantity.text = quantity.ToString();
        }
        return debugItemRow;
    }
}
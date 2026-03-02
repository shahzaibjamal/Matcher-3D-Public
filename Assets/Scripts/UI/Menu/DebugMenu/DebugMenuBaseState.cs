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
        View.SaveButton.onClick.AddListener(() => GameManager.Instance.UseRaycast = !GameManager.Instance.UseRaycast);
        View.LevelIdInput.onSubmit.AddListener((input) => { OnLoadButtonClicked(); });
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
        string id = View.LevelIdInput.text;
        _currentLevel = DataManager.Instance.GetLevelByID(id);

        foreach (Transform child in View.ItemsParent)
            GameObject.Destroy(child.gameObject);

        if (_currentLevel == null)
        {
            Debug.LogWarning("No level found with ID: " + id);
            return;
        }

        foreach (var entry in _currentLevel.ItemsToSpawn)
        {
            CreateDebugItemRow(entry.Id, entry.Count, (val) =>
            {
                entry.Count = (int)val;
            });
        }
    }

    private void LoadGameData()
    {
        CreateDebugItemRow("LeapDuration", View.GameData.LeapDuration,
            (val) => View.GameData.LeapDuration = val);

        CreateDebugItemRow("MergeDuration", View.GameData.MergeDuration,
            (val) => View.GameData.MergeDuration = val);

        CreateDebugItemRow("FlightUpDuration", View.GameData.FlightUpDuration,
            (val) => View.GameData.FlightUpDuration = val);

        CreateDebugItemRow("FlightToTrayDuration", View.GameData.FlightToTrayDuration,
            (val) => View.GameData.FlightToTrayDuration = val);

    }

    private DebugItemRow CreateDebugItemRow(string id, float initialValue, System.Action<float> onValueChanged)
    {
        GameObject row = GameObject.Instantiate(View.ItemRowPrefab, View.ItemsParent);
        var debugItemRow = row.GetComponent<DebugItemRow>();

        if (debugItemRow != null)
        {
            debugItemRow.Id.text = id;
            debugItemRow.Quantity.text = initialValue.ToString();

            // Clear existing listeners to avoid double-firing
            debugItemRow.Quantity.onEndEdit.RemoveAllListeners();
            debugItemRow.Quantity.onEndEdit.AddListener(val =>
            {
                if (float.TryParse(val, out float newValue))
                {
                    onValueChanged?.Invoke(newValue);
                }
            });
        }
        return debugItemRow;
    }
}
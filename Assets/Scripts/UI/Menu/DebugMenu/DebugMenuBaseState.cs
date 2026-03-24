using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugMenuBaseState : MenuBaseState<DebugMenuController, DebugMenuView, DebugMenuData>
{
    public DebugMenuBaseState(DebugMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        View.NextLevelButton.onClick.AddListener(OnLoadNextLevelButtonClicked);
        View.PrevLevelButton.onClick.AddListener(OnLoadPrevLevelButtonClicked);
        View.PowerUpButton.onClick.AddListener(OnAddPowerUpButton);
        View.ResetButton.onClick.AddListener(OnResetButton);

        View.DebugToggle.SetIsOn(DataManager.Instance.Metadata.Settings.IsDebug, false);
        View.GraphicsToggle.SetIsOn(DataManager.Instance.Metadata.Settings.ShowGraphicsSettings, false);
        View.DebugToggle.OnValueChanged += OnDebugToggleChanged;
        View.GraphicsToggle.OnValueChanged += OnGraphicsToggleChanged;
        View.LevelIdText.text = GameManager.Instance.SaveData.CurrentLevelID;
        RefreshUI();
    }

    private void OnGraphicsToggleChanged(bool value)
    {
        DataManager.Instance.Metadata.Settings.ShowGraphicsSettings = value;
    }
    private void OnDebugToggleChanged(bool value)
    {
        DataManager.Instance.Metadata.Settings.IsDebug = value;
    }

    public override void Exit()
    {
        View.NextLevelButton.onClick.RemoveListener(OnLoadNextLevelButtonClicked);
        View.PrevLevelButton.onClick.RemoveListener(OnLoadPrevLevelButtonClicked);
        View.PowerUpButton.onClick.RemoveListener(OnAddPowerUpButton);
        View.ResetButton.onClick.RemoveListener(OnResetButton);
        View.DebugToggle.OnValueChanged -= OnDebugToggleChanged;
        View.GraphicsToggle.OnValueChanged -= OnGraphicsToggleChanged;
    }


    private void OnLoadPrevLevelButtonClicked()
    {
        string currentId = GameManager.Instance.SaveData.CurrentLevelID;
        LevelData prevLevel = LevelManager.Instance.GetPrevLevelInDatabase(currentId);

        if (prevLevel != null)
        {
            // Update Save Data
            GameManager.Instance.SaveData.CurrentLevelID = prevLevel.Id;
            RefreshUI();
        }
    }

    private void OnLoadNextLevelButtonClicked()
    {
        string currentId = GameManager.Instance.SaveData.CurrentLevelID;
        LevelData nextLevel = LevelManager.Instance.GetNextLevelInDatabase(currentId);

        if (nextLevel != null)
        {
            // Update Save Data
            GameManager.Instance.SaveData.CurrentLevelID = nextLevel.Id;
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        var currentLevel = DataManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID);
        if (currentLevel != null)
        {
            View.LevelIdText.text = $"Level {currentLevel.Number}";
        }
    }

    private void OnAddPowerUpButton()
    {
        GameManager.Instance.SaveData.Inventory.AddPowerUp(PowerUpType.Shake, 5);
        GameManager.Instance.SaveData.Inventory.AddPowerUp(PowerUpType.Magnet, 5);
        GameManager.Instance.SaveData.Inventory.AddPowerUp(PowerUpType.Hint, 5);
        GameManager.Instance.SaveData.Inventory.AddPowerUp(PowerUpType.Undo, 5);
    }
    private void OnResetButton()
    {
        SaveSystem.ClearSave();

        // 2. Stop all background CPU tasks (Tweens/Coroutines)
        DOTween.KillAll();

        // 3. The "Nuclear" Part: Destroy ALL persistent objects
        // We get all root objects in the scene and find which ones are persistent
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            // If the object has no parent and survives scene loads, kill it
            if (obj.transform.parent == null && obj.scene.name == "DontDestroyOnLoad")
            {
                GameObject.Destroy(obj);
            }

        }

        // 4. Reload the very first scene in your Build Settings (Index 0)
        // This forces your "Splash" or "Boot" logic to start 100% fresh
        SceneManager.LoadScene(0);
    }
}
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
        float savedScale = DataManager.Instance.Metadata.Settings.ItemScaleMultiplier;
        View.ScaleSlider.value = savedScale;

        var settings = DataManager.Instance.Metadata.Settings;
        float minScale = settings.MinItemScale;
        float maxScale = settings.MaxItemScale;
        float currentScale = settings.ItemScaleMultiplier;

        // 1. Setup Slider Bounds
        View.ScaleSlider.minValue = minScale;
        View.ScaleSlider.maxValue = maxScale;
        View.ScaleSlider.value = currentScale;

        // 2. Initial Text Update
        View.ScaleMinText.text = minScale.ToString("F1");
        View.ScaleMaxText.text = maxScale.ToString("F1");
        UpdateCurrentScaleText(currentScale);

        // 3. Add the Listener
        View.ScaleSlider.onValueChanged.AddListener(OnItemScaleChanged);

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
        View.ScaleSlider.onValueChanged.RemoveListener(OnItemScaleChanged);
    }

    private void OnItemScaleChanged(float newValue)
    {
        // 4. Snap to 0.05 increments for a "cleaner" feel
        float snappedValue = Mathf.Round(newValue * 20f) / 20f;

        // Update metadata and save
        DataManager.Instance.Metadata.Settings.ItemScaleMultiplier = snappedValue;

        // 5. Update UI Text
        UpdateCurrentScaleText(snappedValue);

        // Optional: Only update slider position if it's significantly different 
        // to avoid recursive listener triggers or "jitter"
        if (Mathf.Abs(View.ScaleSlider.value - snappedValue) > 0.01f)
        {
            View.ScaleSlider.value = snappedValue;
        }
    }
    private void UpdateCurrentScaleText(float value)
    {
        // "F2" shows two decimals (e.g., 1.05) which is usually 
        // best for scale multipliers.
        View.ScaleCurrentText.text = value.ToString("F2") + "x";
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
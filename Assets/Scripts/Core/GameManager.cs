using UnityEngine;
using System;
using TS.LocalizationSystem;

public class GameManager : MonoBehaviour
{
    // Singleton Instance

    public bool UseRaycast;    //remove this

    public static GameManager Instance { get; private set; }

    [Header("Prefabs & References")]
    [SerializeField] private Spawner _spawnerPrefab;
    private Spawner _activeSpawner;
    private SlotManager _slotManager;

    const int SLOT_COUNT = 7;
    public GameSaveData SaveData { get; private set; }

    // Events for other systems to subscribe to
    public static event Action OnGameStarted;
    public float _levelStartTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LocaleManager.SetDefaultLocale();
    }

    private void Start()
    {
        SaveData = SaveSystem.Load();
        LevelManager.Instance.Initialize(SaveData);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1; // -1 means "unlimited"

        MainMenuController.OnStartButtonClicked += StartGame;
        Debug.Log("Game Manager: start method called... ");

        GameEvents.OnGameInitializedEvent += SpawnGameSystems;
        GameEvents.OnGameQuitEvent += Cleanup;
        GameEvents.OnGameOverEvent += TriggerGameOver;
        GameEvents.OnLevelRestartEvent += RestartLevel;
        GameEvents.OnPowerUpAmountChangeEvent += HandlePowerUpAmountChange;

        _slotManager = new SlotManager(SLOT_COUNT);

        // 1. Launch the Main Menu on Startup
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(
            Menus.Type.Main,
            new MainMenuData()
        );
    }


    #region Game Lifecycle Functions


    public void StartGame()
    {
        Debug.Log("Game Manager: Starting Game...");

        // 2. Notify any listeners (Menus, Sound, etc.)
        OnGameStarted?.Invoke();
        MenuManager.Instance.OpenMenu<GameMenuView, GameMenuController, GameMenuData>(
            Menus.Type.Game,
            new GameMenuData(_slotManager, SLOT_COUNT)
        );
        Cleanup();
        _levelStartTime = Time.time;

        // 3. Example: Close Main Menu and Open Game UI
        // MenuManager.Instance.CloseMenu(Menus.Type.Main);
        // MenuManager.Instance.OpenMenu<GameUI>(...);
    }

    private void SpawnGameSystems()
    {
        if (_activeSpawner == null && _spawnerPrefab != null)
        {
            // 3. Spawn the world spawner
            _activeSpawner = Instantiate(_spawnerPrefab);
            LevelData levelData = LevelManager.Instance.GetCurrentProgressLevel();
            _activeSpawner.SpawnLevel(levelData, (itemData, sourceTransform) =>
            {
                _slotManager.AddItem(itemData, sourceTransform);
            });
        }
    }
    public void TriggerGameOver(bool won)
    {
        Debug.Log("Game Manager: Game Over!" + (won ? " You won " : " You lost"));
        SaveGame();

        float score = 0;
        if (won)
        {
            int initialTotalCount = _activeSpawner.InitialTotalItems;
            int currentTotalCount = _activeSpawner.CurrentTotalRemaining;
            int initialCollectableItems = _activeSpawner.InitialCollectableItems;
            int currentCollectablesRemaining = _activeSpawner.CurrentCollectablesRemaining;
            score = CalculateScore(currentTotalCount - currentCollectablesRemaining, initialTotalCount - initialCollectableItems, initialCollectableItems);
        }
        GameEvents.OnShowMatchResultEvent?.Invoke(won, score);
    }

    private float CalculateScore(float currentJunk, float totalJunk, int initialCollectableItems)
    {
        // 1. Dynamic Target Calculation
        // We give 2 seconds per item, but clamp it between 30s and 90s.
        // This creates a "Gold Standard" time for that specific level size.
        float avgTimePerItem = 1.0f;
        float targetTime = Mathf.Clamp(initialCollectableItems * avgTimePerItem, 30f, 90f);
        float maxTime = 180f; // 3 Minute Hard Cap
        float timeTaken = Time.time - _levelStartTime;

        // 2. Accuracy (The "Perfect Match" Factor)
        // 1.0 = No junk clicked.
        float accuracy = totalJunk > 0 ? currentJunk / totalJunk : 1.0f;

        // 3. Time Score (The "Speed" Factor)
        float timeScore = 1.0f;

        if (timeTaken > targetTime)
        {
            // How far are we between the Target (1.0) and the Hard Cap (0.5)?
            // If they hit 3 mins, they get 0.5.
            float timeRemainingRatio = (timeTaken - targetTime) / (maxTime - targetTime);
            timeScore = Mathf.Lerp(1.0f, 0.5f, timeRemainingRatio);
        }

        // Ensure timeScore doesn't drop below 0.5 even if they exceed 3 mins
        timeScore = Mathf.Max(0.5f, timeScore);

        // 4. Final Calculation
        // Accuracy is the multiplier. 
        // To get 0.9 (3 stars), you MUST be accurate AND fast.
        float finalScore = accuracy * timeScore;

        Debug.Log($"[SCORE] Actual: {timeTaken:F1}s | Target: {targetTime}s | Acc: {accuracy:F2} | Final: {finalScore:F2}");

        return finalScore;
    }

    private void HandlePowerUpAmountChange(PowerUpType powerUpType, int amount)
    {
        SaveData.Inventory.AddPowerUp(powerUpType, amount);
        SaveGame();
    }

    public void Cleanup()
    {
        if (_activeSpawner != null)
        {
            Destroy(_activeSpawner.gameObject);
        }
        _activeSpawner = null;
        _slotManager.Reset();
    }

    public void SaveGame()
    {
        SaveSystem.Save(SaveData);
    }

    private void RestartLevel()
    {
        Cleanup();
        SpawnGameSystems();
    }
    #endregion

}
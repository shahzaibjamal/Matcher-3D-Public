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

    public const int SLOT_COUNT = 7;
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
        GameEvents.OnGameInitializedEvent += LoadCurrentLevel;
        GameEvents.OnGameQuitEvent += Cleanup;
        GameEvents.OnGameOverEvent += TriggerGameOver;
        GameEvents.OnLevelRestartEvent += RestartLevel;
        GameEvents.OnPowerUpAmountChangeEvent += HandlePowerUpAmountChange;
        GameEvents.OnGoldUpdatedEvent += OnGoldUpdate;
        GameEvents.OnLevelCompleteEvent += HandleLevelComplete;

        _slotManager = new SlotManager(SLOT_COUNT);

        // 1. Launch the Main Menu on Startup
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(
            Menus.Type.Main,
            new MainMenuData()
        );
    }
    void OnDestroy()
    {
        MainMenuController.OnStartButtonClicked -= StartGame;
        GameEvents.OnGameInitializedEvent -= LoadCurrentLevel;
        GameEvents.OnGameQuitEvent -= Cleanup;
        GameEvents.OnGameOverEvent -= TriggerGameOver;
        GameEvents.OnLevelRestartEvent -= RestartLevel;
        GameEvents.OnPowerUpAmountChangeEvent -= HandlePowerUpAmountChange;
        GameEvents.OnGoldUpdatedEvent -= OnGoldUpdate;
        GameEvents.OnLevelCompleteEvent -= HandleLevelComplete;

    }

    private void OnGoldUpdate(int amount)
    {
        SaveGame();
    }
    #region Game Lifecycle Functions


    public void StartGame()
    {
        Debug.Log("Game Manager: Starting Game...");

        OnGameStarted?.Invoke();
        Cleanup();
    }

    private void LoadCurrentLevel()
    {
        LevelData levelData = LevelManager.Instance.GetCurrentProgressLevel();
        LoadLevelSpawner(levelData);
    }

    public void LoadLevelByUid(string uid)
    {
        LevelData levelData = LevelManager.Instance.GetLevelByUID(uid);
        LoadLevelSpawner(levelData);
    }

    private void LoadLevelSpawner(LevelData levelData)
    {
        if (_activeSpawner == null && _spawnerPrefab != null)
        {
            // _activeSpawner = Instantiate(_spawnerPrefab);
            PrefabManager.Instance.InstantiatePrefab("Spawner", (spawner) =>
            {
                if (spawner.TryGetComponent<Spawner>(out _activeSpawner))
                {
                    _activeSpawner.SpawnLevel(levelData, (itemData, sourceTransform) =>
                    {
                        _slotManager.AddItem(itemData, sourceTransform);
                    });
                    _levelStartTime = Time.time;
                }
            });
        }
    }

    public void TriggerGameOver(bool won)
    {
        Debug.Log("Game Manager: Game Over!" + (won ? " You won " : " You lost"));

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
        // We give 1 seconds per item, but clamp it between 30s and 90s.
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
        LoadCurrentLevel();
    }
    private void HandleLevelComplete(bool isComplete, string levelUid, int score, int stars)
    {
        LevelManager.Instance.MarkLevelComplete(levelUid, Time.time - _levelStartTime, score, stars);
        Cleanup();
        SaveGame();
    }


    public bool CanLoadNextLevel()
    {
        return LevelManager.Instance.HasMoreContent();
    }
    #endregion

}
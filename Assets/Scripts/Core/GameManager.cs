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
    private float _levelStartTime;
    private string _currentLevelId;
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

        // QualitySettings.vSyncCount = 0;
        // Application.targetFrameRate = -1; // -1 means "unlimited"

        MainMenuController.OnStartButtonClicked += StartGame;
        GameEvents.OnGameInitializedEvent += LoadLevelById;
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
        GameEvents.OnGameInitializedEvent -= LoadLevelById;
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

    public void LoadLevelById(string levelId = null)
    {
        _currentLevelId = levelId;
        LevelData levelData;
        if (levelId == null)
        {
            levelData = LevelManager.Instance.GetCurrentProgressLevel();
        }
        else
        {
            levelData = LevelManager.Instance.GetLevelByID(levelId);
        }
        LoadLevelSpawner(levelData);
    }

    private void LoadLevelSpawner(LevelData levelData)
    {
        Debug.LogError("LoadLevelSpawner");

        if (_activeSpawner == null && _spawnerPrefab != null)
        {
            AssetLoader.Instance.InstantiatePrefab("Spawner", (spawner) =>
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
        // 1. DYNAMIC TIME TARGETS
        // Increased the time per item and the caps to give players more room to breathe.
        float avgTimePerItem = 2.5f; // Was 1.0s
        float targetTime = Mathf.Clamp(initialCollectableItems * avgTimePerItem, 45f, 120f);
        float maxTime = 300f; // 5 Minute Hard Cap (Was 3)
        float timeTaken = Time.time - _levelStartTime;

        // 2. GENEROUS ACCURACY (Power Curve)
        // Instead of a linear drop (Acc = current/total), we use a power function.
        // Squaring the fraction makes high accuracy stay high for longer.
        float accuracyRaw = totalJunk > 0 ? currentJunk / totalJunk : 1.0f;
        float accuracy = Mathf.Pow(accuracyRaw, 0.5f); // Square Root curve: 80% junk left still gives ~90% score

        // 3. GENEROUS TIME SCORE
        float timeScore = 1.0f;

        if (timeTaken > targetTime)
        {
            // Use a Cosine or SmoothStep lerp so the score drops slowly at first
            float timeRatio = Mathf.InverseLerp(targetTime, maxTime, timeTaken);

            // This ensures the drop-off isn't felt immediately after the targetTime
            float smoothRatio = 1f - Mathf.Pow(timeRatio, 2);
            timeScore = Mathf.Lerp(0.7f, 1.0f, smoothRatio); // Floor is now 0.7 instead of 0.5
        }

        // 4. WEIGHTED CALCULATION
        // We weight Accuracy higher than Time. Players hate being punished for taking their time
        // but feel rewarded for being precise.
        float finalScore = (accuracy * 0.7f) + (timeScore * 0.3f);

        // Final safety clamp: 1.0 max, 0.6 min (The "Participant" trophy floor)
        finalScore = Mathf.Clamp(finalScore, 0.6f, 1.0f);

        Debug.Log($"[SCORE] Time: {timeTaken:F1}s | Acc: {accuracy:F2} | WeightResult: {finalScore:F2}");

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
        LoadLevelById(_currentLevelId);
    }
    private void HandleLevelComplete(bool isComplete, string levelId, int score, int stars)
    {
        LevelManager.Instance.MarkLevelComplete(levelId, Time.time - _levelStartTime, score, stars);
        Cleanup();
        SaveGame();
    }


    public bool CanLoadNextLevel()
    {
        return LevelManager.Instance.HasMoreContent();
    }
    #endregion

}
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
        GameEvents.OnGameInitializedEvent += SpawnGameSystems;
        GameEvents.OnGameQuitEvent += Cleanup;
        GameEvents.OnGameOverEvent += TriggerGameOver;
        GameEvents.OnLevelRestartEvent += RestartLevel;

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
            _activeSpawner.SpawnLevel("level_01", (itemData, sourceTransform) =>
            {
                _slotManager.AddItem(itemData, sourceTransform);
            });
        }
    }
    public void TriggerGameOver(bool won)
    {
        Debug.Log("Game Manager: Game Over!" + (won ? " You won " : " You lost"));
        SaveGame();

        // Cleanup spawner if necessary
        Cleanup();
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
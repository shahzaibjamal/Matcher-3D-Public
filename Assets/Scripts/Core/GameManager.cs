using UnityEngine;
using System;
using TS.LocalizationSystem;

public class GameManager : MonoBehaviour
{
    // Singleton Instance
    public static GameManager Instance { get; private set; }

    [Header("Prefabs & References")]
    [SerializeField] private Spawner spawnerPrefab;
    private Spawner activeSpawner;
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
        GameEvents.OnGameOverEvent += TriggerGameOver;

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

        // 3. Example: Close Main Menu and Open Game UI
        // MenuManager.Instance.CloseMenu(Menus.Type.Main);
        // MenuManager.Instance.OpenMenu<GameUI>(...);
    }

    private void SpawnGameSystems()
    {
        if (activeSpawner == null && spawnerPrefab != null)
        {
            // 3. Spawn the world spawner
            activeSpawner = Instantiate(spawnerPrefab);
            activeSpawner.SpawnLevel("level_01", (itemData, sourceTransform) =>
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
        if (activeSpawner != null) Destroy(activeSpawner.gameObject);
    }

    public void SaveGame()
    {
        SaveSystem.Save(SaveData);
    }
    #endregion

}
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    // Singleton Instance
    public static GameManager Instance { get; private set; }

    [Header("Prefabs & References")]
    [SerializeField] private Spawner spawnerPrefab;
    private Spawner activeSpawner;

    // Events for other systems to subscribe to
    public static event Action OnGameStarted;
    public static event Action OnGameOver;

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
    }

    private void Start()
    {
        MainMenuController.OnStartButtonClicked += StartGame;
        GameMenuController.OnGameStarted += SpawnGameSystems;

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
            new GameMenuData()
        );

        // 3. Example: Close Main Menu and Open Game UI
        // MenuManager.Instance.CloseMenu(Menus.Type.Main);
        // MenuManager.Instance.OpenMenu<GameUI>(...);
    }

    private void SpawnGameSystems()
    {
        Debug.Log("Game Manager: SpawnGameSystems." + (activeSpawner == null) + " " + (spawnerPrefab != null));

        if (activeSpawner == null && spawnerPrefab != null)
        {
            activeSpawner = Instantiate(spawnerPrefab);
            activeSpawner.SpawnLevel("level_01");
            Debug.Log("Game Manager: Spawner Spawned.");
        }


    }

    public void TriggerGameOver()
    {
        Debug.Log("Game Manager: Game Over!");

        // Cleanup spawner if necessary
        if (activeSpawner != null) Destroy(activeSpawner);

        OnGameOver?.Invoke();
    }

    #endregion
}
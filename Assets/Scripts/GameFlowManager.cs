using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Spawner spawner;
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private GameMenu gameMenu;

    [Header("Level Settings")]
    [SerializeField] private string startLevelUID = "level_001";

    void Start()
    {
        // Subscribe to events
        spawner.OnLevelInitialized += HandleLevelInitialized;
        // slotManager.OnLevelComplete += HandleLevelComplete;

        // Kick off the game by spawning the first level
        spawner.SpawnLevel(startLevelUID);

    }

    private void HandleLevelInitialized(LevelData level)
    {
        // Tell SlotManager to initialize slots for this level
        slotManager.InitializeLevel(level);
    }

    private void HandleLevelComplete(LevelData level)
    {
        Debug.Log("Level Complete: " + level.levelName);

        // TODO: Show win screen, load next level, or trigger progression
        // Example: load next level
        // spawner.SpawnLevel("level_002");
    }
}

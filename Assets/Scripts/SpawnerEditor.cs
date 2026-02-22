#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Reflection;

public partial class Spawner : MonoBehaviour
{
    /// <summary>
    /// Shared logic for spawning game systems.
    /// Can be called at runtime or from editor menu.
    /// </summary>
    public static void SpawnGameSystems()
    {
        // Find and delete any Spawner in the scene
        var spawner = Object.FindObjectOfType<Spawner>();
        if (spawner != null)
        {
#if UNITY_EDITOR
            Object.DestroyImmediate(spawner.gameObject);
#else
            Object.Destroy(spawner.gameObject);
#endif
            Debug.Log("Spawner deleted.");
        }
        else
        {
            Debug.Log("No Spawner found to delete.");
        }

        // Find the GameManager in the scene
        var gameManager = Object.FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // Use reflection to call private method SpawnGameSystems
            MethodInfo method = typeof(GameManager).GetMethod("SpawnGameSystems",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (method != null)
            {
                method.Invoke(gameManager, null);
                Debug.Log("GameManager.SpawnGameSystems invoked via reflection.");
            }
            else
            {
                Debug.LogWarning("SpawnGameSystems method not found on GameManager.");
            }
        }
        else
        {
            Debug.LogWarning("No GameManager found in scene.");
        }

        // Initialize TrayView if present
        var trayView = Object.FindObjectOfType<TrayView>();
        if (trayView != null)
        {
            trayView.Initialize(7);
        }
        else
        {
            Debug.LogWarning("No TrayView found in scene.");
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Spawn Game Systems")]
    public static void SpawnViaGameManager()
    {
        // Just call the shared static method
        SpawnGameSystems();
    }
#endif
}

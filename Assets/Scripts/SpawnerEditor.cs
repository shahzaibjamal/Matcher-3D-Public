#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;

public partial class Spawner
{
    [MenuItem("Tools/Spawn Game Systems")]
    private static void SpawnViaGameManager()
    {

        // Find and delete any Spawner in the scene
        var spawner = Object.FindObjectOfType<Spawner>();
        if (spawner != null)
        {
            Object.DestroyImmediate(spawner.gameObject);
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
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public partial class Spawner
{

    [MenuItem("Tools/Spawn")]
    private static void SpawnViaSpawner()
    {
        // Find all Spawner components in the scene
        foreach (var spawner in Object.FindObjectsOfType<Spawner>())
        {
            spawner.SpawnLevel();
        }
        Debug.Log("Spawner spawn complete.");
    }
}
#endif

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Collections.Generic;

public class PrefabManager : MonoBehaviour
{
    public static PrefabManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persist across scenes
    }

    // -------------------------
    // SINGLE PREFAB INSTANTIATION
    // -------------------------

    /// <summary>
    /// Instantiate prefab by key at default position/rotation.
    /// </summary>
    public void InstantiatePrefab(string key, Action<GameObject> onComplete = null)
    {
        Addressables.InstantiateAsync(key).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"Failed to instantiate prefab: {key}");
                onComplete?.Invoke(null);
            }
        };
    }

    /// <summary>
    /// Instantiate prefab by key at given position/rotation.
    /// </summary>
    public void InstantiatePrefab(string key, Vector3 position, Quaternion rotation, Transform parent = null, Action<GameObject> onComplete = null)
    {
        Addressables.InstantiateAsync(key, position, rotation, parent).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"Failed to instantiate prefab: {key}");
                onComplete?.Invoke(null);
            }
        };
    }

    /// <summary>
    /// Release an instantiated prefab.
    /// </summary>
    public void ReleasePrefab(GameObject instance)
    {
        if (instance != null)
        {
            Addressables.ReleaseInstance(instance);
        }
    }

    // -------------------------
    // MULTIPLE PREFAB LOADING
    // -------------------------

    /// <summary>
    /// Preload a list of prefab assets by keys, then invoke callback when all are loaded.
    /// </summary>
    public void PreloadPrefabs(List<string> keys, Action<Dictionary<string, GameObject>> onComplete)
    {
        Dictionary<string, GameObject> loadedPrefabs = new Dictionary<string, GameObject>();
        int total = keys.Count;
        int completed = 0;

        foreach (var key in keys)
        {
            Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
            {
                completed++;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    loadedPrefabs[key] = handle.Result;
                }
                else
                {
                    Debug.LogError($"Failed to load prefab asset: {key}");
                    loadedPrefabs[key] = null;
                }

                if (completed == total)
                {
                    onComplete?.Invoke(loadedPrefabs);
                }
            };
        }
    }

    /// <summary>
    /// Instantiate a list of prefabs by keys at default positions.
    /// </summary>
    public void InstantiatePrefabs(List<string> keys, Action<List<GameObject>> onComplete)
    {
        List<GameObject> instances = new List<GameObject>();
        int total = keys.Count;
        int completed = 0;

        foreach (var key in keys)
        {
            Addressables.InstantiateAsync(key).Completed += handle =>
            {
                completed++;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    instances.Add(handle.Result);
                }
                else
                {
                    Debug.LogError($"Failed to instantiate prefab: {key}");
                }

                if (completed == total)
                {
                    onComplete?.Invoke(instances);
                }
            };
        }
    }

    /// <summary>
    /// Instantiate a list of prefabs by keys with positions/rotations.
    /// </summary>
    public void InstantiatePrefabs(List<(string key, Vector3 pos, Quaternion rot)> requests, Action<List<GameObject>> onComplete)
    {
        List<GameObject> instances = new List<GameObject>();
        int total = requests.Count;
        int completed = 0;

        foreach (var req in requests)
        {
            Addressables.InstantiateAsync(req.key, req.pos, req.rot).Completed += handle =>
            {
                completed++;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    instances.Add(handle.Result);
                }
                else
                {
                    Debug.LogError($"Failed to instantiate prefab: {req.key}");
                }

                if (completed == total)
                {
                    onComplete?.Invoke(instances);
                }
            };
        }
    }
}

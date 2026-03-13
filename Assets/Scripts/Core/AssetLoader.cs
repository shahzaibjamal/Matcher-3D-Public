using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Collections.Generic;

public class AssetLoader : MonoBehaviour
{
    public static AssetLoader Instance { get; private set; }

    // Internal cache for sprites to allow immediate sync access after first load
    private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------
    // ICON / SPRITE LOADING
    // -------------------------

    /// <summary>
    /// Loads a sprite by its Addressable key (e.g., from your Google Sheet iconName).
    /// </summary>
    public void LoadIcon(string key, Action<Sprite> onComplete)
    {
        if (_spriteCache.TryGetValue(key, out Sprite cached))
        {
            onComplete?.Invoke(cached);
            return;
        }

        Addressables.LoadAssetAsync<Sprite>(key).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (!_spriteCache.ContainsKey(key))
                    _spriteCache.Add(key, handle.Result);

                onComplete?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"AssetLoader: Failed to load icon with key: {key}");
                onComplete?.Invoke(null);
            }
        };
    }

    // -------------------------
    // PREFAB INSTANTIATION
    // -------------------------

    public void InstantiatePrefab(string key, Action<GameObject> onComplete = null)
    {
        Addressables.InstantiateAsync(key).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                onComplete?.Invoke(handle.Result);
            else
                onComplete?.Invoke(null);
        };
    }

    public void InstantiatePrefab(string key, Vector3 pos, Quaternion rot, Transform parent = null, Action<GameObject> onComplete = null)
    {
        Addressables.InstantiateAsync(key, pos, rot, parent).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                onComplete?.Invoke(handle.Result);
            else
                onComplete?.Invoke(null);
        };
    }

    /// <summary>
    /// Important: Always use this to destroy objects created via Addressables 
    /// to ensure memory is freed correctly.
    /// </summary>
    public void ReleaseInstance(GameObject instance)
    {
        if (instance != null)
            Addressables.ReleaseInstance(instance);
    }

    // -------------------------
    // BULK LOADING
    // -------------------------

    /// <summary>
    /// Preloads multiple icons into the cache. 
    /// Use this during a loading screen to prevent hitches.
    /// </summary>
    public void PreloadIcons(List<string> keys, Action onAllComplete = null)
    {
        int total = keys.Count;
        int completed = 0;

        foreach (var key in keys)
        {
            LoadIcon(key, _ =>
            {
                completed++;
                if (completed == total) onAllComplete?.Invoke();
            });
        }
    }

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
                    loadedPrefabs[key] = handle.Result;

                if (completed == total)
                    onComplete?.Invoke(loadedPrefabs);
            };
        }
    }
    // -------------------------
    // RELEASING ICONS
    // -------------------------

    /// <summary>
    /// Releases a specific sprite from memory and removes it from the cache.
    /// </summary>
    public void ReleaseIcon(string key)
    {
        if (_spriteCache.TryGetValue(key, out Sprite sprite))
        {
            // 1. Tell Addressables to release the memory
            Addressables.Release(sprite);

            // 2. Remove from our internal dictionary
            _spriteCache.Remove(key);

            Debug.Log($"AssetLoader: Released and uncached icon: {key}");
        }
    }

    /// <summary>
    /// Clears the entire sprite cache. Use this when switching major scenes
    /// or when memory usage is high.
    /// </summary>
    public void ClearSpriteCache()
    {
        foreach (var sprite in _spriteCache.Values)
        {
            if (sprite != null)
                Addressables.Release(sprite);
        }

        _spriteCache.Clear();
        Debug.Log("AssetLoader: Sprite cache cleared completely.");
    }
}
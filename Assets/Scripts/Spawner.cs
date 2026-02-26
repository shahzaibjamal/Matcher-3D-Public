using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Spawner : MonoBehaviour
{
    [Header("References")]
    public Transform Parent;
    public Camera MainCamera => Camera.main;

    [Header("Debug Settings")]
    public bool isDebug = false;
    public Material debugMaterial; // Assign a semi-transparent material

    [Header("Spawn Area Settings")]
    [Range(0.1f, 0.9f)] public float WidthPercent = 0.75f;
    [Range(0.1f, 0.9f)] public float HeightPercent = 0.5f;
    public float WallHeight = 2.0f; // How tall the invisible walls are
    public float VerticalOffset = 0.1f; // Ground level

    private float _spawnXMax;
    private float _spawnZMax;
    private List<ClickableItem> _itemClickables = new();
    private Action<ItemData, Transform> _onItemClicked;
    private LevelData _currentLevelData;

    private Dictionary<String, int> _collectableLeft;
    private int _initialTotalItems;
    private int _initialCollectableItems;

    // For efficiency tracking
    private float _levelStartTime;
    public void GenerateBounds()
    {
        // 1. Calculate world bounds based on camera
        float distanceToCamera = Math.Abs(MainCamera.transform.position.y - Parent.position.y);
        float frustumHeight = 2.0f * distanceToCamera * Mathf.Tan(MainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * MainCamera.aspect;

        _spawnXMax = (frustumWidth * WidthPercent) / 2f;
        _spawnZMax = (frustumHeight * HeightPercent) / 2f;

        // 2. Clear old walls if they exist
        Transform oldContainer = Parent.Find("LevelContainer");
        if (oldContainer != null) DestroyImmediate(oldContainer.gameObject);

        // 3. Create Container Parent
        GameObject container = new GameObject("LevelContainer");
        container.transform.SetParent(Parent);
        container.transform.localPosition = Vector3.zero;

        // 4. Spawn 4 Walls
        SpawnWall("Wall_Left", new Vector3(-_spawnXMax, WallHeight / 2, 0), new Vector3(0.1f, WallHeight, _spawnZMax * 2), container.transform);
        SpawnWall("Wall_Right", new Vector3(_spawnXMax, WallHeight / 2, 0), new Vector3(0.1f, WallHeight, _spawnZMax * 2), container.transform);
        SpawnWall("Wall_Top", new Vector3(0, WallHeight / 2, _spawnZMax), new Vector3(_spawnXMax * 2, WallHeight, 0.1f), container.transform);
        SpawnWall("Wall_Bottom", new Vector3(0, WallHeight / 2, -_spawnZMax), new Vector3(_spawnXMax * 2, WallHeight, 0.1f), container.transform);
    }

    private void SpawnWall(string wallName, Vector3 localPos, Vector3 size, Transform parent)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPos;

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;

        if (isDebug)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "DebugVisual";
            visual.transform.SetParent(wall.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = size;

            if (debugMaterial != null)
                visual.GetComponent<Renderer>().material = debugMaterial;

            // Remove collider from the visual child so it doesn't double-up
            DestroyImmediate(visual.GetComponent<BoxCollider>());
        }
    }

    public void SpawnLevel(string levelUID, Action<ItemData, Transform> onItemClicked)
    {
        _onItemClicked = onItemClicked;

        _itemClickables.Clear();
        _levelStartTime = Time.time; // Start the clock

        // Always ensure bounds match the current resolution before spawning
        GenerateBounds();

        _currentLevelData = Metadata.Instance.levelDatabase.GetLevelByUID(levelUID);
        if (_currentLevelData == null) return;
        GameEvents.OnMatchStartedEvent?.Invoke(_currentLevelData);

        // For debugging
        PopulateCollectableLeft(_currentLevelData);

        foreach (var entry in _currentLevelData.itemsToSpawn)
        {
            ItemData item = Metadata.Instance.itemDatabase.GetItemByUID(entry.itemUID);
            for (int i = 0; i < entry.count; i++)
            {
                float x = UnityEngine.Random.Range(-_spawnXMax + 0.5f, _spawnXMax - 0.5f);
                float z = UnityEngine.Random.Range(-_spawnZMax + 0.5f, _spawnZMax - 0.5f);

                // FIX 1: Randomize Height (Y) so they aren't all in one layer
                float randomHeight = VerticalOffset + UnityEngine.Random.Range(0f, 5.0f);

                // FIX 2: Randomize Rotation so they don't land perfectly flat and stack weirdly
                Quaternion randomRot = UnityEngine.Random.rotation;

                Vector3 spawnPos = Parent.position + new Vector3(x, randomHeight, z);
                GameObject go = Instantiate(item.Prefab, spawnPos, randomRot, Parent);
                // Setup Clickable logic...
                ClickableItem clickable = go.GetComponent<ClickableItem>();
                if (clickable != null)
                {
                    _itemClickables.Add(clickable);
                    clickable.ItemData = item;
                    clickable.OnItemClicked = onItemClicked;
                    clickable.OnItemClicked += HandleInternalItemClicked;
                }
            }
        }

    }

    private void HandleInternalItemClicked(ItemData data, Transform t)
    {
        // Remove based on the transform/instance rather than searching by UID
        _itemClickables.RemoveAll(c => c == null || c.transform == t);

        // Update the dictionary for items we are tracking
        if (_collectableLeft.ContainsKey(data.UID))
        {
            _collectableLeft[data.UID]--;
            if (_collectableLeft[data.UID] <= 0)
                _collectableLeft.Remove(data.UID);
        }

        UpdateCount();
    }

    private void UpdateCount()
    {
        int collectableRemaining = _itemClickables.Count(c =>
            _currentLevelData.itemsToCollect.Contains(c.ItemData.UID));

        int totalRemaining = _itemClickables.Count;

        float progress = 1f - ((float)collectableRemaining / _initialCollectableItems);

        Debug.Log($"Progress: {progress * 100}% | Total Items in Scene: {totalRemaining}");

        if (collectableRemaining == 0)
        {
            float finalTime = Time.time - _levelStartTime;
            Debug.Log($"Level Cleared in {finalTime:F2} seconds!");
            // Trigger your GoldRewardUI here
        }
    }
    public void GetLevelProgress(out float totalProgress, out float collectableProgress, out float efficiencyScore)
    {
        // Total items remaining in the scene
        int currentTotalInScene = _itemClickables.Count;

        // Items remaining that are on the "Target" list
        int currentCollectablesInScene = _itemClickables.Count(c =>
            _currentLevelData.itemsToCollect.Contains(c.ItemData.UID));

        // Progress as 0.0 to 1.0
        totalProgress = 1f - ((float)currentTotalInScene / _initialTotalItems);
        collectableProgress = 1f - ((float)currentCollectablesInScene / _initialCollectableItems);

        // Efficiency: (Items Collected / Total Clicks or Time)
        // Here we use Time as a simple example
        float timeTaken = Time.time - _levelStartTime;
        efficiencyScore = collectableProgress / (timeTaken / 60f); // Progress per minute
    }
    // For debugging
    private void PopulateCollectableLeft(LevelData levelData)
    {
        _collectableLeft = new Dictionary<string, int>();
        foreach (var item in levelData.itemsToCollect)
        {
            int count = levelData.itemsToSpawn.Find(itemData => itemData.itemUID == item).count;
            _collectableLeft.Add(item, count);
        }
    }
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (_itemClickables == null || _itemClickables.Count == 0)
            {
                Debug.Log("No items left to click.");
                return;
            }
            _itemClickables.RemoveAll(i => i == null || i.ItemData == null);

            int index = UnityEngine.Random.Range(0, _itemClickables.Count);
            var item = _itemClickables[index];

            if (item != null && item.ItemData != null)
            {
                _onItemClicked?.Invoke(item.ItemData, item.transform);
            }
            else
            {
                Debug.LogWarning("Item was null or destroyed.");
            }

            _itemClickables.RemoveAt(index);
        }
        if (Input.GetKeyUp(KeyCode.M))
        {
            // 1. Guard Clauses (Early exits to avoid nesting)
            if (_currentLevelData == null || _itemClickables == null || _itemClickables.Count == 0) return;
            if (_collectableLeft.Count == 0) return;

            // 2. Optimization: Get the first key without LINQ ElementAt (which is slow)
            var firstKey = string.Empty;
            foreach (var key in _collectableLeft.Keys) { firstKey = key; break; }

            // 3. Find the item
            ClickableItem clickableItem = _itemClickables.Find(c => c != null && c.ItemData != null && c.ItemData.UID == firstKey);

            if (clickableItem != null)
            {
                // Execute click logic
                _onItemClicked?.Invoke(clickableItem.ItemData, clickableItem.transform);

                // 4. Update Dictionary Safely
                _collectableLeft[firstKey]--;
                if (_collectableLeft[firstKey] <= 0)
                {
                    _collectableLeft.Remove(firstKey);
                }
            }
            else
            {
                Debug.LogWarning($"Item with UID {firstKey} not found in clickables list.");
            }
        }
    }

}
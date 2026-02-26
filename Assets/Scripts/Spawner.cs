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
    public Material debugMaterial;

    [Header("Spawn Area Settings")]
    [Range(0.1f, 0.9f)] public float WidthPercent = 0.75f;
    [Range(0.1f, 0.9f)] public float HeightPercent = 0.5f;
    public float WallHeight = 2.0f;
    public float VerticalOffset = 0.1f;

    // --- Public State ---
    public int InitialTotalItems { get; private set; }
    public int InitialCollectableItems { get; private set; }
    public int CurrentTotalRemaining => _itemClickables.Count;
    public int CurrentCollectablesRemaining => _itemClickables.Count(c =>
        _currentLevelData.itemsToCollect.Contains(c.ItemData.UID));

    public float LevelProgress => InitialCollectableItems > 0
        ? 1f - ((float)CurrentCollectablesRemaining / InitialCollectableItems)
        : 0f;

    // --- Private State ---
    private float _spawnXMax;
    private float _spawnZMax;
    private List<ClickableItem> _itemClickables = new();
    private Action<ItemData, Transform> _onItemClicked;
    private LevelData _currentLevelData;
    private Dictionary<string, int> _collectableLeft;

    #region Level Lifecycle
    void Awake()
    {
        Physics.gravity = new Vector3(0, -4.0f, 0);
    }
    public void SpawnLevel(string levelUID, Action<ItemData, Transform> onItemClicked)
    {
        _onItemClicked = onItemClicked;

        // 1. Prepare Environment
        GenerateBounds();

        // 2. Prepare Data
        _currentLevelData = Metadata.Instance.levelDatabase.GetLevelByUID(levelUID);
        if (_currentLevelData == null) return;

        InitializeLevelStats();

        // 3. Physical Spawn
        ExecuteSpawning();

        GameEvents.OnMatchStartedEvent?.Invoke(_currentLevelData);
    }

    private void InitializeLevelStats()
    {
        _itemClickables.Clear();

        // Capture initial totals for progress tracking
        InitialTotalItems = _currentLevelData.itemsToSpawn.Sum(x => x.count);
        InitialCollectableItems = _currentLevelData.itemsToSpawn
                .Where(entry => _currentLevelData.itemsToCollect.Contains(entry.itemUID))
                .Sum(entry => entry.count);
        PopulateCollectableLeft(_currentLevelData);
    }

    private void ExecuteSpawning()
    {
        foreach (var entry in _currentLevelData.itemsToSpawn)
        {
            ItemData item = Metadata.Instance.itemDatabase.GetItemByUID(entry.itemUID);
            for (int i = 0; i < entry.count; i++)
            {
                CreateItemInstance(item);
            }
        }
    }

    private void CreateItemInstance(ItemData item)
    {
        Vector3 spawnPos = CalculateRandomSpawnPos();
        Quaternion randomRot = UnityEngine.Random.rotation;

        GameObject go = Instantiate(item.Prefab, spawnPos, randomRot, Parent);

        if (go.TryGetComponent<ClickableItem>(out var clickable))
        {
            _itemClickables.Add(clickable);
            clickable.ItemData = item;
            clickable.OnItemClicked = _onItemClicked;
            clickable.OnItemClicked += HandleInternalItemClicked;
        }
    }

    #endregion

    #region Internal Logic

    private void HandleInternalItemClicked(ItemData data, Transform t)
    {
        _itemClickables.RemoveAll(c => c == null || c.transform == t);
        if (_collectableLeft.ContainsKey(data.UID))
        {
            _collectableLeft[data.UID]--;
            if (_collectableLeft[data.UID] <= 0)
                _collectableLeft.Remove(data.UID);
        }
    }

    private Vector3 CalculateRandomSpawnPos()
    {
        float x = UnityEngine.Random.Range(-_spawnXMax + 0.5f, _spawnXMax - 0.5f);
        float z = UnityEngine.Random.Range(-_spawnZMax + 0.5f, _spawnZMax - 0.5f);
        float y = VerticalOffset + UnityEngine.Random.Range(0f, 5.0f);

        return Parent.position + new Vector3(x, y, z);
    }

    #endregion

    #region Bounds & Walls

    public void GenerateBounds()
    {
        float distanceToCamera = Math.Abs(MainCamera.transform.position.y - Parent.position.y);
        float frustumHeight = 2.0f * distanceToCamera * Mathf.Tan(MainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * MainCamera.aspect;

        _spawnXMax = (frustumWidth * WidthPercent) / 2f;
        _spawnZMax = (frustumHeight * HeightPercent) / 2f;

        RefreshContainer();
    }

    private void RefreshContainer()
    {
        Transform oldContainer = Parent.Find("LevelContainer");
        if (oldContainer != null) DestroyImmediate(oldContainer.gameObject);

        GameObject container = new GameObject("LevelContainer");
        container.transform.SetParent(Parent);
        container.transform.localPosition = Vector3.zero;

        // Spawn Walls
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

        if (isDebug) CreateDebugVisual(wall.transform, size);
    }

    private void CreateDebugVisual(Transform wall, Vector3 size)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "DebugVisual";
        visual.transform.SetParent(wall);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = size;

        if (debugMaterial != null)
            visual.GetComponent<Renderer>().material = debugMaterial;

        DestroyImmediate(visual.GetComponent<BoxCollider>());
    }

    #endregion

    #region Helpers & Debug Keys

    private void PopulateCollectableLeft(LevelData levelData)
    {
        _collectableLeft = new Dictionary<string, int>();
        foreach (var itemUID in levelData.itemsToCollect)
        {
            var spawnEntry = levelData.itemsToSpawn.Find(e => e.itemUID == itemUID);
            if (spawnEntry != null)
                _collectableLeft.Add(itemUID, spawnEntry.count);
        }
    }

    public void ShakeLevel()
    {
        foreach (var item in _itemClickables)
        {
            if (item == null) continue;

            if (item.TryGetComponent<Rigidbody>(out var rb))
            {
                // Apply a strong upward and random horizontal burst
                Vector3 force = new Vector3(
                    UnityEngine.Random.Range(-5f, 5f),
                    UnityEngine.Random.Range(5f, 10f),
                    UnityEngine.Random.Range(-5f, 5f)
                );
                rb.AddForce(force, ForceMode.Impulse);

                // Add a random torque to make them tumble
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }
        }
    }

    public void ReturnItemToField(GameObject itemGo, ItemData data)
    {
        if (itemGo.TryGetComponent<ClickableItem>(out var clickable))
        {
            // 1. Re-add to our tracking list
            if (!_itemClickables.Contains(clickable))
                _itemClickables.Add(clickable);

            // 2. Increment the dictionary count back up
            if (_collectableLeft.ContainsKey(data.UID))
                _collectableLeft[data.UID]++;
            else
                _collectableLeft.Add(data.UID, 1);

            // 3. Physics & Visuals
            itemGo.SetActive(true);
            if (itemGo.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false; // Turn physics back on
                                        // Optional: Launch it from the tray back into the center
                Vector3 launchDir = (Parent.position - itemGo.transform.position).normalized + Vector3.up;
                rb.AddForce(launchDir * 5f, ForceMode.Impulse);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space)) Debug_ClickRandom();
        if (Input.GetKeyUp(KeyCode.M)) Debug_ClickTarget();
    }

    private void Debug_ClickRandom()
    {
        _itemClickables.RemoveAll(i => i == null);
        if (_itemClickables.Count == 0) return;

        int index = UnityEngine.Random.Range(0, _itemClickables.Count);
        var item = _itemClickables[index];
        _onItemClicked?.Invoke(item.ItemData, item.transform);
    }

    private void Debug_ClickTarget()
    {
        // 1. Safety Checks
        if (_collectableLeft == null || _collectableLeft.Count == 0) return;

        // 2. Find the target
        string targetUID = _collectableLeft.Keys.First();
        var targetItem = _itemClickables.Find(c => c != null && c.ItemData.UID == targetUID);

        if (targetItem != null)
        {
            // 3. Fire the external event (e.g., for the UI/GameManager)
            _onItemClicked?.Invoke(targetItem.ItemData, targetItem.transform);

            // 4. MUST call the internal handler to update the Dictionary and List
            HandleInternalItemClicked(targetItem.ItemData, targetItem.transform);
        }
    }
    #endregion
}
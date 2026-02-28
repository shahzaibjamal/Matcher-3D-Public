using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
    private Stack<ItemData> _undoHistory = new();

    private void OnEnable()
    {
        GameEvents.OnUndoPowerupEvent += HandleUndoRequest;
        GameEvents.OnShakePowerupEvent += ShakeArea;
        // We listen to this to clear history of items that are officially matched/destroyed
        GameEvents.OnRequestMatchResolveEvent += HandleMatchResolved;
        GameEvents.OnHintPowerupEvent += HandleHintPowerUp;
        GameEvents.OnMagnetPowerupEvent += HandleMagnetPowerUp;
        GameEvents.OnCleanSweepTrayEvent += HandleCleanSweep;

    }

    private void OnDisable()
    {
        GameEvents.OnUndoPowerupEvent -= HandleUndoRequest;
        GameEvents.OnShakePowerupEvent -= ShakeArea;
        GameEvents.OnHintPowerupEvent -= HandleHintPowerUp;
        GameEvents.OnMagnetPowerupEvent -= HandleMagnetPowerUp;
        GameEvents.OnRequestMatchResolveEvent -= HandleMatchResolved;
        GameEvents.OnCleanSweepTrayEvent -= HandleCleanSweep;
    }


    private void HandleCleanSweep()
    {
        if (_undoHistory.Count == 0) return;

        GameEvents.OnUndoPowerupEvent?.Invoke();
        // Schedule the NEXT undo only after this one is done
        DOVirtual.DelayedCall(0.4f, () => HandleCleanSweep());
    }

    void Awake()
    {
        Physics.gravity = new Vector3(0, -4.0f, 0);
    }

    #region Level Lifecycle
    public void SpawnLevel(LevelData levelData, Action<ItemData, Transform> onItemClicked)
    {
        _onItemClicked = onItemClicked;

        // 1. Prepare Environment
        GenerateBounds();

        // 2. Prepare Data
        _currentLevelData = levelData;
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
        // Record the data before the object is destroyed by your Tray logic
        _undoHistory.Push(data);

        _itemClickables.RemoveAll(c => c == null || c.transform == t);

        if (_collectableLeft.ContainsKey(data.UID))
        {
            _collectableLeft[data.UID]--;
            if (_collectableLeft[data.UID] <= 0)
                _collectableLeft.Remove(data.UID);
        }
    }
    private void HandleUndoRequest()
    {
        if (_undoHistory.Count == 0) return;

        ItemData dataToRestore = _undoHistory.Pop();

        // 1. Re-increment Dictionary
        if (_collectableLeft.ContainsKey(dataToRestore.UID))
            _collectableLeft[dataToRestore.UID]++;
        else
            _collectableLeft.Add(dataToRestore.UID, 1);

        // 2. Specialized Spawn Logic
        // We assume 'trayPosition' is where the item was just removed from in the UI
        // For now, we'll use a position slightly above the center of the tray
        Vector3 trayWorldPos = MainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, 100, 5));
        GameEvents.OnUndoAddItemEvent?.Invoke(dataToRestore.UID);
        SpawnFromTray(dataToRestore, trayWorldPos);
        GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Undo);
    }

    private void SpawnFromTray(ItemData item, Vector3 startPos)
    {
        // 1. Instantiate at the Tray's location
        GameObject go = Instantiate(item.Prefab, startPos, UnityEngine.Random.rotation, Parent);

        // 2. Setup Clickable Logic
        if (go.TryGetComponent<ClickableItem>(out var clickable))
        {
            _itemClickables.Add(clickable);
            clickable.ItemData = item;
            clickable.OnItemClicked = _onItemClicked;
            clickable.OnItemClicked += HandleInternalItemClicked;

            // 3. The "Throw" Physics
            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;

                // Calculate direction to the center of the spawn area
                Vector3 targetCenter = Parent.position + new Vector3(0, 1f, 0);
                Vector3 throwDirection = (targetCenter - startPos).normalized;

                // Apply an arc force (Forward + Up)
                // rb.AddForce((throwDirection + Vector3.up) * 12f, ForceMode.Impulse);
                // rb.AddTorque(UnityEngine.Random.insideUnitSphere * 20f, ForceMode.Impulse);
                go.transform.DOJump(CalculateRandomSpawnPos(), 0.5f, 1, 0.5f).OnComplete(() =>
                {
                    if (go.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
                });
            }
        }
    }
    private void HandleMatchResolved(int firstItemIndex, ItemData[] items, Action onComplete)
    {
        // In a stack, we can't easily remove items from the middle.
        // However, since a match usually happens with the most recent clicks:
        // We remove the last 3 items from the Undo History.

        int itemsInMatch = 3;
        for (int i = 0; i < itemsInMatch; i++)
        {
            if (_undoHistory.Count > 0)
            {
                _undoHistory.Pop();
            }
        }
    }


    private void HandleHintPowerUp()
    {
        string targetUID = null;

        // 1. Calculate frequencies in tray for logic
        Dictionary<string, int> historyCounts = new Dictionary<string, int>();
        foreach (var item in _undoHistory)
        {
            if (historyCounts.ContainsKey(item.UID)) historyCounts[item.UID]++;
            else historyCounts[item.UID] = 1;
        }

        // 2. PRIORITY 1: Check Collectables that are already in UndoHistory (Goals in progress)
        int highestCount = 0;
        foreach (var goalUID in _collectableLeft.Keys)
        {
            if (historyCounts.TryGetValue(goalUID, out int countInTray))
            {
                // We only care if it's not already a completed set in the tray (count % 3 != 0)
                if (countInTray % 3 != 0 && countInTray > highestCount)
                {
                    highestCount = countInTray;
                    targetUID = goalUID;
                }
            }
        }

        // 3. PRIORITY 2: If no "Goal in Progress" found, check if ANY item in history is a candidate
        if (string.IsNullOrEmpty(targetUID) && _undoHistory.Count > 0)
        {
            // Pick the most recent item from history that isn't already finished
            foreach (var item in _undoHistory)
            {
                if (historyCounts[item.UID] % 3 != 0)
                {
                    targetUID = item.UID;
                    break;
                }
            }
        }

        // 4. PRIORITY 3: If history is empty, just hint the first goal
        if (string.IsNullOrEmpty(targetUID) && _collectableLeft.Count > 0)
        {
            targetUID = _collectableLeft.Keys.First();
        }

        // 5. Execution
        if (!string.IsNullOrEmpty(targetUID))
        {
            HighlightItemsInField(targetUID);
            GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Hint);
        }
    }

    private void HighlightItemsInField(string uid)
    {
        int count = 0;
        float delay = 0.1f;
        foreach (var item in _itemClickables)
        {
            if (item != null && item.ItemData.UID == uid)
            {
                float calculatedDelay = delay * count++;
                Scheduler.Instance.ExecuteAfterDelay(calculatedDelay, () =>
                {
                    item.Highlight(true);
                    // Auto-stop after 3 seconds. all at once
                    DOVirtual.DelayedCall(3f - calculatedDelay, () => item.Highlight(false));
                });
            }
        }
    }

    private void HandleMagnetPowerUp()
    {
        float delay = 0.4f;
        // 1. Priority: Check items in _collectableLeft and see if they are already in _undoHistory
        foreach (var key in _collectableLeft.Keys.ToList())
        {
            // If this item type is already partially collected (in the tray/undo history)
            int currentlyInTray = _undoHistory.Count(i => i.UID == key);

            // If it's 1 or 2, we have a partial match that needs fulfilling
            if (currentlyInTray > 0)
            {
                int neededToMatch = 3 - (currentlyInTray % 3);

                for (int i = 0; i < neededToMatch; i++)
                {
                    Scheduler.Instance.ExecuteAfterDelay(delay * i, () => TrySelectSpecificItem(key));
                }
                GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Magnet);

                // Once we fulfill the priority match, we consider the magnet's main job done for this trigger
                return;
            }
        }

        // 2. Fallback: If no partial matches found, grab the first available items from the collection list 3 times
        if (_collectableLeft == null || _collectableLeft.Count == 0)
        {
            return;
        }

        string firstUid = _collectableLeft.Keys.First();
        for (int i = 0; i < 3; i++)
        {
            Scheduler.Instance.ExecuteAfterDelay(delay * i, () => TrySelectSpecificItem(firstUid));
        }

        GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Magnet);
    }
    private bool TrySelectSpecificItem(string uid)
    {
        var targetItem = _itemClickables.Find(c => c != null && c.ItemData.UID == uid);
        if (targetItem != null)
        {
            ProcessItemSelection(targetItem);
            return true;
        }
        return false;
    }

    private void ProcessItemSelection(ClickableItem item)
    {
        if (item == null) return;

        // Fire external event for UI/GameManager
        _onItemClicked?.Invoke(item.ItemData, item.transform);

        // Call internal handler to update Dictionaries/Lists
        HandleInternalItemClicked(item.ItemData, item.transform);
    }

    private Vector3 CalculateRandomSpawnPos()
    {
        float x = UnityEngine.Random.Range(-_spawnXMax + 0.5f, _spawnXMax - 0.5f);
        float z = UnityEngine.Random.Range(-_spawnZMax + 0.5f, _spawnZMax - 0.5f);
        float y = VerticalOffset + UnityEngine.Random.Range(0f, 1.0f);

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

    public void ShakeArea()
    {
        if (_itemClickables.Count == 0) return;

        foreach (var item in _itemClickables)
        {
            if (item == null) continue;

            if (item.TryGetComponent<Rigidbody>(out var rb))
            {
                // Apply a burst: Random horizontal + strong upward
                Vector3 force = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                );

                rb.AddForce(force, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 5, ForceMode.Impulse);
            }
        }
        GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Shake);

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
        TrySelectSpecificItem(targetUID);
    }

    #endregion
}
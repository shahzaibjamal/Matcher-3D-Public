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
        _currentLevelData.ItemsToCollect.Contains(c.ItemData.Id));

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
        GameEvents.OnUndoPowerupEvent += HandleUndoPowerUp;
        GameEvents.OnShakePowerupEvent += ShakeArea;
        // We listen to this to clear history of items that are officially matched/destroyed
        GameEvents.OnRequestMatchResolveEvent += HandleMatchResolved;
        GameEvents.OnHintPowerupEvent += HandleHintPowerUp;
        GameEvents.OnMagnetPowerupEvent += HandleMagnetPowerUp;
        GameEvents.OnCleanSweepTrayEvent += HandleCleanSweep;

    }

    private void OnDisable()
    {
        GameEvents.OnUndoPowerupEvent -= HandleUndoPowerUp;
        GameEvents.OnShakePowerupEvent -= ShakeArea;
        GameEvents.OnHintPowerupEvent -= HandleHintPowerUp;
        GameEvents.OnMagnetPowerupEvent -= HandleMagnetPowerUp;
        GameEvents.OnRequestMatchResolveEvent -= HandleMatchResolved;
        GameEvents.OnCleanSweepTrayEvent -= HandleCleanSweep;
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

        if (levelData != null)
        {
            Debug.LogError(levelData.Name);
            if (levelData.ItemsToSpawn.Count > 0)
            {
                Debug.LogError(levelData.ItemsToSpawn[0].Id);

            }

        }
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
        InitialTotalItems = _currentLevelData.ItemsToSpawn.Sum(x => x.Count);
        InitialCollectableItems = _currentLevelData.ItemsToSpawn
                .Where(entry => _currentLevelData.ItemsToCollect.Contains(entry.Id))
                .Sum(entry => entry.Count);
        PopulateCollectableLeft(_currentLevelData);
    }

    private void ExecuteSpawning()
    {
        foreach (var entry in _currentLevelData.ItemsToSpawn)
        {
            ItemData item = DataManager.Instance.GetItemByID(entry.Id);
            for (int i = 0; i < entry.Count; i++)
            {
                CreateItemInstance(item);
            }
        }
    }

    private void CreateItemInstance(ItemData item)
    {
        Vector3 spawnPos = CalculateRandomSpawnPos();
        Quaternion randomRot = UnityEngine.Random.rotation;

        AssetLoader.Instance.InstantiatePrefab(item.PrefabName, spawnPos, randomRot, Parent, (go) =>
        {
            if (go.TryGetComponent<ClickableItem>(out var clickable))
            {
                _itemClickables.Add(clickable);
                clickable.ItemData = item;
                clickable.OnItemClicked = _onItemClicked;
                clickable.OnItemClicked += HandleInternalItemClicked;
            }
        });
    }

    #endregion

    #region Internal Logic

    private void HandleInternalItemClicked(ItemData data, Transform t)
    {
        // Record the data before the object is destroyed by your Tray logic
        _undoHistory.Push(data);

        _itemClickables.RemoveAll(c => c == null || c.transform == t);

        if (_collectableLeft.ContainsKey(data.Id))
        {
            _collectableLeft[data.Id]--;
            if (_collectableLeft[data.Id] <= 0)
                _collectableLeft.Remove(data.Id);
        }
    }

    // 1. CALL THIS FOR THE BUTTON/POWERUP

    private void HandleCleanSweep()
    {
        if (_undoHistory.Count == 0) return;

        GameEvents.OnUndoPowerupEvent?.Invoke(false);
        // Schedule the NEXT undo only after this one is done
        DOVirtual.DelayedCall(0.4f, () => HandleCleanSweep());
    }

    private void HandleUndoPowerUp(bool powerUpUsed)
    {
        if (_undoHistory.Count == 0) return;

        RestoreAndSpawnItem();
        if (powerUpUsed)
        {
            // Only triggered when used as a manual PowerUp
            GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Undo);
        }
    }

    // 2. CALL THIS FOR THE SWEEP (OR INSIDE THE POWERUP ABOVE)
    private void RestoreAndSpawnItem()
    {
        if (_undoHistory.Count == 0) return;

        // Pop and Update Dictionary
        ItemData dataToRestore = _undoHistory.Pop();

        if (_collectableLeft.ContainsKey(dataToRestore.Id))
            _collectableLeft[dataToRestore.Id]++;
        else
            _collectableLeft.Add(dataToRestore.Id, 1);

        // Calculate Screen X (0 to 6 based on current stack count)
        // We use the count BEFORE we popped, or the index in the 7-slot tray
        float segment = Screen.width / (GameManager.SLOT_COUNT + 1);
        float screenX = (_undoHistory.Count + 1) * segment;

        // Position and Spawn
        Vector3 trayWorldPos = MainCamera.ScreenToWorldPoint(new Vector3(screenX, 100, 5));

        GameEvents.OnUndoAddItemEvent?.Invoke(dataToRestore.Id);
        SpawnFromTray(dataToRestore, trayWorldPos);
    }
    // private ItemData RestoreUndoState()
    // {
    //     ItemData data = _undoHistory.Pop();

    //     if (_collectableLeft.ContainsKey(data.ID))
    //         _collectableLeft[data.ID]++;
    //     else
    //         _collectableLeft.Add(data.ID, 1);

    //     return data;
    // }

    // private void TriggerUndoSuccess()
    // {
    //     GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Undo);
    // }

    private void SpawnFromTray(ItemData item, Vector3 startPos)
    {
        // 1. Instantiate at the Tray's location
        // GameObject go = Instantiate(item.Prefab, startPos, UnityEngine.Random.rotation, Parent);

        AssetLoader.Instance.InstantiatePrefab(item.PrefabName, startPos, UnityEngine.Random.rotation, Parent, (go) =>
        {
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
        });
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
        string targetID = null;

        // 1. Calculate frequencies in tray for logic
        Dictionary<string, int> historyCounts = new Dictionary<string, int>();
        foreach (var item in _undoHistory)
        {
            if (historyCounts.ContainsKey(item.Id)) historyCounts[item.Id]++;
            else historyCounts[item.Id] = 1;
        }

        // 2. PRIORITY 1: Check Collectables that are already in UndoHistory (Goals in progress)
        int highestCount = 0;
        foreach (var goalID in _collectableLeft.Keys)
        {
            if (historyCounts.TryGetValue(goalID, out int countInTray))
            {
                // We only care if it's not already a completed set in the tray (count % 3 != 0)
                if (countInTray % 3 != 0 && countInTray > highestCount)
                {
                    highestCount = countInTray;
                    targetID = goalID;
                }
            }
        }

        // 3. PRIORITY 2: If no "Goal in Progress" found, check if ANY item in history is a candidate
        if (string.IsNullOrEmpty(targetID) && _undoHistory.Count > 0)
        {
            // Pick the most recent item from history that isn't already finished
            foreach (var item in _undoHistory)
            {
                if (historyCounts[item.Id] % 3 != 0)
                {
                    targetID = item.Id;
                    break;
                }
            }
        }

        // 4. PRIORITY 3: If history is empty, just hint the first goal
        if (string.IsNullOrEmpty(targetID) && _collectableLeft.Count > 0)
        {
            targetID = _collectableLeft.Keys.First();
        }

        // 5. Execution
        if (!string.IsNullOrEmpty(targetID))
        {
            HighlightItemsInField(targetID);
            GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Hint);
        }
    }

    private void HighlightItemsInField(string ID)
    {
        int count = 0;
        float delay = 0.1f;
        foreach (var item in _itemClickables)
        {
            if (item != null && item.ItemData.Id == ID)
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
            int currentlyInTray = _undoHistory.Count(i => i.Id == key);

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

        string firstID = _collectableLeft.Keys.First();
        for (int i = 0; i < 3; i++)
        {
            Scheduler.Instance.ExecuteAfterDelay(delay * i, () => TrySelectSpecificItem(firstID));
        }

        GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Magnet);
    }
    private bool TrySelectSpecificItem(string Id)
    {
        var targetItem = _itemClickables.Find(c => c != null && c.ItemData.Id == Id);
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
        foreach (var itemID in levelData.ItemsToCollect)
        {
            var spawnEntry = levelData.ItemsToSpawn.Find(e => e.Id == itemID);
            if (spawnEntry != null)
                _collectableLeft.Add(itemID, spawnEntry.Count);
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
        string targetID = _collectableLeft.Keys.First();
        TrySelectSpecificItem(targetID);
    }

    #endregion
}
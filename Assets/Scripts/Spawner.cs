using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.VisualScripting;
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
    private float _levelVerticalOffset = 0;

    private List<ClickableItem> _itemClickables = new();
    private Action<ItemData, Transform> _onItemClicked;
    private LevelData _currentLevelData;
    private Dictionary<string, int> _collectableLeft;
    private Stack<ItemData> _undoHistory = new();

    private void OnEnable()
    {
        GameEvents.OnItemAddedToSlotEvent += HandleInternalItemClicked;
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
        GameEvents.OnItemAddedToSlotEvent += HandleInternalItemClicked;
        GameEvents.OnUndoPowerupEvent -= HandleUndoPowerUp;
        GameEvents.OnShakePowerupEvent -= ShakeArea;
        GameEvents.OnHintPowerupEvent -= HandleHintPowerUp;
        GameEvents.OnMagnetPowerupEvent -= HandleMagnetPowerUp;
        GameEvents.OnRequestMatchResolveEvent -= HandleMatchResolved;
        GameEvents.OnCleanSweepTrayEvent -= HandleCleanSweep;
    }

    #region Level Lifecycle
    public void Cleanup()
    {
        // 1. Stop all active logic (Tweens, Schedulers, and Particles)
        DOTween.KillAll(); // Or use a specific ID if you don't want to kill UI tweens
        shakeParticle.Stop();

        // 2. Destroy physical items
        foreach (var clickable in _itemClickables)
        {
            if (clickable != null && clickable.gameObject != null)
            {
                Destroy(clickable.gameObject);
            }
        }

        // 3. Clear the Level Container (the walls)
        Transform container = Parent.Find("LevelContainer");
        if (container != null)
        {
            // Use Destroy in runtime, DestroyImmediate in editor scripts
            Destroy(container.gameObject);
        }

        // 4. Reset Data State
        _itemClickables.Clear();
        _undoHistory.Clear();

        if (_collectableLeft != null)
            _collectableLeft.Clear();

        _currentLevelData = null;
        _onItemClicked = null;

        // 5. Reset Stats
        InitialTotalItems = 0;
        InitialCollectableItems = 0;
        _levelVerticalOffset = 0;

        Debug.Log("[Spawner] Cleanup Complete.");
    }
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
        InitialTotalItems = _currentLevelData.ItemsToSpawn.Sum(x => x.Count);
        InitialCollectableItems = _currentLevelData.ItemsToSpawn
                .Where(entry => _currentLevelData.ItemsToCollect.Contains(entry.Id))
                .Sum(entry => entry.Count);
        PopulateCollectableLeft(_currentLevelData);
    }

    private void ExecuteSpawning()
    {
        int totalToSpawn = 0;
        foreach (var entry in _currentLevelData.ItemsToSpawn)
        {
            totalToSpawn += entry.Count;
        }

        int spawnedCount = 0;
        foreach (var entry in _currentLevelData.ItemsToSpawn)
        {
            ItemData item = DataManager.Instance.GetItemByID(entry.Id);
            for (int i = 0; i < entry.Count; i++)
            {
                CreateItemInstance(item, () =>
            {
                spawnedCount++;

                // 2. Check if this was the last one
                if (spawnedCount >= totalToSpawn)
                {
                    OnAllItemsSpawned();
                }
            });
            }
        }
    }

    private void CreateItemInstance(ItemData item, System.Action onInstanceReady = null)
    {
        Vector3 spawnPos = CalculateRandomSpawnPos();
        Quaternion randomRot = UnityEngine.Random.rotation;

        AssetLoader.Instance.InstantiatePrefab(item.PrefabName, spawnPos, randomRot, Parent, (go) =>
        {
            if (go.TryGetComponent<ClickableItem>(out var clickable))
            {
                CheckFTUE(item, clickable);
                _itemClickables.Add(clickable);
                clickable.ItemData = item.CreateCopy();
                clickable.OnItemClicked = _onItemClicked;
            }
            onInstanceReady?.Invoke();
        });
    }

    int count = 1;
    private void CheckFTUE(ItemData item, ClickableItem target)
    {
        if (!FTUEManager.Instance.IsSequenceCompleted("Opening") && item.Id == DataManager.Instance.Metadata.Levels[0].ItemsToCollect[0])
        {
            var ftueTarget = target.gameObject.AddComponent<FTUETarget>();
            ftueTarget.Init("Item" + count++);
            target.Highlight(true);
        }
    }

    private void OnAllItemsSpawned()
    {
        // This is where you fire the event for your Curtains or Iris to open
        GameEvents.OnSpawnerInitializedEvent?.Invoke();
    }
    #endregion

    #region Internal Logic

    private void HandleInternalItemClicked(ItemData data, int index, Transform t, bool isAdded, Action callback)
    {
        if (isAdded)
        {
            // Record the data before the object is destroyed by your Tray logic
            _undoHistory.Push(data);

            _itemClickables.RemoveAll(c => c == null || c.transform == t);

            if (_collectableLeft.ContainsKey(data.Id))
            {
                _collectableLeft[data.Id]--;
                SoundController.Instance.PlaySoundEffect("ding");
                if (_collectableLeft[data.Id] <= 0)
                    _collectableLeft.Remove(data.Id);
            }
            else
            {
                SoundController.Instance.PlaySoundEffect("pick");
            }
        }
    }

    // 1. CALL THIS FOR THE BUTTON/POWERUP

    private void HandleCleanSweep()
    {
        if (_undoHistory.Count == 0)
        {
            GameEvents.OnPowerUpEnableEvent?.Invoke(true);
            return;
        }

        GameEvents.OnPowerUpEnableEvent?.Invoke(false);
        Scheduler.Instance.ExecuteAfterDelay(0.35f, () =>
        {
            RestoreAndSpawnItem();
            HandleCleanSweep();
        });
    }


    private void HandleUndoPowerUp(bool powerUpUsed)
    {
        if (_undoHistory.Count == 0) return;

        RestoreAndSpawnItem();
        if (powerUpUsed)
        {
            // Only triggered when used as a manual PowerUp
            GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Undo);
            GameEvents.OnPowerUpEnableEvent?.Invoke(true);
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

    private void SpawnFromTray(ItemData item, Vector3 startPos)
    {
        // 1. Instantiate at the Tray's location
        AssetLoader.Instance.InstantiatePrefab(item.PrefabName, startPos, UnityEngine.Random.rotation, Parent, (go) =>
        {
            // 2. Setup Clickable Logic
            if (go.TryGetComponent<ClickableItem>(out var clickable))
            {
                _itemClickables.Add(clickable);
                clickable.ItemData = item;
                clickable.OnItemClicked = _onItemClicked;

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
    private void HandleMatchResolved(int firstItemIndex, ItemData[] items, Action _)
    {
        if (_undoHistory.Count == 0 || items.Length == 0) return;

        string targetId = items[0].Id;
        int removedCount = 0;
        int amountToRemove = 3;

        // 1. Convert stack to list to manipulate easily
        List<ItemData> historyList = _undoHistory.ToList();
        List<ItemData> filteredList = new List<ItemData>();

        // 2. Iterate and skip only the first 3 matches found
        foreach (var item in historyList)
        {
            if (item.Id == targetId && removedCount < amountToRemove)
            {
                removedCount++;
                continue; // Skip adding to the new list
            }
            filteredList.Add(item);
        }

        // 3. Rebuild the stack
        // Since .ToList() on a Stack returns items in Pop order (Top to Bottom),
        // and the Stack constructor pushes them in order, we reverse it to maintain original order.
        filteredList.Reverse();
        _undoHistory = new Stack<ItemData>(filteredList);
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
        if (_collectableLeft == null || _collectableLeft.Count == 0) return;

        string targetId = string.Empty;
        int amountToFetch = 3;

        // 1. Determine Target: Priority search for partial matches in tray
        foreach (var key in _collectableLeft.Keys)
        {
            int currentlyInTray = _undoHistory.Count(i => i.Id == key) % 3;
            if (currentlyInTray > 0)
            {
                targetId = key;
                amountToFetch = 3 - currentlyInTray;
                break;
            }
        }

        // 2. Fallback: If no partials, just take the first item type available
        if (string.IsNullOrEmpty(targetId))
        {
            targetId = _collectableLeft.Keys.First();
            amountToFetch = 3;
        }

        // 3. Execute the Magnet Action
        ExecuteMagnetSelection(targetId, amountToFetch);
    }

    private void ExecuteMagnetSelection(string key, int count)
    {
        float delay = 0.4f;
        var targets = _itemClickables
            .Where(c => c != null && c.ItemData.Id == key)
            .Take(count)
            .ToList();

        if (targets.Count == 0) return;

        for (int i = 0; i < targets.Count; i++)
        {
            var targetItem = targets[i];
            int index = i; // Local copy for closure

            targetItem.Highlight(true);

            Scheduler.Instance.ExecuteAfterDelay(delay * index, () =>
            {
                ProcessItemSelection(targetItem);

                // If this is the last item of the sequence, re-enable UI
                if (index == targets.Count - 1)
                {
                    GameEvents.OnPowerUpEnableEvent?.Invoke(true);
                }
            });
        }

        GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Magnet);
    }

    private void ProcessItemSelection(ClickableItem item)
    {
        if (item == null) return;

        item.OnHandleClick(default);
    }

    private Vector3 CalculateRandomSpawnPos()
    {
        float padding = 0.0f; // Keep items away from the wall edge

        // Use the calculated max bounds
        float x = UnityEngine.Random.Range(-_spawnXMax + padding, _spawnXMax - padding);
        float z = UnityEngine.Random.Range(-_spawnZMax + padding, _spawnZMax - padding);

        // Fixed Y to ensure they drop into the scene
        float y = VerticalOffset + UnityEngine.Random.Range(0f, 2.0f);

        return Parent.position + new Vector3(x, y, z);
    }

    #endregion

    #region Bounds & Walls
    public void GenerateBounds()
    {
        // 1. Calculate the distance and the RAW Full Frustum
        float distanceToCamera = Mathf.Abs(MainCamera.transform.position.y - Parent.position.y);
        float fullFrustumHeight = 2.0f * distanceToCamera * Mathf.Tan(MainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = fullFrustumHeight * MainCamera.aspect;

        // 2. Calculate the World-Space Height of the Top Notch (Unsafe Area)
        float pixelTopGap = Screen.height - Screen.safeArea.yMax;
        float topGapPercentage = pixelTopGap / Screen.height;
        float worldTopGap = fullFrustumHeight * topGapPercentage;

        // 3. Define the "Available" Height (Everything below the notch)
        float availableHeight = fullFrustumHeight - worldTopGap;

        // 4. Apply HeightPercent to the AVAILABLE space
        // This ensures the 0.9f (or whatever %) stays within the safe zone
        float usableHeight = availableHeight * HeightPercent;
        _spawnZMax = usableHeight / 2f;

        // 5. Apply WidthPercent (Standard)
        _spawnXMax = (frustumWidth * WidthPercent) / 2f;

        // 6. Calculate the Center Offset
        // We shift the container down to account for the Notch
        // PLUS we shift it down slightly more to center the "HeightPercent" 
        // area within the safe zone.
        float centerOfSafeZone = (fullFrustumHeight / 2f) - worldTopGap - (availableHeight / 2f);
        _levelVerticalOffset = centerOfSafeZone;

        RefreshContainer();
    }

    private void RefreshContainer()
    {
        Transform oldContainer = Parent.Find("LevelContainer");
        if (oldContainer != null) DestroyImmediate(oldContainer.gameObject);

        GameObject container = new GameObject("LevelContainer");
        container.transform.SetParent(Parent);

        // Position the container at the calculated offset
        container.transform.localPosition = new Vector3(0, 0, _levelVerticalOffset);

        float thickness = 1.0f;

        // Walls are now placed relative to the Container's offset center
        // Wall_Left
        SpawnWall("Wall_Left",
            new Vector3(-_spawnXMax - (thickness / 2), WallHeight / 2, 0),
            new Vector3(thickness, WallHeight, _spawnZMax * 2),
            container.transform);

        // Wall_Right
        SpawnWall("Wall_Right",
            new Vector3(_spawnXMax + (thickness / 2), WallHeight / 2, 0),
            new Vector3(thickness, WallHeight, _spawnZMax * 2),
            container.transform);

        // Wall_Top (Respects Notch + HeightPercent)
        SpawnWall("Wall_Top",
            new Vector3(0, WallHeight / 2, _spawnZMax + (thickness / 2)),
            new Vector3(_spawnXMax * 2 + (thickness * 2), WallHeight, thickness),
            container.transform);

        // Wall_Bottom
        SpawnWall("Wall_Bottom",
            new Vector3(0, WallHeight / 2, -_spawnZMax - (thickness / 2)),
            new Vector3(_spawnXMax * 2 + (thickness * 2), WallHeight, thickness),
            container.transform);
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

    public ParticleSystem shakeParticle;

    public void ShakeArea()
    {
        if (_itemClickables.Count == 0) return;

        // --- Define Local Variables ---
        int shakesRemaining = 3;
        float shakeInterval = 0.7f;
        float shakeRadius = 12.0f;
        float shakePower = 1.0f;
        float upwardModifier = 0.2f;

        void TriggerSingleRumble()
        {
            if (shakesRemaining <= 0) return;

            // 1. Target a DENSE spot by picking a random active item
            // We filter out nulls to ensure the item hasn't been matched/destroyed
            var activeItems = _itemClickables.FindAll(i => i != null);
            if (activeItems.Count == 0) return;

            int randomIndex = UnityEngine.Random.Range(0, activeItems.Count);
            Vector3 itemPos = activeItems[randomIndex].transform.position;

            // Offset the Y slightly downward so the force pushes UP and OUT
            Vector3 epicenter = new Vector3(itemPos.x, itemPos.y - 0.5f, itemPos.z);

            // 2. Physics check for items in range
            Collider[] hitColliders = Physics.OverlapSphere(epicenter, shakeRadius);

            foreach (var hit in hitColliders)
            {
                if (hit.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddExplosionForce(shakePower, epicenter, shakeRadius, upwardModifier, ForceMode.Impulse);
                    rb.AddTorque(UnityEngine.Random.insideUnitSphere * shakePower, ForceMode.Impulse);
                }
            }

            shakeParticle.transform.position = new Vector3(epicenter.x, 0, epicenter.z);
            shakeParticle.Play();
            SoundController.Instance.PlaySoundEffect("shake");
            // 3. Schedule the next
            shakesRemaining--;
            if (shakesRemaining > 0)
            {
                Scheduler.Instance.ExecuteAfterDelay(shakeInterval, TriggerSingleRumble);
            }
            else
            {
                GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Shake);
                GameEvents.OnPowerUpEnableEvent?.Invoke(true);
            }
        }

        TriggerSingleRumble();
        // GameEvents.OnPowerUpSuccessEvent?.Invoke(PowerUpType.Shake);
    }

    #endregion
}
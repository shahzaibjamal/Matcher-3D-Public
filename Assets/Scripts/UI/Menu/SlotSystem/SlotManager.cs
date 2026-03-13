using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SlotManager
{
    private readonly ItemData[] _slots;
    private Stack<string> _undoStack = new Stack<string>(); // Stores UIds
    private bool _isProcessingMatches;
    private bool _allGoalsReached;
    public SlotManager(int size)
    {
        _slots = new ItemData[size];
    }

    public void Reset()
    {
        Cleanup();
        Scheduler.Instance.SubscribeGUI(OnGUI);
        GameEvents.OnItemsCollectedEvent += OnItemsCollected;
        GameEvents.OnUndoPowerupEvent += OnUndoRequest;
        GameEvents.OnCleanSweepTrayEvent += OnCleanSweepTray;
    }

    public void Cleanup()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] = null;
        }
        Scheduler.Instance.UnsubscribeGUI(OnGUI);
        GameEvents.OnItemsCollectedEvent -= OnItemsCollected;
        GameEvents.OnUndoPowerupEvent -= OnUndoRequest;
        GameEvents.OnCleanSweepTrayEvent -= OnCleanSweepTray;

        _allGoalsReached = false;
        _undoStack.Clear();
    }
    private async void OnCleanSweepTray()
    {
        // Iterate backwards through the tray
        for (int i = _slots.Length - 1; i >= 0; i--)
        {
            if (_slots[i] != null)
            {
                // 1. Get the reference to the item in the slot
                var itemInSlot = _slots[i];

                await ExecuteFlight(_slots[i], i, null, false);

                // 4. Null the slot data
                _slots[i] = null;

                // 5. Optional: Small delay for a nice sequential 'pop' out of the tray
                await Task.Delay(200);
            }
        }
        GameEvents.OnSlotsFillableEvent?.Invoke(IsSlotAvailable());

        _undoStack.Clear();
    }

    private void OnItemsCollected()
    {
        _allGoalsReached = true;
    }

    public async void OnUndoRequest(bool _)
    {
        await UndoLastAction();
    }
    public async Task UndoLastAction()
    {
        if (_undoStack.Count == 0)
        {
            GameEvents.OnUndoInvalidEvent?.Invoke();
            return;
        }

        // 1. Get the UId of the last item the player clicked
        string lastUId = _undoStack.Pop();

        // 2. Find where that item is CURRENTLY sitting
        int currentIdx = -1;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i]?.UId == lastUId)
            {
                currentIdx = i;
                break;
            }
        }

        if (currentIdx == -1) return; // Item was likely already matched

        // 3. Visual: Return item to world
        await ExecuteFlight(_slots[currentIdx], currentIdx, null, false);
        _slots[currentIdx] = null;

        List<Task> compactTasks = new List<Task>();
        for (int i = currentIdx; i < _slots.Length - 1; i++)
        {
            if (_slots[i + 1] != null)
            {
                _slots[i] = _slots[i + 1];
                _slots[i + 1] = null;
                compactTasks.Add(ExecuteSteppedLeap(_slots[i], i + 1, i));
            }
        }

        await Task.WhenAll(compactTasks);
    }

    public async void AddItem(ItemData data, Transform source)
    {
        if (_allGoalsReached)
        {
            TriggerGameOver("All items collected", true);
            return; // Stop here, don't check for Tray Full
        }

        int targetIdx = GetInsertionIndex(data);

        if (targetIdx >= _slots.Length)
        {
            if (_isProcessingMatches)
            {
            }
            else
            {
                TriggerGameOver("new item added - but tray full", false);
            }
            return;
        }

        _undoStack.Push(data.UId);

        // 1. Shift logic (Right to Left)
        for (int i = _slots.Length - 1; i > targetIdx; i--)
        {
            if (_slots[i - 1] != null)
            {
                ItemData itemToMove = _slots[i - 1];
                _slots[i] = itemToMove;
                _slots[i - 1] = null; // CRITICAL: Clear the old logic slot immediately

                // We don't await this; we let the View queue the animation
                GameEvents.OnRequestSteppedLeapEvent?.Invoke(itemToMove, i - 1, i, null);
            }
        }

        _slots[targetIdx] = data;
        GameEvents.OnSlotsFillableEvent?.Invoke(IsSlotAvailable());

        // 2. Flight
        await ExecuteFlight(data, targetIdx, source, true);

        if (!_isProcessingMatches)
        {
            _isProcessingMatches = true;
            await ResolveAllMatches();
            GameEvents.OnSlotsFillableEvent?.Invoke(IsSlotAvailable());
            _isProcessingMatches = false;
        }

        if (IsTrayFull() && FindMatch() == -1)
        {
            TriggerGameOver("Tray full - no more moves possible", false);
        }
    }
    private bool IsTrayFull()
    {
        foreach (var slot in _slots)
        {
            if (slot == null) return false;
        }
        return true;
    }

    public bool IsSlotAvailable()
    {
        return _slots[_slots.Length - 1] == null;
    }
    private void TriggerGameOver(string reason, bool win)
    {
        Debug.LogError($"[GAME OVER] {reason}");
        GameEvents.OnGameOverEvent?.Invoke(win); // Fire an event to show UI
        GameEvents.OnSlotsFillableEvent?.Invoke(false);
    }
    private async Task ResolveAllMatches()
    {
        int matchIdx = FindMatch();
        while (matchIdx != -1)
        {
            ItemData[] matchedData = new ItemData[3];
            for (int i = 0; i < 3; i++)
            {
                matchedData[i] = _slots[matchIdx + i];
                _slots[matchIdx + i] = null;
            }

            await ExecuteMatch(matchIdx, matchedData);

            List<Task> compactTasks = new List<Task>();
            for (int i = matchIdx; i < _slots.Length - 3; i++)
            {
                if (_slots[i + 3] != null)
                {
                    _slots[i] = _slots[i + 3];
                    _slots[i + 3] = null;
                    compactTasks.Add(ExecuteSteppedLeap(_slots[i], i + 3, i));
                }
            }
            await Task.WhenAll(compactTasks);

            var matchedUIds = matchedData.Select(m => m.UId).ToList();
            _undoStack = new Stack<string>(_undoStack.Where(id => !matchedUIds.Contains(id)).Reverse());

            matchIdx = FindMatch();
        }
    }

    private Task ExecuteFlight(ItemData d, int idx, Transform s, bool isAdded)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnItemAddedToSlotEvent?.Invoke(d, idx, s, isAdded, () => tcs.TrySetResult(true));
        return tcs.Task;
    }
    private Task ExecuteSteppedLeap(ItemData d, int from, int to)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestSteppedLeapEvent?.Invoke(d, from, to, () => tcs.TrySetResult(true));
        return tcs.Task;
    }
    private Task ExecuteMatch(int start, ItemData[] data)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestMatchResolveEvent?.Invoke(start, data, () => tcs.TrySetResult(true));
        return tcs.Task;
    }

    private int GetInsertionIndex(ItemData newItem)
    {
        for (int i = _slots.Length - 1; i >= 0; i--)
            if (_slots[i]?.Id == newItem.Id) return i + 1;
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] == null) return i;
        return _slots.Length;
    }

    private int FindMatch()
    {
        for (int i = 0; i <= _slots.Length - 3; i++)
        {
            if (_slots[i] == null) continue;
            if (_slots[i + 1]?.Id == _slots[i].Id && _slots[i + 2]?.Id == _slots[i].Id) return i;
        }
        return -1;
    }


    private void OnGUI()
    {

        // Example: draw _slots info directly
        if (_slots != null && _slots.Length > 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                normal = { textColor = Color.white }
            };

            float startY = Screen.height - 40f;
            float xSpacing = 220f;

            for (int i = 0; i < _slots.Length; i++)
            {
                var item = _slots[i];
                if (item == null) continue;

                string truncatedUniqueId = item.UId.Length > 7
                    ? item.UId.Substring(0, 7)
                    : item.UId;

                string text = $"{truncatedUniqueId} | {item.Id}";

                float x = 10f + (i * xSpacing);
                GUI.Label(new Rect(x, startY, 200, 50), text, style);
            }
        }
    }
}
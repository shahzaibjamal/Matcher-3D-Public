using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SlotManager
{
    private readonly ItemData[] _slots;
    private bool _isProcessingMatches;

    public SlotManager(int size) => _slots = new ItemData[size];

    public async void AddItem(ItemData data, Transform source)
    {
        int targetIdx = GetInsertionIndex(data);
        data.UniqueId = Guid.NewGuid().ToString();

        // Prevent out of bounds if tray is full
        if (targetIdx >= _slots.Length)
        {
            Debug.LogWarning("Tray is full!");
            return;
        }

        // 1. Shift logic
        for (int i = _slots.Length - 1; i > targetIdx; i--)
            _slots[i] = _slots[i - 1];

        _slots[targetIdx] = data;

        // 2. Parallel Shifts
        for (int i = targetIdx + 1; i < _slots.Length - 1; i++)
        {
            if (_slots[i] != null)
                GameEvents.OnRequestSteppedLeap?.Invoke(_slots[i], i, null);
        }

        // 3. Flight
        await ExecuteFlight(data, targetIdx, source);

        if (!_isProcessingMatches)
        {
            _isProcessingMatches = true;
            await ResolveAllMatches();
            _isProcessingMatches = false;
        }
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
                    compactTasks.Add(ExecuteSteppedLeap(_slots[i], i));
                }
            }
            await Task.WhenAll(compactTasks);
            matchIdx = FindMatch();
        }
    }

    private Task ExecuteFlight(ItemData d, int idx, Transform s)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestFlight?.Invoke(d, idx, s, () => tcs.SetResult(true));
        return tcs.Task;
    }
    private Task ExecuteSteppedLeap(ItemData d, int to)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestSteppedLeap?.Invoke(d, to, () => tcs.SetResult(true));
        return tcs.Task;
    }
    private Task ExecuteMatch(int start, ItemData[] data)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestMatchResolve?.Invoke(start, data, () => tcs.SetResult(true));
        return tcs.Task;
    }

    private int GetInsertionIndex(ItemData newItem)
    {
        for (int i = _slots.Length - 1; i >= 0; i--)
            if (_slots[i]?.UID == newItem.UID) return i + 1;
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] == null) return i;
        return _slots.Length;
    }

    private int FindMatch()
    {
        for (int i = 0; i <= _slots.Length - 3; i++)
        {
            if (_slots[i] == null) continue;
            if (_slots[i + 1]?.UID == _slots[i].UID && _slots[i + 2]?.UID == _slots[i].UID) return i;
        }
        return -1;
    }


}
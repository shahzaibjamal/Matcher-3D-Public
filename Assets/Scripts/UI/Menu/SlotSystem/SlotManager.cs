using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SlotManager
{
    private readonly ItemData[] _slots;
    private bool _isProcessing;

    // The single source of truth for all pending actions
    private readonly Queue<(ItemData data, Transform source)> _inputQueue = new();

    public SlotManager(int size)
    {
        _slots = new ItemData[size];
    }

    // Public entry point - just adds to the list and tries to run the processor
    public void AddItem(ItemData data, Transform source)
    {
        _inputQueue.Enqueue((data, source));
        TryProcessNext();
    }

    private async void TryProcessNext()
    {
        if (_isProcessing || _inputQueue.Count == 0) return;

        _isProcessing = true;

        while (_inputQueue.Count > 0)
        {
            var (data, source) = _inputQueue.Dequeue();
            await HandleFullSequence(data, source);
        }

        _isProcessing = false;
    }

    private async Task HandleFullSequence(ItemData data, Transform source)
    {
        int targetIdx = GetInsertionIndex(data);
        if (targetIdx >= _slots.Length) return;

        // 1. PARALLEL STEPPED SHIFT RIGHT
        List<Task> shiftTasks = new List<Task>();

        // Iterate backwards to shift data logically without overwriting
        for (int i = _slots.Length - 1; i > targetIdx; i--)
        {
            if (_slots[i - 1] != null)
            {
                ItemData itemToMove = _slots[i - 1];
                _slots[i] = itemToMove;
                _slots[i - 1] = null;

                // Add the "Forward Step" task (moving from i-1 to i)
                shiftTasks.Add(ExecuteSteppedLeap(i - 1, i, itemToMove));
            }
        }

        // Wait for the "opening" animation to finish
        await Task.WhenAll(shiftTasks);

        // 2. THE FLIGHT
        _slots[targetIdx] = data;
        await ExecuteFlight(data, targetIdx, source);

        // 3. RESOLVE MATCHES
        await ResolveAllMatches();
    }
    private async Task ResolveAllMatches()
    {
        int matchIdx = FindMatch();
        while (matchIdx != -1)
        {
            // 1. Logic Clear
            for (int i = 0; i < 3; i++) _slots[matchIdx + i] = null;

            // 2. Animate Match
            await ExecuteMatch(matchIdx);

            // 3. PARALLEL STEPPED COMPACTION
            List<Task> compactTasks = new List<Task>();

            for (int i = matchIdx; i < _slots.Length - 3; i++)
            {
                if (_slots[i + 3] != null)
                {
                    ItemData itemToMove = _slots[i + 3];
                    _slots[i] = itemToMove;
                    _slots[i + 3] = null;

                    // Fire the task but DON'T await it yet
                    compactTasks.Add(ExecuteSteppedLeap(i + 3, i, itemToMove));
                }
            }

            // Wait for ALL items to finish their multi-hop journeys
            await Task.WhenAll(compactTasks);

            // 4. Check for chain reactions
            matchIdx = FindMatch();
        }
    }
    private Task ExecuteSteppedLeap(int from, int to, ItemData d)
    {
        var tcs = new TaskCompletionSource<bool>();
        // Note: OnRequestSteppedLeap is a new event we'll define
        GameEvents.OnRequestSteppedLeap?.Invoke(from, to, d, () => tcs.SetResult(true));
        return tcs.Task;
    }
    private Task ExecuteFlight(ItemData d, int idx, Transform s)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestFlight?.Invoke(d, idx, s, () => tcs.SetResult(true));
        return tcs.Task;
    }

    private Task ExecuteLeap(int f, int t, ItemData d)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestLeap?.Invoke(f, t, d, () => tcs.SetResult(true));
        return tcs.Task;
    }

    private Task ExecuteMatch(int start)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestMatchResolve?.Invoke(start, () => tcs.SetResult(true));
        return tcs.Task;
    }

    // --- HELPERS ---

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
            string id = _slots[i].UID;
            if (_slots[i + 1]?.UID == id && _slots[i + 2]?.UID == id)
                return i;
        }
        return -1;
    }
}
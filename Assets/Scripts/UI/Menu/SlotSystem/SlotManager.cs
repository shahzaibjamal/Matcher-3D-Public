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

        // 1. Check for Room
        if (targetIdx >= _slots.Length)
        {
            Debug.LogWarning("Tray Full! Waiting for matches...");
            // Optionally: Destroy(source.gameObject) or bounce it back
            return;
        }

        // 2. Logical Shift & Leaps (Moving items to make room)
        // We do this BEFORE the flight so the hole is visually created
        for (int i = _slots.Length - 1; i > targetIdx; i--)
        {
            if (_slots[i - 1] != null)
            {
                _slots[i] = _slots[i - 1];
                _slots[i - 1] = null;
                await ExecuteLeap(i - 1, i, _slots[i]);
            }
        }

        // 3. The Flight (Wait for it to land)
        _slots[targetIdx] = data;
        await ExecuteFlight(data, targetIdx, source);

        // 4. Resolve Matches (Chain reactions included)
        await ResolveAllMatches();
    }

    private async Task ResolveAllMatches()
    {
        int matchIdx = FindMatch();
        while (matchIdx != -1)
        {
            // A. Clear Logic
            for (int i = 0; i < 3; i++) _slots[matchIdx + i] = null;

            // B. Animate Match
            await ExecuteMatch(matchIdx);

            // C. Compact Tray (Shift left)
            for (int i = matchIdx; i < _slots.Length - 3; i++)
            {
                if (_slots[i + 3] != null)
                {
                    _slots[i] = _slots[i + 3];
                    _slots[i + 3] = null;
                    await ExecuteLeap(i + 3, i, _slots[i]);
                }
            }

            // D. Check for next match in the chain
            matchIdx = FindMatch();
        }
    }

    // --- WRAPPERS FOR ASYNC EVENTS ---

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
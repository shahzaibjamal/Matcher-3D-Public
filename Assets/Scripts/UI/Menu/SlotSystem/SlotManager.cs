using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SlotManager
{
    private readonly ItemData[] _slots;
    private bool _isProcessingMatches;

    public SlotManager(int size)
    {
        // Clear old subscription if any
        Scheduler.Instance.UnsubscribeGUI(OnGUI);

        // Reset slots
        _slots = new ItemData[size];

        // Subscribe fresh
        Scheduler.Instance.SubscribeGUI(OnGUI);
    }
    public async void AddItem(ItemData data, Transform source)
    {
        int targetIdx = GetInsertionIndex(data);
        data.UniqueId = Guid.NewGuid().ToString(); // Assigned IMMEDIATELY

        if (targetIdx >= _slots.Length) return;

        // 1. Shift logic (Right to Left)
        for (int i = _slots.Length - 1; i > targetIdx; i--)
        {
            if (_slots[i - 1] != null)
            {
                ItemData itemToMove = _slots[i - 1];
                _slots[i] = itemToMove;
                _slots[i - 1] = null; // CRITICAL: Clear the old logic slot immediately

                // We don't await this; we let the View queue the animation
                GameEvents.OnRequestSteppedLeap?.Invoke(itemToMove, i - 1, i, null);
            }
        }

        _slots[targetIdx] = data;

        // 2. Flight
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
                    compactTasks.Add(ExecuteSteppedLeap(_slots[i], i + 3, i));
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
    private Task ExecuteSteppedLeap(ItemData d, int from, int to)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameEvents.OnRequestSteppedLeap?.Invoke(d, from, to, () => tcs.SetResult(true));
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

                string truncatedUniqueId = item.UniqueId.Length > 7
                    ? item.UniqueId.Substring(0, 7)
                    : item.UniqueId;

                string text = $"{truncatedUniqueId} | {item.UID}";

                float x = 10f + (i * xSpacing);
                GUI.Label(new Rect(x, startY, 200, 50), text, style);
            }
        }
    }
}
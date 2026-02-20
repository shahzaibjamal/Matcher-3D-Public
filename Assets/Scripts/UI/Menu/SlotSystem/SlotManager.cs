using System;
using System.Collections.Generic;

/// <summary>
/// The Pure Logic "Brain" of the Tray system. 
/// It handles data arrays and tells the View what to animate.
/// </summary>
public class SlotManager
{
    private readonly ItemData[] _slots;
    private bool _isProcessing;

    // Events for the TrayView to subscribe to
    public event Action<int, int, ItemData> OnItemLeaped;   // (fromIndex, toIndex, item)
    public event Action<int, ItemData> OnNewItemReserved;  // (index, item)
    public event Action<int> OnMatchFound;                 // (startIndex)
    public event Action OnGameOver;

    public SlotManager(int size)
    {
        _slots = new ItemData[size];
    }

    /// <summary>
    /// Attempts to add an item. Returns true if successful.
    /// </summary>
    public bool AddItem(ItemData data)
    {
        int targetIndex = GetInsertionIndex(data);

        // Check if the tray is full
        if (targetIndex >= _slots.Length)
        {
            OnGameOver?.Invoke();
            return false;
        }

        // 1. Logic Shift: Move existing items to the right to make a hole
        for (int i = _slots.Length - 1; i > targetIndex; i--)
        {
            if (_slots[i - 1] != null)
            {
                _slots[i] = _slots[i - 1];
                _slots[i - 1] = null; // Clear the old spot logic-wise

                // Notify UI to move the icon
                OnItemLeaped?.Invoke(i - 1, i, _slots[i]);
            }
        }

        // 2. Insert the new item into the logic array
        _slots[targetIndex] = data;

        // 3. Tell UI: "Reserve this slot for the 3D item currently flying in"
        OnNewItemReserved?.Invoke(targetIndex, data);

        return true;
    }

    /// <summary>
    /// Triggered by the UI View once an animation finishes settling.
    /// </summary>
    public void RequestMatchCheck()
    {
        if (_isProcessing) return;
        ProcessMatch();
    }

    private void ProcessMatch()
    {
        int startIndex = CheckMatch();

        if (startIndex == -1)
        {
            _isProcessing = false;
            // Final check: if no matches and full, it's game over
            if (IsTrayFull()) OnGameOver?.Invoke();
            return;
        }

        _isProcessing = true;

        // 1. Logic Clear: Remove the 3 matched items
        for (int i = 0; i < 3; i++)
        {
            _slots[startIndex + i] = null;
        }

        // 2. Notify UI to play the Merge/Poof animation
        OnMatchFound?.Invoke(startIndex);

        // 3. Compact Logic: Shift everything on the right to the left
        for (int i = startIndex; i < _slots.Length - 3; i++)
        {
            if (_slots[i + 3] != null)
            {
                _slots[i] = _slots[i + 3];
                _slots[i + 3] = null;

                // Tell UI to move the icon left
                OnItemLeaped?.Invoke(i + 3, i, _slots[i]);
            }
        }

        // 4. Chain Reaction: Check if the shift created a new match
        // Note: In production, you might want a small delay here via the View
        ProcessMatch();
    }

    private int CheckMatch()
    {
        for (int i = 0; i <= _slots.Length - 3; i++)
        {
            if (_slots[i] == null) continue;

            string currentId = _slots[i].UID;
            if (_slots[i + 1]?.UID == currentId &&
                _slots[i + 2]?.UID == currentId)
            {
                return i;
            }
        }
        return -1;
    }

    private int GetInsertionIndex(ItemData newItem)
    {
        // Find last existing twin to group them
        for (int i = _slots.Length - 1; i >= 0; i--)
        {
            if (_slots[i] != null && _slots[i].UID == newItem.UID)
                return i + 1;
        }

        // Otherwise, find the first available empty slot
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
                return i;
        }

        return _slots.Length; // Represents "Full"
    }

    private bool IsTrayFull()
    {
        foreach (var item in _slots)
        {
            if (item == null) return false;
        }
        return true;
    }

    // Helper for the View to know what is where
    public ItemData GetItemAt(int index) => _slots[index];
}
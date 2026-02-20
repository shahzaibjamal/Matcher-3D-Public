using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    public async void RequestMatchCheck()
    {
        if (_isProcessing) return;
        await ProcessMatch();
    }
    private async Task ProcessMatch()
    {
        int startIdx = CheckMatch();

        if (startIdx == -1)
        {
            _isProcessing = false;
            if (IsTrayFull()) OnGameOver?.Invoke();
            return;
        }

        _isProcessing = true;

        // 1. Logic Clear: Remove matched items
        for (int i = 0; i < 3; i++) _slots[startIdx + i] = null;

        // 2. Notify UI: Start Merge Animation
        OnMatchFound?.Invoke(startIdx);

        // --- THE DELAY ---
        // Wait for the Merge Animation (Cheer + Vacuum) to finish
        // Adjust 500ms to match your mergeSeq duration
        await Task.Delay(500);

        // 3. Compact Logic: Now shift items
        for (int i = startIdx; i < _slots.Length - 3; i++)
        {
            if (_slots[i + 3] != null)
            {
                _slots[i] = _slots[i + 3];
                _slots[i + 3] = null;

                // This now fires AFTER the delay
                OnItemLeaped?.Invoke(i + 3, i, _slots[i]);
            }
        }

        // Check for chain reactions
        await ProcessMatch();
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
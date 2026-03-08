#if UNITY_EDITOR
using System.Reflection;
using System.Linq;
#endif
using UnityEngine;

public partial class Spawner : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) Debug_ClickRandom();
        if (Input.GetKeyDown(KeyCode.M)) Debug_ClickTarget();
    }

    private void Debug_ClickRandom()
    {
        // CRITICAL: Clean up destroyed items before picking one
        // _itemClickables.RemoveAll(i => i == null);

        if (_itemClickables.Count == 0)
        {
            Debug.LogWarning("<color=yellow>[Debug]</color> No items left on board to click.");
            return;
        }

        int index = Random.Range(0, _itemClickables.Count);
        InvokeClick(_itemClickables[index]);
    }

    private void Debug_ClickTarget()
    {
        // Clean up here as well to ensure FirstOrDefault doesn't hit a null
        // _itemClickables.RemoveAll(i => i == null);

        if (_collectableLeft == null || _collectableLeft.Count == 0) return;

        string targetID = _collectableLeft.Keys.First();

        var targetItem = _itemClickables.FirstOrDefault(i => i.ItemData.Id == targetID);
        if (targetItem != null)
        {
            InvokeClick(targetItem);
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[Debug]</color> Could not find any board items with ID: {targetID}");
        }
    }

    private void InvokeClick(ClickableItem item)
    {
        Debug.Log($"<color=cyan>[Debug]</color> Simulating click on: {item.ItemData.Id}");
        item.OnHandleClick(default);
    }
#endif
}
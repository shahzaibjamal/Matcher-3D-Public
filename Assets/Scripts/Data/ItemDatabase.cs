using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items;

    private Dictionary<string, ItemData> itemDict;

    public void Init()
    {
        itemDict = new Dictionary<string, ItemData>();
        foreach (var item in items)
        {
            if (!itemDict.ContainsKey(item.UID))
                itemDict.Add(item.UID, item);
        }
    }

    public ItemData GetItemByUID(string uid)
    {
        if (itemDict == null) Init();
        itemDict.TryGetValue(uid, out ItemData result);
        return result;
    }
}

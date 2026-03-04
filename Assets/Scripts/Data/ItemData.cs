using System;
using Newtonsoft.Json;

[Serializable]
public class ItemData : ICloneable
{
    [JsonIgnore]
    public string UId;
    public string Id;
    public string Name;
    public string PrefabName;
    public string IconName;

    public object Clone()
    {
        // 1. Perform the shallow copy
        ItemData copy = (ItemData)this.MemberwiseClone();

        // 2. Assign the unique ID immediately
        copy.UId = Guid.NewGuid().ToString();

        return copy;
    }

    // Helper to avoid casting everywhere in your code
    public ItemData CreateCopy()
    {
        return (ItemData)this.Clone();
    }
}

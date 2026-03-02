using System;
using Newtonsoft.Json;

[Serializable]
public class ItemData
{
    [JsonIgnore]
    public string UId;
    public string Id;
    public string Name;
    public string PrefabName;
    public string IconName;
}

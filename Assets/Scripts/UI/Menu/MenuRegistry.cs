using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MenuRegistry", menuName = "UI/Menu Registry")]
public class MenuRegistry : ScriptableObject
{
    [System.Serializable]
    public struct MenuEntry
    {
        public Menus.Type type;
        public GameObject prefab;
        public Menus.MenuDisplayMode defaultMode; // Optional: suggest a default mode
    }

    public List<MenuEntry> entries;

    public GameObject GetPrefab(Menus.Type type)
    {
        var entry = entries.Find(e => e.type == type);
        return entry.prefab;
    }
}
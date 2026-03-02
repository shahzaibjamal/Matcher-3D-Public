using System;
using UnityEngine;

[Serializable]
public class MapThemeData
{
    public string Id;
    public string ThemeName;
    public string BackgroundSpriteName;
    public string FogColorHex; // The color the fog should turn into for this map
    public int StartLevel; // The level number where this theme begins
}
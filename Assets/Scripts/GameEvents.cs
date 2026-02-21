using System;
using UnityEngine;

public static class GameEvents
{
    public static Action<ItemData, int, Transform, Action> OnRequestFlight;
    public static Action<int, int, ItemData, Action> OnRequestLeap;
    public static Action<int, Action> OnRequestMatchResolve;
    public static Action<int, ItemData> OnItemLanded; // Optional, for debug
}
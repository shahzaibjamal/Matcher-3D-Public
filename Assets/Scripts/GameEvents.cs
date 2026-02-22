using System;
using UnityEngine;

public static class GameEvents
{
    // For new items entering the tray
    public static Action<ItemData, int, Transform, Action> OnRequestFlight;
    // For items shifting left/right
    // Change SteppedLeap to include the 'from' index
    public static Action<ItemData, int, int, Action> OnRequestSteppedLeap;    // For the match-3 merge
    public static Action<int, ItemData[], Action> OnRequestMatchResolve;
}
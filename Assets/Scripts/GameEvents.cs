using System;
using UnityEngine;

public static class GameEvents
{
    // The Spawner calls this, the TrayView listens to it.
    public static Action<ItemData, Transform> OnItemCollected;
}
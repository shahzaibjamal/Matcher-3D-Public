using System;
using UnityEngine;

public static class GameEvents
{
    // For new items entering the tray
    public static Action<ItemData, int, Transform, Action> OnRequestFlightEvent;
    // For items shifting left/right
    public static Action<ItemData, int, int, Action> OnRequestSteppedLeapEvent;    // For the match-3 merge
    public static Action<int, ItemData[], Action> OnRequestMatchResolveEvent;
    public static Action<LevelData> OnMatchStartedEvent;
    public static Action<bool> OnGameOverEvent;

    public static Action OnGameLaunchedEvent; // game (menu) opened. waiting for game start
    public static Action OnGameInitializedEvent; //gameplya started
    public static Action OnGameQuitEvent; //gameplya started
    public static Action OnItemsCollectedEvent;

    public static Action OnLevelCompleteEvent;
    public static Action OnLevelRestartEvent;


}
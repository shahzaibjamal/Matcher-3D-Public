using System;
using System.Collections.Generic;
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
    public static Action<bool, float> OnShowMatchResultEvent; // match status, match rate

    public static Action OnLevelCompleteEvent;
    public static Action OnLevelRestartEvent;

    public static Action OnUndoPowerupEvent;
    public static Action<Transform> OnUndoAddItemEvent;

    public static Action OnHintPowerupEvent;
    public static Action<string> OnHintSlotsItemAvailableEvent; //fired by slotmanager to tell what item can be picked up

    public static Action OnShakePowerupEvent; //shake and shuffle the items 
    public static Action OnMagnetPowerupEvent; // auto pickup collectable items

}